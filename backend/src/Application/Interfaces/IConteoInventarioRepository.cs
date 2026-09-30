using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IConteoInventarioRepository
{
    Task<(List<ConteoInventario> Items, int TotalCount)> GetPagedAsync(ConteoInventarioQueryDto query);
    Task<ConteoInventario?> GetByIdAsync(int id);
    Task<ConteoInventario?> GetByIdForUpdateAsync(int id);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task AddAsync(ConteoInventario conteo);
    void Update(ConteoInventario conteo);
    Task SaveChangesAsync();
}
