using Solqaryn.Domain.Entities.Catalogos;

namespace Solqaryn.Application.Interfaces;

public interface IMetodoPagoRepository
{
    Task<MetodoPago?> GetByIdAsync(int id);
    Task<List<MetodoPago>> GetAllAsync();
    Task<List<MetodoPago>> GetActivosAsync();
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null);
    Task AddAsync(MetodoPago metodoPago);
    void Update(MetodoPago metodoPago);
    Task<int> SaveChangesAsync();
}
