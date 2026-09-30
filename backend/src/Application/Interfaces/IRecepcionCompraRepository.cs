using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IRecepcionCompraRepository
{
    Task<(IReadOnlyList<RecepcionCompra> Items, int Total)> GetPagedAsync(RecepcionCompraQueryDto filtro);
    Task<RecepcionCompra?> GetByIdAsync(int id, bool tracking = false);
    Task<RecepcionCompra?> GetByIdForUpdateAsync(int id);
    Task<RecepcionCompra?> GetByIdempotencyKeyAsync(string idempotencyKey, bool tracking = false);
    Task<decimal> GetCantidadAceptadaAcumuladaPorDetalleAsync(int ordenCompraDetalleId, int? excluirRecepcionId = null);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task AddAsync(RecepcionCompra recepcion);
    Task SaveChangesAsync();
}
