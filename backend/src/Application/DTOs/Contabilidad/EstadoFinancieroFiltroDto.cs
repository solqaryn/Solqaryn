namespace Solqaryn.Application.DTOs.Contabilidad;

public sealed class EstadoFinancieroFiltroDto
{
    public int? PeriodoContableId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}
