using Solqaryn.Application.Common;

namespace Solqaryn.Application.DTOs;

/// <summary>
/// Envelope transversal mínimo para filtros de reportes analíticos de inventario.
/// Los filtros especializados añaden sólo semántica propia de su reporte.
/// </summary>
public class ReporteInventarioFiltroBaseDto : PagedRequest
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? SucursalId { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }

    public ReporteInventarioFiltroBaseDto()
    {
        SortBy = "Fecha";
        SortDirection = "desc";
    }
}
