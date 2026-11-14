using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class ReservaInventario : AuditableEntity
{
    public string Numero { get; set; } = string.Empty;

    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int? PedidoVentaId { get; set; }
    public PedidoVenta? PedidoVenta { get; set; }

    public EstadoReservaInventario Estado { get; private set; } = EstadoReservaInventario.Borrador;
    public DateTime? FechaExpiracion { get; set; }

    public DateTime? FechaActivacion { get; private set; }
    public int? ActivadaPorUsuarioId { get; private set; }
    public DateTime? FechaConsumo { get; private set; }
    public int? ConsumidaPorUsuarioId { get; private set; }
    public DateTime? FechaLiberacion { get; private set; }
    public int? LiberadaPorUsuarioId { get; private set; }
    public string? MotivoLiberacion { get; private set; }
    public DateTime? FechaExpiracionAplicada { get; private set; }
    public int? ExpiradaPorUsuarioId { get; private set; }
    public DateTime? FechaCancelacion { get; private set; }
    public int? CanceladaPorUsuarioId { get; private set; }
    public string? MotivoCancelacion { get; private set; }

    public ICollection<ReservaInventarioDetalle> Detalles { get; set; } = new List<ReservaInventarioDetalle>();

    public void Activar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoReservaInventario.Borrador)
            throw new InvalidOperationException("Solo una reserva en borrador puede activarse.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();

        if (FechaExpiracion.HasValue && FechaExpiracion.Value <= fechaUtc)
            throw new InvalidOperationException("No puede activarse una reserva cuya expiración ya ocurrió.");

        Estado = EstadoReservaInventario.Activa;
        ActivadaPorUsuarioId = usuarioId;
        FechaActivacion = fechaUtc;
    }

    public void Consumir(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoReservaInventario.Activa)
            throw new InvalidOperationException("Solo una reserva activa puede consumirse.");
        ValidarUsuario(usuarioId);
        if (Detalles.Count == 0 || Detalles.Any(x => !x.EstaConsumida))
            throw new InvalidOperationException("Todos los detalles deben registrar su consumo antes de completar la reserva.");

        Estado = EstadoReservaInventario.Consumida;
        ConsumidaPorUsuarioId = usuarioId;
        FechaConsumo = fechaUtc;
    }

    public void Liberar(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoReservaInventario.Activa)
            throw new InvalidOperationException("Solo una reserva activa puede liberarse.");
        ValidarUsuario(usuarioId);
        ValidarMotivo(motivo, nameof(motivo));

        Estado = EstadoReservaInventario.Liberada;
        LiberadaPorUsuarioId = usuarioId;
        FechaLiberacion = fechaUtc;
        MotivoLiberacion = motivo.Trim();
    }

    public void Expirar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoReservaInventario.Activa)
            throw new InvalidOperationException("Solo una reserva activa puede expirar.");
        ValidarUsuario(usuarioId);
        if (!FechaExpiracion.HasValue)
            throw new InvalidOperationException("La reserva no tiene una fecha de expiración configurada.");
        if (fechaUtc < FechaExpiracion.Value)
            throw new InvalidOperationException("La reserva todavía no alcanzó su fecha de expiración.");

        Estado = EstadoReservaInventario.Expirada;
        ExpiradaPorUsuarioId = usuarioId;
        FechaExpiracionAplicada = fechaUtc;
    }

    public void Cancelar(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado is not (EstadoReservaInventario.Borrador or EstadoReservaInventario.Activa))
            throw new InvalidOperationException("Solo una reserva en borrador o activa puede cancelarse.");
        ValidarUsuario(usuarioId);
        ValidarMotivo(motivo, nameof(motivo));

        Estado = EstadoReservaInventario.Cancelada;
        CanceladaPorUsuarioId = usuarioId;
        FechaCancelacion = fechaUtc;
        MotivoCancelacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(Numero))
            throw new InvalidOperationException("El número de reserva es obligatorio.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La reserva debe contener al menos un detalle.");

        foreach (var detalle in Detalles)
        {
            detalle.ValidarClaveFisica();
            if (detalle.CantidadReservada <= 0)
                throw new InvalidOperationException("Todos los detalles deben tener una cantidad reservada válida.");
        }

        var duplicada = Detalles
            .GroupBy(x => new { x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId })
            .Any(x => x.Count() > 1);

        if (duplicada)
            throw new InvalidOperationException("Una reserva no puede repetir la misma clave física de inventario.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static void ValidarMotivo(string motivo, string parametro)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo es obligatorio.", parametro);
    }
}
