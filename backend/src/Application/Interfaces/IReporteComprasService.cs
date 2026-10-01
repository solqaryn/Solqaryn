using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IReporteComprasService
{
    Task<PagedResult<ReporteComprasDetalleDto>> ObtenerDetallePaginadoAsync(ReporteComprasFiltroDto filtro, CancellationToken cancellationToken = default);
}
