using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ICuentaContableRepository
{
    Task<CuentaContable?> GetByIdAsync(int id);
    Task<CuentaContable?> GetByCodigoAsync(string codigo);
    Task<List<CuentaContable>> GetAllAsync();
    Task<List<CuentaContable>> GetRaicesAsync();
    Task AddAsync(CuentaContable cuentaContable);
    void Update(CuentaContable cuentaContable);
    Task<int> SaveChangesAsync();
}
