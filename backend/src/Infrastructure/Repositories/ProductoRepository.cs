using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly AppDbContext _context;

    public ProductoRepository(AppDbContext context) => _context = context;

    private IQueryable<Producto> ConIncludes() =>
        _context.Productos
            .Include(p => p.Imagenes)
            .Include(p => p.Categoria)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Marca)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Modelo)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Color)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Talla)
            .Include(p => p.Variantes.Where(v => !v.Eliminado)).ThenInclude(v => v.Imagenes)
            .AsSplitQuery();

    public Task<Producto?> GetByIdAsync(int id) => ConIncludes().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Producto?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");
        return await _context.Productos
            .FromSqlInterpolated($"SELECT p.* FROM Productos p WHERE p.Id = {id} AND p.Eliminado = 0 FOR UPDATE")
            .AsTracking().FirstOrDefaultAsync();
    }

    public async Task<List<Producto>> GetByIdsForUpdateAsync(IEnumerable<int> ids)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdsForUpdateAsync requiere una transacción activa.");
        var result = new List<Producto>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var p = await GetByIdForUpdateAsync(id);
            if (p is not null) result.Add(p);
        }
        return result;
    }

    public async Task<(List<Producto> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var query = ConIncludes().AsNoTracking().AsQueryable();
        if (request is ProductoPagedRequest filters)
        {
            if (filters.CategoriaId.HasValue) query = query.Where(p => p.CategoriaId == filters.CategoriaId.Value);
            if (filters.ColorId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ColorId == filters.ColorId.Value));
            if (filters.TallaId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.TallaId == filters.TallaId.Value));
            if (filters.MarcaId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.MarcaId == filters.MarcaId.Value));
            if (filters.ModeloId.HasValue) query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ModeloId == filters.ModeloId.Value));
            if (filters.Activo.HasValue) query = query.Where(p => p.Activo == filters.Activo.Value);
            if (filters.EsDestacado.HasValue) query = query.Where(p => p.EsDestacado == filters.EsDestacado.Value);
            if (filters.Agotado.HasValue)
                query = filters.Agotado.Value
                    ? query.Where(p => !p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0))
                    : query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(search) ||
                (p.Descripcion != null && p.Descripcion.ToLower().Contains(search)) ||
                (p.Categoria != null && p.Categoria.Nombre.ToLower().Contains(search)) ||
                p.Variantes.Any(v => !v.Eliminado &&
                    ((v.Sku != null && v.Sku.ToLower().Contains(search)) ||
                     (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(search)) ||
                     (v.Marca != null && v.Marca.Nombre.ToLower().Contains(search)) ||
                     (v.Modelo != null && v.Modelo.Nombre.ToLower().Contains(search)) ||
                     (v.Color != null && v.Color.Nombre.ToLower().Contains(search)) ||
                     (v.Talla != null && v.Talla.Nombre.ToLower().Contains(search)))));
        }

        var totalCount = await query.CountAsync();
        var desc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortBy?.ToLower() switch
        {
            "marca" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault()),
            "modelo" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault()),
            "color" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Color != null ? v.Color.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Color != null ? v.Color.Nombre : string.Empty).FirstOrDefault()),
            "talla" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Talla != null ? v.Talla.Nombre : string.Empty).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Talla != null ? v.Talla.Nombre : string.Empty).FirstOrDefault()),
            "cantidad" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Sum(v => v.Cantidad)) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Sum(v => v.Cantidad)),
            "costo" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Costo ?? 0m).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Costo ?? 0m).FirstOrDefault()),
            "precio" => desc ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Precio ?? 0m).FirstOrDefault()) : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Precio ?? 0m).FirstOrDefault()),
            "fechacreacion" => desc ? query.OrderByDescending(p => p.FechaCreacion) : query.OrderBy(p => p.FechaCreacion),
            _ => desc ? query.OrderByDescending(p => p.Nombre) : query.OrderBy(p => p.Nombre)
        };

        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();
        return (items, totalCount);
    }

    public Task<List<Producto>> GetStockBajoAsync() => ConIncludes().AsNoTracking()
        .Where(p => !p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > v.UmbralStockBajo))
        .OrderBy(p => p.Nombre).ToListAsync();

    public Task<List<Producto>> GetUltimosAgregadosAsync(int cantidad = 5) => ConIncludes().AsNoTracking()
        .OrderByDescending(p => p.FechaCreacion).Take(cantidad).ToListAsync();

    public Task<int> GetTotalProductosAsync() => _context.Productos.CountAsync();
    public async Task<int> GetTotalUnidadesAsync() => await _context.ProductoVariantes.Where(v => !v.Eliminado).SumAsync(v => (int?)v.Cantidad) ?? 0;
    public async Task<decimal> GetValorTotalCostoAsync() => await _context.ProductoVariantes.Where(v => !v.Eliminado).SumAsync(v => (decimal?)((v.Costo ?? 0m) * v.Cantidad)) ?? 0m;
    public async Task<decimal> GetValorTotalPrecioAsync() => await _context.ProductoVariantes.Where(v => !v.Eliminado).SumAsync(v => (decimal?)((v.Precio ?? 0m) * v.Cantidad)) ?? 0m;
    public Task<int> GetTotalProductosPorTipoAsync(TipoInventario tipoInventario) => _context.Productos.CountAsync(p => p.TipoInventario == tipoInventario);
    public async Task<int> GetTotalUnidadesPorTipoAsync(TipoInventario tipoInventario) => await _context.ProductoVariantes.Where(v => !v.Eliminado && v.Producto.TipoInventario == tipoInventario).SumAsync(v => (int?)v.Cantidad) ?? 0;
    public async Task<decimal> GetValorTotalCostoPorTipoAsync(TipoInventario tipoInventario) => await _context.ProductoVariantes.Where(v => !v.Eliminado && v.Producto.TipoInventario == tipoInventario).SumAsync(v => (decimal?)((v.Costo ?? 0m) * v.Cantidad)) ?? 0m;
    public async Task<decimal> GetValorTotalPrecioPorTipoAsync(TipoInventario tipoInventario) => await _context.ProductoVariantes.Where(v => !v.Eliminado && v.Producto.TipoInventario == tipoInventario).SumAsync(v => (decimal?)((v.Precio ?? 0m) * v.Cantidad)) ?? 0m;

    public Task AddAsync(Producto producto) => _context.Productos.AddAsync(producto).AsTask();
    public void Update(Producto producto) => _context.Productos.Update(producto);
    public void Remove(Producto producto) => _context.Productos.Remove(producto);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
}
