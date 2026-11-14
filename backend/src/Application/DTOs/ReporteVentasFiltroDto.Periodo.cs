using Solqaryn.Application.Common;

namespace Solqaryn.Application.DTOs;

/// <summary>
/// Domain contract for the period/time-range part of the sales-report filter (N5.3.B).
/// Defines explicit Desde/Hasta date-time filter semantics suitable for server-side validation.
/// </summary>
public partial class ReporteVentasFiltroDto : PagedRequest
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
