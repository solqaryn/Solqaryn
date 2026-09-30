using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IOrdenCompraRepository
{
    Task<(IReadOnlyList<OrdenCompra> Items, int Total)> GetPagedAsync(OrdenCompraFiltroDto filtro);
    Task<OrdenCompra?> GetByIdAsync(int id, bool tracking = false);
    Task<OrdenCompra?> GetByIdForUpdateAsync(int id);
    Task<OrdenCompra?> GetByIdempotencyKeyAsync(string idempotencyKey, bool tracking = false);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task<string?> GetUltimoNumeroAsync(string prefijo);
    Task AddAsync(OrdenCompra orden);
    Task SaveChangesAsync();
}
