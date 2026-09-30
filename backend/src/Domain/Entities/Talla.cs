using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>Talla o tamaño comercial normalizado e independiente.</summary>
public class Talla : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
    public string? NombreActivoUnico { get; private set; }
}
