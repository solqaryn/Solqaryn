using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class TrazabilidadInventarioRepository : ITrazabilidadInventarioRepository
{
    private readonly AppDbContext _context;

    public TrazabilidadInventarioRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<LoteInventario> Lotes => _context.Set<LoteInventario>();
    private DbSet<SerieInventario> Series => _context.Set<SerieInventario>();

    public async Task<(IReadOnlyList<LoteInventario> Items, int Total)> GetLotesPagedAsync(LoteInventarioQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        IQueryable<LoteInventario> query = Lotes.AsNoTracking();

        if (filtro.ProductoVarianteId.HasValue)
            query = query.Where(x => x.ProductoVarianteId == filtro.ProductoVarianteId.Value);
        if (filtro.Activo.HasValue)
            query = query.Where(x => x.Activo == filtro.Activo.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(x => x.Codigo.Contains(search));
        }
        if (filtro.VenceDesde.HasValue)
            query = query.Where(x => x.FechaVencimiento >= filtro.VenceDesde.Value.Date);
        if (filtro.VenceHasta.HasValue)
            query = query.Where(x => x.FechaVencimiento <= filtro.VenceHasta.Value.Date);
        if (filtro.SoloVencidos.HasValue)
        {
            var hoy = DateTime.UtcNow.Date;
            query = filtro.SoloVencidos.Value
                ? query.Where(x => x.FechaVencimiento.HasValue && x.FechaVencimiento.Value < hoy)
                : query.Where(x => !x.FechaVencimiento.HasValue || x.FechaVencimiento.Value >= hoy);
        }

        var total = await query.CountAsync();
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);
        var items = await query
            .OrderBy(x => x.FechaVencimiento == null)
            .ThenBy(x => x.FechaVencimiento)
            .ThenBy(x => x.Codigo)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public Task<LoteInventario?> GetLoteByIdAsync(int id, bool tracking = false)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return Lotes
                .FromSqlInterpolated($"SELECT li.* FROM LotesInventario li WHERE li.Id = {id} FOR UPDATE")
                .AsTracking()
                .FirstOrDefaultAsync();
        }

        var query = tracking ? Lotes.AsTracking() : Lotes.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<LoteInventario?> GetLoteByCodigoAsync(int productoVarianteId, string codigo, bool tracking = false)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        var query = tracking ? Lotes.AsTracking() : Lotes.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.ProductoVarianteId == productoVarianteId && x.Codigo == normalizado);
    }

    public async Task<bool> TryAddLoteAsync(LoteInventario lote)
    {
        await Lotes.AddAsync(lote);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (EsClaveDuplicada(ex))
        {
            _context.Entry(lote).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<(IReadOnlyList<SerieInventario> Items, int Total)> GetSeriesPagedAsync(SerieInventarioQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        IQueryable<SerieInventario> query = Series.AsNoTracking();

        if (filtro.ProductoVarianteId.HasValue)
            query = query.Where(x => x.ProductoVarianteId == filtro.ProductoVarianteId.Value);
        if (filtro.LoteInventarioId.HasValue)
            query = query.Where(x => x.LoteInventarioId == filtro.LoteInventarioId.Value);
        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(x => x.NumeroSerie.Contains(search));
        }

        var total = await query.CountAsync();
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);
        var items = await query
            .OrderBy(x => x.NumeroSerie)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public Task<SerieInventario?> GetSerieByIdAsync(int id, bool tracking = false)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return Series
                .FromSqlInterpolated($"SELECT si.* FROM SeriesInventario si WHERE si.Id = {id} FOR UPDATE")
                .AsTracking()
                .FirstOrDefaultAsync();
        }

        var query = tracking ? Series.AsTracking() : Series.AsNoTracking();
        return query.Include(x => x.LoteInventario).FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<SerieInventario?> GetSerieByNumeroAsync(string numeroSerie, bool tracking = false)
    {
        var normalizado = numeroSerie.Trim().ToUpperInvariant();
        var query = tracking ? Series.AsTracking() : Series.AsNoTracking();
        return query.Include(x => x.LoteInventario).FirstOrDefaultAsync(x => x.NumeroSerie == normalizado);
    }

    public async Task<bool> TryAddSerieAsync(SerieInventario serie)
    {
        await Series.AddAsync(serie);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (EsClaveDuplicada(ex))
        {
            _context.Entry(serie).State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> TieneStockFisicoAsync(int productoVarianteId) =>
        _context.Set<ExistenciaVariante>().AsNoTracking().AnyAsync(x =>
            x.ProductoVarianteId == productoVarianteId &&
            (x.StockFisico != 0 || x.StockReservado != 0 || x.StockTransito != 0));

    public Task<bool> TieneLotesActivosAsync(int productoVarianteId) =>
        Lotes.AsNoTracking().AnyAsync(x => x.ProductoVarianteId == productoVarianteId && x.Activo);

    public Task<bool> TieneLotesActivosSinVencimientoAsync(int productoVarianteId) =>
        Lotes.AsNoTracking().AnyAsync(x =>
            x.ProductoVarianteId == productoVarianteId && x.Activo && !x.FechaVencimiento.HasValue);

    public Task<bool> TieneSeriesActivasAsync(int productoVarianteId) =>
        Series.AsNoTracking().AnyAsync(x =>
            x.ProductoVarianteId == productoVarianteId &&
            (x.Estado == EstadoSerieInventario.Disponible ||
             x.Estado == EstadoSerieInventario.Reservada ||
             x.Estado == EstadoSerieInventario.EnTransito));

    public Task<bool> TieneSeriesActivasEnLoteAsync(int loteInventarioId) =>
        Series.AsNoTracking().AnyAsync(x =>
            x.LoteInventarioId == loteInventarioId &&
            (x.Estado == EstadoSerieInventario.Disponible ||
             x.Estado == EstadoSerieInventario.Reservada ||
             x.Estado == EstadoSerieInventario.EnTransito));

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    private static bool EsClaveDuplicada(DbUpdateException exception) =>
        exception.InnerException is MySqlException { Number: 1062 };
}
