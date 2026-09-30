using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;

namespace Solqaryn.Application.Services;

public sealed class PedidoVentaService : IPedidoVentaService
{
    private readonly IPedidoVentaRepository _repository;
    private readonly ICotizacionRepository _cotizacionRepository;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReservaInventarioRepository? _reservaRepository;
    private readonly IProductoVarianteRepository? _variantes;
    private readonly IExistenciaVarianteConcurrencyService? _existencias;

    public PedidoVentaService(
        IPedidoVentaRepository repository,
        ICotizacionRepository cotizacionRepository,
        IAuditoriaService auditoriaService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IReservaInventarioRepository? reservaRepository = null,
        IProductoVarianteRepository? variantes = null,
        IExistenciaVarianteConcurrencyService? existencias = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cotizacionRepository = cotizacionRepository ?? throw new ArgumentNullException(nameof(cotizacionRepository));
        _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _reservaRepository = reservaRepository;
        _variantes = variantes;
        _existencias = existencias;
    }

    public async Task<PedidoVentaDto> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador del pedido debe ser mayor que cero.");

        var pedido = await _repository.GetByIdAsync(id, asNoTracking: true)
            ?? throw new ResourceNotFoundException($"Pedido de venta con Id {id} no encontrado.");

        return MapToDto(pedido);
    }

    public async Task<PagedResult<PedidoVentaDto>> GetPagedAsync(PedidoVentaFiltroDto request)
    {
        ValidarFiltro(request);
        var (items, total) = await _repository.GetPagedAsync(request);
        return new PagedResult<PedidoVentaDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<PedidoVentaDto> CrearDesdeCotizacionAsync(CreatePedidoVentaDto dto, string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.CotizacionId <= 0)
            throw new BusinessRuleException("CotizacionId debe ser mayor que cero.");

        var key = NormalizarIdempotencyKey(idempotencyKey);
        var fingerprint = CalcularFingerprint(dto.CotizacionId, dto.Observaciones);
        var (usuarioId, _) = RequerirUsuario();
        var pedidoId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var porKey = await _repository.GetByIdempotencyKeyForUpdateAsync(key);
            if (porKey is not null)
            {
                ValidarReplay(porKey, fingerprint);
                pedidoId = porKey.Id;
                return;
            }

            var cotizacion = await _cotizacionRepository.GetByIdForUpdateAsync(dto.CotizacionId)
                ?? throw new ResourceNotFoundException($"Cotización con Id {dto.CotizacionId} no encontrada.");

            var porCotizacion = await _repository.GetByCotizacionIdForUpdateAsync(dto.CotizacionId);
            if (porCotizacion is not null)
            {
                if (!string.Equals(porCotizacion.IdempotencyKey, key, StringComparison.Ordinal) ||
                    !string.Equals(porCotizacion.IdempotencyFingerprint, fingerprint, StringComparison.Ordinal))
                    throw new BusinessRuleException("La cotización ya originó un pedido con otra clave o payload idempotente.");

                pedidoId = porCotizacion.Id;
                return;
            }

            var pedido = PedidoVenta.CrearDesdeCotizacion(cotizacion);
            pedido.EstablecerIdempotencia(key, fingerprint);
            if (!string.IsNullOrWhiteSpace(dto.Observaciones))
                pedido.ActualizarObservaciones(dto.Observaciones);

            cotizacion.Convertir(usuarioId, DateTime.UtcNow);

            await _repository.AddAsync(pedido);
            _cotizacionRepository.Update(cotizacion);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Crear,
                "Pedido de venta creado desde cotización.",
                pedido.Id,
                nameof(PedidoVenta),
                valoresNuevos: new { pedido.CotizacionId, pedido.ClienteId, pedido.Estado, pedido.Total });

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Aplicar,
                "Cotización convertida al originar pedido de venta.",
                cotizacion.Id,
                nameof(Cotizacion),
                valoresNuevos: new { cotizacion.Estado, PedidoVentaId = pedido.Id });

            pedidoId = pedido.Id;
        });

        return await GetByIdAsync(pedidoId);
    }

    public async Task<PedidoVentaDto> ActualizarAsync(UpdatePedidoVentaDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id <= 0)
            throw new BusinessRuleException("El identificador del pedido debe ser mayor que cero.");

        RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pedido = await RequerirForUpdateAsync(dto.Id);
            var anterior = pedido.Observaciones;
            pedido.ActualizarObservaciones(dto.Observaciones);
            _repository.Update(pedido);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Editar,
                "Pedido de venta actualizado.",
                pedido.Id,
                nameof(PedidoVenta),
                valoresAnteriores: new { Observaciones = anterior },
                valoresNuevos: new { pedido.Observaciones });
        });

        return await GetByIdAsync(dto.Id);
    }

    public async Task<PedidoVentaDto> ConfirmarAsync(int id, ConfirmarPedidoVentaDto dto)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador del pedido debe ser mayor que cero.");
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Asignaciones is null || dto.Asignaciones.Count == 0)
            throw new BusinessRuleException("La confirmación requiere asignaciones físicas explícitas de inventario.");

        var reservas = _reservaRepository
            ?? throw new InvalidOperationException("IReservaInventarioRepository no está configurado para confirmar pedidos con reserva.");
        var variantes = _variantes
            ?? throw new InvalidOperationException("IProductoVarianteRepository no está configurado para confirmar pedidos con reserva.");
        var existencias = _existencias
            ?? throw new InvalidOperationException("IExistenciaVarianteConcurrencyService no está configurado para confirmar pedidos con reserva.");
        var (usuarioId, nombreUsuario) = RequerirUsuario();
        var asignaciones = dto.Asignaciones.Select(x =>
            AsignacionReservaAutomatica.Crear(x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId, x.Cantidad)).ToArray();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pedido = await RequerirForUpdateAsync(id);
            var reservaExistente = await reservas.GetByPedidoVentaIdAsync(id, tracking: true);

            if (pedido.Estado == EstadoPedidoVenta.Confirmado)
            {
                if (reservaExistente is null || reservaExistente.Estado != EstadoReservaInventario.Activa)
                    throw new BusinessRuleException("El pedido confirmado no tiene una reserva automática activa consistente.");
                ValidarReplayReserva(reservaExistente, asignaciones);
                return;
            }

            if (pedido.Estado != EstadoPedidoVenta.Borrador)
                throw new BusinessRuleException("Solo un pedido en borrador puede confirmarse con reserva automática.");
            if (reservaExistente is not null)
                throw new BusinessRuleException("El pedido ya tiene una reserva automática persistida y no puede crear otra.");

            var plan = pedido.PrepararReservaAutomatica(asignaciones);
            var varianteIds = plan.Asignaciones.Select(x => x.ProductoVarianteId).Distinct().ToArray();
            var variantesPersistidas = await variantes.GetByIdsForUpdateAsync(varianteIds);
            if (variantesPersistidas.Count != varianteIds.Length)
                throw new BusinessRuleException("Una o más variantes asignadas no existen.");

            var porId = variantesPersistidas.ToDictionary(x => x.Id);
            foreach (var variante in variantesPersistidas)
            {
                if (variante.Eliminado || !variante.Activo)
                    throw new BusinessRuleException($"La variante {variante.Id} no está activa para reservar inventario.");
            }

            var demandas = plan.Asignaciones.Select(asignacion =>
            {
                var variante = porId[asignacion.ProductoVarianteId];
                return new InventarioDemandaExistencia(
                    variante.ProductoId,
                    asignacion.ProductoVarianteId,
                    asignacion.AlmacenId,
                    asignacion.UbicacionAlmacenId,
                    asignacion.Cantidad);
            }).ToArray();

            var lockSet = await existencias.BloquearYValidarExistenciasAsync(demandas, esDeduccion: true);
            var ahora = DateTime.UtcNow;
            var nombreReserva = await GenerarNumeroReservaAsync(reservas, ahora);
            var reserva = new ReservaInventario
            {
                Numero = nombreReserva,
                PedidoVentaId = pedido.Id,
                FechaCreacion = ahora,
                FechaActualizacion = ahora,
                CreadoPorUsuarioId = usuarioId,
                CreadoPorNombreUsuario = _currentUserService.NombreUsuario,
                ActualizadoPorUsuarioId = usuarioId,
                ActualizadoPorNombreUsuario = _currentUserService.NombreUsuario
            };

            foreach (var asignacion in plan.Asignaciones)
            {
                var variante = porId[asignacion.ProductoVarianteId];
                var detalle = new ReservaInventarioDetalle
                {
                    ReservaInventario = reserva,
                    ProductoVarianteId = variante.Id,
                    ProductoVariante = variante,
                    AlmacenId = asignacion.AlmacenId,
                    UbicacionAlmacenId = asignacion.UbicacionAlmacenId,
                    ProductoSkuSnapshot = variante.Sku,
                    ProductoMarcaSnapshot = variante.Marca?.Nombre,
                    ProductoModeloSnapshot = variante.Modelo?.Nombre,
                    ProductoColorSnapshot = variante.Color?.Nombre,
                    ProductoTallaSnapshot = variante.Talla?.Nombre,
                    FechaCreacion = ahora,
                    FechaActualizacion = ahora,
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = _currentUserService.NombreUsuario,
                    ActualizadoPorUsuarioId = usuarioId,
                    ActualizadoPorNombreUsuario = _currentUserService.NombreUsuario
                };
                detalle.EstablecerCantidadReservada(asignacion.Cantidad);
                detalle.ValidarClaveFisica();
                reserva.Detalles.Add(detalle);
            }

            reserva.ValidarDocumento();
            await reservas.AddAsync(reserva);
            await reservas.SaveChangesAsync();

            foreach (var demanda in lockSet.Demandas)
            {
                var existencia = lockSet.Existencias[demanda.Clave];
                await existencias.AjustarStockReservadoPesimistaAsync(
                    demanda.Clave,
                    existencia.StockReservado,
                    checked(existencia.StockReservado + demanda.Cantidad));
            }

            reserva.Activar(usuarioId, ahora);
            pedido.Confirmar(usuarioId, nombreUsuario, ahora);
            _repository.Update(pedido);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Confirmar,
                "Reserva automática activada al confirmar pedido de venta.",
                reserva.Id,
                nameof(ReservaInventario),
                valoresNuevos: new
                {
                    reserva.PedidoVentaId,
                    reserva.Numero,
                    reserva.Estado,
                    Detalles = reserva.Detalles.Select(x => new
                    {
                        x.ProductoVarianteId,
                        x.AlmacenId,
                        x.UbicacionAlmacenId,
                        x.CantidadReservada
                    }).ToArray()
                });

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Confirmar,
                "Pedido de venta confirmado con reserva automática.",
                pedido.Id,
                nameof(PedidoVenta),
                valoresNuevos: new { pedido.Estado, ReservaInventarioId = reserva.Id });
        });

        return await GetByIdAsync(id);
    }

    public async Task<PedidoVentaDto> AnularAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        return await CambiarEstadoAsync(
            id,
            AccionPermiso.Anular,
            "Pedido de venta anulado.",
            (pedido, usuarioId, nombre) => pedido.Anular(usuarioId, nombre, motivo, DateTime.UtcNow),
            motivo.Trim());
    }

    private async Task<PedidoVentaDto> CambiarEstadoAsync(
        int id,
        AccionPermiso accion,
        string descripcion,
        Action<PedidoVenta, int, string> mutacion,
        string? motivo = null)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador del pedido debe ser mayor que cero.");

        var (usuarioId, nombre) = RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pedido = await RequerirForUpdateAsync(id);
            var estadoAnterior = pedido.Estado;
            mutacion(pedido, usuarioId, nombre);
            _repository.Update(pedido);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                accion,
                descripcion,
                pedido.Id,
                nameof(PedidoVenta),
                valoresAnteriores: new { Estado = estadoAnterior },
                valoresNuevos: new { pedido.Estado },
                motivo: motivo);
        });

        return await GetByIdAsync(id);
    }

    private async Task<PedidoVenta> RequerirForUpdateAsync(int id) =>
        await _repository.GetByIdForUpdateAsync(id)
        ?? throw new ResourceNotFoundException($"Pedido de venta con Id {id} no encontrado.");

    private (int UsuarioId, string Nombre) RequerirUsuario()
    {
        if (_currentUserService.UsuarioId is not > 0)
            throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");

        var nombre = _currentUserService.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            nombre = _currentUserService.NombreUsuario?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ForbiddenAccessException("No se pudo resolver la identidad del usuario autenticado.");

        return (_currentUserService.UsuarioId.Value, nombre);
    }

    private static async Task<string> GenerarNumeroReservaAsync(IReservaInventarioRepository reservas, DateTime fechaUtc)
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var numero = $"RSV-{fechaUtc:yyyyMMddHHmmssfff}-{sufijo}";
            if (!await reservas.ExisteNumeroAsync(numero))
                return numero;
        }

        throw new BusinessRuleException("No fue posible generar un número único para la reserva automática.");
    }

    private static void ValidarReplayReserva(
        ReservaInventario reserva,
        IReadOnlyCollection<AsignacionReservaAutomatica> asignaciones)
    {
        var persistidas = reserva.Detalles
            .Select(x => (x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId, x.CantidadReservada))
            .OrderBy(x => x.ProductoVarianteId)
            .ThenBy(x => x.AlmacenId)
            .ThenBy(x => x.UbicacionAlmacenId)
            .ThenBy(x => x.CantidadReservada)
            .ToArray();
        var solicitadas = asignaciones
            .Select(x => (x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId, x.Cantidad))
            .OrderBy(x => x.ProductoVarianteId)
            .ThenBy(x => x.AlmacenId)
            .ThenBy(x => x.UbicacionAlmacenId)
            .ThenBy(x => x.Cantidad)
            .ToArray();

        if (!persistidas.SequenceEqual(solicitadas))
            throw new BusinessRuleException("El pedido ya fue confirmado con una asignación física diferente.");
    }

    private static string NormalizarIdempotencyKey(string key)
    {
        var normalizada = key?.Trim() ?? string.Empty;
        if (normalizada.Length is < 1 or > 128)
            throw new BusinessRuleException("Idempotency-Key es obligatoria y debe tener entre 1 y 128 caracteres.");
        if (normalizada.Any(char.IsControl))
            throw new BusinessRuleException("Idempotency-Key contiene caracteres no permitidos.");
        return normalizada;
    }

    private static string CalcularFingerprint(int cotizacionId, string? observaciones)
    {
        var payload = $"{cotizacionId}|{NormalizarOpcional(observaciones) ?? string.Empty}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static void ValidarReplay(PedidoVenta existente, string fingerprint)
    {
        if (!string.Equals(existente.IdempotencyFingerprint, fingerprint, StringComparison.Ordinal))
            throw new BusinessRuleException("Idempotency-Key ya fue utilizada con un payload diferente.");
    }

    private static void ValidarFiltro(PedidoVentaFiltroDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CotizacionId is <= 0)
            throw new BusinessRuleException("CotizacionId debe ser mayor que cero.");
        if (request.ClienteId is <= 0)
            throw new BusinessRuleException("ClienteId debe ser mayor que cero.");
        if (request.Estado.HasValue && !Enum.IsDefined(request.Estado.Value))
            throw new BusinessRuleException("El estado de pedido no es válido.");
        ValidarFechaUtc(request.FechaDesdeUtc, nameof(request.FechaDesdeUtc));
        ValidarFechaUtc(request.FechaHastaUtc, nameof(request.FechaHastaUtc));
        if (request.FechaDesdeUtc.HasValue && request.FechaHastaUtc.HasValue && request.FechaDesdeUtc > request.FechaHastaUtc)
            throw new BusinessRuleException("FechaDesdeUtc no puede ser posterior a FechaHastaUtc.");
        if (!string.IsNullOrWhiteSpace(request.SortDirection) &&
            !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("SortDirection debe ser 'asc' o 'desc'.");
    }

    private static void ValidarFechaUtc(DateTime? fecha, string nombre)
    {
        if (fecha.HasValue && fecha.Value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException($"{nombre} debe estar expresada en UTC.");
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static PedidoVentaDto MapToDto(PedidoVenta pedido) => new()
    {
        Id = pedido.Id,
        CotizacionId = pedido.CotizacionId,
        ClienteId = pedido.ClienteId,
        ClienteNombreSnapshot = pedido.ClienteNombreSnapshot,
        ClienteDocumentoSnapshot = pedido.ClienteDocumentoSnapshot,
        Observaciones = pedido.Observaciones,
        Estado = pedido.Estado,
        Total = pedido.Total,
        FechaConfirmacionUtc = pedido.FechaConfirmacion,
        ConfirmadoPorUsuarioId = pedido.ConfirmadoPorUsuarioId,
        FechaAnulacionUtc = pedido.FechaAnulacion,
        AnuladoPorUsuarioId = pedido.AnuladoPorUsuarioId,
        MotivoAnulacion = pedido.MotivoAnulacion,
        Detalles = pedido.Detalles.Select(x => new PedidoVentaDetalleDto
        {
            Id = x.Id,
            ProductoId = x.ProductoId,
            ProductoVarianteId = x.ProductoVarianteId,
            ProductoSkuSnapshot = x.ProductoSkuSnapshot,
            ProductoNombreSnapshot = x.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = x.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = x.ProductoModeloSnapshot,
            ProductoColorSnapshot = x.ProductoColorSnapshot,
            ProductoTallaSnapshot = x.ProductoTallaSnapshot,
            Cantidad = x.Cantidad,
            PrecioUnitario = x.PrecioUnitario,
            Total = x.Total
        }).ToList()
    };
}
