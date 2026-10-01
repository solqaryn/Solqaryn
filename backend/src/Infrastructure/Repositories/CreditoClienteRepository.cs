using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class CreditoClienteRepository : ICreditoClienteRepository
{
    private readonly AppDbContext _context;

    public CreditoClienteRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<CreditoCliente> Creditos => _context.Set<CreditoCliente>();

    public async Task<CreditoCliente?> GetByIdAsync(int id, bool tracking = false)
    {
        var query = tracking ? Creditos.AsTracking() : Creditos.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CreditoCliente?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        return await Creditos
            .FromSqlInterpolated($"SELECT c.* FROM CreditosCliente c WHERE c.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<List<CreditoCliente>> GetByClienteIdAsync(int clienteId)
    {
        return await Creditos.AsNoTracking()
            .Where(x => x.ClienteId == clienteId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public Task AddAsync(CreditoCliente credito) => Creditos.AddAsync(credito).AsTask();
    public void Update(CreditoCliente credito) => Creditos.Update(credito);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
}
