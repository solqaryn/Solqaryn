using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class CuentaContableRepository : ICuentaContableRepository
{
    private readonly AppDbContext _context;

    public CuentaContableRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CuentaContable?> GetByIdAsync(int id)
    {
        return await _context.Set<CuentaContable>()
            .Include(c => c.Subcuentas)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CuentaContable?> GetByCodigoAsync(string codigo)
    {
        return await _context.Set<CuentaContable>()
            .FirstOrDefaultAsync(c => c.Codigo == codigo);
    }

    public async Task<List<CuentaContable>> GetAllAsync()
    {
        return await _context.Set<CuentaContable>()
            .OrderBy(c => c.Codigo)
            .ToListAsync();
    }

    public async Task<List<CuentaContable>> GetRaicesAsync()
    {
        return await _context.Set<CuentaContable>()
            .Where(c => c.CuentaPadreId == null)
            .Include(c => c.Subcuentas)
            .OrderBy(c => c.Codigo)
            .ToListAsync();
    }

    public async Task AddAsync(CuentaContable cuentaContable)
    {
        await _context.Set<CuentaContable>().AddAsync(cuentaContable);
    }

    public void Update(CuentaContable cuentaContable)
    {
        _context.Set<CuentaContable>().Update(cuentaContable);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
