using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ISucursalRepository
{
    Task<Sucursal?> GetByIdAsync(int id);
    Task<Sucursal?> GetByCodigoAsync(string codigo);
    Task<(List<Sucursal> Items, int Total)> BuscarAsync(
        string? termino,
        bool? activa,
        int? empresaId,
        int pagina,
        int tamanoPagina);
    Task<List<Sucursal>> GetActivasAsync(int? empresaId = null);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null);
    Task<bool> ExisteCodigoAsync(string codigo, int empresaId, int? excluirId = null);
    Task AddAsync(Sucursal sucursal);
    void Update(Sucursal sucursal);
    Task<bool> SaveChangesAsync();
}
