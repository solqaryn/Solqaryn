using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class PreparacionPedidoVentaService : IPreparacionPedidoVentaService
{
    private readonly IPreparacionPedidoVentaRepository _preparaciones;
    private readonly IPedidoVentaRepository _pedidos;
    private readonly IReservaInventarioRepository _reservas;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public PreparacionPedidoVentaService(
        IPreparacionPedidoVentaRepository preparaciones,
        IPedidoVentaRepository pedidos,
        IReservaInventarioRepository reservas,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _preparaciones = preparaciones ?? throw new ArgumentNullException(nameof(preparaciones));
        _pedidos = pedidos ?? throw new ArgumentNullException(nameof(pedidos));
        _reservas = reservas ?? throw new ArgumentNullException(nameof(reservas));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PreparacionPedidoVentaDto> GetByIdAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de preparación debe ser mayor que cero.");
        var entity = await _preparaciones.GetByIdAsync(id, asNoTracking: true)
            ?? throw new ResourceNotFoundException($"Preparación con Id {id} no encontrada.");
        return Map(entity);
    }

    public async Task<PreparacionPedidoVentaDto> GetByPedidoVentaIdAsync(int pedidoVentaId)
    {
        if (pedidoVentaId <= 0) throw new BusinessRuleException("PedidoVentaId debe ser mayor que cero.");
        var entity = await _preparaciones.GetByPedidoVentaIdAsync(pedidoVentaId, asNoTracking: true)
            ?? throw new ResourceNotFoundException($"No existe preparación para el pedido {pedidoVentaId}.");
        return Map(entity);
    }

    public async Task<PreparacionPedidoVentaDto> IniciarAsync(int pedidoVentaId)
    {
        if (pedidoVentaId <= 0) throw new BusinessRuleException("PedidoVentaId debe ser mayor que cero.");
        var (usuarioId, _) = RequerirUsuario();
        var preparacionId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pedido = await _pedidos.GetByIdForUpdateAsync(pedidoVentaId)
                ?? throw new ResourceNotFoundException($"Pedido de venta con Id {pedidoVentaId} no encontrado.");
            var existente = await _preparaciones.GetByPedidoVentaIdForUpdateAsync(pedidoVentaId);
            if (existente is not null)
            {
                preparacionId = existente.Id;
                return;
            }

            var reserva = await _reservas.GetByPedidoVentaIdAsync(pedidoVentaId, tracking: true)
                ?? throw new BusinessRuleException("El pedido necesita una reserva automática activa antes de iniciar preparación.");

            var preparacion = PreparacionPedidoVenta.Crear(pedido, reserva);
            preparacion.CreadoPorUsuarioId = usuarioId;
            preparacion.CreadoPorNombreUsuario = _currentUser.NombreUsuario;
            preparacion.ActualizadoPorUsuarioId = usuarioId;
            preparacion.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            foreach (var detalle in preparacion.Detalles)
            {
                detalle.CreadoPorUsuarioId = usuarioId;
                detalle.CreadoPorNombreUsuario = _currentUser.NombreUsuario;
                detalle.ActualizadoPorUsuarioId = usuarioId;
                detalle.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            }

            await _preparaciones.AddAsync(preparacion);
            await _preparaciones.SaveChangesAsync();
            await AuditarAsync(AccionPermiso.Crear, "Preparación logística iniciada.", preparacion);
            preparacionId = preparacion.Id;
        });

        return await GetByIdAsync(preparacionId);
    }

    public Task<PreparacionPedidoVentaDto> CompletarPickingAsync(int id) =>
        TransicionarAsync(id, AccionPermiso.Editar, "Picking completado.", (x, u, f) => x.CompletarPicking(u, f));

    public Task<PreparacionPedidoVentaDto> CompletarPackingAsync(int id) =>
        TransicionarAsync(id, AccionPermiso.Editar, "Packing completado.", (x, u, f) => x.CompletarPacking(u, f));

    public Task<PreparacionPedidoVentaDto> MarcarDespachadoAsync(int id) =>
        TransicionarAsync(id, AccionPermiso.Confirmar, "Preparación marcada como despachada.", (x, u, f) => x.MarcarDespachado(u, f));

    public Task<PreparacionPedidoVentaDto> MarcarEntregadoAsync(int id) =>
        TransicionarAsync(id, AccionPermiso.Confirmar, "Preparación marcada como entregada.", (x, u, f) => x.MarcarEntregado(u, f));

    public async Task<PreparacionPedidoVentaDto> CancelarAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new BusinessRuleException("El motivo de cancelación es obligatorio.");
        return await TransicionarAsync(id, AccionPermiso.Anular, "Preparación logística cancelada.",
            (x, u, f) => x.Cancelar(u, motivo.Trim(), f), motivo.Trim());
    }

    private async Task<PreparacionPedidoVentaDto> TransicionarAsync(
        int id,
        AccionPermiso accion,
        string descripcion,
        Action<PreparacionPedidoVenta, int, DateTime> mutacion,
        string? motivo = null)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de preparación debe ser mayor que cero.");
        var (usuarioId, _) = RequerirUsuario();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _preparaciones.GetByIdForUpdateAsync(id)
                ?? throw new ResourceNotFoundException($"Preparación con Id {id} no encontrada.");
            var estadoAnterior = entity.Estado;
            mutacion(entity, usuarioId, DateTime.UtcNow);
            entity.ActualizadoPorUsuarioId = usuarioId;
            entity.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            _preparaciones.Update(entity);
            await _preparaciones.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                accion,
                descripcion,
                entity.Id,
                nameof(PreparacionPedidoVenta),
                valoresAnteriores: new { Estado = estadoAnterior },
                valoresNuevos: new { entity.Estado, entity.PedidoVentaId, entity.ReservaInventarioId },
                motivo: motivo);
        });

        return await GetByIdAsync(id);
    }

    private Task AuditarAsync(AccionPermiso accion, string descripcion, PreparacionPedidoVenta entity) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Ventas,
            accion,
            descripcion,
            entity.Id,
            nameof(PreparacionPedidoVenta),
            valoresNuevos: new { entity.PedidoVentaId, entity.ReservaInventarioId, entity.Estado });

    private (int UsuarioId, string Nombre) RequerirUsuario()
    {
        if (_currentUser.UsuarioId is not > 0)
            throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");
        var nombre = _currentUser.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) nombre = _currentUser.NombreUsuario?.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) throw new ForbiddenAccessException("No se pudo resolver la identidad del usuario autenticado.");
        return (_currentUser.UsuarioId.Value, nombre);
    }

    private static PreparacionPedidoVentaDto Map(PreparacionPedidoVenta entity) => new()
    {
        Id = entity.Id,
        PedidoVentaId = entity.PedidoVentaId,
        ReservaInventarioId = entity.ReservaInventarioId,
        Estado = entity.Estado,
        FechaPickingCompletadoUtc = entity.FechaPickingCompletadoUtc,
        FechaPackingCompletadoUtc = entity.FechaPackingCompletadoUtc,
        FechaDespachoUtc = entity.FechaDespachoUtc,
        FechaEntregaUtc = entity.FechaEntregaUtc,
        FechaCancelacionUtc = entity.FechaCancelacionUtc,
        MotivoCancelacion = entity.MotivoCancelacion,
        Detalles = entity.Detalles.Select(d => new PreparacionPedidoVentaDetalleDto
        {
            Id = d.Id,
            ProductoVarianteId = d.ProductoVarianteId,
            AlmacenId = d.AlmacenId,
            UbicacionAlmacenId = d.UbicacionAlmacenId,
            CantidadPreparar = d.CantidadPreparar,
            ProductoSkuSnapshot = d.ProductoSkuSnapshot,
            ProductoMarcaSnapshot = d.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = d.ProductoModeloSnapshot,
            ProductoColorSnapshot = d.ProductoColorSnapshot,
            ProductoTallaSnapshot = d.ProductoTallaSnapshot
        }).ToArray()
    };
}
