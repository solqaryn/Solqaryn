using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Casos de uso empresariales de reservas. El documento permanece editable en
/// Borrador; al activarlo se incrementa StockReservado sobre ExistenciaVariante
/// bajo lock pesimista. Liberar, expirar, cancelar una reserva activa o consumirla
/// retira exactamente la misma reserva sin usar cantidades legacy como autoridad.
/// Las mutaciones y su auditoría crítica se confirman dentro de la misma transacción.
/// </summary>
public sealed class ReservaInventarioService : IReservaInventarioService
{
    private readonly IReservaInventarioRepository _repository;
    private readonly IProductoVarianteRepository _variantes;
    private readonly IExistenciaVarianteConcurrencyService _existencias;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ReservaInventarioService(
        IReservaInventarioRepository repository,
        IProductoVarianteRepository variantes,
        IExistenciaVarianteConcurrencyService existencias,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _variantes = variantes ?? throw new ArgumentNullException(nameof(variantes));
        _existencias = existencias ?? throw new ArgumentNullException(nameof(existencias));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PagedResult<ReservaInventarioDto>> GetPagedAsync(ReservaInventarioQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (items, total) = await _repository.GetPagedAsync(query);
        return new PagedResult<ReservaInventarioDto>
        {
            Items = items.Select(Map).ToList(),
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 100),
            TotalCount = total
        };
    }

    public async Task<ReservaInventarioDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var reserva = await _repository.GetByIdAsync(id);
        return reserva is null ? null : Map(reserva);
    }

    public async Task<ReservaInventarioDto> CreateAsync(CreateReservaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var usuarioId = ObtenerUsuarioId();
        var ahora = DateTime.UtcNow;
        ValidarFechaExpiracionBorrador(dto.FechaExpiracion, ahora);
        var detalles = await ConstruirDetallesAsync(dto.Detalles, usuarioId);

        var reserva = new ReservaInventario
        {
            Numero = await GenerarNumeroAsync(ahora),
            VentaId = dto.VentaId,
            FechaExpiracion = dto.FechaExpiracion,
            FechaCreacion = ahora,
            FechaActualizacion = ahora,
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario,
            ActualizadoPorUsuarioId = usuarioId,
            ActualizadoPorNombreUsuario = _currentUser.NombreUsuario,
            Detalles = detalles
        };
        reserva.ValidarDocumento();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repository.AddAsync(reserva);
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Crear,
                reserva,
                "Reserva de inventario creada.");
        });

        return Map(await _repository.GetByIdAsync(reserva.Id) ?? reserva);
    }

    public async Task<ReservaInventarioDto> UpdateAsync(int id, UpdateReservaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) throw new BusinessRuleException("La reserva indicada no es válida.");
        var usuarioId = ObtenerUsuarioId();
        ValidarFechaExpiracionBorrador(dto.FechaExpiracion, DateTime.UtcNow);
        var detalles = await ConstruirDetallesAsync(dto.Detalles, usuarioId);
        ReservaInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var reserva = await _repository.GetByIdAsync(id, tracking: true)
                ?? throw new BusinessRuleException("La reserva indicada no existe.");
            if (reserva.Estado != EstadoReservaInventario.Borrador)
                throw new BusinessRuleException("Solo una reserva en borrador puede editarse.");

            reserva.FechaExpiracion = dto.FechaExpiracion;
            reserva.Detalles.Clear();
            foreach (var detalle in detalles)
                reserva.Detalles.Add(detalle);
            MarcarActualizacion(reserva, usuarioId);
            reserva.ValidarDocumento();
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Editar,
                reserva,
                "Reserva de inventario actualizada.");
            resultado = reserva;
        });

        return Map(await _repository.GetByIdAsync(id) ?? resultado!);
    }

    public Task<ReservaInventarioDto> ActivarAsync(int id) =>
        CambiarReservaAsync(
            id,
            EstadoReservaInventario.Activa,
            AccionPermiso.Confirmar,
            "Reserva activada correctamente.",
            async (reserva, usuarioId, ahora) =>
            {
                var lockSet = await BloquearReservaAsync(reserva, validarDisponible: true);
                foreach (var demanda in lockSet.Demandas)
                {
                    var existencia = lockSet.Existencias[demanda.Clave];
                    await _existencias.AjustarStockReservadoPesimistaAsync(
                        demanda.Clave,
                        existencia.StockReservado,
                        checked(existencia.StockReservado + demanda.Cantidad));
                }
                reserva.Activar(usuarioId, ahora);
            });

    public Task<ReservaInventarioDto> ConsumirAsync(int id) =>
        CambiarReservaAsync(
            id,
            EstadoReservaInventario.Consumida,
            AccionPermiso.Confirmar,
            "Reserva consumida correctamente.",
            async (reserva, usuarioId, ahora) =>
            {
                if (reserva.Estado != EstadoReservaInventario.Activa)
                    throw new BusinessRuleException("Solo una reserva activa puede consumirse.");

                foreach (var detalle in reserva.Detalles)
                {
                    if (!detalle.EstaConsumida)
                        detalle.RegistrarConsumo(detalle.CantidadReservada);
                }

                await RetirarStockReservadoAsync(reserva);
                reserva.Consumir(usuarioId, ahora);
            });

    public Task<ReservaInventarioDto> LiberarAsync(int id, LiberarReservaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return CambiarReservaAsync(
            id,
            EstadoReservaInventario.Liberada,
            AccionPermiso.Anular,
            "Reserva liberada.",
            async (reserva, usuarioId, ahora) =>
            {
                if (reserva.Estado != EstadoReservaInventario.Activa)
                    throw new BusinessRuleException("Solo una reserva activa puede liberarse.");
                await RetirarStockReservadoAsync(reserva);
                reserva.Liberar(usuarioId, dto.Motivo, ahora);
            },
            dto.Motivo);
    }

    public Task<ReservaInventarioDto> ExpirarAsync(int id) =>
        CambiarReservaAsync(
            id,
            EstadoReservaInventario.Expirada,
            AccionPermiso.CambiarEstado,
            "Reserva expirada correctamente.",
            async (reserva, usuarioId, ahora) =>
            {
                if (reserva.Estado != EstadoReservaInventario.Activa)
                    throw new BusinessRuleException("Solo una reserva activa puede expirar.");
                if (!reserva.FechaExpiracion.HasValue || ahora < reserva.FechaExpiracion.Value)
                    throw new BusinessRuleException("La reserva todavía no alcanzó su fecha de expiración.");
                await RetirarStockReservadoAsync(reserva);
                reserva.Expirar(usuarioId, ahora);
            });

    public Task<ReservaInventarioDto> CancelarAsync(int id, CancelarReservaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return CambiarReservaAsync(
            id,
            EstadoReservaInventario.Cancelada,
            AccionPermiso.Anular,
            "Reserva cancelada.",
            async (reserva, usuarioId, ahora) =>
            {
                if (reserva.Estado == EstadoReservaInventario.Activa)
                    await RetirarStockReservadoAsync(reserva);
                else if (reserva.Estado != EstadoReservaInventario.Borrador)
                    throw new BusinessRuleException("Solo una reserva en borrador o activa puede cancelarse.");
                reserva.Cancelar(usuarioId, dto.Motivo, ahora);
            },
            dto.Motivo);
    }

    private async Task<ReservaInventarioDto> CambiarReservaAsync(
        int id,
        EstadoReservaInventario estadoIdempotente,
        AccionPermiso accionAuditoria,
        string descripcionAuditoria,
        Func<ReservaInventario, int, DateTime, Task> operacion,
        string? motivoAuditoria = null)
    {
        if (id <= 0) throw new BusinessRuleException("La reserva indicada no es válida.");
        var usuarioId = ObtenerUsuarioId();
        ReservaInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var reserva = await _repository.GetByIdAsync(id, tracking: true)
                ?? throw new BusinessRuleException("La reserva indicada no existe.");
            if (reserva.Estado != estadoIdempotente)
            {
                await operacion(reserva, usuarioId, DateTime.UtcNow);
                MarcarActualizacion(reserva, usuarioId);
                await _repository.SaveChangesAsync();
            }

            await AuditarEstrictoAsync(
                accionAuditoria,
                reserva,
                descripcionAuditoria,
                motivoAuditoria);
            resultado = reserva;
        });

        return Map(await _repository.GetByIdAsync(id) ?? resultado!);
    }

    private async Task<InventarioExistenciaLockSet> BloquearReservaAsync(
        ReservaInventario reserva,
        bool validarDisponible)
    {
        reserva.ValidarDocumento();
        var demandas = reserva.Detalles.Select(detalle => new InventarioDemandaExistencia(
            detalle.ProductoVariante.ProductoId,
            detalle.ProductoVarianteId,
            detalle.AlmacenId,
            detalle.UbicacionAlmacenId,
            detalle.CantidadReservada));
        return await _existencias.BloquearYValidarExistenciasAsync(demandas, validarDisponible);
    }

    private async Task RetirarStockReservadoAsync(ReservaInventario reserva)
    {
        var lockSet = await BloquearReservaAsync(reserva, validarDisponible: false);
        foreach (var demanda in lockSet.Demandas)
        {
            var existencia = lockSet.Existencias[demanda.Clave];
            if (existencia.StockReservado < demanda.Cantidad)
            {
                throw new BusinessRuleException(
                    "El stock reservado autoritativo es menor que la reserva que se intenta retirar.");
            }

            await _existencias.AjustarStockReservadoPesimistaAsync(
                demanda.Clave,
                existencia.StockReservado,
                existencia.StockReservado - demanda.Cantidad);
        }
    }

    private async Task<List<ReservaInventarioDetalle>> ConstruirDetallesAsync(
        IReadOnlyCollection<ReservaInventarioDetalleInputDto>? inputs,
        int usuarioId)
    {
        if (inputs is null || inputs.Count == 0)
            throw new BusinessRuleException("La reserva debe contener al menos un detalle.");

        var duplicada = inputs.GroupBy(x => new { x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId })
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicada is not null)
            throw new BusinessRuleException("No puede repetirse la misma clave física dentro de una reserva.");

        var detalles = new List<ReservaInventarioDetalle>(inputs.Count);
        foreach (var input in inputs)
        {
            if (input.ProductoVarianteId <= 0 || input.AlmacenId <= 0 || input.Cantidad <= 0)
                throw new BusinessRuleException("Cada detalle requiere variante, almacén y cantidad válidos.");

            var variante = await _variantes.GetByIdAsync(input.ProductoVarianteId);
            if (variante is null || variante.Eliminado || !variante.Activo)
                throw new BusinessRuleException($"La variante {input.ProductoVarianteId} no existe o no está activa.");

            var detalle = new ReservaInventarioDetalle
            {
                ProductoVarianteId = variante.Id,
                ProductoVariante = variante,
                AlmacenId = input.AlmacenId,
                UbicacionAlmacenId = input.UbicacionAlmacenId,
                ProductoSkuSnapshot = variante.Sku,
                ProductoMarcaSnapshot = variante.Marca?.Nombre,
                ProductoModeloSnapshot = variante.Modelo?.Nombre,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoTallaSnapshot = variante.Talla?.Nombre,
                CreadoPorUsuarioId = usuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario,
                ActualizadoPorUsuarioId = usuarioId,
                ActualizadoPorNombreUsuario = _currentUser.NombreUsuario
            };
            detalle.EstablecerCantidadReservada(input.Cantidad);
            detalle.ValidarClaveFisica();
            detalles.Add(detalle);
        }

        return detalles;
    }

    private async Task<string> GenerarNumeroAsync(DateTime fechaUtc)
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var numero = $"RSV-{fechaUtc:yyyyMMddHHmmssfff}-{sufijo}";
            if (!await _repository.ExisteNumeroAsync(numero)) return numero;
        }
        throw new BusinessRuleException("No fue posible generar un número único de reserva.");
    }

    private int ObtenerUsuarioId()
    {
        if (!_currentUser.EstaAutenticado || !_currentUser.UsuarioId.HasValue || _currentUser.UsuarioId.Value <= 0)
            throw new BusinessRuleException("La operación requiere un usuario autenticado válido.");
        return _currentUser.UsuarioId.Value;
    }

    private void MarcarActualizacion(ReservaInventario reserva, int usuarioId)
    {
        reserva.ActualizadoPorUsuarioId = usuarioId;
        reserva.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        reserva.FechaActualizacion = DateTime.UtcNow;
    }

    private Task AuditarEstrictoAsync(
        AccionPermiso accion,
        ReservaInventario reserva,
        string descripcion,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            accion,
            descripcion,
            reserva.Id,
            entidad: nameof(ReservaInventario),
            valoresNuevos: new
            {
                reserva.Numero,
                reserva.VentaId,
                Estado = reserva.Estado.ToString(),
                reserva.FechaExpiracion,
                reserva.FechaActivacion,
                reserva.FechaConsumo,
                reserva.FechaLiberacion,
                reserva.FechaExpiracionAplicada,
                reserva.FechaCancelacion,
                reserva.ActualizadoPorUsuarioId,
                reserva.ActualizadoPorNombreUsuario,
                Detalles = reserva.Detalles.Select(d => new
                {
                    d.ProductoVarianteId,
                    d.AlmacenId,
                    d.UbicacionAlmacenId,
                    d.CantidadReservada,
                    d.CantidadConsumida
                }).ToArray()
            },
            motivo: motivo);

    private static void ValidarFechaExpiracionBorrador(DateTime? fechaExpiracion, DateTime ahora)
    {
        if (fechaExpiracion.HasValue && fechaExpiracion.Value <= ahora)
            throw new BusinessRuleException("La fecha de expiración debe ser futura.");
    }

    private static ReservaInventarioDto Map(ReservaInventario reserva) => new()
    {
        Id = reserva.Id,
        Numero = reserva.Numero,
        VentaId = reserva.VentaId,
        Estado = reserva.Estado.ToString(),
        FechaExpiracion = reserva.FechaExpiracion,
        FechaCreacion = reserva.FechaCreacion,
        FechaActivacion = reserva.FechaActivacion,
        FechaConsumo = reserva.FechaConsumo,
        FechaLiberacion = reserva.FechaLiberacion,
        FechaExpiracionAplicada = reserva.FechaExpiracionAplicada,
        FechaCancelacion = reserva.FechaCancelacion,
        MotivoLiberacion = reserva.MotivoLiberacion,
        MotivoCancelacion = reserva.MotivoCancelacion,
        Detalles = reserva.Detalles.OrderBy(x => x.Id).Select(x => new ReservaInventarioDetalleDto
        {
            Id = x.Id,
            ProductoVarianteId = x.ProductoVarianteId,
            AlmacenId = x.AlmacenId,
            UbicacionAlmacenId = x.UbicacionAlmacenId,
            CantidadReservada = x.CantidadReservada,
            CantidadConsumida = x.CantidadConsumida,
            ProductoSku = x.ProductoSkuSnapshot,
            ProductoMarca = x.ProductoMarcaSnapshot,
            ProductoModelo = x.ProductoModeloSnapshot,
            ProductoColor = x.ProductoColorSnapshot,
            ProductoTalla = x.ProductoTallaSnapshot
        }).ToList()
    };
}
