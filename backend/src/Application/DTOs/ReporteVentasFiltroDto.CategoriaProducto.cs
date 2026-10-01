namespace Solqaryn.Application.DTOs;

/// <summary>
/// Facet for filtering sales reports by category and product dimension.
/// Note: Historical-category caveat applies. This filter only uses current category associations.
/// Do not claim category snapshots that do not exist.
/// </summary>
public partial class ReporteVentasFiltroDto
{
    public int? CategoriaId { get; set; }
    public int? ProductoId { get; set; }
}
