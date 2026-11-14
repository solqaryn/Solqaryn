using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>Modelo normalizado. Todo modelo pertenece a una marca.</summary>
public class Modelo : AuditableEntity
{
    public int MarcaId { get; set; }
    public Marca Marca { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
    public string? NombreMarcaActivoUnico { get; private set; }
}
