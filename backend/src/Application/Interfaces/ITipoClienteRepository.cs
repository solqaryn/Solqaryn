using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ITipoClienteRepository
{
    Task<TipoCliente?> GetByIdAsync(int id);
    Task<TipoCliente?> GetByCodigoAsync(string codigo);
    Task<TipoCliente?> GetByNombreNormalizadoAsync(string nombreNormalizado);
    Task<List<TipoCliente>> GetAllAsync();
    Task<List<TipoCliente>> GetActivosAsync();
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null);
    Task<bool> ExisteNombreNormalizadoAsync(string nombreNormalizado, int? excluirId = null);
    Task<bool> TieneClientesAsignadosAsync(int id);
    Task AddAsync(TipoCliente tipoCliente);
    void Update(TipoCliente tipoCliente);
    void Remove(TipoCliente tipoCliente);
    Task<bool> SaveChangesAsync();
}
