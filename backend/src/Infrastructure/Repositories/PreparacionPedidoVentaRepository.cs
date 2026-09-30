using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class PreparacionPedidoVentaRepository : IPreparacionPedidoVentaRepository
{
    private readonly AppDbContext _context;

    public PreparacionPedidoVentaRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<PreparacionPedidoVenta> Preparaciones => _context.Set<PreparacionPedidoVenta>();

    private IQueryable<PreparacionPedidoVenta> ConDetalles(bool tracking)
    {
        var query = tracking ? Preparaciones.AsTracking() : Preparaciones.AsNoTracking();
        return query.Include(x => x.Detalles).AsSplitQuery();
    }

    public Task<PreparacionPedidoVenta?> GetByIdAsync(int id, bool asNoTracking = false) =>
        ConDetalles(!asNoTracking).FirstOrDefaultAsync(x => x.Id == id);

    public Task<PreparacionPedidoVenta?> GetByPedidoVentaIdAsync(int pedidoVentaId, bool asNoTracking = false) =>
        ConDetalles(!asNoTracking).FirstOrDefaultAsync(x => x.PedidoVentaId == pedidoVentaId);

    public async Task<PreparacionPedidoVenta?> GetByIdForUpdateAsync(int id)
    {
        ExigirTransaccion();
        var entity = await Preparaciones
            .FromSqlInterpolated($"SELECT p.* FROM PreparacionesPedidoVenta p WHERE p.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
        if (entity is not null)
            await _context.Entry(entity).Collection(x => x.Detalles).LoadAsync();
        return entity;
    }

    public async Task<PreparacionPedidoVenta?> GetByPedidoVentaIdForUpdateAsync(int pedidoVentaId)
    {
        ExigirTransaccion();
        var entity = await Preparaciones
            .FromSqlInterpolated($"SELECT p.* FROM PreparacionesPedidoVenta p WHERE p.PedidoVentaId = {pedidoVentaId} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
        if (entity is not null)
            await _context.Entry(entity).Collection(x => x.Detalles).LoadAsync();
        return entity;
    }

    public Task AddAsync(PreparacionPedidoVenta preparacion) => Preparaciones.AddAsync(preparacion).AsTask();
    public void Update(PreparacionPedidoVenta preparacion) => Preparaciones.Update(preparacion);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

    private void ExigirTransaccion()
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La operación de preparación requiere una transacción activa.");
    }
}
