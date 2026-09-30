using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Services;

public class CompraService : ICompraService
{
    private readonly ICompraRepository _compraRepository;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IInventarioConcurrencyService _inventarioConcurrency;
    private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
    private readonly IKardexMovimientoWriter _kardexMovimientoWriter;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly ICalculoService _calculoService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public CompraService(
        ICompraRepository compraRepository,
        IProveedorRepository proveedorRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IInventarioConcurrencyService inventarioConcurrency,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        ICalculoService calculoService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria,
        IKardexMovimientoWriter? kardexMovimientoWriter = null)
    {
        _compraRepository = compraRepository;
        _proveedorRepository = proveedorRepository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _inventarioConcurrency = inventarioConcurrency;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _kardexMovimientoWriter = kardexMovimientoWriter ?? new KardexMovimientoWriter(movimientoInventarioRepository);
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _calculoService = calculoService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
    }

    public async Task<CompraDto?> GetByIdAsync(int id)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        return compra is null ? null : ToDto(compra);
    }

    public async Task<PagedResult<CompraDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, total) = await _compraRepository.GetPagedAsync(request);
        return new PagedResult<CompraDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<CompraDto> CreateAsync(CreateCompraDto dto)
    {
        var metodoPago = await ResolverMetodoPagoAsync(dto.MetodoPago);
        var compra = new Compra
        {
            NumeroCompra = await GenerarNumeroAsync(),
            ProveedorNombre = dto.ProveedorNombre,
            ProveedorTelefono = dto.ProveedorTelefono,
            ProveedorDocumento = dto.ProveedorDocumento,
            DocumentoReferencia = dto.DocumentoReferencia,
            MetodoPagoId = metodoPago.Id,
            MetodoPagoCatalogo = metodoPago,
            MetodoPago = DerivarMetodoPagoLegacy(metodoPago),
            EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente),
            Estado = EstadoDocumento.Borrador,
            Notas = dto.Notas,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await VincularProveedorAsync(compra, dto);
        await ArmarDetallesAsync(compra, dto.Detalles);
        await CalcularTotalesAsync(compra);

        await _compraRepository.AddAsync(compra);
        await _compraRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Crear,
            $"Compra creada: {compra.NumeroCompra}.",
            compra.Id,
            entidad: "Compra",
            valoresNuevos: new
            {
                compra.NumeroCompra,
                compra.ProveedorNombre,
                compra.MetodoPagoId,
                MetodoPagoCodigo = compra.MetodoPagoCatalogo?.Codigo,
                compra.Subtotal,
                compra.Impuesto,
                compra.Total,
                Detalles = compra.Detalles.Count
            });

        return ToDto(compra);
    }

    public async Task<CompraDto?> UpdateAsync(int id, UpdateCompraDto dto)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return null;
        if (compra.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException("Solo se pueden editar compras en estado Borrador.");

        var metodoPago = await ResolverMetodoPagoAsync(dto.MetodoPago);
        var valoresAnteriores = new
        {
            compra.ProveedorNombre,
            compra.DocumentoReferencia,
            compra.MetodoPagoId,
            MetodoPagoCodigo = compra.MetodoPagoCatalogo?.Codigo,
            compra.MetodoPago,
            compra.EstadoPago,
            compra.Subtotal,
            compra.Impuesto,
            compra.Total,
            Detalles = compra.Detalles.Count
        };

        compra.ProveedorNombre = dto.ProveedorNombre;
        compra.ProveedorTelefono = dto.ProveedorTelefono;
        compra.ProveedorDocumento = dto.ProveedorDocumento;
        compra.DocumentoReferencia = dto.DocumentoReferencia;
        compra.MetodoPagoId = metodoPago.Id;
        compra.MetodoPagoCatalogo = metodoPago;
        compra.MetodoPago = DerivarMetodoPagoLegacy(metodoPago);
        compra.EstadoPago = ParseEnum(dto.EstadoPago, EstadoPago.Pendiente);
        compra.Notas = dto.Notas;
        compra.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        compra.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        compra.FechaActualizacion = DateTime.UtcNow;

        await VincularProveedorAsync(compra, dto);
        compra.Detalles.Clear();
        compra.ImpuestosAplicados.Clear();
        await ArmarDetallesAsync(compra, dto.Detalles);
        await CalcularTotalesAsync(compra);

        _compraRepository.Update(compra);
        await _compraRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Editar,
            $"Compra actualizada: {compra.NumeroCompra}.",
            compra.Id,
            entidad: "Compra",
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: new
            {
                compra.ProveedorNombre,
                compra.DocumentoReferencia,
                compra.MetodoPagoId,
                MetodoPagoCodigo = compra.MetodoPagoCatalogo?.Codigo,
                compra.MetodoPago,
                compra.EstadoPago,
                compra.Subtotal,
                compra.Impuesto,
                compra.Total,
                Detalles = compra.Detalles.Count
            });

        return ToDto(compra);
    }

    public async Task<CompraDto?> ConfirmarAsync(int id)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var compra = await _compraRepository.GetByIdForUpdateAsync(id);
            if (compra is null)
                throw new KeyNotFoundException($"Compra ID '{id}' no encontrada.");

            if (compra.Estado != EstadoDocumento.Borrador)
                throw new BusinessRuleException("Solo se pueden confirmar compras en estado Borrador.");
            if (compra.Detalles.Count == 0)
                throw new BusinessRuleException("La compra debe tener al menos un producto para confirmarse.");
            if (!compra.MetodoPagoId.HasValue || compra.MetodoPagoCatalogo is null)
                throw new BusinessRuleException("La compra no tiene un método de pago relacional válido.");
            if (!compra.MetodoPagoCatalogo.Activo || compra.MetodoPagoCatalogo.Eliminado)
                throw new BusinessRuleException("El método de pago de la compra está inactivo y no puede utilizarse para confirmar una operación nueva.");

            var demanda = compra.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: false);

            var stocksProductoAnteriores = inventario.Productos.ToDictionary(x => x.Key, x => x.Value.Cantidad);
            var costosProductoAnteriores = inventario.Productos.ToDictionary(x => x.Key, x => x.Value.Costo);
            var correlationId = KardexCorrelationId.CompraConfirmar(compra.Id);

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = compra.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var valorEntrada = detallesClave.Sum(d => d.CostoUnitario * d.Cantidad);
                var costoEntradaPonderado = valorEntrada / item.Cantidad;

                var stockAnteriorMovimiento = stocksProductoAnteriores[item.ProductoId];
                var stockNuevoMovimiento = stockAnteriorMovimiento + item.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    var valorAnteriorVariante = (variante.Costo ?? 0m) * variante.Cantidad;
                    variante.Cantidad += item.Cantidad;
                    variante.Costo = Math.Round(
                        (valorAnteriorVariante + valorEntrada) / variante.Cantidad,
                        2,
                        MidpointRounding.AwayFromZero);
                    variante.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                    variante.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                    variante.FechaActualizacion = DateTime.UtcNow;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _kardexMovimientoWriter.RegistrarCorrelacionadoAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
                    ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    CostoUnitario = costoEntradaPonderado,
                    Descripcion = $"Entrada por compra {compra.NumeroCompra}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                }, OrigenMovimientoInventario.DesdeCompra(compra.Id), correlationId);
            }

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                var cantidadEntrada = productoGrupo.Sum(x => x.Cantidad);
                var valorEntrada = compra.Detalles
                    .Where(d => d.ProductoId == producto.Id)
                    .Sum(d => d.CostoUnitario * d.Cantidad);
                var stockAnterior = stocksProductoAnteriores[producto.Id];
                var costoAnterior = costosProductoAnteriores[producto.Id];
                var stockNuevo = stockAnterior + cantidadEntrada;

                producto.Cantidad = stockNuevo;
                producto.Costo = stockNuevo > 0
                    ? Math.Round(
                        ((costoAnterior * stockAnterior) + valorEntrada) / stockNuevo,
                        2,
                        MidpointRounding.AwayFromZero)
                    : 0m;
                _productoRepository.Update(producto);
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Egreso,
                Categoria = CategoriaMovimientoFinanciero.Compra,
                Concepto = $"Compra {compra.NumeroCompra} - {compra.ProveedorNombre}",
                Monto = compra.Total,
                Estado = compra.EstadoPago == EstadoPago.Pagado
                    ? EstadoMovimientoFinanciero.Pagado
                    : EstadoMovimientoFinanciero.Pendiente,
                MetodoPagoId = compra.MetodoPagoId,
                MetodoPagoCatalogo = compra.MetodoPagoCatalogo,
                MetodoPago = DerivarMetodoPagoLegacy(compra.MetodoPagoCatalogo),
                EsAutomatico = true,
                ModuloOrigen = "Compra",
                ReferenciaId = compra.Id,
                CompraId = compra.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            compra.Estado = EstadoDocumento.Confirmada;
            compra.ConfirmadoPorUsuarioId = _currentUser.UsuarioId;
            compra.ConfirmadoPorNombreUsuario = _currentUser.NombreUsuario;
            compra.FechaConfirmacion = DateTime.UtcNow;
            _compraRepository.Update(compra);

            await _calculoService.RegistrarUsoCompraAsync(compra.Id, compra.ImpuestosAplicados.ToList());
            await _compraRepository.SaveChangesAsync();
        });

        var actualizada = await _compraRepository.GetByIdAsync(id);
        await _auditoria.RegistrarAsync(ModuloSistema.Compras, AccionPermiso.Confirmar, $"Compra confirmada: {actualizada!.NumeroCompra}.", id);
        return ToDto(actualizada);
    }

    public async Task<CompraDto?> AnularAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var compra = await _compraRepository.GetByIdForUpdateAsync(id);
            if (compra is null)
                throw new KeyNotFoundException($"Compra ID '{id}' no encontrada.");

            if (compra.Estado != EstadoDocumento.Confirmada)
                throw new BusinessRuleException("Solo se pueden anular compras confirmadas.");

            var demanda = compra.Detalles
                .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                .ToList();
            var inventario = await _inventarioConcurrency.BloquearYValidarInventarioAsync(demanda, esDeduccion: true);

            var productoIds = inventario.Productos.Keys.OrderBy(x => x).ToArray();
            var ultimoMovimientoOriginalId = await _movimientoInventarioRepository
                .GetUltimoMovimientoOriginalCompraIdAsync(compra.Id)
                ?? throw new BusinessRuleException(
                    "No se encontraron los movimientos originales de la compra; la anulación no puede ejecutarse de forma segura.");

            if (await _movimientoInventarioRepository.ExisteMovimientoPosteriorAsync(
                    ultimoMovimientoOriginalId,
                    productoIds))
            {
                throw new BusinessRuleException(
                    "No se puede anular la compra porque existen movimientos posteriores de inventario sobre sus productos o variantes.");
            }

            var stocksProductoAnteriores = inventario.Productos.ToDictionary(x => x.Key, x => x.Value.Cantidad);
            var correlationId = KardexCorrelationId.CompraAnular(compra.Id);

            foreach (var item in inventario.Demandas)
            {
                var producto = inventario.Productos[item.ProductoId];
                var detallesClave = compra.Detalles
                    .Where(d => d.ProductoId == item.ProductoId && d.ProductoVarianteId == item.ProductoVarianteId)
                    .ToList();
                var detalle = detallesClave[0];
                var costoUnitarioMovimiento = detallesClave.Sum(d => d.CostoUnitario * d.Cantidad) / item.Cantidad;

                var stockAnteriorMovimiento = stocksProductoAnteriores[item.ProductoId];
                var stockNuevoMovimiento = stockAnteriorMovimiento - item.Cantidad;

                if (item.ProductoVarianteId.HasValue)
                {
                    var variante = inventario.Variantes[item.ProductoVarianteId.Value];
                    stockAnteriorMovimiento = variante.Cantidad;
                    variante.Cantidad -= item.Cantidad;
                    stockNuevoMovimiento = variante.Cantidad;
                    _productoVarianteRepository.Update(variante);
                }

                await _kardexMovimientoWriter.RegistrarCorrelacionadoAsync(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = item.ProductoVarianteId,
                    ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
                    ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
                    ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                    ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
                    ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                    Tipo = TipoMovimientoInventario.Salida,
                    Causa = CausaMovimientoInventario.AnulacionCompra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnteriorMovimiento,
                    StockNuevo = stockNuevoMovimiento,
                    CostoUnitario = costoUnitarioMovimiento,
                    Descripcion = $"Salida por anulación de compra {compra.NumeroCompra}. Motivo: {motivo}",
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                }, OrigenMovimientoInventario.DesdeCompra(compra.Id), correlationId);
            }

            foreach (var productoGrupo in inventario.Demandas.GroupBy(x => x.ProductoId))
            {
                var producto = inventario.Productos[productoGrupo.Key];
                producto.Cantidad = stocksProductoAnteriores[producto.Id] - productoGrupo.Sum(x => x.Cantidad);
                _productoRepository.Update(producto);
            }

            await _movimientoFinancieroRepository.AddAsync(new MovimientoFinanciero
            {
                Tipo = TipoMovimientoFinanciero.Ingreso,
                Categoria = CategoriaMovimientoFinanciero.Reversion,
                Concepto = $"Reversión de compra anulada {compra.NumeroCompra}",
                Monto = compra.Total,
                Estado = EstadoMovimientoFinanciero.Pagado,
                EsAutomatico = true,
                ModuloOrigen = "Reversion",
                ReferenciaId = compra.Id,
                CompraId = compra.Id,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });

            compra.Estado = EstadoDocumento.Anulada;
            compra.AnuladoPorUsuarioId = _currentUser.UsuarioId;
            compra.AnuladoPorNombreUsuario = _currentUser.NombreUsuario;
            compra.FechaAnulacion = DateTime.UtcNow;
            compra.MotivoAnulacion = motivo.Trim();
            _compraRepository.Update(compra);
            await _compraRepository.SaveChangesAsync();
        });

        var actualizada = await _compraRepository.GetByIdAsync(id);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Anular,
            $"Compra anulada: {actualizada!.NumeroCompra}.",
            id,
            entidad: "Compra",
            motivo: motivo);

        return ToDto(actualizada);
    }

    public async Task<bool> DeleteBorradorAsync(int id)
    {
        var compra = await _compraRepository.GetByIdAsync(id);
        if (compra is null) return false;
        if (compra.Estado != EstadoDocumento.Borrador)
            throw new BusinessRuleException(
                "Solo se pueden eliminar lógicamente compras en estado Borrador.");

        compra.Eliminado = true;
        compra.FechaEliminacion = DateTime.UtcNow;
        compra.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        compra.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        compra.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        compra.FechaActualizacion = DateTime.UtcNow;

        _compraRepository.Update(compra);
        var eliminado = await _compraRepository.SaveChangesAsync();
        if (eliminado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Compras,
                AccionPermiso.EliminarLogico,
                $"Borrador de compra eliminado lógicamente: {compra.NumeroCompra}.",
                compra.Id,
                entidad: "Compra",
                valoresAnteriores: new
                {
                    compra.NumeroCompra,
                    compra.ProveedorNombre,
                    compra.Total,
                    Detalles = compra.Detalles.Count
                },
                valoresNuevos: new
                {
                    compra.Eliminado,
                    compra.FechaEliminacion
                });
        }

        return eliminado;
    }

    public async Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularCompraRequest request)
    {
        if (request.Detalles.Count == 0)
            throw new BusinessRuleException("La compra debe tener al menos un producto.");

        var entradas = new List<DetalleCalculoInput>();
        foreach (var detalle in request.Detalles)
        {
            if (detalle.Cantidad <= 0 || detalle.PrecioUnitario <= 0)
                throw new BusinessRuleException("La cantidad y el costo unitario deben ser mayores a cero.");

            var producto = await ObtenerProductoActivoAsync(detalle.ProductoId);
            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            });
        }

        return await _calculoService.CalcularCompraAsync(entradas, request.ProveedorId);
    }

    private async Task VincularProveedorAsync(Compra compra, CreateCompraDto dto)
    {
        Proveedor? proveedor = null;

        if (dto.ProveedorId.HasValue)
        {
            proveedor = await _proveedorRepository.GetByIdAsync(dto.ProveedorId.Value)
                ?? throw new BusinessRuleException("El proveedor seleccionado no existe.");
            if (!proveedor.Activo)
                throw new BusinessRuleException("El proveedor seleccionado está inactivo.");
        }
        else if (DebeGestionarProveedor(dto.ProveedorNombre, dto.ProveedorDocumento, dto.ProveedorTelefono))
        {
            proveedor = await _proveedorRepository.BuscarCoincidenciaActivaAsync(
                dto.ProveedorDocumento,
                null,
                dto.ProveedorTelefono,
                dto.ProveedorNombre);

            if (proveedor is null)
            {
                proveedor = new Proveedor
                {
                    Nombre = dto.ProveedorNombre.Trim(),
                    Telefono = dto.ProveedorTelefono,
                    Documento = dto.ProveedorDocumento,
                    Activo = true,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                await _proveedorRepository.AddAsync(proveedor);
            }
        }

        if (proveedor is null)
        {
            compra.ProveedorId = null;
            compra.Proveedor = null;
            return;
        }

        compra.Proveedor = proveedor;
        compra.ProveedorId = proveedor.Id == 0 ? null : proveedor.Id;
        compra.ProveedorNombre = proveedor.Nombre;
        compra.ProveedorTelefono = proveedor.Telefono;
        compra.ProveedorDocumento = proveedor.Documento;
    }

    private static bool DebeGestionarProveedor(string? nombre, string? documento, string? telefono) =>
        !string.IsNullOrWhiteSpace(documento)
        || !string.IsNullOrWhiteSpace(telefono)
        || !string.IsNullOrWhiteSpace(nombre);

    private async Task ArmarDetallesAsync(Compra compra, List<CompraDetalleInputDto> detallesInput)
    {
        if (detallesInput.Count == 0)
            throw new BusinessRuleException("La compra debe tener al menos un producto.");

        foreach (var input in detallesInput)
        {
            if (input.Cantidad <= 0)
                throw new BusinessRuleException("La cantidad de cada producto debe ser mayor a 0.");
            if (input.CostoUnitario <= 0)
                throw new BusinessRuleException("El costo unitario de cada producto debe ser mayor a 0.");

            var producto = await ObtenerProductoActivoAsync(input.ProductoId);
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
                    ?? throw new BusinessRuleException($"El producto '{producto.Nombre}' no tiene una variante operativa activa. Corrige el inventario antes de comprarlo.");
            }

            compra.Detalles.Add(new CompraDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante.Id,
                Cantidad = input.Cantidad,
                CostoUnitario = input.CostoUnitario,
                Subtotal = input.Cantidad * input.CostoUnitario,
                ProductoNombreSnapshot = producto.Nombre,
                ProductoMarcaSnapshot = variante.Marca?.Nombre ?? string.Empty,
                ProductoModeloSnapshot = variante.Modelo?.Nombre ?? string.Empty,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoTallaSnapshot = variante.Talla?.Nombre,
                ProductoSkuSnapshot = variante.Sku
            });
        }
    }

    private async Task CalcularTotalesAsync(Compra compra)
    {
        var entradas = new List<DetalleCalculoInput>();
        foreach (var detalle in compra.Detalles)
        {
            var producto = await ObtenerProductoActivoAsync(detalle.ProductoId);
            entradas.Add(new DetalleCalculoInput
            {
                ProductoId = producto.Id,
                CategoriaId = producto.CategoriaId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.CostoUnitario
            });
        }

        var resultado = await _calculoService.CalcularCompraAsync(entradas, compra.ProveedorId);
        compra.Subtotal = resultado.Subtotal;
        compra.Descuento = resultado.TotalDescuento;
        compra.Impuesto = resultado.TotalImpuesto;
        compra.Total = resultado.Total;
        compra.ImpuestosAplicados = resultado.ImpuestosAplicados.Select(impuesto => new CompraImpuesto
        {
            ImpuestoId = impuesto.ImpuestoId,
            ImpuestoNombreSnapshot = impuesto.Nombre,
            ImpuestoCodigoSnapshot = impuesto.Codigo,
            TasaSnapshot = impuesto.Tasa,
            BaseImponible = impuesto.BaseImponible,
            MontoAplicado = impuesto.Monto,
            IncluidoEnPrecioSnapshot = impuesto.IncluidoEnPrecio
        }).ToList();

        if (compra.Total < 0)
            throw new BusinessRuleException("El total de la compra no puede ser negativo.");
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

    private async Task<Producto> ObtenerProductoActivoAsync(int productoId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId)
            ?? throw new BusinessRuleException($"El producto con id {productoId} no existe.");
        if (!producto.Activo)
            throw new BusinessRuleException(
                $"El producto '{producto.Nombre}' está inactivo. Actívalo antes de utilizarlo en una compra.");
        return producto;
    }

    private async Task<CatalogoMetodoPago> ResolverMetodoPagoAsync(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException("El método de pago es obligatorio.");

        var metodoPago = await _compraRepository.GetMetodoPagoPorCodigoONombreAsync(valor.Trim());
        return metodoPago
            ?? throw new BusinessRuleException($"El método de pago '{valor.Trim()}' no existe en el catálogo.");
    }

    private static MetodoPago DerivarMetodoPagoLegacy(CatalogoMetodoPago metodoPago)
    {
        if (Enum.TryParse<MetodoPago>(metodoPago.Codigo, true, out var porCodigo))
            return porCodigo;
        if (Enum.TryParse<MetodoPago>(metodoPago.Nombre, true, out var porNombre))
            return porNombre;

        return MetodoPago.Otro;
    }

    private async Task<string> GenerarNumeroAsync()
    {
        var total = await _compraRepository.ContarTodasAsync();
        return $"COM-{(total + 1):D6}";
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum valorPorDefecto) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var resultado) ? resultado : valorPorDefecto;

    private static CompraDto ToDto(Compra compra) => new()
    {
        Id = compra.Id,
        NumeroCompra = compra.NumeroCompra,
        Fecha = compra.Fecha,
        ProveedorId = compra.ProveedorId,
        ProveedorNombre = compra.ProveedorNombre,
        ProveedorTelefono = compra.ProveedorTelefono,
        ProveedorDocumento = compra.ProveedorDocumento,
        DocumentoReferencia = compra.DocumentoReferencia,
        Estado = compra.Estado.ToString(),
        EstadoPago = compra.EstadoPago.ToString(),
        MetodoPago = compra.MetodoPagoCatalogo?.Nombre ?? compra.MetodoPago.ToString(),
        Subtotal = compra.Subtotal,
        Descuento = compra.Descuento,
        Impuesto = compra.Impuesto,
        Total = compra.Total,
        Notas = compra.Notas,
        Detalles = compra.Detalles.Select(detalle => new CompraDetalleDto
        {
            Id = detalle.Id,
            ProductoId = detalle.ProductoId,
            ProductoVarianteId = detalle.ProductoVarianteId,
            ProductoNombre = detalle.ProductoNombreSnapshot,
            ProductoMarca = detalle.ProductoMarcaSnapshot,
            ProductoModelo = detalle.ProductoModeloSnapshot,
            ProductoColor = detalle.ProductoColorSnapshot,
            ProductoTalla = detalle.ProductoTallaSnapshot,
            ProductoSku = detalle.ProductoSkuSnapshot,
            ProductoImagenPrincipalUrl = detalle.Producto?.ImagenPrincipal?.Url,
            Cantidad = detalle.Cantidad,
            CostoUnitario = detalle.CostoUnitario,
            Subtotal = detalle.Subtotal
        }).ToList(),
        ImpuestosAplicados = compra.ImpuestosAplicados.Select(impuesto => new ImpuestoAplicadoDto
        {
            ImpuestoId = impuesto.ImpuestoId,
            Nombre = impuesto.ImpuestoNombreSnapshot,
            Codigo = impuesto.ImpuestoCodigoSnapshot,
            Tasa = impuesto.TasaSnapshot,
            BaseImponible = impuesto.BaseImponible,
            Monto = impuesto.MontoAplicado,
            IncluidoEnPrecio = impuesto.IncluidoEnPrecioSnapshot
        }).ToList(),
        CreadoPorNombreUsuario = compra.CreadoPorNombreUsuario,
        FechaCreacion = compra.FechaCreacion,
        ConfirmadoPorNombreUsuario = compra.ConfirmadoPorNombreUsuario,
        FechaConfirmacion = compra.FechaConfirmacion,
        AnuladoPorNombreUsuario = compra.AnuladoPorNombreUsuario,
        FechaAnulacion = compra.FechaAnulacion,
        MotivoAnulacion = compra.MotivoAnulacion
    };
}
