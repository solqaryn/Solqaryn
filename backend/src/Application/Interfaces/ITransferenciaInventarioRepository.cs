using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ITransferenciaInventarioRepository
{
    Task<(List<TransferenciaInventario> Items, int TotalCount)> GetPagedAsync(TransferenciaInventarioFiltroDto filtro);
    Task<TransferenciaInventario?> GetByIdAsync(int id);
    Task<TransferenciaInventario?> GetByIdForUpdateAsync(int id);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task AddAsync(TransferenciaInventario transferencia);
    void Update(TransferenciaInventario transferencia);
    Task SaveChangesAsync();
}
