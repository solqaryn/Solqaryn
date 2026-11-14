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

public sealed class RecepcionCompraService : IRecepcionCompraService
{
    private const string IdempotencyConstraint = "UX_RecepcionesCompra_IdempotencyKey";
    private const string EntidadAuditoria = "RecepcionCompra";
    private readonly IRecepcionCompraRepository _repository;
    private readonly IOrdenCompraRepository _ordenes;
    private readonly IAlmacenRepository _almacenes;
    private readonly IUbicacionAlmacenRepository _ubicaciones;
    private readonly IMovimientoInventarioRepository _movimientosInventario;
    private readonly RecepcionCompraExistenciaMaterializador _existencias;
    private readonly RecepcionCompraKardexRegistrar _kardex;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public RecepcionCompraService(
        IRecepcionCompraRepository repository,
        IOrdenCompraRepository ordenes,
        IAlmacenRepository almacenes,
        IUbicacionAlmacenRepository ubicaciones,
        IMovimientoInventarioRepository movimientosInventario,
        RecepcionCompraExistenciaMaterializador existencias,
        RecepcionCompraKardexRegistrar kardex,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _ordenes = ordenes ?? throw new ArgumentNullException(nameof(ordenes));
        _almacenes = almacenes ?? throw new ArgumentNullException(nameof(almacenes));
        _ubicaciones = ubicaciones ?? throw new ArgumentNullException(nameof(ubicaciones));
        _movimientosInventario = movimientosInventario ?? throw new ArgumentNullException(nameof(movimientosInventario));
        _existencias = existencias ?? throw new ArgumentNullException(nameof(existencias));
        _kardex = kardex ?? throw new ArgumentNullException(nameof(kardex));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<PagedResult<RecepcionCompraDto>> GetPagedAsync(RecepcionCompraQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.DesdeUtc.HasValue && filtro.HastaUtc.HasValue && filtro.DesdeUtc > filtro.HastaUtc)
            throw new BusinessRuleException("El rango de fechas es inválido.");

        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<RecepcionCompraDto>
        {
            Items = items.Select(Map).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    public async Task<RecepcionCompraDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var recepcion = await _repository.GetByIdAsync(id);
        return recepcion is null ? null : Map(recepcion);
    }

    public async Task<RecepcionCompraSaldoOrdenDto?> GetSaldoOrdenAsync(int ordenCompraId)
    {
        if (ordenCompraId <= 0)
            return null;

        var orden = await _ordenes.GetByIdAsync(ordenCompraId);
        if (orden is null)
            return null;

        var lineas = new List<RecepcionCompraSaldoLineaDto>(orden.Detalles.Count);
        foreach (var detalle in orden.Detalles.OrderBy(x => x.Id))
        {
            var acumulada = await _repository.GetCantidadAceptadaAcumuladaPorDetalleAsync(detalle.Id);
            var pendiente = Math.Max(0m, detalle.CantidadOrdenada - acumulada);
            lineas.Add(new RecepcionCompraSaldoLineaDto
            {
                OrdenCompraDetalleId = detalle.Id,
                ProductoId = detalle.ProductoId,
                ProductoVarianteId = detalle.ProductoVarianteId,
                ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                ProductoNombreSnapshot = detalle.ProductoNombreSnapshot,
                CantidadOrdenada = detalle.CantidadOrdenada,
                CantidadAceptadaAcumulada = acumulada,
                CantidadPendiente = pendiente
            });
        }

        return new RecepcionCompraSaldoOrdenDto
        {
            OrdenCompraId = orden.Id,
            NumeroOrden = orden.NumeroOrden,
            EstadoOrden = orden.Estado,
            Lineas = lineas
        };
    }

    public async Task<RecepcionCompraDto> CreateAsync(CreateRecepcionCompraDto dto, string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var key = NormalizarIdempotencyKey(idempotencyKey);
        var fingerprint = CalcularFingerprint(dto);

        var previa = await _repository.GetByIdempotencyKeyAsync(key);
        if (previa is not null)
            return Map(ResolverReintento(previa, fingerprint));

        RecepcionCompra? creada = null;
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

                var orden = await ObtenerOrdenAprobadaAsync(dto.OrdenCompraId);
                var ahora = DateTime.UtcNow;
                var usuarioId = ObtenerUsuarioId();
                var recepcion = new RecepcionCompra
                {
                    NumeroRecepcion = await GenerarNumeroAsync(),
                    OrdenCompraId = orden.Id,
                    OrdenCompra = orden,
                    Observaciones = Normalizar(dto.Observaciones),
                    FechaCreacion = ahora,
                    FechaActualizacion = ahora,
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario),
                    Detalles = await ConstruirDetallesAsync(orden, dto.Detalles)
                };
                recepcion.EstablecerIdempotencia(key, fingerprint);
                ValidarDominio(recepcion.ValidarDocumento);

                await _repository.AddAsync(recepcion);
                await _repository.SaveChangesAsync();
                await RegistrarAuditoriaAsync(
                    AccionPermiso.Crear,
                    "Recepción de compra creada en borrador",
                    recepcion,
                    valoresNuevos: Snapshot(recepcion));
                creada = recepcion;
            });
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == IdempotencyConstraint)
        {
            var concurrente = await _repository.GetByIdempotencyKeyAsync(key)
                ?? throw new ConflictException("La clave de idempotencia fue consumida concurrentemente y no pudo recuperarse de forma segura.");
            creada = ResolverReintento(concurrente, fingerprint);
        }

        return Map(creada ?? throw new InvalidOperationException("La creación de la recepción no produjo un resultado."));
    }

    public async Task<RecepcionCompraDto> UpdateAsync(int id, UpdateRecepcionCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RecepcionCompra? actualizada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var recepcion = await ObtenerBloqueadaAsync(id);
            ValidarDominio(recepcion.AsegurarEditable);
            var orden = await ObtenerOrdenAprobadaAsync(recepcion.OrdenCompraId);
            var anterior = Snapshot(recepcion);

            recepcion.Observaciones = Normalizar(dto.Observaciones);
            recepcion.Detalles.Clear();
            foreach (var detalle in await ConstruirDetallesAsync(orden, dto.Detalles))
                recepcion.Detalles.Add(detalle);
            MarcarActualizacion(recepcion);
            ValidarDominio(recepcion.ValidarDocumento);

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Editar,
                "Recepción de compra editada",
                recepcion,
                anterior,
                Snapshot(recepcion));
            actualizada = recepcion;
        });
        return Map(actualizada!);
    }

    public async Task<RecepcionCompraDto> ConfirmarAsync(int id)
    {
        RecepcionCompra? resultado = null;
        var usuarioId = ObtenerUsuarioId();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var recepcion = await ObtenerBloqueadaAsync(id);
            if (recepcion.Estado == EstadoRecepcionCompra.Recibida)
            {
                resultado = recepcion;
                return;
            }

            if (recepcion.Estado != EstadoRecepcionCompra.Borrador)
                throw new BusinessRuleException("Solo una recepción en borrador puede confirmarse.");

            var orden = await ObtenerOrdenAprobadaAsync(recepcion.OrdenCompraId);
            await ValidarRecepcionesMultiplesAsync(recepcion, orden);
            var anterior = Snapshot(recepcion);

            var transiciones = await _existencias.AplicarAsync(recepcion.Detalles);
            await _kardex.RegistrarConfirmacionAsync(recepcion, transiciones);
            ValidarDominio(() => recepcion.Confirmar(usuarioId, _currentUser.NombreUsuario, DateTime.UtcNow));
            MarcarActualizacion(recepcion);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Confirmar,
                "Recepción de compra confirmada y materializada en inventario",
                recepcion,
                anterior,
                Snapshot(recepcion));
            resultado = recepcion;
        });

        return Map(resultado ?? throw new InvalidOperationException("La confirmación de la recepción no produjo un resultado."));
    }

    public async Task<RecepcionCompraDto> AnularAsync(int id, AnularRecepcionCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RecepcionCompra? resultado = null;
        var usuarioId = ObtenerUsuarioId();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var recepcion = await ObtenerBloqueadaAsync(id);
            if (recepcion.Estado == EstadoRecepcionCompra.Anulada)
            {
                resultado = recepcion;
                return;
            }

            if (recepcion.Estado != EstadoRecepcionCompra.Recibida)
                throw new BusinessRuleException("Solo una recepción materializada puede anularse.");

            if (await _movimientosInventario.ExisteMovimientoPosteriorRecepcionAsync(recepcion.Id))
            {
                throw new BusinessRuleException(
                    "No se puede anular la recepción porque existen movimientos de inventario posteriores relacionados.");
            }

            var anterior = Snapshot(recepcion);
            var transiciones = await _existencias.RevertirAsync(recepcion.Detalles);
            await _kardex.RegistrarAnulacionAsync(recepcion, transiciones);
            ValidarDominio(() => recepcion.Anular(usuarioId, dto.Motivo, DateTime.UtcNow));
            MarcarActualizacion(recepcion);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Anular,
                "Recepción de compra anulada y stock físico revertido",
                recepcion,
                anterior,
                Snapshot(recepcion),
                dto.Motivo);
            resultado = recepcion;
        });

        return Map(resultado ?? throw new InvalidOperationException("La anulación de la recepción no produjo un resultado."));
    }

    private async Task ValidarRecepcionesMultiplesAsync(RecepcionCompra recepcion, OrdenCompra orden)
    {
        var detallesOrden = orden.Detalles.ToDictionary(x => x.Id);
        foreach (var detalle in recepcion.Detalles)
        {
            if (!detallesOrden.TryGetValue(detalle.OrdenCompraDetalleId, out var detalleOrden))
                throw new BusinessRuleException($"La línea {detalle.OrdenCompraDetalleId} ya no pertenece a la orden de compra.");

            var acumulada = await _repository.GetCantidadAceptadaAcumuladaPorDetalleAsync(
                detalle.OrdenCompraDetalleId,
                recepcion.Id);
            var proyectada = acumulada + detalle.CantidadAceptada;
            if (proyectada > detalleOrden.CantidadOrdenada)
            {
                throw new BusinessRuleException(
                    $"La recepción de la línea {detalle.OrdenCompraDetalleId} supera la cantidad ordenada. " +
                    $"Ordenada={detalleOrden.CantidadOrdenada}; recibida previamente={acumulada}; actual={detalle.CantidadAceptada}.");
            }
        }
    }

    private async Task<OrdenCompra> ObtenerOrdenAprobadaAsync(int ordenCompraId)
    {
        if (ordenCompraId <= 0)
            throw new BusinessRuleException("La orden de compra es obligatoria.");
        var orden = await _ordenes.GetByIdAsync(ordenCompraId)
            ?? throw new BusinessRuleException("La orden de compra indicada no existe.");
        if (orden.Estado != EstadoOrdenCompra.Aprobada)
            throw new BusinessRuleException("Solo una orden de compra aprobada puede recibir mercancía.");
        return orden;
    }

    private async Task<List<RecepcionCompraDetalle>> ConstruirDetallesAsync(
        OrdenCompra orden,
        IReadOnlyCollection<RecepcionCompraDetalleInputDto>? inputs)
    {
        if (inputs is null || inputs.Count == 0)
            throw new BusinessRuleException("La recepción debe contener al menos un detalle.");

        var detallesOrden = orden.Detalles.ToDictionary(x => x.Id);
        var resultado = new List<RecepcionCompraDetalle>(inputs.Count);
        foreach (var input in inputs)
        {
            if (!detallesOrden.TryGetValue(input.OrdenCompraDetalleId, out var detalleOrden))
                throw new BusinessRuleException($"La línea {input.OrdenCompraDetalleId} no pertenece a la orden de compra indicada.");

            var almacen = await _almacenes.GetByIdAsync(input.AlmacenId)
                ?? throw new BusinessRuleException($"El almacén {input.AlmacenId} no existe.");
            if (!almacen.Activo || almacen.Eliminado)
                throw new BusinessRuleException($"El almacén {input.AlmacenId} no está disponible para recepción.");

            if (input.UbicacionAlmacenId.HasValue)
            {
                var ubicacion = await _ubicaciones.GetByIdAsync(input.UbicacionAlmacenId.Value)
                    ?? throw new BusinessRuleException($"La ubicación {input.UbicacionAlmacenId.Value} no existe.");
                if (!ubicacion.Activa || ubicacion.Eliminado)
                    throw new BusinessRuleException($"La ubicación {ubicacion.Id} no está disponible para recepción.");
                if (ubicacion.AlmacenId != almacen.Id)
                    throw new BusinessRuleException($"La ubicación {ubicacion.Id} no pertenece al almacén {almacen.Id}.");
            }

            var detalle = new RecepcionCompraDetalle
            {
                OrdenCompraDetalleId = detalleOrden.Id,
                ProductoId = detalleOrden.ProductoId,
                ProductoVarianteId = detalleOrden.ProductoVarianteId,
                AlmacenId = almacen.Id,
                UbicacionAlmacenId = input.UbicacionAlmacenId,
                CostoUnitarioSnapshot = detalleOrden.PrecioUnitario,
                ProductoSkuSnapshot = detalleOrden.ProductoSkuSnapshot,
                ProductoNombreSnapshot = detalleOrden.ProductoNombreSnapshot,
                ProductoMarcaSnapshot = detalleOrden.ProductoMarcaSnapshot,
                ProductoModeloSnapshot = detalleOrden.ProductoModeloSnapshot,
                ProductoColorSnapshot = detalleOrden.ProductoColorSnapshot,
                ProductoTallaSnapshot = detalleOrden.ProductoTallaSnapshot
            };
            ValidarDominio(() => detalle.EstablecerCantidades(
                input.CantidadRecibida,
                input.CantidadDanada,
                input.CantidadFaltante,
                input.CantidadSobrante));
            ValidarDominio(detalle.Validar);
            resultado.Add(detalle);
        }
        return resultado;
    }

    private async Task<RecepcionCompra> ObtenerBloqueadaAsync(int id)
    {
        if (id <= 0)
            throw new ResourceNotFoundException("Recepción de compra no encontrada.");
        return await _repository.GetByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException("Recepción de compra no encontrada.");
    }

    private async Task<string> GenerarNumeroAsync()
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var baseNumero = $"RC-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".ToUpperInvariant();
            var numero = baseNumero[..Math.Min(40, baseNumero.Length)];
            if (!await _repository.ExisteNumeroAsync(numero))
                return numero;
        }
        throw new ConflictException("No fue posible generar un número único de recepción de compra.");
    }

    private int ObtenerUsuarioId() => _currentUser.EstaAutenticado && _currentUser.UsuarioId is > 0
        ? _currentUser.UsuarioId.Value
        : throw new ForbiddenAccessException("No existe un usuario autenticado válido para ejecutar la operación.");

    private void MarcarActualizacion(RecepcionCompra recepcion)
    {
        recepcion.FechaActualizacion = DateTime.UtcNow;
        recepcion.ActualizadoPorUsuarioId = ObtenerUsuarioId();
        recepcion.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
    }

    private Task RegistrarAuditoriaAsync(
        AccionPermiso accion,
        string descripcion,
        RecepcionCompra recepcion,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            accion,
            descripcion,
            referenciaId: recepcion.Id,
            entidad: EntidadAuditoria,
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: valoresNuevos,
            motivo: motivo);

    private static object Snapshot(RecepcionCompra recepcion) => new
    {
        recepcion.NumeroRecepcion,
        recepcion.OrdenCompraId,
        Estado = recepcion.Estado.ToString(),
        Lineas = recepcion.Detalles.Count,
        recepcion.CantidadRecibidaTotal,
        recepcion.CantidadAceptadaTotal,
        recepcion.CantidadDanadaTotal,
        recepcion.CantidadFaltanteTotal,
        recepcion.CantidadSobranteTotal,
        recepcion.FechaRecepcionUtc,
        recepcion.FechaAnulacionUtc
    };

    private static RecepcionCompra ResolverReintento(RecepcionCompra existente, string fingerprint)
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

    private static string CalcularFingerprint(CreateRecepcionCompraDto dto)
    {
        var canonico = new
        {
            dto.OrdenCompraId,
            Observaciones = Normalizar(dto.Observaciones),
            Detalles = (dto.Detalles ?? new List<RecepcionCompraDetalleInputDto>()).Select(x => new
            {
                x.OrdenCompraDetalleId,
                x.AlmacenId,
                x.UbicacionAlmacenId,
                x.CantidadRecibida,
                x.CantidadDanada,
                x.CantidadFaltante,
                x.CantidadSobrante
            }).ToArray()
        };
        var json = JsonSerializer.Serialize(canonico);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static RecepcionCompraDto Map(RecepcionCompra recepcion) => new()
    {
        Id = recepcion.Id,
        NumeroRecepcion = recepcion.NumeroRecepcion,
        OrdenCompraId = recepcion.OrdenCompraId,
        NumeroOrdenCompra = recepcion.OrdenCompra?.NumeroOrden,
        Estado = recepcion.Estado,
        Observaciones = recepcion.Observaciones,
        FechaRecepcionUtc = recepcion.FechaRecepcionUtc,
        RecibidaPorUsuarioId = recepcion.RecibidaPorUsuarioId,
        RecibidaPorNombreSnapshot = recepcion.RecibidaPorNombreSnapshot,
        FechaAnulacionUtc = recepcion.FechaAnulacionUtc,
        AnuladaPorUsuarioId = recepcion.AnuladaPorUsuarioId,
        MotivoAnulacion = recepcion.MotivoAnulacion,
        CantidadRecibidaTotal = recepcion.CantidadRecibidaTotal,
        CantidadAceptadaTotal = recepcion.CantidadAceptadaTotal,
        CantidadDanadaTotal = recepcion.CantidadDanadaTotal,
        CantidadFaltanteTotal = recepcion.CantidadFaltanteTotal,
        CantidadSobranteTotal = recepcion.CantidadSobranteTotal,
        Detalles = recepcion.Detalles.Select(x => new RecepcionCompraDetalleDto
        {
            Id = x.Id,
            OrdenCompraDetalleId = x.OrdenCompraDetalleId,
            ProductoId = x.ProductoId,
            ProductoVarianteId = x.ProductoVarianteId,
            AlmacenId = x.AlmacenId,
            UbicacionAlmacenId = x.UbicacionAlmacenId,
            CantidadRecibida = x.CantidadRecibida,
            CantidadAceptada = x.CantidadAceptada,
            CantidadDanada = x.CantidadDanada,
            CantidadFaltante = x.CantidadFaltante,
            CantidadSobrante = x.CantidadSobrante,
            CostoUnitarioSnapshot = x.CostoUnitarioSnapshot,
            ProductoSkuSnapshot = x.ProductoSkuSnapshot,
            ProductoNombreSnapshot = x.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = x.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = x.ProductoModeloSnapshot,
            ProductoColorSnapshot = x.ProductoColorSnapshot,
            ProductoTallaSnapshot = x.ProductoTallaSnapshot
        }).ToList()
    };

    private static string? Normalizar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}