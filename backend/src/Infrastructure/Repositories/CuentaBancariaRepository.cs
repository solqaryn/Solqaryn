using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Application.Bancos;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class CuentaBancariaRepository : ICuentaBancariaRepository
{
    private readonly AppDbContext _context;

    public CuentaBancariaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CuentaBancaria?> GetByIdAsync(int id)
    {
        return await _context.CuentasBancarias.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CuentaBancariaPage<CuentaBancaria>> GetAllAsync(CuentaBancariaQueryFilter filter)
    {
        var query = _context.CuentasBancarias.AsQueryable();

        if (filter.BancoId.HasValue)
            query = query.Where(c => c.BancoId == filter.BancoId.Value);

        if (filter.Estado.HasValue)
            query = query.Where(c => c.Estado == filter.Estado.Value);

        if (!string.IsNullOrWhiteSpace(filter.Moneda))
            query = query.Where(c => c.Moneda == filter.Moneda);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(c => c.Nombre.Contains(filter.SearchTerm) || c.NumeroCuenta.Contains(filter.SearchTerm));

        query = query.OrderBy(c => c.Nombre).ThenBy(c => c.Id);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new CuentaBancariaPage<CuentaBancaria>(items, filter.Page, filter.PageSize, totalCount);
    }

    public async Task<List<CuentaBancaria>> GetActivasAsync()
    {
        return await _context.CuentasBancarias
            .Where(c => c.Estado == EstadoCuentaBancaria.Activa)
            .ToListAsync();
    }

    public async Task AddAsync(CuentaBancaria cuenta)
    {
        await _context.CuentasBancarias.AddAsync(cuenta);
    }

    public void Update(CuentaBancaria cuenta)
    {
        _context.CuentasBancarias.Update(cuenta);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
