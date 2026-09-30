using Solqaryn.Application.Common;
using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.DTOs.Contabilidad;

public sealed class PeriodoContableQueryDto : PagedRequest
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public EstadoPeriodoContable? Estado { get; set; }
}
