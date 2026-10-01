using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly AppDbContext _context;

    public CategoriaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Categoria?> GetByIdAsync(int id) =>
        await _context.Categorias.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Categoria?> GetByIdConProductosAsync(int id) =>
        await _context.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Categoria>> GetAllAsync() =>
        await _context.Categorias
            .Include(c => c.Productos)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

    public async Task<List<Categoria>> GetActivasAsync() =>
        await _context.Categorias
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        await _context.Categorias.AnyAsync(c =>
            c.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || c.Id != excluirId));

    public async Task AddAsync(Categoria categoria) =>
        await _context.Categorias.AddAsync(categoria);

    public void Update(Categoria categoria) =>
        _context.Categorias.Update(categoria);

    public void Remove(Categoria categoria) =>
        _context.Categorias.Remove(categoria);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
