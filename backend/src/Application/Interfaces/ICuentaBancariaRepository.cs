using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Application.Bancos;

namespace Solqaryn.Application.Interfaces;

public interface ICuentaBancariaRepository
{
    Task<CuentaBancaria?> GetByIdAsync(int id);
    Task<CuentaBancariaPage<CuentaBancaria>> GetAllAsync(CuentaBancariaQueryFilter filter);
    Task<List<CuentaBancaria>> GetActivasAsync();
    Task AddAsync(CuentaBancaria cuenta);
    void Update(CuentaBancaria cuenta);
    Task<int> SaveChangesAsync();
}
