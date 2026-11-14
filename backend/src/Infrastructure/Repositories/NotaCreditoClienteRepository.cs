using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class NotaCreditoClienteRepository : INotaCreditoClienteRepository
{
    private readonly AppDbContext _context;

    public NotaCreditoClienteRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<NotaCreditoCliente?> GetByIdAsync(int id, bool tracking = false)
    {
        var query = _context.Set<NotaCreditoCliente>().AsQueryable();
        return tracking
            ? query.AsTracking().FirstOrDefaultAsync(x => x.Id == id)
            : query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task AddAsync(NotaCreditoCliente notaCredito) =>
        _context.Set<NotaCreditoCliente>().AddAsync(notaCredito).AsTask();

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
