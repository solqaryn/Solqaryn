using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Application.Services;

public sealed class FacturaProveedorService : IFacturaProveedorService
{
    private const string NumeroConstraint = "UX_FacturasProveedor_Proveedor_NumeroFactura";
    private const string EntidadAuditoria = "FacturaProveedor";
    private readonly IFacturaProveedorRepository _repository;
    private readonly IOrdenCompraRepository _ordenesCompra;
    private readonly IRecepcionCompraRepository _recepcionesCompra;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<FacturaProveedorService> _logger;

    public FacturaProveedorService(
        IFacturaProveedorRepository repository,
        IOrdenCompraRepository ordenesCompra,
        IRecepcionCompraRepository recepcionesCompra,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria,
        ILogger<FacturaProveedorService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _ordenesCompra = ordenesCompra ?? throw new ArgumentNullException(nameof(ordenesCompra));
        _recepcionesCompra = recepcionesCompra ?? throw new ArgumentNullException(nameof(recepcionesCompra));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<FacturaProveedorDto>> GetPagedAsync(FacturaProveedorFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
            throw new BusinessRuleException("El rango de fechas es inválido.");
        if (filtro.ProveedorId is <= 0)
            throw new BusinessRuleException("El proveedor del filtro no es válido.");
        if (filtro.OrdenCompraId is <= 0)
            throw new BusinessRuleException("La orden de compra del filtro no es válida.");

        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<FacturaProveedorDto>
        {
            Items = items.Select(Map).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    public async Task<FacturaProveedorDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var factura = await _repository.GetByIdAsync(id);
        return factura is null ? null : Map(factura);
    }

    public async Task<FacturaProveedorDto> CreateAsync(CreateFacturaProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        NormalizarYValidarCabecera(dto);

        var existente = await _repository.GetByProveedorNumeroAsync(dto.ProveedorId, dto.NumeroFactura);
        if (existente is not null)
        {
            _logger.LogInformation("Factura de proveedor ya existente. Resolviendo reintento idempotente.");
            return Map(ResolverReintento(existente, dto));
        }

        FacturaProveedor? creada = null;
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var concurrente = await _repository.GetByProveedorNumeroAsync(dto.ProveedorId, dto.NumeroFactura, tracking: true);
                if (concurrente is not null)
                {
                    _logger.LogInformation("Factura concurrente detectada dentro de la transacción.");
                    creada = ResolverReintento(concurrente, dto);
                    return;
                }

                var orden = await ObtenerOrdenAprobadaAsync(dto.OrdenCompraId, dto.ProveedorId);
                var ahora = DateTime.UtcNow;
                var factura = ConstruirFactura(dto, orden);
                factura.FechaCreacion = ahora;
                factura.FechaActualizacion = ahora;
                factura.CreadoPorUsuarioId = ObtenerUsuarioId();
                factura.CreadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
                ValidarDominio(factura.ValidarDocumento);

                await _repository.AddAsync(factura);
                await _repository.SaveChangesAsync();
                _logger.LogInformation("Factura de proveedor {Id} creada en base de datos.", factura.Id);
                await RegistrarAuditoriaAsync(AccionPermiso.Crear, "Factura de proveedor creada", factura, valoresNuevos: Snapshot(factura));
                creada = factura;
            });
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == NumeroConstraint)
        {
            _logger.LogWarning(ex, "Violación de constraint única detectada para proveedor {ProveedorId} y número de factura.", dto.ProveedorId);
            var concurrente = await _repository.GetByProveedorNumeroAsync(dto.ProveedorId, dto.NumeroFactura)
                ?? throw new ConflictException("La factura fue creada concurrentemente y no pudo recuperarse de forma segura.");
            creada = ResolverReintento(concurrente, dto);
        }

        return Map(creada ?? throw new InvalidOperationException("La creación de la factura de proveedor no produjo un resultado."));
    }

    public async Task<FacturaProveedorDto> UpdateAsync(int id, UpdateFacturaProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        NormalizarYValidarCabecera(dto);
        FacturaProveedor? actualizada = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var factura = await ObtenerBloqueadaAsync(id);
            ValidarDominio(factura.AsegurarEditable);
            var anterior = Snapshot(factura);

            var duplicada = await _repository.GetByProveedorNumeroAsync(dto.ProveedorId, dto.NumeroFactura);
            if (duplicada is not null && duplicada.Id != factura.Id)
                throw new ConflictException("El número de factura ya existe para el proveedor indicado.");

            var orden = await ObtenerOrdenAprobadaAsync(dto.OrdenCompraId, dto.ProveedorId);
            AplicarDocumento(factura, dto, orden);
            factura.FechaActualizacion = DateTime.UtcNow;
            factura.ActualizadoPorUsuarioId = ObtenerUsuarioId();
            factura.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
            ValidarDominio(factura.ValidarDocumento);

            await _repository.SaveChangesAsync();
            _logger.LogInformation("Factura de proveedor {Id} editada exitosamente.", factura.Id);
            await RegistrarAuditoriaAsync(AccionPermiso.Editar, "Factura de proveedor editada", factura, anterior, Snapshot(factura));
            actualizada = factura;
        });

        return Map(actualizada!);
    }

    public async Task<FacturaProveedorDto> RegistrarAsync(int id)
    {
        FacturaProveedor? registrada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var factura = await ObtenerBloqueadaAsync(id);
            if (factura.Estado == EstadoFacturaProveedor.Registrada)
            {
                _logger.LogInformation("La factura de proveedor {Id} ya se encontraba registrada.", factura.Id);
                registrada = factura;
                return;
            }
            if (factura.Estado != EstadoFacturaProveedor.Borrador)
                throw new BusinessRuleException("Solo una factura de proveedor en borrador puede registrarse.");

            var usuarioId = ObtenerUsuarioId();
            var orden = await _ordenesCompra.GetByIdForUpdateAsync(factura.OrdenCompraId)
                ?? throw new BusinessRuleException("La orden de compra asociada a la factura ya no existe.");
            if (orden.Estado != EstadoOrdenCompra.Aprobada)
                throw new BusinessRuleException("Solo puede registrarse una factura respaldada por una orden de compra aprobada.");
            if (orden.ProveedorId != factura.ProveedorId)
                throw new BusinessRuleException("El proveedor de la factura ya no coincide con el proveedor de la orden de compra.");

            await ValidarLimitesRegistroAsync(factura, orden);

            var anterior = Snapshot(factura);
            ValidarDominio(() => factura.Registrar(usuarioId, _currentUser.NombreCompleto ?? _currentUser.NombreUsuario, DateTime.UtcNow));
            factura.FechaActualizacion = DateTime.UtcNow;
            factura.ActualizadoPorUsuarioId = usuarioId;
            factura.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Factura de proveedor {Id} confirmada/registrada exitosamente.", factura.Id);
            await RegistrarAuditoriaAsync(AccionPermiso.Confirmar, "Factura de proveedor registrada", factura, anterior, Snapshot(factura));
            registrada = factura;
        });
        return Map(registrada!);
    }

    public async Task<FacturaProveedorDto> AnularAsync(int id, AnularFacturaProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var motivo = Normalizar(dto.Motivo) ?? throw new BusinessRuleException("El motivo de anulación es obligatorio.");
        if (motivo.Length > 500)
            throw new BusinessRuleException("El motivo de anulación no puede superar 500 caracteres.");

        FacturaProveedor? anulada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var factura = await ObtenerBloqueadaAsync(id);
            if (factura.Estado == EstadoFacturaProveedor.Anulada)
            {
                _logger.LogInformation("La factura de proveedor {Id} ya se encontraba anulada.", factura.Id);
                anulada = factura;
                return;
            }
            if (factura.Estado != EstadoFacturaProveedor.Registrada)
                throw new BusinessRuleException("Solo una factura de proveedor registrada puede anularse.");

            var anterior = Snapshot(factura);
            var usuarioId = ObtenerUsuarioId();
            ValidarDominio(() => factura.Anular(usuarioId, motivo, DateTime.UtcNow));
            factura.FechaActualizacion = DateTime.UtcNow;
            factura.ActualizadoPorUsuarioId = usuarioId;
            factura.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Factura de proveedor {Id} anulada exitosamente.", factura.Id);
            await RegistrarAuditoriaAsync(AccionPermiso.Anular, "Factura de proveedor anulada", factura, anterior, Snapshot(factura), motivo);
            anulada = factura;
        });
        return Map(anulada!);
    }

    private async Task ValidarLimitesRegistroAsync(FacturaProveedor factura, OrdenCompra orden)
    {
        var detallesOrden = orden.Detalles.ToDictionary(x => x.Id);
        foreach (var detalle in factura.Detalles)
        {
            if (!detallesOrden.TryGetValue(detalle.OrdenCompraDetalleId, out var detalleOrden))
                throw new BusinessRuleException($"La línea {detalle.OrdenCompraDetalleId} ya no pertenece a la orden de compra asociada.");

            var facturadoPrevio = await _repository.GetCantidadRegistradaAcumuladaPorDetalleAsync(
                detalle.OrdenCompraDetalleId,
                factura.Id);
            var recibidoAceptado = await _recepcionesCompra.GetCantidadAceptadaAcumuladaPorDetalleAsync(
                detalle.OrdenCompraDetalleId);
            var facturadoProyectado = facturadoPrevio + detalle.CantidadFacturada;

            if (facturadoProyectado > detalleOrden.CantidadOrdenada)
            {
                throw new BusinessRuleException(
                    $"La facturación acumulada de la línea {detalle.OrdenCompraDetalleId} supera la cantidad comprada. " +
                    $"Comprada={detalleOrden.CantidadOrdenada}; facturada previamente={facturadoPrevio}; actual={detalle.CantidadFacturada}.");
            }

            if (facturadoProyectado > recibidoAceptado)
            {
                throw new BusinessRuleException(
                    $"La facturación acumulada de la línea {detalle.OrdenCompraDetalleId} supera la cantidad recibida y aceptada. " +
                    $"Recibida={recibidoAceptado}; facturada previamente={facturadoPrevio}; actual={detalle.CantidadFacturada}.");
            }
        }
    }

    private async Task<OrdenCompra> ObtenerOrdenAprobadaAsync(int ordenCompraId, int proveedorId)
    {
        if (ordenCompraId <= 0)
            throw new BusinessRuleException("La orden de compra es obligatoria.");
        var orden = await _ordenesCompra.GetByIdAsync(ordenCompraId)
            ?? throw new BusinessRuleException("La orden de compra indicada no existe.");
        if (orden.Estado != EstadoOrdenCompra.Aprobada)
            throw new BusinessRuleException("Solo una orden de compra aprobada puede respaldar una factura de proveedor.");
        if (orden.ProveedorId != proveedorId)
            throw new BusinessRuleException("El proveedor de la factura debe coincidir con el proveedor de la orden de compra.");
        return orden;
    }

    private static FacturaProveedor ConstruirFactura(CreateFacturaProveedorDto dto, OrdenCompra orden)
    {
        var factura = new FacturaProveedor();
        AplicarDocumento(factura, dto, orden);
        return factura;
    }

    private static void AplicarDocumento(FacturaProveedor factura, CreateFacturaProveedorDto dto, OrdenCompra orden)
    {
        var lineasOrden = orden.Detalles.ToDictionary(x => x.Id);
        var repetidas = dto.Detalles.GroupBy(x => x.OrdenCompraDetalleId).Any(x => x.Count() > 1);
        if (repetidas)
            throw new BusinessRuleException("Una línea de orden de compra no puede repetirse dentro de la factura.");

        var detalles = new List<FacturaProveedorDetalle>(dto.Detalles.Count);
        foreach (var input in dto.Detalles)
        {
            if (!lineasOrden.TryGetValue(input.OrdenCompraDetalleId, out var linea))
                throw new BusinessRuleException($"La línea de orden {input.OrdenCompraDetalleId} no pertenece a la orden indicada.");

            var detalle = new FacturaProveedorDetalle
            {
                OrdenCompraDetalleId = linea.Id,
                ProductoId = linea.ProductoId,
                ProductoVarianteId = linea.ProductoVarianteId,
                ProductoSkuSnapshot = Normalizar(linea.ProductoSkuSnapshot),
                ProductoNombreSnapshot = Normalizar(linea.ProductoNombreSnapshot) ?? $"Producto {linea.ProductoId}",
                ProductoMarcaSnapshot = Normalizar(linea.ProductoMarcaSnapshot),
                ProductoModeloSnapshot = Normalizar(linea.ProductoModeloSnapshot),
                ProductoColorSnapshot = Normalizar(linea.ProductoColorSnapshot),
                ProductoTallaSnapshot = Normalizar(linea.ProductoTallaSnapshot),
                Observacion = Normalizar(input.Observacion)
            };
            ValidarDominio(() => detalle.EstablecerValores(input.CantidadFacturada, input.PrecioUnitario, input.Descuento, input.Impuesto));
            detalles.Add(detalle);
        }

        factura.NumeroFactura = dto.NumeroFactura.Trim().ToUpperInvariant();
        factura.ProveedorId = dto.ProveedorId;
        factura.OrdenCompraId = orden.Id;
        factura.ProveedorNombreSnapshot = orden.ProveedorNombreSnapshot.Trim();
        factura.ProveedorDocumentoSnapshot = Normalizar(orden.ProveedorDocumentoSnapshot);
        factura.Moneda = dto.Moneda.Trim().ToUpperInvariant();
        factura.FechaEmisionUtc = dto.FechaEmisionUtc;
        factura.FechaVencimientoUtc = dto.FechaVencimientoUtc;
        factura.ReferenciaFiscal = Normalizar(dto.ReferenciaFiscal);
        factura.Observaciones = Normalizar(dto.Observaciones);
        factura.Detalles.Clear();
        foreach (var detalle in detalles)
            factura.Detalles.Add(detalle);
    }

    private async Task<FacturaProveedor> ObtenerBloqueadaAsync(int id)
    {
        if (id <= 0)
            throw new ResourceNotFoundException("Factura de proveedor no encontrada.");
        return await _repository.GetByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException("Factura de proveedor no encontrada.");
    }

    private static FacturaProveedor ResolverReintento(FacturaProveedor existente, CreateFacturaProveedorDto dto)
    {
        if (!CoincideDocumento(existente, dto))
            throw new ConflictException("El número de factura ya existe para el proveedor con un payload diferente.");
        return existente;
    }

    private static bool CoincideDocumento(FacturaProveedor factura, CreateFacturaProveedorDto dto)
    {
        if (factura.ProveedorId != dto.ProveedorId
            || factura.OrdenCompraId != dto.OrdenCompraId
            || !string.Equals(factura.NumeroFactura, dto.NumeroFactura.Trim().ToUpperInvariant(), StringComparison.Ordinal)
            || !string.Equals(factura.Moneda, dto.Moneda.Trim().ToUpperInvariant(), StringComparison.Ordinal)
            || factura.FechaEmisionUtc != dto.FechaEmisionUtc
            || factura.FechaVencimientoUtc != dto.FechaVencimientoUtc
            || !string.Equals(Normalizar(factura.ReferenciaFiscal), Normalizar(dto.ReferenciaFiscal), StringComparison.Ordinal)
            || !string.Equals(Normalizar(factura.Observaciones), Normalizar(dto.Observaciones), StringComparison.Ordinal)
            || factura.Detalles.Count != dto.Detalles.Count)
            return false;

        var inputs = dto.Detalles.ToDictionary(x => x.OrdenCompraDetalleId);
        return factura.Detalles.All(detalle =>
            inputs.TryGetValue(detalle.OrdenCompraDetalleId, out var input)
            && detalle.CantidadFacturada == input.CantidadFacturada
            && detalle.PrecioUnitarioSnapshot == input.PrecioUnitario
            && detalle.DescuentoSnapshot == input.Descuento
            && detalle.ImpuestoSnapshot == input.Impuesto
            && string.Equals(Normalizar(detalle.Observacion), Normalizar(input.Observacion), StringComparison.Ordinal));
    }

    private static void NormalizarYValidarCabecera(CreateFacturaProveedorDto dto)
    {
        if (dto.ProveedorId <= 0)
            throw new BusinessRuleException("El proveedor es obligatorio.");
        if (dto.OrdenCompraId <= 0)
            throw new BusinessRuleException("La orden de compra es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.NumeroFactura))
            throw new BusinessRuleException("El número de factura es obligatorio.");
        dto.NumeroFactura = dto.NumeroFactura.Trim().ToUpperInvariant();
        if (dto.NumeroFactura.Length > 80)
            throw new BusinessRuleException("El número de factura no puede superar 80 caracteres.");
        if (string.IsNullOrWhiteSpace(dto.Moneda) || dto.Moneda.Trim().Length != 3)
            throw new BusinessRuleException("La moneda debe usar un código ISO de tres caracteres.");
        dto.Moneda = dto.Moneda.Trim().ToUpperInvariant();
        if (dto.FechaEmisionUtc == default)
            throw new BusinessRuleException("La fecha de emisión es obligatoria.");
        if (dto.FechaEmisionUtc.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("La fecha de emisión debe expresarse en UTC.");
        if (dto.FechaVencimientoUtc.HasValue && dto.FechaVencimientoUtc.Value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("La fecha de vencimiento debe expresarse en UTC.");
        if (dto.FechaVencimientoUtc.HasValue && dto.FechaVencimientoUtc.Value < dto.FechaEmisionUtc)
            throw new BusinessRuleException("La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
        if (dto.Detalles is null || dto.Detalles.Count == 0)
            throw new BusinessRuleException("La factura de proveedor debe contener al menos un detalle.");
        if (Normalizar(dto.ReferenciaFiscal)?.Length > 120)
            throw new BusinessRuleException("La referencia fiscal no puede superar 120 caracteres.");
        if (Normalizar(dto.Observaciones)?.Length > 1000)
            throw new BusinessRuleException("Las observaciones no pueden superar 1000 caracteres.");
    }

    private int ObtenerUsuarioId() => _currentUser.EstaAutenticado && _currentUser.UsuarioId is > 0
        ? _currentUser.UsuarioId.Value
        : throw new ForbiddenAccessException("No existe un usuario autenticado válido para ejecutar la operación.");

    private Task RegistrarAuditoriaAsync(
        AccionPermiso accion,
        string descripcion,
        FacturaProveedor factura,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            accion,
            descripcion,
            referenciaId: factura.Id,
            entidad: EntidadAuditoria,
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: valoresNuevos,
            motivo: motivo);

    private static object Snapshot(FacturaProveedor factura) => new
    {
        factura.NumeroFactura,
        Estado = factura.Estado.ToString(),
        factura.ProveedorId,
        factura.OrdenCompraId,
        factura.Moneda,
        Lineas = factura.Detalles.Count,
        factura.Subtotal,
        factura.Descuento,
        factura.Impuesto,
        factura.Total,
        factura.FechaRegistroUtc,
        factura.FechaAnulacionUtc
    };

    private static FacturaProveedorDto Map(FacturaProveedor factura) => new()
    {
        Id = factura.Id,
        NumeroFactura = factura.NumeroFactura,
        ProveedorId = factura.ProveedorId,
        OrdenCompraId = factura.OrdenCompraId,
        ProveedorNombreSnapshot = factura.ProveedorNombreSnapshot,
        ProveedorDocumentoSnapshot = factura.ProveedorDocumentoSnapshot,
        Moneda = factura.Moneda,
        FechaEmisionUtc = factura.FechaEmisionUtc,
        FechaVencimientoUtc = factura.FechaVencimientoUtc,
        ReferenciaFiscal = factura.ReferenciaFiscal,
        Observaciones = factura.Observaciones,
        Estado = factura.Estado,
        FechaRegistroUtc = factura.FechaRegistroUtc,
        RegistradaPorUsuarioId = factura.RegistradaPorUsuarioId,
        RegistradaPorNombreSnapshot = factura.RegistradaPorNombreSnapshot,
        FechaAnulacionUtc = factura.FechaAnulacionUtc,
        AnuladaPorUsuarioId = factura.AnuladaPorUsuarioId,
        MotivoAnulacion = factura.MotivoAnulacion,
        Subtotal = factura.Subtotal,
        Descuento = factura.Descuento,
        Impuesto = factura.Impuesto,
        Total = factura.Total,
        EsEditable = factura.EsEditable,
        Detalles = factura.Detalles.OrderBy(x => x.Id).Select(x => new FacturaProveedorDetalleDto
        {
            Id = x.Id,
            OrdenCompraDetalleId = x.OrdenCompraDetalleId,
            ProductoId = x.ProductoId,
            ProductoVarianteId = x.ProductoVarianteId,
            CantidadFacturada = x.CantidadFacturada,
            PrecioUnitario = x.PrecioUnitarioSnapshot,
            Descuento = x.DescuentoSnapshot,
            Impuesto = x.ImpuestoSnapshot,
            Subtotal = x.SubtotalSnapshot,
            Total = x.TotalSnapshot,
            ProductoSkuSnapshot = x.ProductoSkuSnapshot,
            ProductoNombreSnapshot = x.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = x.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = x.ProductoModeloSnapshot,
            ProductoColorSnapshot = x.ProductoColorSnapshot,
            ProductoTallaSnapshot = x.ProductoTallaSnapshot,
            Observacion = x.Observacion
        }).ToList()
    };

    private static void ValidarDominio(Action action)
    {
        try
        {
            action();
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
