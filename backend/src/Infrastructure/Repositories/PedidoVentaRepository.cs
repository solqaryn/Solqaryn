using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class PedidoVentaRepository : IPedidoVentaRepository
{
    private readonly AppDbContext _context;

    public PedidoVentaRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<PedidoVenta> Pedidos => _context.Set<PedidoVenta>();

    private IQueryable<PedidoVenta> ConDetalles(bool tracking)
    {
        var query = tracking ? Pedidos.AsTracking() : Pedidos.AsNoTracking();
        return query.Include(x => x.Detalles).AsSplitQuery();
    }

    public Task<PedidoVenta?> GetByIdAsync(int id, bool asNoTracking = false) =>
        ConDetalles(!asNoTracking).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<PedidoVenta?> GetByIdForUpdateAsync(int id)
    {
        RequerirTransaccion();
        var pedido = await Pedidos
            .FromSqlInterpolated($"SELECT p.* FROM PedidosVenta p WHERE p.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (pedido is not null)
            await _context.Entry(pedido).Collection(x => x.Detalles).LoadAsync();

        return pedido;
    }

    public async Task<PedidoVenta?> GetByCotizacionIdForUpdateAsync(int cotizacionId)
    {
        RequerirTransaccion();
        var pedido = await Pedidos
            .FromSqlInterpolated($"SELECT p.* FROM PedidosVenta p WHERE p.CotizacionId = {cotizacionId} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (pedido is not null)
            await _context.Entry(pedido).Collection(x => x.Detalles).LoadAsync();

        return pedido;
    }

    public async Task<PedidoVenta?> GetByIdempotencyKeyForUpdateAsync(string idempotencyKey)
    {
        RequerirTransaccion();
        var pedido = await Pedidos
            .FromSqlInterpolated($"SELECT p.* FROM PedidosVenta p WHERE p.IdempotencyKey = {idempotencyKey} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (pedido is not null)
            await _context.Entry(pedido).Collection(x => x.Detalles).LoadAsync();

        return pedido;
    }

    public async Task<(List<PedidoVenta> Items, int Total)> GetPagedAsync(PedidoVentaFiltroDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        IQueryable<PedidoVenta> query = Pedidos.AsNoTracking();

        if (request.CotizacionId.HasValue)
            query = query.Where(x => x.CotizacionId == request.CotizacionId.Value);
        if (request.ClienteId.HasValue)
            query = query.Where(x => x.ClienteId == request.ClienteId.Value);
        if (request.Estado.HasValue)
            query = query.Where(x => x.Estado == request.Estado.Value);
        if (request.FechaDesdeUtc.HasValue)
            query = query.Where(x => x.FechaCreacion >= request.FechaDesdeUtc.Value);
        if (request.FechaHastaUtc.HasValue)
            query = query.Where(x => x.FechaCreacion <= request.FechaHastaUtc.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.ClienteNombreSnapshot.Contains(search) ||
                (x.ClienteDocumentoSnapshot != null && x.ClienteDocumentoSnapshot.Contains(search)) ||
                (x.Observaciones != null && x.Observaciones.Contains(search)));
        }

        var total = await query.CountAsync();

        query = request.SortBy?.Trim().ToLowerInvariant() switch
        {
            "fecha" => string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(x => x.FechaCreacion).ThenBy(x => x.Id)
                : query.OrderByDescending(x => x.FechaCreacion).ThenByDescending(x => x.Id),
            "estado" => string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(x => x.Estado).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.Estado).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Id)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(x => x.Detalles)
            .AsSplitQuery()
            .ToListAsync();

        return (items, total);
    }

    public Task AddAsync(PedidoVenta pedido) => Pedidos.AddAsync(pedido).AsTask();
    public void Update(PedidoVenta pedido) => Pedidos.Update(pedido);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

    private void RequerirTransaccion()
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La operación con FOR UPDATE requiere una transacción activa.");
    }
}
