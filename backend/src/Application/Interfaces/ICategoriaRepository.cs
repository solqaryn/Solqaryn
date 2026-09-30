using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ICategoriaRepository
{
    Task<Categoria?> GetByIdAsync(int id);
    Task<Categoria?> GetByIdConProductosAsync(int id);
    Task<List<Categoria>> GetAllAsync();
    Task<List<Categoria>> GetActivasAsync();
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
    Task AddAsync(Categoria categoria);
    void Update(Categoria categoria);
    void Remove(Categoria categoria);
    Task<bool> SaveChangesAsync();
}
