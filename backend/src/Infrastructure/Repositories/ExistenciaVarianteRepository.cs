using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class ExistenciaVarianteRepository : IExistenciaVarianteRepository
{
    private readonly AppDbContext _context;

    public ExistenciaVarianteRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<ExistenciaVariante> Existencias => _context.Set<ExistenciaVariante>();

    public async Task<ExistenciaVariante?> GetByIdAsync(int id) =>
        await BaseQuery(tracking: true)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<ExistenciaVariante?> GetByClaveAsync(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        bool forUpdate = false)
    {
        if (forUpdate)
        {
            if (_context.Database.CurrentTransaction is null)
                throw new InvalidOperationException("El bloqueo de ExistenciaVariante requiere una transacción activa.");

            return await Existencias
                .FromSqlInterpolated($@"
                    SELECT ev.*
                    FROM ExistenciasVariante ev
                    WHERE ev.ProductoVarianteId = {productoVarianteId}
                      AND ev.AlmacenId = {almacenId}
                      AND (({ubicacionAlmacenId} IS NULL AND ev.UbicacionAlmacenId IS NULL)
                           OR ev.UbicacionAlmacenId = {ubicacionAlmacenId})
                    FOR UPDATE")
                .Include(e => e.ProductoVariante).ThenInclude(v => v.Producto)
                .Include(e => e.ProductoVariante).ThenInclude(v => v.Marca)
                .Include(e => e.ProductoVariante).ThenInclude(v => v.Modelo)
                .Include(e => e.ProductoVariante).ThenInclude(v => v.Color)
                .Include(e => e.ProductoVariante).ThenInclude(v => v.Talla)
                .Include(e => e.Almacen)
                .Include(e => e.UbicacionAlmacen)
                .SingleOrDefaultAsync();
        }

        return await BaseQuery(tracking: true)
            .SingleOrDefaultAsync(e =>
                e.ProductoVarianteId == productoVarianteId &&
                e.AlmacenId == almacenId &&
                e.UbicacionAlmacenId == ubicacionAlmacenId);
    }

    public async Task<ExistenciaVariante?> GetByClaveParaReversionAsync(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El bloqueo de reversión de ExistenciaVariante requiere una transacción activa.");

        return await Existencias
            .FromSqlInterpolated($@"
                SELECT ev.*
                FROM ExistenciasVariante ev
                WHERE ev.ProductoVarianteId = {productoVarianteId}
                  AND ev.AlmacenId = {almacenId}
                  AND (({ubicacionAlmacenId} IS NULL AND ev.UbicacionAlmacenId IS NULL)
                       OR ev.UbicacionAlmacenId = {ubicacionAlmacenId})
                FOR UPDATE")
            .IgnoreQueryFilters()
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Producto)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Marca)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Modelo)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Color)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Talla)
            .Include(e => e.Almacen)
            .Include(e => e.UbicacionAlmacen)
            .SingleOrDefaultAsync();
    }

    public async Task<List<ExistenciaVariante>> GetOperativasPublicasPorVariantesAsync(
        IReadOnlyCollection<int> productoVarianteIds)
    {
        var ids = productoVarianteIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
            return new List<ExistenciaVariante>();

        return await Existencias
            .AsNoTracking()
            .Where(e =>
                ids.Contains(e.ProductoVarianteId)
                && e.ProductoVariante.Activo
                && !e.ProductoVariante.Eliminado
                && e.ProductoVariante.Producto.Activo
                && !e.ProductoVariante.Producto.Eliminado
                && e.Almacen.Activo
                && !e.Almacen.Eliminado
                && e.Almacen.Sucursal.Activa
                && !e.Almacen.Sucursal.Eliminado
                && (e.UbicacionAlmacenId == null
                    || (e.UbicacionAlmacen != null
                        && e.UbicacionAlmacen.Activa
                        && !e.UbicacionAlmacen.Eliminado))
                && (e.Almacen.Tipo == TipoAlmacen.Tienda || e.Almacen.Tipo == TipoAlmacen.Bodega))
            .ToListAsync();
    }

    public async Task<(List<ExistenciaVariante> Items, int Total)> BuscarAsync(
        int? productoId,
        int? productoVarianteId,
        int? almacenId,
        int? ubicacionAlmacenId,
        bool? soloSinUbicacion,
        bool? stockBajo,
        bool? agotada,
        int pagina,
        int tamanoPagina)
    {
        var query = BaseQuery(tracking: false);

        if (productoId.HasValue)
            query = query.Where(e => e.ProductoVariante.ProductoId == productoId.Value);
        if (productoVarianteId.HasValue)
            query = query.Where(e => e.ProductoVarianteId == productoVarianteId.Value);
        if (almacenId.HasValue)
            query = query.Where(e => e.AlmacenId == almacenId.Value);
        if (ubicacionAlmacenId.HasValue)
            query = query.Where(e => e.UbicacionAlmacenId == ubicacionAlmacenId.Value);
        if (soloSinUbicacion == true)
            query = query.Where(e => e.UbicacionAlmacenId == null);
        if (stockBajo.HasValue)
            query = stockBajo.Value
                ? query.Where(e => e.StockDisponible <= e.StockMinimo)
                : query.Where(e => e.StockDisponible > e.StockMinimo);
        if (agotada.HasValue)
            query = agotada.Value
                ? query.Where(e => e.StockDisponible <= 0)
                : query.Where(e => e.StockDisponible > 0);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.ProductoVariante.Producto.Nombre)
            .ThenBy(e => e.ProductoVariante.Sku)
            .ThenBy(e => e.Almacen.Codigo)
            .ThenBy(e => e.UbicacionAlmacenId.HasValue)
            .ThenBy(e => e.UbicacionAlmacenId)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    public Task<bool> ExisteClaveAsync(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        int? excluirId = null) =>
        Existencias.AnyAsync(e =>
            e.ProductoVarianteId == productoVarianteId &&
            e.AlmacenId == almacenId &&
            e.UbicacionAlmacenId == ubicacionAlmacenId &&
            (!excluirId.HasValue || e.Id != excluirId.Value));

    public Task AddAsync(ExistenciaVariante existencia) =>
        Existencias.AddAsync(existencia).AsTask();

    public void Update(ExistenciaVariante existencia) =>
        Existencias.Update(existencia);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private IQueryable<ExistenciaVariante> BaseQuery(bool tracking)
    {
        IQueryable<ExistenciaVariante> query = Existencias
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Producto)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Marca)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Modelo)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Color)
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Talla)
            .Include(e => e.Almacen)
            .Include(e => e.UbicacionAlmacen);

        if (!tracking)
            query = query.AsNoTracking();

        return query;
    }
}