using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class RevisionFinanciera
{
    public int Id { get; set; }
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }

    public int RevisadoPorUsuarioId { get; set; }
    public string RevisadoPorNombreUsuario { get; set; } = string.Empty;
    public DateTime FechaRevision { get; set; } = DateTime.UtcNow;

    public EstadoRevisionFinanciera EstadoRevision { get; set; } = EstadoRevisionFinanciera.Revisado;
    public string? Observaciones { get; set; }
}
