namespace Solqaryn.Domain.Common;

/// Entidad con auditoría básica de creación/actualización por usuario.
public abstract class AuditableEntity : BaseEntity
{
    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }
}
