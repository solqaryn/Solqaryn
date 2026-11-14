using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IDevolucionProveedorRepository
{
    Task<(IReadOnlyList<DevolucionProveedor> Items, int Total)> GetPagedAsync(DevolucionProveedorQueryDto filtro);
    Task<DevolucionProveedor?> GetByIdAsync(int id, bool tracking = false);
    Task<DevolucionProveedor?> GetByIdForUpdateAsync(int id);
    Task<DevolucionProveedor?> GetByIdempotencyKeyAsync(string idempotencyKey, bool tracking = false);
    Task<decimal> GetCantidadConfirmadaDevueltaPorDetalleAsync(int recepcionCompraDetalleId, int? excluirDevolucionId = null);
    Task<decimal> GetCantidadConfirmadaDevueltaPorFacturaLineaAsync(int facturaProveedorId, int ordenCompraDetalleId, int? excluirDevolucionId = null);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task AddAsync(DevolucionProveedor devolucion);
    Task SaveChangesAsync();
}
