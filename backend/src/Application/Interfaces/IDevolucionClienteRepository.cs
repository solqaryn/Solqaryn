using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IDevolucionClienteRepository
{
    Task<DevolucionCliente?> GetByIdAsync(int id, bool asNoTracking = false);
    Task<DevolucionCliente?> GetByIdForUpdateAsync(int id);
    Task<DevolucionCliente?> GetByIdempotencyKeyAsync(string key, bool tracking = false);
    Task<DevolucionCliente?> GetByIdempotencyKeyForUpdateAsync(string key);
    Task<(List<DevolucionCliente> Items, int Total)> GetPagedAsync(DevolucionClienteFiltroDto filtro);
    Task<int> GetCantidadConfirmadaPorVentaDetalleAsync(int ventaDetalleId);
    Task AddAsync(DevolucionCliente devolucion);
    void Update(DevolucionCliente devolucion);
    Task<bool> SaveChangesAsync();
}
