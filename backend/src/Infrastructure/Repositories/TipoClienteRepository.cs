using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class TipoClienteRepository : ITipoClienteRepository
{
    private readonly AppDbContext _context;

    public TipoClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TipoCliente?> GetByIdAsync(int id) =>
        await _context.TipoClientes.FirstOrDefaultAsync(tc => tc.Id == id);

    public async Task<TipoCliente?> GetByCodigoAsync(string codigo) =>
        await _context.TipoClientes.FirstOrDefaultAsync(tc => tc.Codigo.ToLower() == codigo.ToLower());

    public async Task<TipoCliente?> GetByNombreNormalizadoAsync(string nombreNormalizado) =>
        await _context.TipoClientes.FirstOrDefaultAsync(tc => tc.NombreNormalizado.ToLower() == nombreNormalizado.ToLower());

    public async Task<List<TipoCliente>> GetAllAsync() =>
        await _context.TipoClientes.Include(tc => tc.Clientes).OrderBy(tc => tc.Orden).ToListAsync();

    public async Task<List<TipoCliente>> GetActivosAsync() =>
        await _context.TipoClientes.Where(tc => tc.Activo).OrderBy(tc => tc.Orden).ToListAsync();

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null) =>
        await _context.TipoClientes.AnyAsync(tc => tc.Codigo.ToLower() == codigo.ToLower() && (excluirId == null || tc.Id != excluirId));

    public async Task<bool> ExisteNombreNormalizadoAsync(string nombreNormalizado, int? excluirId = null) =>
        await _context.TipoClientes.AnyAsync(tc => tc.NombreNormalizado.ToLower() == nombreNormalizado.ToLower() && (excluirId == null || tc.Id != excluirId));

    public async Task<bool> TieneClientesAsignadosAsync(int id) =>
        await _context.Clientes.IgnoreQueryFilters().AnyAsync(c => c.TipoClienteId == id);

    public async Task AddAsync(TipoCliente tipoCliente) =>
        await _context.TipoClientes.AddAsync(tipoCliente);

    public void Update(TipoCliente tipoCliente) =>
        _context.TipoClientes.Update(tipoCliente);

    public void Remove(TipoCliente tipoCliente) =>
        _context.TipoClientes.Update(tipoCliente); // El soft delete se gestiona en la capa de servicios al marcar Eliminado = true.

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
