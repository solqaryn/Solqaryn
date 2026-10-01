using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Documento empresarial que registra un ajuste controlado de existencias.
/// Su persistencia y efectos de stock se implementan en N0.7.C/D.
/// </summary>
public class AjusteInventario : ConfirmableEntity
{
    public string NumeroAjuste { get; set; } = string.Empty;
    public DateTime FechaAjuste { get; set; } = DateTime.UtcNow;
    public EstadoAjusteInventario Estado { get; private set; } = EstadoAjusteInventario.Borrador;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public ICollection<AjusteInventarioDetalle> Detalles { get; set; } = new List<AjusteInventarioDetalle>();

    public void Confirmar(int usuarioId, string nombreUsuario, DateTime fechaUtc)
    {
        if (Estado != EstadoAjusteInventario.Borrador)
            throw new InvalidOperationException("Solo un ajuste en borrador puede confirmarse.");
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario de confirmación debe ser válido.");
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El nombre del usuario de confirmación es obligatorio.", nameof(nombreUsuario));
        if (string.IsNullOrWhiteSpace(NumeroAjuste))
            throw new InvalidOperationException("El número de ajuste es obligatorio antes de confirmar.");
        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException("El motivo del ajuste es obligatorio antes de confirmar.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("El ajuste debe contener al menos un detalle.");
        if (Detalles.Any(x => !x.TieneSnapshotConfirmacion))
            throw new InvalidOperationException("Todos los detalles deben materializar sus snapshots bajo lock antes de confirmar.");

        Estado = EstadoAjusteInventario.Confirmado;
        FechaConfirmacion = fechaUtc;
        ConfirmadoPorUsuarioId = usuarioId;
        ConfirmadoPorNombreUsuario = nombreUsuario.Trim();
    }

    public void Anular(int usuarioId, string nombreUsuario, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoAjusteInventario.Confirmado)
            throw new InvalidOperationException("Solo un ajuste confirmado puede anularse.");
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario de anulación debe ser válido.");
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El nombre del usuario de anulación es obligatorio.", nameof(nombreUsuario));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        Estado = EstadoAjusteInventario.Anulado;
        FechaAnulacion = fechaUtc;
        AnuladoPorUsuarioId = usuarioId;
        AnuladoPorNombreUsuario = nombreUsuario.Trim();
        MotivoAnulacion = motivo.Trim();
    }
}
