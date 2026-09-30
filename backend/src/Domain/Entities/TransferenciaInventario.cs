using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class TransferenciaInventario : AuditableEntity
{
    public string Numero { get; set; } = string.Empty;

    public int AlmacenOrigenId { get; set; }
    public Almacen AlmacenOrigen { get; set; } = null!;
    public int AlmacenDestinoId { get; set; }
    public Almacen AlmacenDestino { get; set; } = null!;

    public EstadoTransferenciaInventario Estado { get; private set; } = EstadoTransferenciaInventario.Borrador;
    public string? Observaciones { get; set; }

    public DateTime? FechaSolicitud { get; private set; }
    public int? SolicitadaPorUsuarioId { get; private set; }
    public DateTime? FechaAprobacion { get; private set; }
    public int? AprobadaPorUsuarioId { get; private set; }
    public DateTime? FechaDespacho { get; private set; }
    public int? DespachadaPorUsuarioId { get; private set; }
    public DateTime? FechaRecepcion { get; private set; }
    public int? RecibidaPorUsuarioId { get; private set; }
    public DateTime? FechaCancelacion { get; private set; }
    public int? CanceladaPorUsuarioId { get; private set; }
    public string? MotivoCancelacion { get; private set; }

    public ICollection<TransferenciaInventarioDetalle> Detalles { get; set; } = new List<TransferenciaInventarioDetalle>();

    public void ValidarTopologia()
    {
        if (AlmacenOrigenId <= 0)
            throw new InvalidOperationException("El almacén de origen es obligatorio.");
        if (AlmacenDestinoId <= 0)
            throw new InvalidOperationException("El almacén de destino es obligatorio.");
        if (AlmacenOrigenId == AlmacenDestinoId)
            throw new InvalidOperationException("El almacén de origen y destino deben ser distintos.");
    }

    public void Solicitar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoTransferenciaInventario.Borrador)
            throw new InvalidOperationException("Solo una transferencia en borrador puede solicitarse.");
        ValidarUsuario(usuarioId);
        ValidarTopologia();
        ValidarDocumento();

        Estado = EstadoTransferenciaInventario.Solicitada;
        SolicitadaPorUsuarioId = usuarioId;
        FechaSolicitud = fechaUtc;
    }

    public void Aprobar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoTransferenciaInventario.Solicitada)
            throw new InvalidOperationException("Solo una transferencia solicitada puede aprobarse.");
        ValidarUsuario(usuarioId);
        if (Detalles.Any(x => x.CantidadAprobada <= 0))
            throw new InvalidOperationException("Todos los detalles deben tener cantidad aprobada antes de aprobar la transferencia.");

        Estado = EstadoTransferenciaInventario.Aprobada;
        AprobadaPorUsuarioId = usuarioId;
        FechaAprobacion = fechaUtc;
    }

    public void MarcarEnTransito(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoTransferenciaInventario.Aprobada)
            throw new InvalidOperationException("Solo una transferencia aprobada puede despacharse.");
        ValidarUsuario(usuarioId);
        if (Detalles.Any(x => x.CantidadDespachada <= 0))
            throw new InvalidOperationException("Todos los detalles deben registrar cantidad despachada antes de pasar a tránsito.");

        Estado = EstadoTransferenciaInventario.EnTransito;
        DespachadaPorUsuarioId = usuarioId;
        FechaDespacho = fechaUtc;
    }

    public void Recibir(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoTransferenciaInventario.EnTransito)
            throw new InvalidOperationException("Solo una transferencia en tránsito puede recibirse.");
        ValidarUsuario(usuarioId);
        if (Detalles.Any(x => !x.RecepcionCerrada))
            throw new InvalidOperationException("Todos los detalles deben cerrar su recepción antes de completar la transferencia.");

        Estado = EstadoTransferenciaInventario.Recibida;
        RecibidaPorUsuarioId = usuarioId;
        FechaRecepcion = fechaUtc;
    }

    public void Cancelar(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado is EstadoTransferenciaInventario.Recibida or EstadoTransferenciaInventario.Cancelada)
            throw new InvalidOperationException("Una transferencia recibida o cancelada no puede cancelarse nuevamente.");
        ValidarUsuario(usuarioId);
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(motivo));

        Estado = EstadoTransferenciaInventario.Cancelada;
        CanceladaPorUsuarioId = usuarioId;
        FechaCancelacion = fechaUtc;
        MotivoCancelacion = motivo.Trim();
    }

    private void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(Numero))
            throw new InvalidOperationException("El número de transferencia es obligatorio.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La transferencia debe contener al menos un detalle.");
        if (Detalles.Any(x => x.CantidadSolicitada <= 0))
            throw new InvalidOperationException("Todos los detalles deben tener cantidad solicitada válida.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }
}
