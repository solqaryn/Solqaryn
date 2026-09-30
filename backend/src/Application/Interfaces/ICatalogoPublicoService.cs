using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICatalogoPublicoService
{
    Task<PagedResult<TiendaProductoResumenDto>> BuscarAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken = default);

    Task<List<TiendaProductoResumenDto>> ObtenerDestacadosAsync(
        int limite,
        CancellationToken cancellationToken = default);

    Task<ProductoCatalogoPublicoDto?> ObtenerDetalleAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<ProductoCatalogoPublicoDto>> ObtenerPorIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);
}
