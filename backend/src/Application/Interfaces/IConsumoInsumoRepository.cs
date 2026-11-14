using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IConsumoInsumoRepository
{
    Task<List<ConsumoInsumo>> GetAllAsync();
    Task<ConsumoInsumo?> GetByIdAsync(int id);
    Task<ConsumoInsumo?> GetByIdForUpdateAsync(int id);
    Task AddAsync(ConsumoInsumo consumo);
    void Update(ConsumoInsumo consumo);
    Task SaveChangesAsync();
}
