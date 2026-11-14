using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ICostoEnvioRepository
{
    Task<List<CostoEnvio>> GetAllAsync();
    Task<CostoEnvio?> GetByIdAsync(int id);
    Task<CostoEnvio?> GetPredeterminadoVigenteAsync(DateTime fecha);
    Task<bool> ExisteNombreAsync(string nombreNormalizado, int? excluirId = null);
    Task DesmarcarPredeterminadosAsync(int? excluirId = null);
    Task AddAsync(CostoEnvio costoEnvio);
    void Update(CostoEnvio costoEnvio);
    Task<bool> SaveChangesAsync();
}
