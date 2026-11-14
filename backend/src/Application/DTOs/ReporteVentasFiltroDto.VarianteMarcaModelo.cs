namespace Solqaryn.Application.DTOs;

/// <summary>
/// Facet for filtering sales reports by variant, brand, and model dimension.
/// Uses current master-identity associations only; historical snapshot/display semantics remain distinct.
/// </summary>
public partial class ReporteVentasFiltroDto
{
    public int? VarianteId { get; set; }
    public int? MarcaId { get; set; }
    public int? ModeloId { get; set; }
}
