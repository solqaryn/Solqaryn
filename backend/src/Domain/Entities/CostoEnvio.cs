using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class CostoEnvio : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Departamento { get; set; }
    public string? Ciudad { get; set; }
    public string? Zona { get; set; }
    public string? Modalidad { get; set; }
    public decimal Monto { get; set; }
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public int Prioridad { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
    public string? PredeterminadoActivoUnico { get; private set; }

    public bool EstaVigente(DateTime fechaUtc) =>
        Activo &&
        !Eliminado &&
        (!VigenteDesde.HasValue || VigenteDesde.Value <= fechaUtc) &&
        (!VigenteHasta.HasValue || VigenteHasta.Value >= fechaUtc);
}
