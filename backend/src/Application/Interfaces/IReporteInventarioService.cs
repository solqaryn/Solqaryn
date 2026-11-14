using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IReporteInventarioService
{
    Task<PagedResult<ReporteInventarioReconciliacionDto>> ObtenerReporteReconciliacionAsync(
        ReporteInventarioReconciliacionFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReporteInventarioStockHealthDto>> ObtenerStockHealthAsync(
        ReporteInventarioStockHealthFiltroDto filtro,
        CancellationToken cancellationToken = default);
}
