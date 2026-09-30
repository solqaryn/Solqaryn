using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class CotizacionRepository : ICotizacionRepository
{
    private readonly AppDbContext _context;

    public CotizacionRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<Cotizacion> Cotizaciones => _context.Set<Cotizacion>();

    private IQueryable<Cotizacion> ConDetalles(bool tracking)
    {
        var query = tracking ? Cotizaciones.AsTracking() : Cotizaciones.AsNoTracking();
        return query.Include(x => x.Detalles).AsSplitQuery();
    }

    public Task<Cotizacion?> GetByIdAsync(int id, bool asNoTracking = false) =>
        ConDetalles(!asNoTracking).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Cotizacion?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var cotizacion = await Cotizaciones
            .FromSqlInterpolated($"SELECT c.* FROM Cotizaciones c WHERE c.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (cotizacion is not null)
            await _context.Entry(cotizacion).Collection(x => x.Detalles).LoadAsync();

        return cotizacion;
    }

    public async Task<(List<Cotizacion> Items, int Total)> GetPagedAsync(CotizacionFiltroDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        IQueryable<Cotizacion> query = Cotizaciones.AsNoTracking();

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
                (x.ClienteDocumentoSnapshot != null && x.ClienteDocumentoSnapshot.Contains(search)));
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

    public Task AddAsync(Cotizacion cotizacion) => Cotizaciones.AddAsync(cotizacion).AsTask();
    public void Update(Cotizacion cotizacion) => Cotizaciones.Update(cotizacion);
    public void Remove(Cotizacion cotizacion) => Cotizaciones.Remove(cotizacion);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
}
