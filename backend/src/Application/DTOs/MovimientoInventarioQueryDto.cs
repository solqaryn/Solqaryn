using Solqaryn.Application.Common;

namespace Solqaryn.Application.DTOs;

public sealed class MovimientoInventarioQueryDto : PagedRequest
{
    public MovimientoInventarioQueryDto()
    {
        SortBy = "Fecha";
        SortDirection = "desc";
    }

    public int? ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public string? Tipo { get; set; }
    public string? Causa { get; set; }
    public string? CorrelationId { get; set; }
    public string? OrigenTipo { get; set; }
    public int? OrigenId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
