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

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken) > 0;
}
