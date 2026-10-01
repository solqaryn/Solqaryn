using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IAjusteInventarioRepository
{
    Task<List<AjusteInventario>> GetAllAsync();
    Task<(List<AjusteInventario> Items, int TotalCount)> GetPagedAsync(AjusteInventarioFiltroDto filtro);
    Task<AjusteInventario?> GetByIdAsync(int id);
    Task<AjusteInventario?> GetByIdForUpdateAsync(int id);
    Task AddAsync(AjusteInventario ajuste);
    void Update(AjusteInventario ajuste);
    Task SaveChangesAsync();
}
