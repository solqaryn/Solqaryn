using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class CotizacionFiltroDto : PagedRequest
{
    public int? ClienteId { get; set; }
    public EstadoCotizacion? Estado { get; set; }
    public DateTime? FechaDesdeUtc { get; set; }
    public DateTime? FechaHastaUtc { get; set; }
}
