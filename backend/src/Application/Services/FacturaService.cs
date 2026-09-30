using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using CatalogoBanco = Solqaryn.Domain.Entities.Catalogos.Banco;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Services;

public class FacturaService : IFacturaService
{
    private readonly IFacturaRepository _repository;
    private readonly IEmpresaConfiguracionService _empresaConfiguracionService;
    private readonly IMovimientoFinancieroRepository? _movimientoFinancieroRepository;

    public FacturaService(IFacturaRepository repository, IEmpresaConfiguracionService empresaConfiguracionService)
        : this(repository, empresaConfiguracionService, null)
    {
    }

    public FacturaService(
        IFacturaRepository repository,
        IEmpresaConfiguracionService empresaConfiguracionService,
        IMovimientoFinancieroRepository? movimientoFinancieroRepository)
    {
        _repository = repository;
        _empresaConfiguracionService = empresaConfiguracionService;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
    }

    public async Task<FacturaDto?> GetByIdAsync(int id)
    {
        var factura = await _repository.GetByIdAsync(id);
        return factura is null ? null : await ToDtoAsync(factura);
    }

    public async Task<FacturaDto?> GetByVentaIdAsync(int ventaId)
    {
        var factura = await _repository.GetByVentaIdAsync(ventaId);
        return factura is null ? null : await ToDtoAsync(factura);
    }

    public async Task<List<FacturaDto>> GetAllAsync()
    {
        var facturas = await _repository.GetAllAsync();
        var resultado = new List<FacturaDto>();
        foreach (var f in facturas)
            resultado.Add(await ToDtoAsync(f));
        return resultado;
    }

    public async Task<FacturaDto> RegistrarPagoAsync(
        int facturaId,
        RegistrarFacturaPagoDto dto,
        int? usuarioId,
        string? nombreUsuario)
    {
        var factura = await ObtenerOperableAsync(facturaId);
        var montoRecibido = Math.Round(dto.Monto, 2, MidpointRounding.AwayFromZero);
        if (montoRecibido <= 0)
            throw new BusinessRuleException("El monto del pago debe ser mayor que cero.");

        var metodoPagoCatalogo = await ResolverMetodoPagoAsync(dto.MetodoPago);
        var referencia = Normalizar(dto.Referencia, 120);
        if (metodoPagoCatalogo.RequiereReferencia && string.IsNullOrWhiteSpace(referencia))
            throw new BusinessRuleException("Debe indicar la referencia para el método de pago seleccionado.");

        var banco = await ResolverBancoAsync(dto.BancoId, metodoPagoCatalogo.RequiereBanco);

        RecalcularPago(factura);
        if (factura.SaldoPendiente <= 0)
            throw new BusinessRuleException("La factura ya se encuentra pagada.");

        var montoAplicado = montoRecibido;
        var cambio = 0m;
        if (montoRecibido > factura.SaldoPendiente)
        {
            if (!metodoPagoCatalogo.PermiteCambio)
                throw new BusinessRuleException("El pago no puede superar el saldo pendiente de la factura para el método de pago seleccionado.");

            montoAplicado = factura.SaldoPendiente;
            cambio = Math.Round(montoRecibido - montoAplicado, 2, MidpointRounding.AwayFromZero);
        }

        factura.Pagos.Add(new FacturaPago
        {
            FechaPago = dto.FechaPago?.ToUniversalTime() ?? DateTime.UtcNow,
            Monto = montoAplicado,
            MontoRecibido = montoRecibido,
            Cambio = cambio,
            MetodoPagoId = metodoPagoCatalogo.Id,
            MetodoPagoCatalogo = metodoPagoCatalogo,
            MetodoPago = DerivarMetodoPagoLegacy(metodoPagoCatalogo),
            MetodoPagoCodigoSnapshot = metodoPagoCatalogo.Codigo,
            MetodoPagoNombreSnapshot = metodoPagoCatalogo.Nombre,
            BancoId = banco?.Id,
            Banco = banco,
            BancoCodigoSnapshot = banco?.Codigo,
            BancoNombreSnapshot = banco?.Nombre,
            Referencia = referencia,
            Observaciones = Normalizar(dto.Observaciones, 500),
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = Normalizar(nombreUsuario, 150)
        });

        RecalcularPago(factura);
        await SincronizarMovimientoVentaAsync(factura);
        _repository.Update(factura);
        await _repository.SaveChangesAsync();
        return await ToDtoAsync(factura);
    }

    public async Task<FacturaDto> AnularPagoAsync(
        int facturaId,
        int pagoId,
        AnularFacturaPagoDto dto,
        int? usuarioId,
        string? nombreUsuario)
    {
        var factura = await ObtenerOperableAsync(facturaId);
        var motivo = Normalizar(dto.Motivo, 500);
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("Debe indicar el motivo de anulación del pago.");

        var pago = factura.Pagos.FirstOrDefault(p => p.Id == pagoId);
        if (pago is null)
            throw new BusinessRuleException("El pago indicado no pertenece a la factura.");
        if (pago.Anulado)
            throw new BusinessRuleException("El pago ya se encuentra anulado.");

        pago.Anulado = true;
        pago.FechaAnulacion = DateTime.UtcNow;
        pago.AnuladoPorUsuarioId = usuarioId;
        pago.AnuladoPorNombreUsuario = Normalizar(nombreUsuario, 150);
        pago.MotivoAnulacion = motivo;
        pago.FechaActualizacion = DateTime.UtcNow;

        RecalcularPago(factura);
        await SincronizarMovimientoVentaAsync(factura);
        _repository.Update(factura);
        await _repository.SaveChangesAsync();
        return await ToDtoAsync(factura);
    }

    public async Task<FacturaDto> CambiarEstadoAsync(
        int facturaId,
        CambiarEstadoFacturaDto dto,
        int? usuarioId,
        string? nombreUsuario)
    {
        var factura = await _repository.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("Factura no encontrada.");

        if (!Enum.TryParse<EstadoFactura>(dto.Estado?.Trim(), true, out var nuevoEstado))
            throw new BusinessRuleException("El estado de factura no es válido.");
        if (nuevoEstado is EstadoFactura.Pagada or EstadoFactura.ParcialmentePagada)
            throw new BusinessRuleException("Los estados de pago se calculan automáticamente al registrar pagos.");
        if (nuevoEstado == EstadoFactura.Anulada)
            throw new BusinessRuleException("La anulación debe ejecutarse desde el flujo transaccional de la venta para revertir inventario.");
        if (!TransicionPermitida(factura.Estado, nuevoEstado))
            throw new BusinessRuleException($"No se permite cambiar una factura de {factura.Estado} a {nuevoEstado}.");

        factura.Estado = nuevoEstado;
        if (nuevoEstado == EstadoFactura.Cancelada)
        {
            var motivo = Normalizar(dto.Motivo, 500);
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BusinessRuleException("Debe indicar el motivo de cancelación.");
            factura.FechaAnulacion = DateTime.UtcNow;
            factura.AnuladaPorUsuarioId = usuarioId;
            factura.AnuladaPorNombreUsuario = Normalizar(nombreUsuario, 150);
            factura.MotivoAnulacion = motivo;
        }

        _repository.Update(factura);
        await _repository.SaveChangesAsync();
        return await ToDtoAsync(factura);
    }

    public async Task<FacturaDto?> GetByIdParaEnlacePublicoValidadoAsync(int id)
    {
        var factura = await _repository.GetByIdParaEnlacePublicoValidadoAsync(id);
        return factura is null ? null : await ToDtoAsync(factura);
    }

    private async Task<Factura> ObtenerOperableAsync(int id)
    {
        var factura = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("Factura no encontrada.");
        if (factura.Estado is EstadoFactura.Anulada or EstadoFactura.Cancelada)
            throw new BusinessRuleException("No se pueden registrar operaciones sobre una factura anulada o cancelada.");
        return factura;
    }

    private async Task<CatalogoMetodoPago> ResolverMetodoPagoAsync(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException("El método de pago es obligatorio.");

        return await _repository.GetMetodoPagoPorCodigoONombreAsync(valor.Trim())
            ?? throw new BusinessRuleException($"El método de pago '{valor.Trim()}' no existe en el catálogo.");
    }

    private async Task<CatalogoBanco?> ResolverBancoAsync(int? bancoId, bool requerido)
    {
        if (!bancoId.HasValue || bancoId.Value <= 0)
        {
            if (requerido)
                throw new BusinessRuleException("Debe indicar un banco válido para el método de pago seleccionado.");
            return null;
        }

        return await _repository.GetBancoActivoPorIdAsync(bancoId.Value)
            ?? throw new BusinessRuleException("El banco indicado no existe o no se encuentra activo.");
    }

    private static MetodoPago DerivarMetodoPagoLegacy(CatalogoMetodoPago metodoPago)
    {
        if (Enum.TryParse<MetodoPago>(metodoPago.Codigo, true, out var porCodigo))
            return porCodigo;
        if (Enum.TryParse<MetodoPago>(metodoPago.Nombre, true, out var porNombre))
            return porNombre;
        return MetodoPago.Otro;
    }

    private static void RecalcularPago(Factura factura)
    {
        factura.TotalPagado = Math.Round(
            factura.Pagos.Where(p => !p.Anulado).Sum(p => p.Monto),
            2,
            MidpointRounding.AwayFromZero);
        factura.SaldoPendiente = Math.Max(0, Math.Round(factura.Total - factura.TotalPagado, 2, MidpointRounding.AwayFromZero));

        if (factura.TotalPagado <= 0)
            factura.Estado = factura.FechaVencimiento.HasValue && factura.FechaVencimiento.Value < DateTime.UtcNow
                ? EstadoFactura.Vencida
                : EstadoFactura.Emitida;
        else if (factura.SaldoPendiente <= 0)
            factura.Estado = EstadoFactura.Pagada;
        else
            factura.Estado = EstadoFactura.ParcialmentePagada;

        if (factura.Venta is not null)
            factura.Venta.EstadoPago = factura.TotalPagado <= 0
                ? EstadoPago.Pendiente
                : factura.SaldoPendiente <= 0
                    ? EstadoPago.Pagado
                    : EstadoPago.Parcial;
    }

    private async Task SincronizarMovimientoVentaAsync(Factura factura)
    {
        if (_movimientoFinancieroRepository is null || factura.VentaId <= 0)
            return;

        var movimiento = await _movimientoFinancieroRepository.GetByVentaIdAsync(factura.VentaId);
        if (movimiento is null
            || !movimiento.EsAutomatico
            || movimiento.Tipo != TipoMovimientoFinanciero.Ingreso
            || movimiento.Categoria != CategoriaMovimientoFinanciero.Venta)
            return;

        movimiento.Estado = factura.Estado == EstadoFactura.Pagada
            ? EstadoMovimientoFinanciero.Pagado
            : EstadoMovimientoFinanciero.Pendiente;
        _movimientoFinancieroRepository.Update(movimiento);
    }

    private static bool TransicionPermitida(EstadoFactura actual, EstadoFactura siguiente) =>
        actual == siguiente ||
        (actual == EstadoFactura.Borrador && siguiente is EstadoFactura.Emitida or EstadoFactura.Cancelada) ||
        (actual == EstadoFactura.Emitida && siguiente is EstadoFactura.Vencida or EstadoFactura.Cancelada) ||
        (actual == EstadoFactura.Vencida && siguiente is EstadoFactura.Emitida or EstadoFactura.Cancelada);

    private static string? Normalizar(string? valor, int maximo)
    {
        var limpio = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        return limpio is not null && limpio.Length > maximo ? limpio[..maximo] : limpio;
    }

    private async Task<FacturaDto> ToDtoAsync(Factura f)
    {
        var empresa = await _empresaConfiguracionService.GetActivaAsync();
        var dto = ToDto(f);
        dto.EmpresaLogoUrl = empresa?.LogoUrl;
        dto.EmpresaEslogan = empresa?.Eslogan;
        dto.EmpresaTextoFactura = empresa?.TextoFactura;
        dto.EmpresaTextoLegal = empresa?.TextoLegal;
        dto.EmpresaCopyright = empresa?.MostrarCopyright == true ? empresa.Copyright : null;
        return dto;
    }

    private static FacturaDto ToDto(Factura f)
    {
        var detalles = f.Detalles.Select(d => new FacturaDetalleDto
        {
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            ProductoNombre = d.ProductoNombre,
            ProductoMarca = d.ProductoMarca,
            ProductoModelo = d.ProductoModelo,
            VarianteColor = d.VarianteColor,
            VarianteTalla = d.VarianteTalla,
            VarianteSku = d.VarianteSku,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Descuento = d.Descuento,
            Impuesto = d.Impuesto,
            Subtotal = d.Subtotal,
            TotalLinea = d.TotalLinea == 0 ? d.Subtotal : d.TotalLinea,
            Observaciones = d.Observaciones
        }).ToList();

        var importeBruto = f.ImporteBruto > 0 ? f.ImporteBruto : detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        var impuestos = f.Venta?.ImpuestosAplicados.Select(i => new ImpuestoAplicadoDto
        {
            ImpuestoId = i.ImpuestoId,
            Nombre = i.ImpuestoNombreSnapshot,
            Codigo = i.ImpuestoCodigoSnapshot,
            Tasa = i.TasaSnapshot,
            BaseImponible = i.BaseImponible,
            Monto = i.MontoAplicado,
            IncluidoEnPrecio = i.IncluidoEnPrecioSnapshot
        }).ToList() ?? new List<ImpuestoAplicadoDto>();

        var totalDespuesDescuento = Math.Max(0, importeBruto - f.Descuento);
        if (impuestos.Count > 0 && impuestos.All(i => !i.IncluidoEnPrecio) && Math.Abs(f.Total - totalDespuesDescuento) <= 0.01m)
            foreach (var impuesto in impuestos) impuesto.IncluidoEnPrecio = true;

        var impuestoIncluido = impuestos.Where(i => i.IncluidoEnPrecio).Sum(i => i.Monto);
        var impuestoAdicional = impuestos.Where(i => !i.IncluidoEnPrecio).Sum(i => i.Monto);
        var pagos = f.Pagos.OrderByDescending(p => p.FechaPago).Select(p => new FacturaPagoDto
        {
            Id = p.Id,
            FechaPago = p.FechaPago,
            Monto = p.Monto,
            MontoRecibido = p.MontoRecibido > 0 ? p.MontoRecibido : p.Monto,
            Cambio = p.Cambio,
            MetodoPago = p.MetodoPagoNombreSnapshot ?? p.MetodoPagoCatalogo?.Nombre ?? p.MetodoPago.ToString(),
            BancoId = p.BancoId,
            BancoCodigo = p.BancoCodigoSnapshot ?? p.Banco?.Codigo,
            BancoNombre = p.BancoNombreSnapshot ?? p.Banco?.Nombre,
            Referencia = p.Referencia,
            Observaciones = p.Observaciones,
            Anulado = p.Anulado,
            FechaAnulacion = p.FechaAnulacion,
            MotivoAnulacion = p.MotivoAnulacion
        }).ToList();
        var totalPagado = pagos.Where(p => !p.Anulado).Sum(p => p.Monto);
        var saldoPendiente = Math.Max(0, f.Total - totalPagado);

        return new FacturaDto
        {
            Id = f.Id,
            VentaId = f.VentaId,
            NumeroVentaOrigen = f.Venta?.NumeroVenta ?? string.Empty,
            NumeroFactura = f.NumeroFactura,
            CodigoInterno = f.CodigoInterno,
            FechaEmision = f.FechaEmision,
            FechaVencimiento = f.FechaVencimiento,
            Estado = f.Estado.ToString(),
            Moneda = f.Moneda,
            CondicionPago = f.CondicionPago,
            Referencia = f.Referencia,
            EmpresaNombre = f.EmpresaNombre,
            EmpresaRTN = f.EmpresaRTN,
            EmpresaTelefono = f.EmpresaTelefono,
            EmpresaCorreo = f.EmpresaCorreo,
            EmpresaDireccion = f.EmpresaDireccion,
            ClienteNombre = f.ClienteNombre,
            ClienteTelefono = f.ClienteTelefono,
            ClienteIdentidadORTN = f.ClienteIdentidadORTN,
            ClienteCorreo = f.ClienteCorreo,
            ClienteDireccion = f.ClienteDireccion,
            VendedorNombreUsuario = f.VendedorNombreUsuario,
            GeneradaPorNombreUsuario = f.GeneradaPorNombreUsuario,
            ImporteBruto = importeBruto,
            Subtotal = f.Subtotal,
            Descuento = f.Descuento,
            Impuesto = f.Impuesto,
            ImpuestoIncluido = impuestoIncluido,
            ImpuestoAdicional = impuestoAdicional,
            CostoEnvio = f.CostoEnvio,
            CostoEnvioId = f.CostoEnvioId,
            CostoEnvioNombre = f.CostoEnvioNombreSnapshot,
            CostoEnvioDepartamento = f.CostoEnvioDepartamentoSnapshot,
            CostoEnvioCiudad = f.CostoEnvioCiudadSnapshot,
            CostoEnvioZona = f.CostoEnvioZonaSnapshot,
            CostoEnvioModalidad = f.CostoEnvioModalidadSnapshot,
            EnvioExonerado = f.EnvioExonerado,
            MotivoExoneracionEnvio = f.MotivoExoneracionEnvio,
            Total = f.Total,
            TotalPagado = totalPagado,
            SaldoPendiente = saldoPendiente,
            MetodoPago = f.MetodoPagoNombreSnapshot ?? f.Venta?.MetodoPagoCatalogo?.Nombre ?? f.Venta?.MetodoPago.ToString() ?? string.Empty,
            EstadoPago = f.Venta?.EstadoPago.ToString() ?? string.Empty,
            Observaciones = f.Observaciones,
            Detalles = detalles,
            Pagos = pagos,
            DescuentosAplicados = f.Venta?.DescuentosAplicados.Select(d => new DescuentoAplicadoDto
            {
                DescuentoId = d.DescuentoId,
                Nombre = d.DescuentoNombreSnapshot,
                Codigo = d.DescuentoCodigoSnapshot,
                Tipo = d.TipoSnapshot.ToString(),
                Valor = d.ValorSnapshot,
                Monto = d.MontoAplicado
            }).ToList() ?? new List<DescuentoAplicadoDto>(),
            ImpuestosAplicados = impuestos,
            FechaAnulacion = f.FechaAnulacion,
            AnuladaPorNombreUsuario = f.AnuladaPorNombreUsuario,
            MotivoAnulacion = f.MotivoAnulacion
        };
    }
}
