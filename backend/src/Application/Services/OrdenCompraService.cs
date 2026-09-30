using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class OrdenCompraService : IOrdenCompraService
{
    private const string IdempotencyConstraint = "UX_OrdenesCompra_IdempotencyKey";
    private const string EntidadAuditoria = "OrdenCompra";
    private readonly IOrdenCompraRepository _repository;
    private readonly IProveedorRepository _proveedores;
    private readonly IProductoRepository _productos;
    private readonly ISolicitudCompraRepository _solicitudes;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public OrdenCompraService(
        IOrdenCompraRepository repository,
        IProveedorRepository proveedores,
        IProductoRepository productos,
        ISolicitudCompraRepository solicitudes,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _proveedores = proveedores ?? throw new ArgumentNullException(nameof(proveedores));
        _productos = productos ?? throw new ArgumentNullException(nameof(productos));
        _solicitudes = solicitudes ?? throw new ArgumentNullException(nameof(solicitudes));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<PagedResult<OrdenCompraDto>> GetPagedAsync(OrdenCompraFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
            throw new BusinessRuleException("El rango de fechas es inválido.");
        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<OrdenCompraDto>
        {
            Items = items.Select(Map).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    public async Task<OrdenCompraDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var orden = await _repository.GetByIdAsync(id);
        return orden is null ? null : Map(orden);
    }

    public async Task<OrdenCompraDto> CreateAsync(CreateOrdenCompraDto dto, string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var key = NormalizarIdempotencyKey(idempotencyKey);
        var fingerprint = CalcularFingerprint(dto);

        var previa = await _repository.GetByIdempotencyKeyAsync(key);
        if (previa is not null)
            return Map(ResolverReintento(previa, fingerprint));

        OrdenCompra? creada = null;
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var concurrente = await _repository.GetByIdempotencyKeyAsync(key, tracking: true);
                if (concurrente is not null)
                {
                    creada = ResolverReintento(concurrente, fingerprint);
                    return;
                }

                var usuarioId = ObtenerUsuarioId();
                var ahora = DateTime.UtcNow;
                var documento = await ConstruirDocumentoAsync(dto);
                var orden = new OrdenCompra
                {
                    NumeroOrden = await GenerarNumeroAsync(),
                    SolicitudCompraId = dto.SolicitudCompraId,
                    ProveedorId = documento.Proveedor.Id,
                    ProveedorNombreSnapshot = documento.Proveedor.Nombre.Trim(),
                    ProveedorDocumentoSnapshot = Normalizar(documento.Proveedor.Documento),
                    Moneda = NormalizarMoneda(dto.Moneda),
                    CondicionesCompra = Normalizar(dto.CondicionesCompra),
                    FechaEsperadaUtc = dto.FechaEsperadaUtc,
                    Observaciones = Normalizar(dto.Observaciones),
                    FechaCreacion = ahora,
                    FechaActualizacion = ahora,
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario),
                    Detalles = documento.Detalles
                };
                orden.EstablecerIdempotencia(key, fingerprint);
                ValidarDominio(orden.ValidarDocumento);

                await _repository.AddAsync(orden);
                await _repository.SaveChangesAsync();
                await RegistrarAuditoriaAsync(AccionPermiso.Crear, "Orden de compra creada", orden, valoresNuevos: Snapshot(orden));
                creada = orden;
            });
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == IdempotencyConstraint)
        {
            var concurrente = await _repository.GetByIdempotencyKeyAsync(key)
                ?? throw new ConflictException("La clave de idempotencia fue consumida concurrentemente y no pudo recuperarse de forma segura.");
            creada = ResolverReintento(concurrente, fingerprint);
        }

        return Map(creada ?? throw new InvalidOperationException("La creación de la orden no produjo un resultado."));
    }

    public async Task<OrdenCompraDto> UpdateAsync(int id, UpdateOrdenCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        OrdenCompra? actualizada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orden = await ObtenerBloqueadaAsync(id);
            ValidarDominio(orden.AsegurarEditable);
            var anterior = Snapshot(orden);
            var documento = await ConstruirDocumentoAsync(dto);

            orden.SolicitudCompraId = dto.SolicitudCompraId;
            orden.ProveedorId = documento.Proveedor.Id;
            orden.ProveedorNombreSnapshot = documento.Proveedor.Nombre.Trim();
            orden.ProveedorDocumentoSnapshot = Normalizar(documento.Proveedor.Documento);
            orden.Moneda = NormalizarMoneda(dto.Moneda);
            orden.CondicionesCompra = Normalizar(dto.CondicionesCompra);
            orden.FechaEsperadaUtc = dto.FechaEsperadaUtc;
            orden.Observaciones = Normalizar(dto.Observaciones);
            orden.Detalles.Clear();
            foreach (var detalle in documento.Detalles)
                orden.Detalles.Add(detalle);
            MarcarActualizacion(orden);
            ValidarDominio(orden.ValidarDocumento);

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(AccionPermiso.Editar, "Orden de compra editada", orden, anterior, Snapshot(orden));
            actualizada = orden;
        });
        return Map(actualizada!);
    }

    public Task<OrdenCompraDto> EnviarAprobacionAsync(int id) =>
        EjecutarTransicionAsync(id, AccionPermiso.Confirmar, "Orden de compra enviada a aprobación",
            orden => orden.EnviarAprobacion(ObtenerUsuarioId(), DateTime.UtcNow));

    public Task<OrdenCompraDto> AprobarAsync(int id) =>
        EjecutarTransicionAsync(id, AccionPermiso.Aprobar, "Orden de compra aprobada",
            orden => orden.Aprobar(ObtenerUsuarioId(), _currentUser.NombreCompleto ?? _currentUser.NombreUsuario, DateTime.UtcNow));

    public async Task<OrdenCompraDto> CancelarAsync(int id, CancelarOrdenCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var motivo = Normalizar(dto.Motivo) ?? throw new BusinessRuleException("El motivo de cancelación es obligatorio.");
        return await EjecutarTransicionAsync(id, AccionPermiso.Anular, "Orden de compra cancelada",
            orden => orden.Cancelar(ObtenerUsuarioId(), motivo, DateTime.UtcNow), motivo);
    }

    private async Task<OrdenCompraDto> EjecutarTransicionAsync(
        int id,
        AccionPermiso accion,
        string descripcion,
        Action<OrdenCompra> transicion,
        string? motivo = null)
    {
        OrdenCompra? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orden = await ObtenerBloqueadaAsync(id);
            var anterior = Snapshot(orden);
            ValidarDominio(() => transicion(orden));
            MarcarActualizacion(orden);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(accion, descripcion, orden, anterior, Snapshot(orden), motivo);
            resultado = orden;
        });
        return Map(resultado!);
    }

    private async Task<(Proveedor Proveedor, List<OrdenCompraDetalle> Detalles)> ConstruirDocumentoAsync(CreateOrdenCompraDto dto)
    {
        if (dto.ProveedorId <= 0)
            throw new BusinessRuleException("El proveedor es obligatorio.");
        if (dto.Detalles is null || dto.Detalles.Count == 0)
            throw new BusinessRuleException("La orden de compra debe contener al menos un detalle.");

        var proveedor = await _proveedores.GetByIdAsync(dto.ProveedorId)
            ?? throw new BusinessRuleException("El proveedor indicado no existe.");
        if (!proveedor.Activo)
            throw new BusinessRuleException("El proveedor indicado está inactivo.");

        if (dto.SolicitudCompraId.HasValue)
        {
            if (dto.SolicitudCompraId.Value <= 0)
                throw new BusinessRuleException("La solicitud de compra vinculada no es válida.");
            var solicitud = await _solicitudes.GetByIdAsync(dto.SolicitudCompraId.Value)
                ?? throw new BusinessRuleException("La solicitud de compra vinculada no existe.");
            if (solicitud.Estado != EstadoSolicitudCompra.Aprobada)
                throw new BusinessRuleException("Solo una solicitud de compra aprobada puede originar una orden de compra.");
            if (solicitud.ProveedorId.HasValue && solicitud.ProveedorId.Value != proveedor.Id)
                throw new BusinessRuleException("El proveedor de la orden debe coincidir con el proveedor de la solicitud aprobada.");
        }

        var cacheProductos = new Dictionary<int, Producto>();
        var detalles = new List<OrdenCompraDetalle>(dto.Detalles.Count);
        foreach (var input in dto.Detalles)
        {
            if (input.ProductoId <= 0)
                throw new BusinessRuleException("Cada detalle debe indicar un producto válido.");
            if (!cacheProductos.TryGetValue(input.ProductoId, out var producto))
            {
                producto = await _productos.GetByIdAsync(input.ProductoId)
                    ?? throw new BusinessRuleException($"El producto {input.ProductoId} no existe.");
                if (!producto.Activo || producto.Eliminado)
                    throw new BusinessRuleException($"El producto {input.ProductoId} no está disponible para compras.");
                cacheProductos[input.ProductoId] = producto;
            }

            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = producto.Variantes.FirstOrDefault(x => x.Id == input.ProductoVarianteId.Value && !x.Eliminado)
                    ?? throw new BusinessRuleException($"La variante {input.ProductoVarianteId.Value} no pertenece al producto {producto.Id} o fue eliminada.");
                if (!variante.Activo)
                    throw new BusinessRuleException($"La variante {variante.Id} está inactiva.");
            }

            var detalle = new OrdenCompraDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante?.Id,
                Observacion = Normalizar(input.Observacion),
                ProductoSkuSnapshot = Normalizar(variante?.Sku),
                ProductoNombreSnapshot = producto.Nombre.Trim(),
                ProductoMarcaSnapshot = Normalizar(variante?.Marca?.Nombre) ?? Normalizar(producto.Marca),
                ProductoModeloSnapshot = Normalizar(variante?.Modelo?.Nombre) ?? Normalizar(producto.Modelo),
                ProductoColorSnapshot = Normalizar(variante?.Color?.Nombre),
                ProductoTallaSnapshot = Normalizar(variante?.Talla?.Nombre)
            };
            ValidarDominio(() => detalle.EstablecerValores(input.CantidadOrdenada, input.PrecioUnitario, input.Descuento, input.Impuesto));
            detalles.Add(detalle);
        }

        return (proveedor, detalles);
    }

    private async Task<OrdenCompra> ObtenerBloqueadaAsync(int id)
    {
        if (id <= 0)
            throw new ResourceNotFoundException("Orden de compra no encontrada.");
        return await _repository.GetByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException("Orden de compra no encontrada.");
    }

    private async Task<string> GenerarNumeroAsync()
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var baseNumero = $"OC-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".ToUpperInvariant();
            var numero = baseNumero[..Math.Min(32, baseNumero.Length)];
            if (!await _repository.ExisteNumeroAsync(numero))
                return numero;
        }
        throw new ConflictException("No fue posible generar un número único de orden de compra.");
    }

    private int ObtenerUsuarioId() => _currentUser.EstaAutenticado && _currentUser.UsuarioId is > 0
        ? _currentUser.UsuarioId.Value
        : throw new ForbiddenAccessException("No existe un usuario autenticado válido para ejecutar la operación.");

    private void MarcarActualizacion(OrdenCompra orden)
    {
        orden.FechaActualizacion = DateTime.UtcNow;
        orden.ActualizadoPorUsuarioId = ObtenerUsuarioId();
        orden.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
    }

    private Task RegistrarAuditoriaAsync(
        AccionPermiso accion,
        string descripcion,
        OrdenCompra orden,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            accion,
            descripcion,
            referenciaId: orden.Id,
            entidad: EntidadAuditoria,
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: valoresNuevos,
            motivo: motivo);

    private static object Snapshot(OrdenCompra orden) => new
    {
        orden.NumeroOrden,
        Estado = orden.Estado.ToString(),
        orden.SolicitudCompraId,
        orden.ProveedorId,
        orden.Moneda,
        Lineas = orden.Detalles.Count,
        orden.Subtotal,
        orden.Descuento,
        orden.Impuesto,
        orden.Total,
        orden.FechaEnvioAprobacionUtc,
        orden.FechaAprobacionUtc,
        orden.FechaCancelacionUtc
    };

    private static OrdenCompra ResolverReintento(OrdenCompra existente, string fingerprint)
    {
        if (!string.Equals(existente.IdempotencyFingerprint, fingerprint, StringComparison.Ordinal))
            throw new ConflictException("La clave de idempotencia ya fue utilizada con un payload diferente.");
        return existente;
    }

    private static string NormalizarIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new BusinessRuleException("El encabezado Idempotency-Key es obligatorio.");
        var normalized = key.Trim();
        if (normalized.Length > 128)
            throw new BusinessRuleException("Idempotency-Key no puede superar 128 caracteres.");
        if (normalized.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' or ':')))
            throw new BusinessRuleException("Idempotency-Key contiene caracteres no permitidos.");
        return normalized;
    }

    private static string CalcularFingerprint(CreateOrdenCompraDto dto)
    {
        var canonico = new
        {
            dto.SolicitudCompraId,
            dto.ProveedorId,
            Moneda = NormalizarMoneda(dto.Moneda),
            CondicionesCompra = Normalizar(dto.CondicionesCompra),
            dto.FechaEsperadaUtc,
            Observaciones = Normalizar(dto.Observaciones),
            Detalles = (dto.Detalles ?? new List<OrdenCompraDetalleInputDto>()).Select(x => new
            {
                x.ProductoId,
                x.ProductoVarianteId,
                x.CantidadOrdenada,
                x.PrecioUnitario,
                x.Descuento,
                x.Impuesto,
                Observacion = Normalizar(x.Observacion)
            }).ToArray()
        };
        var json = JsonSerializer.Serialize(canonico);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string NormalizarMoneda(string? moneda)
    {
        var normalizada = string.IsNullOrWhiteSpace(moneda) ? "HNL" : moneda.Trim().ToUpperInvariant();
        if (normalizada.Length != 3 || normalizada.Any(ch => ch < 'A' || ch > 'Z'))
            throw new BusinessRuleException("La moneda debe usar un código ISO alfabético de tres caracteres.");
        return normalizada;
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static void ValidarDominio(Action accion)
    {
        try
        {
            accion();
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private static OrdenCompraDto Map(OrdenCompra orden) => new()
    {
        Id = orden.Id,
        NumeroOrden = orden.NumeroOrden,
        Estado = orden.Estado,
        SolicitudCompraId = orden.SolicitudCompraId,
        ProveedorId = orden.ProveedorId,
        ProveedorNombre = orden.ProveedorNombreSnapshot,
        Moneda = orden.Moneda,
        CondicionesCompra = orden.CondicionesCompra,
        FechaEsperadaUtc = orden.FechaEsperadaUtc,
        Observaciones = orden.Observaciones,
        Subtotal = orden.Subtotal,
        Descuento = orden.Descuento,
        Impuesto = orden.Impuesto,
        Total = orden.Total,
        FechaEnvioAprobacionUtc = orden.FechaEnvioAprobacionUtc,
        FechaAprobacionUtc = orden.FechaAprobacionUtc,
        FechaCancelacionUtc = orden.FechaCancelacionUtc,
        Detalles = orden.Detalles.OrderBy(x => x.Id).Select(x => new OrdenCompraDetalleDto
        {
            Id = x.Id,
            ProductoId = x.ProductoId,
            ProductoVarianteId = x.ProductoVarianteId,
            CantidadOrdenada = x.CantidadOrdenada,
            PrecioUnitario = x.PrecioUnitario,
            Descuento = x.Descuento,
            Impuesto = x.Impuesto,
            Subtotal = x.Subtotal,
            Total = x.Total,
            Observacion = x.Observacion,
            ProductoSkuSnapshot = x.ProductoSkuSnapshot,
            ProductoNombreSnapshot = x.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = x.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = x.ProductoModeloSnapshot,
            ProductoColorSnapshot = x.ProductoColorSnapshot,
            ProductoTallaSnapshot = x.ProductoTallaSnapshot
        }).ToList()
    };
}
