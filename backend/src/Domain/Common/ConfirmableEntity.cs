namespace Solqaryn.Domain.Common;

/// Entidad de flujo documental: Borrador -> Confirmada -> Anulada (Compras, Ventas).
public abstract class ConfirmableEntity : AuditableEntity
{
    public int? ConfirmadoPorUsuarioId { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }
    public DateTime? FechaConfirmacion { get; set; }

    public int? AnuladoPorUsuarioId { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
}
