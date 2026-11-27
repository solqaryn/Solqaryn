using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class EmpresaRepository : IEmpresaRepository
{
    private readonly AppDbContext _context;

    public EmpresaRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<Empresa> Empresas => _context.Set<Empresa>();

    public async Task<List<Empresa>> ListAsync(bool? activa = null, CancellationToken cancellationToken = default)
    {
        var query = Empresas.AsNoTracking().AsQueryable();
        if (activa.HasValue)
            query = query.Where(e => e.Activa == activa.Value);

        return await query
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Empresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Empresas.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task AddAsync(Empresa empresa, CancellationToken cancellationToken = default) =>
        await Empresas.AddAsync(empresa, cancellationToken);

    public void Update(Empresa empresa) => Empresas.Update(empresa);

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var provision = _context.ChangeTracker.Entries<Empresa>()
            .Where(e => e.Entity.Activa && (e.State == EntityState.Added ||
                (e.State == EntityState.Modified && e.Property(x => x.Activa).IsModified &&
                 !e.Property(x => x.Activa).OriginalValue)))
            .Select(e => e.Entity).ToArray();
        if (provision.Length == 0 || _context.Database.ProviderName != "MySql.EntityFrameworkCore")
            return await _context.SaveChangesAsync(cancellationToken) > 0;

        // Provision defaults atomically with company creation or activation.
        if (_context.Database.CurrentTransaction is not null)
        {
            var changed = await _context.SaveChangesAsync(cancellationToken) > 0;
            foreach (var empresa in provision)
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(_context, empresa.Id, cancellationToken);
            return changed;
        }
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        foreach (var empresa in provision)
            await QaCommercialOwnershipV3.ProvisionDefaultsAsync(_context, empresa.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return saved;
    }
}
