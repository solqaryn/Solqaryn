using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IProductoCatalogoPublicoRepository
{
    Task<(List<ProductoCatalogoResumenReadModel> Items, int TotalCount)> GetPagedSummaryAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken = default);

    Task<List<ProductoCatalogoResumenReadModel>> GetSummariesByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);

    Task<(List<ProductoCatalogoReadModel> Items, int TotalCount)> GetPagedAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetOrderedIdsAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductoCatalogoReadModel?> GetByIdAsync(
        int id,
        bool includeGalleries,
        CancellationToken cancellationToken = default);

    Task<List<ProductoCatalogoReadModel>> GetByIdsAsync(
        IEnumerable<int> ids,
        bool includeGalleries,
        CancellationToken cancellationToken = default);
}
