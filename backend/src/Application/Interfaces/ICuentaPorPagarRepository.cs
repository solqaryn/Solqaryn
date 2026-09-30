using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ICuentaPorPagarRepository
{
    Task<(IReadOnlyList<CuentaPorPagar> Items, int Total)> GetPagedAsync(CuentaPorPagarFiltroDto filtro);
    Task<CuentaPorPagar?> GetByIdAsync(int id, bool tracking = false);
    Task<CuentaPorPagar?> GetByIdForUpdateAsync(int id);
    Task<CuentaPorPagar?> GetByFacturaProveedorIdAsync(int facturaProveedorId, bool tracking = false);
    Task AddAsync(CuentaPorPagar cuenta);
    Task SaveChangesAsync();
}
