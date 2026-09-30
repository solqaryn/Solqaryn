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

public sealed class DevolucionProveedorService : IDevolucionProveedorService
{
    private const string IdempotencyConstraint = "UX_DevolucionesProveedor_IdempotencyKey";
    private const string EntidadAuditoria = "DevolucionProveedor";

    private readonly IDevolucionProveedorRepository _repository;
    private readonly IRecepcionCompraRepository _recepciones;
    private readonly IFacturaProveedorRepository _facturas;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public DevolucionProveedorService(
        IDevolucionProveedorRepository repository,
        IRecepcionCompraRepository recepciones,
        IFacturaProveedorRepository facturas,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _recepciones = recepciones ?? throw new ArgumentNullException(nameof(recepciones));
        _facturas = facturas ?? throw new ArgumentNullException(nameof(facturas));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<PagedResult<DevolucionProveedorDto>> GetPagedAsync(DevolucionProveedorQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.DesdeUtc.HasValue && filtro.HastaUtc.HasValue && filtro.DesdeUtc > filtro.HastaUtc)
            throw new BusinessRuleException("El rango de fechas es inválido.");

        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<DevolucionProveedorDto>
        {
            Items = items.Select(Map).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    public async Task<DevolucionProveedorDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var devolucion = await _repository.GetByIdAsync(id);
        return devolucion is null ? null : Map(devolucion);
    }

    public async Task<DevolucionProveedorDto> CreateAsync(CreateDevolucionProveedorDto dto, string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var key = NormalizarIdempotencyKey(idempotencyKey);
        var fingerprint = CalcularFingerprint(dto);

        var previa = await _repository.GetByIdempotencyKeyAsync(key);
        if (previa is not null)
            return Map(ResolverReintento(previa, fingerprint));

        DevolucionProveedor? creada = null;
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

                var contexto = await CargarContextoOrigenAsync(dto.RecepcionCompraId, dto.FacturaProveedorId);
                var ahora = DateTime.UtcNow;
                var usuarioId = ObtenerUsuarioId();

                var devolucion = new DevolucionProveedor
                {
                    NumeroDevolucion = await GenerarNumeroAsync(),
                    ProveedorId = contexto.Factura.ProveedorId,
                    OrdenCompraId = contexto.Factura.OrdenCompraId,
                    RecepcionCompraId = contexto.Recepcion.Id,
                    FacturaProveedorId = contexto.Factura.Id,
                    ProveedorNombreSnapshot = contexto.Factura.ProveedorNombreSnapshot.Trim(),
                    Moneda = contexto.Factura.Moneda.Trim().ToUpperInvariant(),
                    Motivo = NormalizarRequerido(dto.Motivo, "El motivo de devolución es obligatorio."),
                    Observaciones = Normalizar(dto.Observaciones),
                    FechaCreacion = ahora,
                    FechaActualizacion = ahora,
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario),
                    Detalles = ConstruirDetalles(contexto, dto.Detalles)
                };
                devolucion.EstablecerIdempotencia(key, fingerprint);
                ValidarDominio(devolucion.ValidarDocumento);

                await _repository.AddAsync(devolucion);
                await _repository.SaveChangesAsync();
                await RegistrarAuditoriaAsync(
                    AccionPermiso.Crear,
                    "Devolución a proveedor creada en borrador",
                    devolucion,
                    valoresNuevos: Snapshot(devolucion));
                creada = devolucion;
            });
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == IdempotencyConstraint)
        {
            var concurrente = await _repository.GetByIdempotencyKeyAsync(key)
                ?? throw new ConflictException("La clave de idempotencia fue consumida concurrentemente y no pudo recuperarse de forma segura.");
            creada = ResolverReintento(concurrente, fingerprint);
        }

        return Map(creada ?? throw new InvalidOperationException("La creación de la devolución no produjo un resultado."));
    }

    public async Task<DevolucionProveedorDto> UpdateAsync(int id, UpdateDevolucionProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        DevolucionProveedor? actualizada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var devolucion = await ObtenerBloqueadaAsync(id);
            ValidarDominio(devolucion.AsegurarEditable);
            var contexto = await CargarContextoOrigenAsync(devolucion.RecepcionCompraId, devolucion.FacturaProveedorId);
            var anterior = Snapshot(devolucion);

            devolucion.Motivo = NormalizarRequerido(dto.Motivo, "El motivo de devolución es obligatorio.");
            devolucion.Observaciones = Normalizar(dto.Observaciones);
            devolucion.Detalles.Clear();
            foreach (var detalle in ConstruirDetalles(contexto, dto.Detalles))
                devolucion.Detalles.Add(detalle);
            MarcarActualizacion(devolucion);
            ValidarDominio(devolucion.ValidarDocumento);

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Editar,
                "Devolución a proveedor editada",
                devolucion,
                anterior,
                Snapshot(devolucion));
            actualizada = devolucion;
        });
        return Map(actualizada!);
    }

    public async Task<DevolucionProveedorDto> ConfirmarAsync(int id)
    {
        DevolucionProveedor? resultado = null;
        var usuarioId = ObtenerUsuarioId();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var devolucion = await ObtenerBloqueadaAsync(id);
            if (devolucion.Estado == EstadoDevolucionProveedor.Confirmada)
            {
                resultado = devolucion;
                return;
            }
            if (devolucion.Estado != EstadoDevolucionProveedor.Borrador)
                throw new BusinessRuleException("Solo una devolución a proveedor en borrador puede confirmarse.");

            var contexto = await CargarContextoOrigenAsync(devolucion.RecepcionCompraId, devolucion.FacturaProveedorId);
            await ValidarSaldoDevueltoAsync(devolucion, contexto);
            var anterior = Snapshot(devolucion);

            ValidarDominio(() => devolucion.Confirmar(usuarioId, _currentUser.NombreUsuario, DateTime.UtcNow));
            MarcarActualizacion(devolucion);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Confirmar,
                "Devolución a proveedor confirmada",
                devolucion,
                anterior,
                Snapshot(devolucion));
            resultado = devolucion;
        });
        return Map(resultado ?? throw new InvalidOperationException("La confirmación de la devolución no produjo un resultado."));
    }

    public async Task<DevolucionProveedorDto> AnularAsync(int id, AnularDevolucionProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        DevolucionProveedor? resultado = null;
        var usuarioId = ObtenerUsuarioId();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var devolucion = await ObtenerBloqueadaAsync(id);
            if (devolucion.Estado == EstadoDevolucionProveedor.Anulada)
            {
                resultado = devolucion;
                return;
            }
            if (devolucion.Estado != EstadoDevolucionProveedor.Confirmada)
                throw new BusinessRuleException("Solo una devolución a proveedor confirmada puede anularse.");

            var motivo = NormalizarRequerido(dto.Motivo, "El motivo de anulación es obligatorio.");
            var anterior = Snapshot(devolucion);
            ValidarDominio(() => devolucion.Anular(usuarioId, motivo, DateTime.UtcNow));
            MarcarActualizacion(devolucion);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Anular,
                "Devolución a proveedor anulada",
                devolucion,
                anterior,
                Snapshot(devolucion),
                motivo);
            resultado = devolucion;
        });
        return Map(resultado ?? throw new InvalidOperationException("La anulación de la devolución no produjo un resultado."));
    }

    private async Task<ContextoOrigen> CargarContextoOrigenAsync(int recepcionCompraId, int facturaProveedorId)
    {
        if (recepcionCompraId <= 0)
            throw new BusinessRuleException("La recepción de compra es obligatoria.");
        if (facturaProveedorId <= 0)
            throw new BusinessRuleException("La factura de proveedor es obligatoria.");

        var recepcion = await _recepciones.GetByIdAsync(recepcionCompraId)
            ?? throw new BusinessRuleException("La recepción de compra indicada no existe.");
        if (recepcion.Estado != EstadoRecepcionCompra.Recibida)
            throw new BusinessRuleException("Solo una recepción materializada puede originar una devolución a proveedor.");

        var factura = await _facturas.GetByIdAsync(facturaProveedorId)
            ?? throw new BusinessRuleException("La factura de proveedor indicada no existe.");
        if (factura.Estado != EstadoFacturaProveedor.Registrada)
            throw new BusinessRuleException("Solo una factura de proveedor registrada puede respaldar una devolución.");
        if (factura.OrdenCompraId != recepcion.OrdenCompraId)
            throw new BusinessRuleException("La factura y la recepción deben pertenecer a la misma orden de compra.");
        if (recepcion.OrdenCompra is null || recepcion.OrdenCompra.ProveedorId != factura.ProveedorId)
            throw new BusinessRuleException("La factura, la recepción y la orden deben pertenecer al mismo proveedor.");
        if (string.IsNullOrWhiteSpace(factura.ProveedorNombreSnapshot))
            throw new BusinessRuleException("La factura no contiene un snapshot válido del proveedor.");
        if (string.IsNullOrWhiteSpace(factura.Moneda) || factura.Moneda.Trim().Length != 3)
            throw new BusinessRuleException("La factura no contiene una moneda ISO válida.");

        return new ContextoOrigen(recepcion, factura);
    }

    private static List<DevolucionProveedorDetalle> ConstruirDetalles(
        ContextoOrigen contexto,
        IReadOnlyCollection<DevolucionProveedorDetalleInputDto>? inputs)
    {
        if (inputs is null || inputs.Count == 0)
            throw new BusinessRuleException("La devolución debe contener al menos un detalle.");
        if (inputs.GroupBy(x => x.RecepcionCompraDetalleId).Any(x => x.Count() > 1))
            throw new BusinessRuleException("Una línea de recepción no puede repetirse dentro de la misma devolución.");

        var recepcionPorId = contexto.Recepcion.Detalles.ToDictionary(x => x.Id);
        var facturaPorOrdenDetalle = contexto.Factura.Detalles.ToDictionary(x => x.OrdenCompraDetalleId);
        var resultado = new List<DevolucionProveedorDetalle>(inputs.Count);

        foreach (var input in inputs)
        {
            if (!recepcionPorId.TryGetValue(input.RecepcionCompraDetalleId, out var recibido))
                throw new BusinessRuleException($"La línea de recepción {input.RecepcionCompraDetalleId} no pertenece a la recepción indicada.");
            if (recibido.CantidadAceptada <= 0m)
                throw new BusinessRuleException($"La línea de recepción {input.RecepcionCompraDetalleId} no contiene cantidad aceptada devolvible.");
            if (input.Cantidad <= 0m || input.Cantidad > recibido.CantidadAceptada)
                throw new BusinessRuleException($"La cantidad a devolver de la línea {input.RecepcionCompraDetalleId} debe ser mayor que cero y no superar lo aceptado.");
            if (!facturaPorOrdenDetalle.TryGetValue(recibido.OrdenCompraDetalleId, out var facturado))
                throw new BusinessRuleException($"La factura indicada no contiene la línea de orden {recibido.OrdenCompraDetalleId} asociada a la recepción.");
            if (input.Cantidad > facturado.CantidadFacturada)
                throw new BusinessRuleException($"La cantidad a devolver de la línea {input.RecepcionCompraDetalleId} no puede superar la cantidad facturada.");

            var descuentoUnitario = facturado.DescuentoSnapshot / facturado.CantidadFacturada;
            var costoNetoUnitario = decimal.Round(facturado.PrecioUnitarioSnapshot - descuentoUnitario, 4, MidpointRounding.AwayFromZero);
            var impuestoUnitario = decimal.Round(facturado.ImpuestoSnapshot / facturado.CantidadFacturada, 4, MidpointRounding.AwayFromZero);

            var detalle = new DevolucionProveedorDetalle
            {
                RecepcionCompraDetalleId = recibido.Id,
                OrdenCompraDetalleId = recibido.OrdenCompraDetalleId,
                ProductoId = recibido.ProductoId,
                ProductoVarianteId = recibido.ProductoVarianteId,
                AlmacenId = recibido.AlmacenId,
                UbicacionAlmacenId = recibido.UbicacionAlmacenId,
                Cantidad = input.Cantidad,
                CostoUnitarioSnapshot = costoNetoUnitario,
                ImpuestoUnitarioSnapshot = impuestoUnitario,
                ProductoSkuSnapshot = recibido.ProductoSkuSnapshot,
                ProductoNombreSnapshot = NormalizarRequerido(recibido.ProductoNombreSnapshot, "El detalle de recepción no contiene snapshot del producto."),
                ProductoMarcaSnapshot = recibido.ProductoMarcaSnapshot,
                ProductoModeloSnapshot = recibido.ProductoModeloSnapshot,
                ProductoColorSnapshot = recibido.ProductoColorSnapshot,
                ProductoTallaSnapshot = recibido.ProductoTallaSnapshot
            };
            ValidarDominio(detalle.Validar);
            resultado.Add(detalle);
        }
        return resultado;
    }

    private async Task ValidarSaldoDevueltoAsync(DevolucionProveedor devolucion, ContextoOrigen contexto)
    {
        var recepcionPorId = contexto.Recepcion.Detalles.ToDictionary(x => x.Id);
        var facturaPorOrdenDetalle = contexto.Factura.Detalles.ToDictionary(x => x.OrdenCompraDetalleId);
        foreach (var detalle in devolucion.Detalles)
        {
            if (!recepcionPorId.TryGetValue(detalle.RecepcionCompraDetalleId, out var recibido))
                throw new BusinessRuleException("Una línea de la devolución ya no existe en la recepción de origen.");
            if (!facturaPorOrdenDetalle.TryGetValue(detalle.OrdenCompraDetalleId, out var facturado))
                throw new BusinessRuleException("Una línea de la devolución ya no existe en la factura de origen.");

            var devueltoRecepcion = await _repository.GetCantidadConfirmadaDevueltaPorDetalleAsync(detalle.RecepcionCompraDetalleId, devolucion.Id);
            if (devueltoRecepcion + detalle.Cantidad > recibido.CantidadAceptada)
                throw new BusinessRuleException($"La devolución de la línea {detalle.RecepcionCompraDetalleId} supera la cantidad aceptada disponible.");

            var devueltoFactura = await _repository.GetCantidadConfirmadaDevueltaPorFacturaLineaAsync(
                devolucion.FacturaProveedorId,
                detalle.OrdenCompraDetalleId,
                devolucion.Id);
            if (devueltoFactura + detalle.Cantidad > facturado.CantidadFacturada)
                throw new BusinessRuleException($"La devolución de la línea de orden {detalle.OrdenCompraDetalleId} supera la cantidad facturada disponible.");
        }
    }

    private async Task<DevolucionProveedor> ObtenerBloqueadaAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la devolución debe ser válido.");
        return await _repository.GetByIdForUpdateAsync(id)
            ?? throw new BusinessRuleException("La devolución a proveedor indicada no existe.");
    }

    private async Task<string> GenerarNumeroAsync()
    {
        var prefijo = $"DVP-{DateTime.UtcNow:yyyyMMdd}";
        for (var secuencia = 1; secuencia <= 999999; secuencia++)
        {
            var numero = $"{prefijo}-{secuencia:000000}";
            if (!await _repository.ExisteNumeroAsync(numero))
                return numero;
        }
        throw new ConflictException("No fue posible generar un número único de devolución a proveedor.");
    }

    private Task RegistrarAuditoriaAsync(
        AccionPermiso accion,
        string descripcion,
        DevolucionProveedor devolucion,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            accion,
            descripcion,
            devolucion.Id,
            EntidadAuditoria,
            valoresAnteriores,
            valoresNuevos,
            motivo);

    private static DevolucionProveedor ResolverReintento(DevolucionProveedor existente, string fingerprint)
    {
        if (!string.Equals(existente.IdempotencyFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("La clave de idempotencia ya fue usada con un payload diferente.");
        return existente;
    }

    private static string NormalizarIdempotencyKey(string? value)
    {
        var key = value?.Trim();
        if (string.IsNullOrWhiteSpace(key))
            throw new BusinessRuleException("Idempotency-Key es obligatorio.");
        if (key.Length > 128)
            throw new BusinessRuleException("Idempotency-Key no puede superar 128 caracteres.");
        return key;
    }

    private static string CalcularFingerprint(CreateDevolucionProveedorDto dto)
    {
        var canonical = new
        {
            dto.RecepcionCompraId,
            dto.FacturaProveedorId,
            Motivo = Normalizar(dto.Motivo),
            Observaciones = Normalizar(dto.Observaciones),
            Detalles = dto.Detalles.OrderBy(x => x.RecepcionCompraDetalleId)
                .Select(x => new { x.RecepcionCompraDetalleId, x.Cantidad }).ToArray()
        };
        var json = JsonSerializer.Serialize(canonical);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private int ObtenerUsuarioId() =>
        _currentUser.UsuarioId is > 0
            ? _currentUser.UsuarioId.Value
            : throw new UnauthorizedAccessException("No existe un usuario autenticado válido para la operación.");

    private void MarcarActualizacion(DevolucionProveedor devolucion)
    {
        devolucion.FechaActualizacion = DateTime.UtcNow;
        devolucion.ActualizadoPorUsuarioId = ObtenerUsuarioId();
        devolucion.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
    }

    private static DevolucionProveedorDto Map(DevolucionProveedor devolucion) => new()
    {
        Id = devolucion.Id,
        NumeroDevolucion = devolucion.NumeroDevolucion,
        ProveedorId = devolucion.ProveedorId,
        OrdenCompraId = devolucion.OrdenCompraId,
        RecepcionCompraId = devolucion.RecepcionCompraId,
        FacturaProveedorId = devolucion.FacturaProveedorId,
        ProveedorNombreSnapshot = devolucion.ProveedorNombreSnapshot,
        Moneda = devolucion.Moneda,
        Motivo = devolucion.Motivo,
        Observaciones = devolucion.Observaciones,
        Estado = devolucion.Estado,
        FechaConfirmacionUtc = devolucion.FechaConfirmacionUtc,
        ConfirmadaPorUsuarioId = devolucion.ConfirmadaPorUsuarioId,
        ConfirmadaPorNombreSnapshot = devolucion.ConfirmadaPorNombreSnapshot,
        FechaAnulacionUtc = devolucion.FechaAnulacionUtc,
        AnuladaPorUsuarioId = devolucion.AnuladaPorUsuarioId,
        MotivoAnulacion = devolucion.MotivoAnulacion,
        SubtotalCredito = devolucion.SubtotalCredito,
        ImpuestoCredito = devolucion.ImpuestoCredito,
        TotalCredito = devolucion.TotalCredito,
        Detalles = devolucion.Detalles.OrderBy(x => x.Id).Select(x => new DevolucionProveedorDetalleDto
        {
            Id = x.Id,
            RecepcionCompraDetalleId = x.RecepcionCompraDetalleId,
            OrdenCompraDetalleId = x.OrdenCompraDetalleId,
            ProductoId = x.ProductoId,
            ProductoVarianteId = x.ProductoVarianteId,
            AlmacenId = x.AlmacenId,
            UbicacionAlmacenId = x.UbicacionAlmacenId,
            Cantidad = x.Cantidad,
            CostoUnitarioSnapshot = x.CostoUnitarioSnapshot,
            ImpuestoUnitarioSnapshot = x.ImpuestoUnitarioSnapshot,
            SubtotalCredito = x.SubtotalCredito,
            ImpuestoCredito = x.ImpuestoCredito,
            TotalCredito = x.TotalCredito,
            ProductoSkuSnapshot = x.ProductoSkuSnapshot,
            ProductoNombreSnapshot = x.ProductoNombreSnapshot
        }).ToList()
    };

    private static object Snapshot(DevolucionProveedor devolucion) => new
    {
        devolucion.Id,
        devolucion.NumeroDevolucion,
        devolucion.ProveedorId,
        devolucion.OrdenCompraId,
        devolucion.RecepcionCompraId,
        devolucion.FacturaProveedorId,
        devolucion.Moneda,
        devolucion.Motivo,
        devolucion.Observaciones,
        Estado = devolucion.Estado.ToString(),
        devolucion.SubtotalCredito,
        devolucion.ImpuestoCredito,
        devolucion.TotalCredito,
        Detalles = devolucion.Detalles.Select(x => new
        {
            x.Id,
            x.RecepcionCompraDetalleId,
            x.OrdenCompraDetalleId,
            x.ProductoId,
            x.ProductoVarianteId,
            x.AlmacenId,
            x.UbicacionAlmacenId,
            x.Cantidad,
            x.CostoUnitarioSnapshot,
            x.ImpuestoUnitarioSnapshot
        }).ToArray()
    };

    private static string? Normalizar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizarRequerido(string? value, string error)
    {
        var normalizado = Normalizar(value);
        if (normalizado is null) throw new BusinessRuleException(error);
        return normalizado;
    }

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
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private sealed record ContextoOrigen(RecepcionCompra Recepcion, FacturaProveedor Factura);
}
