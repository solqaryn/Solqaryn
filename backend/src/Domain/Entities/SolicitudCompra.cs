using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class SolicitudCompra : AuditableEntity
{
    public string NumeroSolicitud { get; set; } = string.Empty;
    public EstadoSolicitudCompra Estado { get; private set; } = EstadoSolicitudCompra.Borrador;

    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public string? Notas { get; set; }

    public DateTime? FechaSolicitudUtc { get; private set; }
    public int? SolicitadaPorUsuarioId { get; private set; }
    public string? SolicitadaPorNombreSnapshot { get; private set; }

    public DateTime? FechaDecisionUtc { get; private set; }
    public int? DecididaPorUsuarioId { get; private set; }
    public string? DecididaPorNombreSnapshot { get; private set; }
    public string? MotivoRechazo { get; private set; }

    public ICollection<SolicitudCompraDetalle> Detalles { get; set; } = new List<SolicitudCompraDetalle>();

    public bool EsEditable => Estado == EstadoSolicitudCompra.Borrador;
    public bool EsTerminal => Estado is EstadoSolicitudCompra.Aprobada or EstadoSolicitudCompra.Rechazada;

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una solicitud de compra en borrador puede modificarse.");
    }

    public void Solicitar(int usuarioId, string? usuarioNombre, DateTime fechaUtc)
    {
        if (Estado != EstadoSolicitudCompra.Borrador)
            throw new InvalidOperationException("Solo una solicitud en borrador puede enviarse a aprobación.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();

        Estado = EstadoSolicitudCompra.Solicitada;
        FechaSolicitudUtc = fechaUtc;
        SolicitadaPorUsuarioId = usuarioId;
        SolicitadaPorNombreSnapshot = NormalizarSnapshot(usuarioNombre);
    }

    public void Aprobar(int usuarioId, string? usuarioNombre, DateTime fechaUtc)
    {
        if (Estado != EstadoSolicitudCompra.Solicitada)
            throw new InvalidOperationException("Solo una solicitud enviada puede aprobarse.");

        ValidarUsuario(usuarioId);

        Estado = EstadoSolicitudCompra.Aprobada;
        FechaDecisionUtc = fechaUtc;
        DecididaPorUsuarioId = usuarioId;
        DecididaPorNombreSnapshot = NormalizarSnapshot(usuarioNombre);
        MotivoRechazo = null;
    }

    public void Rechazar(int usuarioId, string? usuarioNombre, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoSolicitudCompra.Solicitada)
            throw new InvalidOperationException("Solo una solicitud enviada puede rechazarse.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de rechazo es obligatorio.", nameof(motivo));

        ValidarUsuario(usuarioId);

        Estado = EstadoSolicitudCompra.Rechazada;
        FechaDecisionUtc = fechaUtc;
        DecididaPorUsuarioId = usuarioId;
        DecididaPorNombreSnapshot = NormalizarSnapshot(usuarioNombre);
        MotivoRechazo = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(NumeroSolicitud))
            throw new InvalidOperationException("El número de solicitud es obligatorio.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La solicitud de compra debe contener al menos un detalle.");

        foreach (var detalle in Detalles)
            detalle.Validar();
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static string? NormalizarSnapshot(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
