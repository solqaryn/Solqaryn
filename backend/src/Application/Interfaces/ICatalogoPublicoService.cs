using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface ICatalogoPublicoService
{
    Task<PagedResult<ProductoCatalogoPublicoDto>> BuscarAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken = default);

    Task<List<ProductoCatalogoPublicoDto>> ObtenerDestacadosAsync(
        int limite,
        CancellationToken cancellationToken = default);

    Task<ProductoCatalogoPublicoDto?> ObtenerDetalleAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<ProductoCatalogoPublicoDto>> ObtenerPorIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);
}
