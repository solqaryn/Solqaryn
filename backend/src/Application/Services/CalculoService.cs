using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class CalculoService : ICalculoService
{
    private readonly IDescuentoRepository _descuentoRepository;
    private readonly IImpuestoRepository _impuestoRepository;
    private readonly ICostoEnvioRepository _costoEnvioRepository;

    public CalculoService(
        IDescuentoRepository descuentoRepository,
        IImpuestoRepository impuestoRepository,
        ICostoEnvioRepository costoEnvioRepository)
    {
        _descuentoRepository = descuentoRepository;
        _impuestoRepository = impuestoRepository;
        _costoEnvioRepository = costoEnvioRepository;
    }

    public async Task<ResultadoCalculoDto> CalcularVentaAsync(
        List<DetalleCalculoInput> detalles,
        int? clienteId,
        int? rolIdUsuario,
        string? codigoPromocional,
        int? costoEnvioId = null,
        bool envioExonerado = false,
        string? motivoExoneracionEnvio = null)
    {
        var importeBruto = RedondearMoneda(detalles.Sum(CalcularSubtotalLinea));
        var envio = await ResolverEnvioAsync(costoEnvioId, envioExonerado, motivoExoneracionEnvio, importeBruto);
        var importeProductos = RedondearMoneda(Math.Max(0, importeBruto - envio.Monto));
        var cantidadTotal = detalles.Sum(d => d.Cantidad);
        var ahora = DateTime.UtcNow;
        var descuentos = await _descuentoRepository.GetVigentesConRelacionesAsync(ahora);
        var codigoNormalizado = string.IsNullOrWhiteSpace(codigoPromocional)
            ? null
            : codigoPromocional.Trim().ToUpperInvariant();

        if (codigoNormalizado is not null && descuentos.All(d => d.CodigoPromocionalNormalizado != codigoNormalizado))
            throw new BusinessRuleException("El código promocional no existe o no está vigente.");

        var descuentosAplicados = new List<DescuentoAplicadoDto>();
        decimal totalDescuento = 0;

        foreach (var descuento in descuentos.OrderBy(d => d.Prioridad))
        {
            if (codigoNormalizado is not null)
            {
                if (descuento.CodigoPromocionalNormalizado != codigoNormalizado) continue;
            }
            else if (!string.IsNullOrWhiteSpace(descuento.CodigoPromocionalNormalizado))
            {
                continue;
            }

            if (importeProductos <= 0) continue;
            if (descuento.MontoMinimo.HasValue && importeProductos < descuento.MontoMinimo.Value) continue;
            if (descuento.CantidadMinima.HasValue && cantidadTotal < descuento.CantidadMinima.Value) continue;
            if (descuento.Clientes.Any() && (!clienteId.HasValue || descuento.Clientes.All(c => c.ClienteId != clienteId.Value))) continue;
            if (descuento.Roles.Any() && (!rolIdUsuario.HasValue || descuento.Roles.All(r => r.RolId != rolIdUsuario.Value))) continue;

            var baseElegibleBruta = CalcularBaseElegible(
                detalles,
                descuento.Productos.Select(p => p.ProductoId),
                descuento.Categorias.Select(c => c.CategoriaId));
            var baseElegible = AjustarBaseSinEnvio(baseElegibleBruta, importeBruto, importeProductos);
            if (baseElegible <= 0) continue;

            if (descuento.LimiteTotalUsos.HasValue &&
                await _descuentoRepository.ContarUsosAsync(descuento.Id) >= descuento.LimiteTotalUsos.Value)
                continue;

            if (descuento.LimiteUsosPorCliente.HasValue && clienteId.HasValue &&
                await _descuentoRepository.ContarUsosPorClienteAsync(descuento.Id, clienteId.Value) >= descuento.LimiteUsosPorCliente.Value)
                continue;

            var monto = descuento.Tipo == TipoDescuento.Porcentaje
                ? RedondearMoneda(baseElegible * descuento.Valor / 100m)
                : RedondearMoneda(descuento.Valor);

            if (descuento.MontoMaximoDescuento.HasValue && monto > descuento.MontoMaximoDescuento.Value)
                monto = RedondearMoneda(descuento.MontoMaximoDescuento.Value);

            var disponible = RedondearMoneda(Math.Max(0, importeProductos - totalDescuento));
            if (monto > disponible) monto = disponible;
            if (monto <= 0) continue;

            descuentosAplicados.Add(new DescuentoAplicadoDto
            {
                DescuentoId = descuento.Id,
                Nombre = descuento.Nombre,
                Codigo = descuento.CodigoPromocional,
                Tipo = descuento.Tipo.ToString(),
                Valor = descuento.Valor,
                Monto = monto
            });
            totalDescuento = RedondearMoneda(totalDescuento + monto);

            if (!descuento.Acumulable) break;
        }

        var impuestosAplicados = await CalcularImpuestosAsync(
            detalles,
            OperacionImpuesto.Venta,
            importeBruto,
            importeProductos,
            totalDescuento,
            clienteId,
            proveedorId: null);

        return ConstruirResultado(
            importeBruto,
            importeProductos,
            totalDescuento,
            descuentosAplicados,
            impuestosAplicados,
            envio);
    }

    public async Task<ResultadoCalculoDto> CalcularCompraAsync(List<DetalleCalculoInput> detalles, int? proveedorId)
    {
        var importeBruto = RedondearMoneda(detalles.Sum(CalcularSubtotalLinea));
        var impuestosAplicados = await CalcularImpuestosAsync(
            detalles,
            OperacionImpuesto.Compra,
            importeBruto,
            importeBruto,
            totalDescuento: 0,
            clienteId: null,
            proveedorId);

        return ConstruirResultado(
            importeBruto,
            importeBruto,
            totalDescuento: 0,
            descuentosAplicados: new List<DescuentoAplicadoDto>(),
            impuestosAplicados,
            new EnvioResuelto());
    }

    private async Task<EnvioResuelto> ResolverEnvioAsync(
        int? costoEnvioId,
        bool exonerado,
        string? motivoExoneracion,
        decimal importeBruto)
    {
        if (exonerado)
        {
            if (string.IsNullOrWhiteSpace(motivoExoneracion))
                throw new BusinessRuleException("Debe indicar el motivo de exoneración del envío.");

            return new EnvioResuelto
            {
                Exonerado = true,
                MotivoExoneracion = motivoExoneracion.Trim()
            };
        }

        CostoEnvio? costo = costoEnvioId.HasValue
            ? await _costoEnvioRepository.GetByIdAsync(costoEnvioId.Value)
            : await _costoEnvioRepository.GetPredeterminadoVigenteAsync(DateTime.UtcNow);

        if (costo is null)
            throw new BusinessRuleException("No existe un costo de envío vigente para aplicar a la venta.");
        if (!costo.EstaVigente(DateTime.UtcNow))
            throw new BusinessRuleException("El costo de envío seleccionado no está vigente.");
        if (costo.Monto > importeBruto)
            throw new BusinessRuleException("El costo de envío no puede superar el total comercial de la venta.");

        return new EnvioResuelto
        {
            Id = costo.Id,
            Nombre = costo.Nombre,
            Departamento = costo.Departamento,
            Ciudad = costo.Ciudad,
            Zona = costo.Zona,
            Modalidad = costo.Modalidad,
            Monto = RedondearMoneda(costo.Monto)
        };
    }

    private static ResultadoCalculoDto ConstruirResultado(
        decimal importeBruto,
        decimal importeProductos,
        decimal totalDescuento,
        List<DescuentoAplicadoDto> descuentosAplicados,
        List<ImpuestoAplicadoDto> impuestosAplicados,
        EnvioResuelto envio)
    {
        importeBruto = RedondearMoneda(importeBruto);
        importeProductos = RedondearMoneda(importeProductos);
        totalDescuento = RedondearMoneda(totalDescuento);
        var impuestoIncluido = RedondearMoneda(impuestosAplicados.Where(i => i.IncluidoEnPrecio).Sum(i => i.Monto));
        var impuestoAdicional = RedondearMoneda(impuestosAplicados.Where(i => !i.IncluidoEnPrecio).Sum(i => i.Monto));

        // El subtotal y el impuesto incluido describen el precio comercial antes
        // del descuento. El descuento se presenta y descuenta como componente separado.
        var subtotalNeto = RedondearMoneda(Math.Max(0, importeProductos - impuestoIncluido));
        var total = RedondearMoneda(Math.Max(0,
            subtotalNeto + impuestoIncluido + impuestoAdicional + envio.Monto - totalDescuento));

        // Invariante monetaria de documento: después de redondear todos los componentes
        // a centavos, el total persistible siempre se deriva de esos mismos componentes.
        var totalConciliado = RedondearMoneda(
            subtotalNeto + impuestoIncluido + impuestoAdicional + envio.Monto - totalDescuento);
        if (total != Math.Max(0, totalConciliado))
            throw new BusinessRuleException("No fue posible conciliar los componentes monetarios del documento al centavo.");

        return new ResultadoCalculoDto
        {
            ImporteBruto = importeBruto,
            ImporteProductos = importeProductos,
            Subtotal = subtotalNeto,
            DescuentosAplicados = descuentosAplicados,
            TotalDescuento = totalDescuento,
            ImpuestosAplicados = impuestosAplicados,
            TotalImpuesto = RedondearMoneda(impuestoIncluido + impuestoAdicional),
            ImpuestoIncluido = impuestoIncluido,
            ImpuestoAdicional = impuestoAdicional,
            CostoEnvioId = envio.Id,
            CostoEnvioNombre = envio.Nombre,
            CostoEnvioDepartamento = envio.Departamento,
            CostoEnvioCiudad = envio.Ciudad,
            CostoEnvioZona = envio.Zona,
            CostoEnvioModalidad = envio.Modalidad,
            CostoEnvio = RedondearMoneda(envio.Monto),
            EnvioExonerado = envio.Exonerado,
            MotivoExoneracionEnvio = envio.MotivoExoneracion,
            Total = total
        };
    }

    public async Task RegistrarUsoVentaAsync(int ventaId, int? clienteId, List<VentaDescuento> descuentos, List<VentaImpuesto> impuestos)
    {
        foreach (var d in descuentos)
        {
            await _descuentoRepository.AddHistorialAsync(new HistorialUsoDescuento
            {
                DescuentoId = d.DescuentoId,
                VentaId = ventaId,
                ClienteId = clienteId,
                MontoAplicado = d.MontoAplicado
            });

            var entidad = await _descuentoRepository.GetByIdAsync(d.DescuentoId);
            if (entidad is not null)
            {
                entidad.UsosRealizados += 1;
                _descuentoRepository.Update(entidad);
            }
        }

        foreach (var i in impuestos)
        {
            await _impuestoRepository.AddHistorialAsync(new HistorialAplicacionImpuesto
            {
                ImpuestoId = i.ImpuestoId,
                DocumentoTipo = "Venta",
                DocumentoId = ventaId,
                BaseImponible = i.BaseImponible,
                TasaAplicada = i.TasaSnapshot,
                MontoAplicado = i.MontoAplicado
            });
        }

        await _descuentoRepository.SaveChangesAsync();
        await _impuestoRepository.SaveChangesAsync();
    }

    public async Task RegistrarUsoCompraAsync(int compraId, List<CompraImpuesto> impuestos)
    {
        foreach (var i in impuestos)
        {
            await _impuestoRepository.AddHistorialAsync(new HistorialAplicacionImpuesto
            {
                ImpuestoId = i.ImpuestoId,
                DocumentoTipo = "Compra",
                DocumentoId = compraId,
                BaseImponible = i.BaseImponible,
                TasaAplicada = i.TasaSnapshot,
                MontoAplicado = i.MontoAplicado
            });
        }

        await _impuestoRepository.SaveChangesAsync();
    }

    private async Task<List<ImpuestoAplicadoDto>> CalcularImpuestosAsync(
        List<DetalleCalculoInput> detalles,
        OperacionImpuesto operacion,
        decimal importeBruto,
        decimal importeProductos,
        decimal totalDescuento,
        int? clienteId,
        int? proveedorId)
    {
        var candidatos = await _impuestoRepository.GetVigentesConRelacionesAsync(DateTime.UtcNow, operacion);
        var impuestosAplicados = new List<ImpuestoAplicadoDto>();

        foreach (var impuesto in candidatos.OrderBy(i => i.Prioridad))
        {
            if (operacion == OperacionImpuesto.Venta &&
                clienteId.HasValue &&
                impuesto.ClientesExentos.Any(c => c.ClienteId == clienteId.Value))
                continue;

            if (operacion == OperacionImpuesto.Compra &&
                proveedorId.HasValue &&
                impuesto.ProveedoresExentos.Any(p => p.ProveedorId == proveedorId.Value))
                continue;

            var baseElegibleBruta = CalcularBaseElegible(
                detalles,
                impuesto.Productos.Select(p => p.ProductoId),
                impuesto.Categorias.Select(c => c.CategoriaId));
            var baseElegibleProductos = AjustarBaseSinEnvio(baseElegibleBruta, importeBruto, importeProductos);
            if (baseElegibleProductos <= 0) continue;

            var descuentoProrrateado = importeProductos <= 0
                ? 0
                : RedondearMoneda(totalDescuento * (baseElegibleProductos / importeProductos));

            // El descuento reduce el total final, pero no reescribe la composición
            // histórica de un impuesto que ya estaba incluido en el precio comercial.
            var importeSujeto = RedondearMoneda(impuesto.IncluidoEnPrecio || impuesto.SeCalculaAntesDescuento
                ? baseElegibleProductos
                : Math.Max(0, baseElegibleProductos - descuentoProrrateado));

            decimal baseImponible;
            decimal monto;

            if (impuesto.IncluidoEnPrecio)
            {
                if (impuesto.Tipo == TipoImpuesto.Porcentaje)
                {
                    if (impuesto.Tasa <= 0) continue;
                    baseImponible = RedondearMoneda(
                        importeSujeto / (1m + impuesto.Tasa / 100m));
                    monto = RedondearMoneda(importeSujeto - baseImponible);
                }
                else
                {
                    monto = RedondearMoneda(Math.Min(impuesto.MontoFijo ?? 0, importeSujeto));
                    baseImponible = RedondearMoneda(Math.Max(0, importeSujeto - monto));
                }
            }
            else
            {
                baseImponible = RedondearMoneda(importeSujeto);
                monto = impuesto.Tipo == TipoImpuesto.Porcentaje
                    ? RedondearMoneda(baseImponible * impuesto.Tasa / 100m)
                    : RedondearMoneda(impuesto.MontoFijo ?? 0);
            }

            if (monto <= 0) continue;

            impuestosAplicados.Add(new ImpuestoAplicadoDto
            {
                ImpuestoId = impuesto.Id,
                Nombre = impuesto.Nombre,
                Codigo = impuesto.Codigo,
                Tasa = impuesto.Tasa,
                BaseImponible = baseImponible,
                Monto = monto,
                IncluidoEnPrecio = impuesto.IncluidoEnPrecio
            });

            if (!impuesto.Acumulativo) break;
        }

        return impuestosAplicados;
    }

    private static decimal AjustarBaseSinEnvio(decimal baseElegibleBruta, decimal importeBruto, decimal importeProductos)
    {
        if (importeBruto <= 0 || importeProductos >= importeBruto)
            return RedondearMoneda(baseElegibleBruta);
        return RedondearMoneda(baseElegibleBruta * (importeProductos / importeBruto));
    }

    private static decimal CalcularBaseElegible(
        List<DetalleCalculoInput> detalles,
        IEnumerable<int> productoIds,
        IEnumerable<int> categoriaIds)
    {
        var productos = productoIds.ToHashSet();
        var categorias = categoriaIds.ToHashSet();

        if (productos.Count == 0 && categorias.Count == 0)
            return RedondearMoneda(detalles.Sum(CalcularSubtotalLinea));

        return RedondearMoneda(detalles
            .Where(d => productos.Contains(d.ProductoId) ||
                (d.CategoriaId.HasValue && categorias.Contains(d.CategoriaId.Value)))
            .Sum(CalcularSubtotalLinea));
    }

    private static decimal CalcularSubtotalLinea(DetalleCalculoInput detalle) =>
        RedondearMoneda(detalle.Cantidad * detalle.PrecioUnitario);

    private static decimal RedondearMoneda(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private sealed class EnvioResuelto
    {
        public int? Id { get; init; }
        public string? Nombre { get; init; }
        public string? Departamento { get; init; }
        public string? Ciudad { get; init; }
        public string? Zona { get; init; }
        public string? Modalidad { get; init; }
        public decimal Monto { get; init; }
        public bool Exonerado { get; init; }
        public string? MotivoExoneracion { get; init; }
    }
}