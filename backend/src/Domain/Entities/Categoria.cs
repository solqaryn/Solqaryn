using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class Categoria : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
    public bool Eliminada { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadaPorUsuarioId { get; set; }
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
