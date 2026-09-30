using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class RevisionFinancieraRepository : IRevisionFinancieraRepository
{
    private readonly AppDbContext _context;

    public RevisionFinancieraRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RevisionFinanciera revision) =>
        await _context.RevisionesFinancieras.AddAsync(revision);

    public async Task<RevisionFinanciera?> GetUltimaAsync() =>
        await _context.RevisionesFinancieras.OrderByDescending(r => r.FechaRevision).FirstOrDefaultAsync();

    public async Task<List<RevisionFinanciera>> GetAllAsync() =>
        await _context.RevisionesFinancieras.OrderByDescending(r => r.FechaRevision).ToListAsync();

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
