using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ICreditoClienteRepository
{
    Task<CreditoCliente?> GetByIdAsync(int id, bool tracking = false);
    Task<CreditoCliente?> GetByIdForUpdateAsync(int id);
    Task<List<CreditoCliente>> GetByClienteIdAsync(int clienteId);
    Task AddAsync(CreditoCliente credito);
    void Update(CreditoCliente credito);
    Task<bool> SaveChangesAsync();
}
