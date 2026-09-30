using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IInventarioConcurrencyService _inventarioConcurrency;
    private readonly IFacturaRepository _facturaRepository;
    private readonly VentaKardexMovimientoRegistrar _ventaKardexMovimientoRegistrar;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly IEmpresaConfiguracionService _empresaConfiguracionService;
    private readonly ICalculoService _calculoService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;
    private readonly ITipoClientePredeterminadoResolver _predeterminadoResolver;

    public VentaService(
        IVentaRepository ventaRepository,
        IClienteRepository clienteRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IInventarioConcurrencyService inventarioConcurrency,
        IFacturaRepository facturaRepository,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        IEmpresaConfiguracionService empresaConfiguracionService,
        ICalculoService calculoService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria,
        ITipoClientePredeterminadoResolver predeterminadoResolver,
        IKardexMovimientoWriter? kardexMovimientoWriter = null)
    {
        _ventaRepository = ventaRepository;
        _clienteRepository = clienteRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _inventarioConcurrency = inventarioConcurrency;
        _facturaRepository = facturaRepository;
        _ventaKardexMovimientoRegistrar = new VentaKardexMovimientoRegistrar(
            kardexMovimientoWriter ?? new KardexMovimientoWriter(movimientoInventarioRepository));
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _empresaConfiguracionService = empresaConfiguracionService;
        _calculoService = calculoService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
        _predeterminadoResolver = predeterminadoResolver;
    }

    public async Task<VentaDto?> GetByIdAsync(int id)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        return venta is null ? null : ToDto(venta);
    }

    public async Task<PagedResult<VentaDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, total) = await _ventaRepository.GetPagedAsync(request);
        return new PagedResult<VentaDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<VentaDto> CreateAsync(CreateVentaDto dto)
    {
        var metodoPagoCatalogo = await ResolverMetodoPagoAsync(dto.MetodoPago);
        var venta = new Venta
        {
            NumeroVenta = CrearNumeroTemporal("VEN"),
            ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente final" : dto.ClienteNombre,
            ClienteTelefono = dto.ClienteTelefono,
            ClienteIdentidadORTN = dto.ClienteIdentidadORTN,
            ClienteCorreo = dto.ClienteCorreo,
            ClienteDireccion = dto.ClienteDireccion,
            MetodoPagoId = metodoPagoCatalogo.Id,
            MetodoPagoCatalogo = metodoPagoCatalogo,
            MetodoPago = DerivarMetodoPagoLegacy(metodoPagoCatalogo),
            EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente),
            Estado = EstadoDocumento.Borrador,
            // Descuento/Impuesto NO se toman de dto: se recalculan abajo (sección 13).
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await VincularClienteAsync(venta, dto);
        await ArmarDetallesAsync(venta, dto.Detalles, validarStock: false);
        await CalcularTotalesAsync(venta, dto.CodigoPromocional, dto.CostoEnvioId, dto.EnvioExonerado, dto.MotivoExoneracionEnvio);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _ventaRepository.AddAsync(venta);
            await _ventaRepository.SaveChangesAsync();

            // El identificador autoincremental es la única fuente de numeración.
            // El número temporal evita colisiones mientras MySQL asigna el Id.
            venta.NumeroVenta = $"VEN-{venta.Id:D6}";
            _ventaRepository.Update(venta);
            await _ventaRepository.SaveChangesAsync();
        });

        await _auditoria.RegistrarAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Crear,
            $"Venta creada: {venta.NumeroVenta}",
            venta.Id,
            entidad: "Venta",
            valoresNuevos: new
            {
                venta.NumeroVenta,
                venta.ImporteBruto,
                venta.Subtotal,
                venta.Impuesto,
                venta.Descuento,
                venta.CostoEnvio,
                venta.EnvioExonerado,
                venta.MotivoExoneracionEnvio,
                venta.Total
            });

        return ToDto(venta);
    }

    public async Task<VentaDto?> UpdateAsync(int id, UpdateVentaDto dto)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        if (venta is null) return null;

        if (venta.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden editar ventas en estado Borrador.");

        var metodoPagoCatalogo = await ResolverMetodoPagoAsync(dto.MetodoPago);
        venta.ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente final" : dto.ClienteNombre;
        venta.ClienteTelefono = dto.ClienteTelefono;
        venta.ClienteIdentidadORTN = dto.ClienteIdentidadORTN;
        venta.ClienteCorreo = dto.ClienteCorreo;
        venta.ClienteDireccion = dto.ClienteDireccion;
        venta.MetodoPagoId = metodoPagoCatalogo.Id;
        venta.MetodoPagoCatalogo = metodoPagoCatalogo;
        venta.MetodoPago = DerivarMetodoPagoLegacy(metodoPagoCatalogo);
        venta.EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente);
        venta.Notas = dto.Notas;
        venta.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        venta.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        venta.FechaActualizacion = DateTime.UtcNow;

        await VincularClienteAsync(venta, dto);
        venta.Detalles.Clear();
        venta.DescuentosAplicados.Clear();
        venta.ImpuestosAplicados.Clear();
        await ArmarDetallesAsync(venta, dto.Detalles, validarStock: false);
        await CalcularTotalesAsync(venta, dto.CodigoPromocional, dto.CostoEnvioId, dto.EnvioExonerado, dto.MotivoExoneracionEnvio);

        _ventaRepository.Update(venta);
        await _ventaRepository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Editar, $"Venta actualizada: {venta.NumeroVenta}", venta.Id);

        return ToDto(venta);
    }

    public async Task<VentaDto?> ConfirmarAsync(int id)
    {
        var empresa = await _empresaConfiguracionService.GetActivaEntidadAsync();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var venta = await _ventaRepository.GetByIdForUpdateAsync(id);
            if (venta is null)
                throw new KeyNotFoundException($"Venta ID '{id}' no encontrada.");

            if (venta.Estado != EstadoDocumento.Borrador)
                throw new BusinessRuleException("Solo se pueden confirmar ventas en estado Borrador.");
            if (venta.Detalles.Count == 0)
                throw new BusinessRuleException("La venta debe tener al menos un producto para confirmarse.");

            var demanda = venta.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: true);

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad -= productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = venta.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var precioUnitarioMovimiento = detallesClave.Sum(d => d.Subtotal) / item.Cantidad;
                var costoUnitarioMovimiento = detallesClave.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad) / item.Cantidad;

                var stockAnteriorMovimiento = producto.Cantidad + item.Cantidad;
                var stockNuevoMovimiento = producto.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    if (!variante.Activo)
                        throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva y no puede venderse.");

                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad -= item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _ventaKardexMovimientoRegistrar.RegistrarConfirmacionAsync(venta.Id, new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
                    ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Salida,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    PrecioUnitario = precioUnitarioMovimiento,
                    CostoUnitario = costoUnitarioMovimiento,
                    Descripcion = $"Salida por venta {venta.NumeroVenta}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Categoria = CategoriaMovimientoFinanciero.Venta,
                Concepto = $"Venta {venta.NumeroVenta} - {venta.ClienteNombre}",
                Monto = venta.Total,
                Estado = venta.EstadoPago == EstadoPago.Pagado
                    ? EstadoMovimientoFinanciero.Pagado
                    : EstadoMovimientoFinanciero.Pendiente,
                MetodoPagoId = venta.MetodoPagoId,
                MetodoPagoCatalogo = venta.MetodoPagoCatalogo,
                MetodoPago = venta.MetodoPagoCatalogo is null ? null : DerivarMetodoPagoLegacy(venta.MetodoPagoCatalogo),
                EsAutomatico = true,
                ModuloOrigen = "Venta",
                ReferenciaId = venta.Id,
                VentaId = venta.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            venta.Estado = EstadoDocumento.Confirmada;
            venta.ConfirmadoPorUsuarioId = _currentUser.UsuarioId;
            venta.ConfirmadoPorNombreUsuario = _currentUser.NombreUsuario;
            venta.FechaConfirmacion = DateTime.UtcNow;
            _ventaRepository.Update(venta);

            var factura = new Factura
            {
                VentaId = venta.Id,
                NumeroFactura = CrearNumeroTemporal("FAC"),
                Estado = EstadoFactura.Emitida,
                EmpresaNombre = empresa.NombreComercial,
                EmpresaRTN = empresa.RTN,
                EmpresaTelefono = empresa.Telefono,
                EmpresaCorreo = empresa.Correo,
                EmpresaDireccion = empresa.Direccion,
                ClienteNombre = venta.ClienteNombre,
                ClienteTelefono = venta.ClienteTelefono,
                ClienteIdentidadORTN = venta.ClienteIdentidadORTN,
                ClienteCorreo = venta.ClienteCorreo,
                ClienteDireccion = venta.ClienteDireccion,
                VendedorUsuarioId = _currentUser.UsuarioId ?? 0,
                VendedorNombreUsuario = _currentUser.NombreCompleto ?? _currentUser.NombreUsuario ?? "—",
                GeneradaPorUsuarioId = _currentUser.UsuarioId,
                GeneradaPorNombreUsuario = _currentUser.NombreCompleto ?? _currentUser.NombreUsuario,
                ImporteBruto = venta.ImporteBruto,
                Subtotal = venta.Subtotal,
                Descuento = venta.Descuento,
                Impuesto = venta.Impuesto,
                CostoEnvioId = venta.CostoEnvioId,
                CostoEnvioNombreSnapshot = venta.CostoEnvioNombreSnapshot,
                CostoEnvioDepartamentoSnapshot = venta.CostoEnvioDepartamentoSnapshot,
                CostoEnvioCiudadSnapshot = venta.CostoEnvioCiudadSnapshot,
                CostoEnvioZonaSnapshot = venta.CostoEnvioZonaSnapshot,
                CostoEnvioModalidadSnapshot = venta.CostoEnvioModalidadSnapshot,
                CostoEnvioMontoSnapshot = venta.CostoEnvioMontoSnapshot,
                CostoEnvio = venta.CostoEnvio,
                EnvioExonerado = venta.EnvioExonerado,
                MotivoExoneracionEnvio = venta.MotivoExoneracionEnvio,
                Total = venta.Total,
                SaldoPendiente = venta.Total,
                MetodoPagoCodigoSnapshot = venta.MetodoPagoCatalogo?.Codigo,
                MetodoPagoNombreSnapshot = venta.MetodoPagoCatalogo?.Nombre,
                Detalles = venta.Detalles.Select(d => new FacturaDetalle
                {
                    ProductoId = d.ProductoId,
                    ProductoVarianteId = d.ProductoVarianteId,
                    ProductoNombre = d.ProductoNombreSnapshot,
                    ProductoMarca = d.ProductoMarcaSnapshot,
                    ProductoModelo = d.ProductoModeloSnapshot,
                    VarianteColor = d.ProductoColorSnapshot,
                    VarianteTalla = d.ProductoTallaSnapshot,
                    VarianteSku = d.ProductoSkuSnapshot,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                }).ToList()
            };

            await _facturaRepository.AddAsync(factura);
            await _facturaRepository.SaveChangesAsync();

            factura.NumeroFactura = $"FAC-{factura.Id:D6}";
            _facturaRepository.Update(factura);

            await _calculoService.RegistrarUsoVentaAsync(
                venta.Id, venta.ClienteId,
                venta.DescuentosAplicados.ToList(),
                venta.ImpuestosAplicados.ToList());

            await _ventaRepository.SaveChangesAsync();
        });

        var actualizada = await _ventaRepository.GetByIdAsync(id);
        await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Confirmar, $"Venta confirmada: {actualizada!.NumeroVenta}", id);
        return ToDto(actualizada);
    }

    public async Task<VentaDto?> AnularAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var venta = await _ventaRepository.GetByIdForUpdateAsync(id);
            if (venta is null)
                throw new KeyNotFoundException($"Venta ID '{id}' no encontrada.");

            if (venta.Estado != EstadoDocumento.Confirmada)
                throw new BusinessRuleException("Solo se pueden anular ventas confirmadas.");

            var demanda = venta.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: false);

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad += productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = venta.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var precioUnitarioMovimiento = detallesClave.Sum(d => d.Subtotal) / item.Cantidad;
                var costoUnitarioMovimiento = detallesClave.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad) / item.Cantidad;

                var stockAnteriorMovimiento = producto.Cantidad - item.Cantidad;
                var stockNuevoMovimiento = producto.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad += item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _ventaKardexMovimientoRegistrar.RegistrarAnulacionAsync(venta.Id, new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
                    ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Causa = CausaMovimientoInventario.AnulacionVenta,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    PrecioUnitario = precioUnitarioMovimiento,
                    CostoUnitario = costoUnitarioMovimiento,
                    Descripcion = $"Entrada por anulación de venta {venta.NumeroVenta}. Motivo: {motivo}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                });
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Egreso,
                Categoria = CategoriaMovimientoFinanciero.Reversion,
                Concepto = $"Reversión de venta anulada {venta.NumeroVenta}",
                Monto = venta.Total,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = true,
                ModuloOrigen = "Reversion",
                ReferenciaId = venta.Id,
                VentaId = venta.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            venta.Estado = EstadoDocumento.Anulada;
            venta.AnuladoPorUsuarioId = _currentUser.UsuarioId;
            venta.AnuladoPorNombreUsuario = _currentUser.NombreUsuario;
            venta.FechaAnulacion = DateTime.UtcNow;
            venta.MotivoAnulacion = motivo;
            _ventaRepository.Update(venta);

            var factura = await _facturaRepository.GetByVentaIdAsync(venta.Id);
            if (factura is not null)
            {
                factura.Estado = EstadoFactura.Anulada;
                factura.FechaAnulacion = DateTime.UtcNow;
                factura.AnuladaPorUsuarioId = _currentUser.UsuarioId;
                factura.AnuladaPorNombreUsuario = _currentUser.NombreUsuario;
                factura.MotivoAnulacion = motivo;
                _facturaRepository.Update(factura);
            }

            await _ventaRepository.SaveChangesAsync();
        });

        var actualizada = await _ventaRepository.GetByIdAsync(id);
        await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Anular,
            $"Venta anulada: {actualizada!.NumeroVenta}.", id, entidad: "Venta", motivo: motivo);
        return ToDto(actualizada);
    }

    public async Task<bool> DeleteBorradorAsync(int id)
    {
        var venta = await _ventaRepository.GetByIdAsync(id);
        if (venta is null) return false;

        if (venta.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden eliminar ventas en estado Borrador.");

        venta.Detalles.Clear();
        _ventaRepository.Update(venta);
        var eliminado = await _ventaRepository.SaveChangesAsync();
        if (eliminado)
            await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Eliminar, $"Borrador de venta eliminado: {venta.NumeroVenta}", venta.Id);
        return eliminado;
    }

    private async Task VincularClienteAsync(Venta venta, CreateVentaDto dto)
    {
        Cliente? cliente = null;

        if (dto.ClienteId.HasValue)
        {
            cliente = await _clienteRepository.GetByIdAsync(dto.ClienteId.Value)
                ?? throw new BusinessRuleException("El cliente seleccionado no existe.");

            if (!cliente.Activo)
                throw new BusinessRuleException("El cliente seleccionado está inactivo.");
        }
        else if (DebeGestionarCliente(dto.ClienteNombre, dto.ClienteIdentidadORTN, dto.ClienteCorreo, dto.ClienteTelefono))
        {
            cliente = await _clienteRepository.BuscarCoincidenciaActivaAsync(
                dto.ClienteIdentidadORTN,
                dto.ClienteCorreo,
                dto.ClienteTelefono,
                dto.ClienteNombre);

            if (cliente is null)
            {
                int tipoClienteId = await _predeterminadoResolver.ResolverIdPredeterminadoAsync();

                cliente = new Cliente
                {
                    Nombre = dto.ClienteNombre.Trim(),
                    Telefono = dto.ClienteTelefono,
                    IdentidadORTN = dto.ClienteIdentidadORTN,
                    Correo = dto.ClienteCorreo,
                    Direccion = dto.ClienteDireccion,
                    Activo = true,
                    TipoClienteId = tipoClienteId,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                await _clienteRepository.AddAsync(cliente);
            }
        }

        if (cliente is null)
        {
            venta.ClienteId = null;
            venta.Cliente = null;
            venta.ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente final" : dto.ClienteNombre.Trim();
            return;
        }

        venta.Cliente = cliente;
        venta.ClienteId = cliente.Id == 0 ? null : cliente.Id;
        venta.ClienteNombre = cliente.Nombre;
        venta.ClienteTelefono = cliente.Telefono;
        venta.ClienteIdentidadORTN = cliente.IdentidadORTN;
        venta.ClienteCorreo = cliente.Correo;
        venta.ClienteDireccion = cliente.Direccion;
    }

    private static bool DebeGestionarCliente(string? nombre, string? identidad, string? correo, string? telefono)
    {
        var nombreNormalizado = nombre?.Trim().ToLower() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(identidad)
            || !string.IsNullOrWhiteSpace(correo)
            || !string.IsNullOrWhiteSpace(telefono)
            || (!string.IsNullOrWhiteSpace(nombreNormalizado) && nombreNormalizado != "cliente final");
    }

    private async Task ArmarDetallesAsync(Venta venta, List<VentaDetalleInputDto> detallesInput, bool validarStock)
    {
        if (detallesInput.Count == 0)
            throw new BusinessRuleException("La venta debe tener al menos un producto.");

        foreach (var input in detallesInput)
        {
            if (input.Cantidad <= 0)
                throw new BusinessRuleException("La cantidad de cada producto debe ser mayor a 0.");
            var producto = await _productoRepository.GetByIdAsync(input.ProductoId)
                ?? throw new BusinessRuleException($"El producto con id {input.ProductoId} no existe.");

            ProductoVariante variante;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else
            {
                var tecnica = producto.Variantes.SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado);
                if (tecnica is null && producto.Variantes.Any(v => !v.EsTecnica && v.Activo && !v.Eliminado))
                    throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
                variante = tecnica
                    ?? throw new BusinessRuleException($"El producto '{producto.Nombre}' no tiene una variante operativa activa. Corrige el inventario antes de venderlo.");
            }

            if (validarStock && variante.Cantidad < input.Cantidad)
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}' / '{variante.Sku}': disponible {variante.Cantidad}, solicitado {input.Cantidad}.");

            var costoUnitario = variante.Costo ?? 0m;
            var precioUnitario = ResolverPrecioAutorizado(producto, variante);
            var subtotal = input.Cantidad * precioUnitario;
            var costoTotal = input.Cantidad * costoUnitario;

            venta.Detalles.Add(new VentaDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante.Id,
                Cantidad = input.Cantidad,
                PrecioUnitario = precioUnitario,
                CostoUnitarioSnapshot = costoUnitario,
                Subtotal = subtotal,
                UtilidadBruta = subtotal - costoTotal,
                ProductoNombreSnapshot = producto.Nombre,
                ProductoMarcaSnapshot = variante.Marca?.Nombre ?? string.Empty,
                ProductoModeloSnapshot = variante.Modelo?.Nombre ?? string.Empty,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoTallaSnapshot = variante.Talla?.Nombre,
                ProductoSkuSnapshot = variante.Sku
            });
        }
    }

    private async Task<ProductoVariante> ObtenerVarianteAsync(int varianteId, int productoId, bool exigirActiva)
    {
        var variante = await _productoVarianteRepository.GetByIdAsync(varianteId)
            ?? throw new BusinessRuleException("La variante seleccionada no existe.");
        if (variante.ProductoId != productoId)
            throw new BusinessRuleException("La variante seleccionada no pertenece al producto indicado.");
        if (exigirActiva && !variante.Activo)
            throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva.");
        return variante;
    }

    public async Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularVentaRequest request)
    {
        if (request.Detalles.Count == 0)
            throw new BusinessRuleException("La venta debe tener al menos un producto.");

        var entradas = new List<DetalleCalculoInput>();
        foreach (var d in request.Detalles)
        {
            if (d.Cantidad <= 0)
                throw new BusinessRuleException("La cantidad de cada producto debe ser mayor a 0.");

            var producto = await _productoRepository.GetByIdAsync(d.ProductoId)
                ?? throw new BusinessRuleException($"El producto con id {d.ProductoId} no existe.");
            if (!producto.Activo)
                throw new BusinessRuleException($"El producto '{producto.Nombre}' está inactivo.");

            ProductoVariante variante;
            if (d.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(d.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else
            {
                var tecnica = producto.Variantes.SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado);
                if (tecnica is null && producto.Variantes.Any(v => !v.EsTecnica && v.Activo && !v.Eliminado))
                    throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
                variante = tecnica
                    ?? throw new BusinessRuleException($"El producto '{producto.Nombre}' no tiene una variante operativa activa. Corrige el inventario antes de cotizarlo.");
            }

            var precioUnitario = ResolverPrecioAutorizado(producto, variante);

            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = d.Cantidad,
                PrecioUnitario = precioUnitario
            });
        }

        return await _calculoService.CalcularVentaAsync(entradas, request.ClienteId, _currentUser.RolId, request.CodigoPromocional, request.CostoEnvioId, request.EnvioExonerado, request.MotivoExoneracionEnvio);
    }

    private static decimal ResolverPrecioAutorizado(Producto producto, ProductoVariante variante)
    {
        var precio = variante.Precio ?? producto.Precio;
        if (precio <= 0)
            throw new BusinessRuleException(
                $"El producto '{producto.Nombre}' no tiene un precio de venta vigente configurado.");
        return precio;
    }

    private async Task CalcularTotalesAsync(Venta venta, string? codigoPromocional, int? costoEnvioId, bool envioExonerado, string? motivoExoneracionEnvio)
    {
        var entradas = venta.Detalles.Select(d => new DetalleCalculoInput
        {
            ProductoId = d.ProductoId,
            CategoriaId = null,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario
        }).ToList();

        foreach (var entrada in entradas)
        {
            var producto = await _productoRepository.GetByIdAsync(entrada.ProductoId);
            entrada.CategoriaId = producto?.CategoriaId;
        }

        var resultado = await _calculoService.CalcularVentaAsync(
            entradas, venta.ClienteId, _currentUser.RolId, codigoPromocional, costoEnvioId, envioExonerado, motivoExoneracionEnvio);

        venta.ImporteBruto = resultado.ImporteBruto;
        venta.ImporteProductos = resultado.ImporteProductos;
        venta.Subtotal = resultado.Subtotal;
        venta.Descuento = resultado.TotalDescuento;
        venta.Impuesto = resultado.TotalImpuesto;
        venta.CostoEnvioId = resultado.CostoEnvioId;
        venta.CostoEnvioNombreSnapshot = resultado.CostoEnvioNombre;
        venta.CostoEnvioDepartamentoSnapshot = resultado.CostoEnvioDepartamento;
        venta.CostoEnvioCiudadSnapshot = resultado.CostoEnvioCiudad;
        venta.CostoEnvioZonaSnapshot = resultado.CostoEnvioZona;
        venta.CostoEnvioModalidadSnapshot = resultado.CostoEnvioModalidad;
        venta.CostoEnvioMontoSnapshot = resultado.CostoEnvio;
        venta.CostoEnvio = resultado.CostoEnvio;
        venta.EnvioExonerado = resultado.EnvioExonerado;
        venta.MotivoExoneracionEnvio = resultado.MotivoExoneracionEnvio;
        venta.Total = resultado.Total;
        venta.CostoTotal = venta.Detalles.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad);
        venta.UtilidadBruta = venta.Detalles.Sum(d => d.UtilidadBruta) - venta.Descuento;

        venta.DescuentosAplicados = resultado.DescuentosAplicados.Select(d => new VentaDescuento
        {
            DescuentoId = d.DescuentoId,
            DescuentoNombreSnapshot = d.Nombre,
            DescuentoCodigoSnapshot = d.Codigo ?? string.Empty,
            TipoSnapshot = Enum.Parse<TipoDescuento>(d.Tipo),
            ValorSnapshot = d.Valor,
            MontoAplicado = d.Monto
        }).ToList();

        venta.ImpuestosAplicados = resultado.ImpuestosAplicados.Select(i => new VentaImpuesto
        {
            ImpuestoId = i.ImpuestoId,
            ImpuestoNombreSnapshot = i.Nombre,
            ImpuestoCodigoSnapshot = i.Codigo,
            TasaSnapshot = i.Tasa,
            BaseImponible = i.BaseImponible,
            MontoAplicado = i.Monto,
            IncluidoEnPrecioSnapshot = i.IncluidoEnPrecio
        }).ToList();

        if (venta.Total < 0)
            throw new BusinessRuleException("El total de la venta no puede ser negativo.");
    }

    private async Task<CatalogoMetodoPago> ResolverMetodoPagoAsync(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException("El método de pago es obligatorio.");

        var metodoPago = await _ventaRepository.GetMetodoPagoPorCodigoONombreAsync(valor.Trim());
        return metodoPago
            ?? throw new BusinessRuleException($"El método de pago '{valor.Trim()}' no existe en el catálogo.");
    }

    private static MetodoPago DerivarMetodoPagoLegacy(CatalogoMetodoPago metodoPago)
    {
        if (Enum.TryParse<MetodoPago>(metodoPago.Codigo, true, out var porCodigo))
            return porCodigo;
        if (Enum.TryParse<MetodoPago>(metodoPago.Nombre, true, out var porNombre))
            return porNombre;

        // Compatibilidad transitoria: un método administrable nuevo no representable por el enum
        // se proyecta en la columna legacy como Otro. La FK MetodoPagoId sigue siendo la autoridad.
        return MetodoPago.Otro;
    }

    private static string CrearNumeroTemporal(string prefijo)
    {
        var token = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return $"{prefijo}-TMP-{token}";
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum valorPorDefecto) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var resultado) ? resultado : valorPorDefecto;

    private static VentaDto ToDto(Venta v) => new()
    {
        Id = v.Id,
        NumeroVenta = v.NumeroVenta,
        Fecha = v.Fecha,
        ClienteId = v.ClienteId,
        ClienteNombre = v.ClienteNombre,
        ClienteTelefono = v.ClienteTelefono,
        ClienteIdentidadORTN = v.ClienteIdentidadORTN,
        ClienteCorreo = v.ClienteCorreo,
        ClienteDireccion = v.ClienteDireccion,
        Estado = v.Estado.ToString(),
        EstadoPago = v.EstadoPago.ToString(),
        MetodoPago = v.MetodoPagoCatalogo?.Nombre ?? string.Empty,
        ImporteBruto = v.ImporteBruto,
        ImporteProductos = v.ImporteProductos,
        Subtotal = v.Subtotal,
        Descuento = v.Descuento,
        Impuesto = v.Impuesto,
        CostoEnvio = v.CostoEnvio,
        CostoEnvioId = v.CostoEnvioId,
        CostoEnvioNombre = v.CostoEnvioNombreSnapshot,
        CostoEnvioDepartamento = v.CostoEnvioDepartamentoSnapshot,
        CostoEnvioCiudad = v.CostoEnvioCiudadSnapshot,
        CostoEnvioZona = v.CostoEnvioZonaSnapshot,
        CostoEnvioModalidad = v.CostoEnvioModalidadSnapshot,
        EnvioExonerado = v.EnvioExonerado,
        MotivoExoneracionEnvio = v.MotivoExoneracionEnvio,
        Total = v.Total,
        CostoTotal = v.CostoTotal,
        UtilidadBruta = v.UtilidadBruta,
        Notas = v.Notas,
        Detalles = v.Detalles.Select(d => new VentaDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            ProductoNombre = d.ProductoNombreSnapshot,
            ProductoMarca = d.ProductoMarcaSnapshot,
            ProductoModelo = d.ProductoModeloSnapshot,
            ProductoColor = d.ProductoColorSnapshot,
            ProductoTalla = d.ProductoTallaSnapshot,
            ProductoSku = d.ProductoSkuSnapshot,
            ProductoImagenPrincipalUrl = d.Producto?.ImagenPrincipal?.Url,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Subtotal,
            UtilidadBruta = d.UtilidadBruta
        }).ToList(),
        DescuentosAplicados = v.DescuentosAplicados.Select(d => new DescuentoAplicadoDto
        {
            DescuentoId = d.DescuentoId,
            Nombre = d.DescuentoNombreSnapshot,
            Codigo = d.DescuentoCodigoSnapshot,
            Tipo = d.TipoSnapshot.ToString(),
            Valor = d.ValorSnapshot,
            Monto = d.MontoAplicado
        }).ToList(),
        ImpuestosAplicados = v.ImpuestosAplicados.Select(i => new ImpuestoAplicadoDto
        {
            ImpuestoId = i.ImpuestoId,
            Nombre = i.ImpuestoNombreSnapshot,
            Codigo = i.ImpuestoCodigoSnapshot,
            Tasa = i.TasaSnapshot,
            BaseImponible = i.BaseImponible,
            Monto = i.MontoAplicado,
            IncluidoEnPrecio = i.IncluidoEnPrecioSnapshot
        }).ToList(),
        FacturaId = v.Factura?.Id,
        NumeroFactura = v.Factura?.NumeroFactura,
        CreadoPorNombreUsuario = v.CreadoPorNombreUsuario,
        FechaCreacion = v.FechaCreacion,
        ConfirmadoPorNombreUsuario = v.ConfirmadoPorNombreUsuario,
        FechaConfirmacion = v.FechaConfirmacion,
        AnuladoPorNombreUsuario = v.AnuladoPorNombreUsuario,
        FechaAnulacion = v.FechaAnulacion,
        MotivoAnulacion = v.MotivoAnulacion
    };
}
