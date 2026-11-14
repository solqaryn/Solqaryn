using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class TemaVisualRepository : ITemaVisualRepository
{
    private readonly AppDbContext _context;

    public TemaVisualRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TemaVisual?> GetAsync() =>
        await _context.TemasVisuales.FirstOrDefaultAsync();

    public async Task AddAsync(TemaVisual tema) =>
        await _context.TemasVisuales.AddAsync(tema);

    public void Update(TemaVisual tema) =>
        _context.TemasVisuales.Update(tema);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
