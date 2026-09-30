using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Trazabilidad logística de la preparación física de un PedidoVenta ya confirmado.
/// No es autoridad de stock ni documento comercial: esas responsabilidades permanecen
/// en PedidoVenta, ReservaInventario y ExistenciaVariante.
/// </summary>
public class PreparacionPedidoVenta : AuditableEntity
{
    private readonly List<PreparacionPedidoVentaDetalle> _detalles = new();

    public int PedidoVentaId { get; private set; }
    public PedidoVenta PedidoVenta { get; private set; } = null!;

    public int ReservaInventarioId { get; private set; }
    public ReservaInventario ReservaInventario { get; private set; } = null!;

    public EstadoPreparacionPedidoVenta Estado { get; private set; } = EstadoPreparacionPedidoVenta.PendientePicking;
    public IReadOnlyCollection<PreparacionPedidoVentaDetalle> Detalles => _detalles.AsReadOnly();

    public DateTime? FechaPickingCompletadoUtc { get; private set; }
    public DateTime? FechaPackingCompletadoUtc { get; private set; }
    public DateTime? FechaDespachoUtc { get; private set; }
    public DateTime? FechaEntregaUtc { get; private set; }
    public DateTime? FechaCancelacionUtc { get; private set; }
    public int? UltimoUsuarioId { get; private set; }
    public string? MotivoCancelacion { get; private set; }

    public static PreparacionPedidoVenta Crear(PedidoVenta pedido, ReservaInventario reserva)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        ArgumentNullException.ThrowIfNull(reserva);

        if (pedido.Id <= 0)
            throw new InvalidOperationException("El pedido debe estar persistido antes de iniciar su preparación.");
        if (pedido.Estado != EstadoPedidoVenta.Confirmado)
            throw new InvalidOperationException("Solo un pedido confirmado puede iniciar preparación.");
        if (reserva.Id <= 0)
            throw new InvalidOperationException("La reserva debe estar persistida antes de iniciar preparación.");
        if (reserva.PedidoVentaId != pedido.Id)
            throw new InvalidOperationException("La reserva debe pertenecer al mismo pedido que se prepara.");
        if (reserva.Estado != EstadoReservaInventario.Activa)
            throw new InvalidOperationException("La preparación exige una reserva de inventario activa.");

        reserva.ValidarDocumento();
        ValidarConsistenciaPedidoReserva(pedido, reserva);

        var preparacion = new PreparacionPedidoVenta
        {
            PedidoVentaId = pedido.Id,
            PedidoVenta = pedido,
            ReservaInventarioId = reserva.Id,
            ReservaInventario = reserva
        };

        foreach (var detalleReserva in reserva.Detalles)
            preparacion._detalles.Add(PreparacionPedidoVentaDetalle.CrearDesdeReserva(detalleReserva));

        preparacion.ValidarDocumento();
        return preparacion;
    }

    public void CompletarPicking(int usuarioId, DateTime fechaUtc)
    {
        ExigirEstado(EstadoPreparacionPedidoVenta.PendientePicking, "Solo una preparación pendiente puede completar picking.");
        RegistrarTransicion(usuarioId, fechaUtc);
        Estado = EstadoPreparacionPedidoVenta.PickingCompletado;
        FechaPickingCompletadoUtc = fechaUtc;
    }

    public void CompletarPacking(int usuarioId, DateTime fechaUtc)
    {
        ExigirEstado(EstadoPreparacionPedidoVenta.PickingCompletado, "Solo una preparación con picking completo puede completar packing.");
        RegistrarTransicion(usuarioId, fechaUtc);
        Estado = EstadoPreparacionPedidoVenta.PackingCompletado;
        FechaPackingCompletadoUtc = fechaUtc;
    }

    public void MarcarDespachado(int usuarioId, DateTime fechaUtc)
    {
        ExigirEstado(EstadoPreparacionPedidoVenta.PackingCompletado, "Solo una preparación empacada puede marcarse despachada.");
        RegistrarTransicion(usuarioId, fechaUtc);
        Estado = EstadoPreparacionPedidoVenta.Despachado;
        FechaDespachoUtc = fechaUtc;
    }

    public void MarcarEntregado(int usuarioId, DateTime fechaUtc)
    {
        ExigirEstado(EstadoPreparacionPedidoVenta.Despachado, "Solo una preparación despachada puede marcarse entregada.");
        RegistrarTransicion(usuarioId, fechaUtc);
        Estado = EstadoPreparacionPedidoVenta.Entregado;
        FechaEntregaUtc = fechaUtc;
    }

    public void Cancelar(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado is EstadoPreparacionPedidoVenta.Despachado or EstadoPreparacionPedidoVenta.Entregado or EstadoPreparacionPedidoVenta.Cancelado)
            throw new InvalidOperationException("La preparación solo puede cancelarse antes del despacho.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(motivo));

        RegistrarTransicion(usuarioId, fechaUtc);
        Estado = EstadoPreparacionPedidoVenta.Cancelado;
        FechaCancelacionUtc = fechaUtc;
        MotivoCancelacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (PedidoVentaId <= 0 || ReservaInventarioId <= 0)
            throw new InvalidOperationException("Pedido y reserva son obligatorios para la preparación.");
        if (_detalles.Count == 0)
            throw new InvalidOperationException("La preparación debe contener al menos un detalle físico.");

        foreach (var detalle in _detalles)
            detalle.Validar();

        var duplicada = _detalles
            .GroupBy(x => new { x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId })
            .Any(x => x.Count() > 1);

        if (duplicada)
            throw new InvalidOperationException("La preparación no puede repetir la misma clave física reservada.");
    }

    private static void ValidarConsistenciaPedidoReserva(PedidoVenta pedido, ReservaInventario reserva)
    {
        var requeridas = pedido.Detalles
            .GroupBy(x => x.ProductoVarianteId ?? 0)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Cantidad));

        if (requeridas.Keys.Any(x => x <= 0))
            throw new InvalidOperationException("La preparación logística requiere variantes físicas explícitas en todas las líneas del pedido.");

        var reservadas = reserva.Detalles
            .GroupBy(x => x.ProductoVarianteId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.CantidadReservada));

        if (requeridas.Count != reservadas.Count ||
            requeridas.Any(x => !reservadas.TryGetValue(x.Key, out var cantidad) || cantidad != x.Value))
        {
            throw new InvalidOperationException("La reserva activa debe cubrir exactamente las cantidades por variante del pedido.");
        }
    }

    private void ExigirEstado(EstadoPreparacionPedidoVenta esperado, string mensaje)
    {
        if (Estado != esperado)
            throw new InvalidOperationException(mensaje);
    }

    private void RegistrarTransicion(int usuarioId, DateTime fechaUtc)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
        if (fechaUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(fechaUtc));

        UltimoUsuarioId = usuarioId;
    }
}

public class PreparacionPedidoVentaDetalle : AuditableEntity
{
    public int PreparacionPedidoVentaId { get; private set; }
    public PreparacionPedidoVenta PreparacionPedidoVenta { get; private set; } = null!;

    public int ProductoVarianteId { get; private set; }
    public int AlmacenId { get; private set; }
    public int? UbicacionAlmacenId { get; private set; }
    public int CantidadPreparar { get; private set; }

    public string? ProductoSkuSnapshot { get; private set; }
    public string? ProductoMarcaSnapshot { get; private set; }
    public string? ProductoModeloSnapshot { get; private set; }
    public string? ProductoColorSnapshot { get; private set; }
    public string? ProductoTallaSnapshot { get; private set; }

    internal static PreparacionPedidoVentaDetalle CrearDesdeReserva(ReservaInventarioDetalle reserva)
    {
        ArgumentNullException.ThrowIfNull(reserva);
        reserva.ValidarClaveFisica();
        if (reserva.CantidadReservada <= 0)
            throw new InvalidOperationException("La línea reservada debe tener cantidad positiva.");

        return new PreparacionPedidoVentaDetalle
        {
            ProductoVarianteId = reserva.ProductoVarianteId,
            AlmacenId = reserva.AlmacenId,
            UbicacionAlmacenId = reserva.UbicacionAlmacenId,
            CantidadPreparar = reserva.CantidadReservada,
            ProductoSkuSnapshot = reserva.ProductoSkuSnapshot,
            ProductoMarcaSnapshot = reserva.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = reserva.ProductoModeloSnapshot,
            ProductoColorSnapshot = reserva.ProductoColorSnapshot,
            ProductoTallaSnapshot = reserva.ProductoTallaSnapshot
        };
    }

    public void Validar()
    {
        if (ProductoVarianteId <= 0)
            throw new InvalidOperationException("La variante es obligatoria en el detalle de preparación.");
        if (AlmacenId <= 0)
            throw new InvalidOperationException("El almacén es obligatorio en el detalle de preparación.");
        if (UbicacionAlmacenId is <= 0)
            throw new InvalidOperationException("La ubicación debe ser válida cuando se especifica.");
        if (CantidadPreparar <= 0)
            throw new InvalidOperationException("La cantidad a preparar debe ser mayor que cero.");
    }
}
