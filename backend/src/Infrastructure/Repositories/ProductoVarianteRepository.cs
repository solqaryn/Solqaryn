using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class ProductoVarianteRepository : IProductoVarianteRepository
{
    private readonly AppDbContext _context;

    public ProductoVarianteRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<ProductoVariante> Query() =>
        _context.ProductoVariantes
            .Include(v => v.Producto)
            .Include(v => v.Marca)
            .Include(v => v.Modelo)
            .Include(v => v.Color)
            .Include(v => v.Talla);

    public Task<ProductoVariante?> GetByIdAsync(int id) =>
        Query().FirstOrDefaultAsync(v => v.Id == id && !v.Eliminado);

    public async Task<ProductoVariante?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        return await _context.ProductoVariantes
            .FromSqlInterpolated($"SELECT pv.* FROM ProductoVariantes pv WHERE pv.Id = {id} AND pv.Eliminado = 0 FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProductoVariante>> GetByIdsForUpdateAsync(IEnumerable<int> ids)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdsForUpdateAsync requiere una transacción activa.");

        var result = new List<ProductoVariante>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var v = await GetByIdForUpdateAsync(id);
            if (v != null) result.Add(v);
        }
        return result;
    }

    public Task<List<ProductoVariante>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true)
    {
        var query = Query().Where(v => v.ProductoId == productoId && !v.Eliminado);
        if (!incluirInactivas)
            query = query.Where(v => v.Activo);
        return query
            .OrderBy(v => v.Marca != null ? v.Marca.Nombre : string.Empty)
            .ThenBy(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty)
            .ThenBy(v => v.Color != null ? v.Color.Nombre : string.Empty)
            .ThenBy(v => v.Talla != null ? v.Talla.Nombre : string.Empty)
            .ThenBy(v => v.Sku)
            .ToListAsync();
    }

    public Task<List<ProductoVariante>> GetForReporteAsync(
        int? productoId = null,
        int? marcaId = null,
        int? modeloId = null,
        int? colorId = null,
        int? tallaId = null,
        bool incluirInactivas = true,
        CancellationToken cancellationToken = default)
    {
        var query = Query()
            .AsNoTracking()
            .Where(v => !v.Eliminado && !v.Producto.Eliminado);
        if (!incluirInactivas)
            query = query.Where(v => v.Activo && v.Producto.Activo);
        if (productoId.HasValue) query = query.Where(v => v.ProductoId == productoId.Value);
        if (marcaId.HasValue) query = query.Where(v => v.MarcaId == marcaId.Value);
        if (modeloId.HasValue) query = query.Where(v => v.ModeloId == modeloId.Value);
        if (colorId.HasValue) query = query.Where(v => v.ColorId == colorId.Value);
        if (tallaId.HasValue) query = query.Where(v => v.TallaId == tallaId.Value);

        return query
            .OrderBy(v => v.Producto.Nombre)
            .ThenBy(v => v.Marca != null ? v.Marca.Nombre : string.Empty)
            .ThenBy(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty)
            .ThenBy(v => v.Color != null ? v.Color.Nombre : string.Empty)
            .ThenBy(v => v.Talla != null ? v.Talla.Nombre : string.Empty)
            .ThenBy(v => v.Sku)
            .ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductoVariante?> GetTecnicaByProductoIdAsync(int productoId, bool incluirEliminada = false)
    {
        var query = Query()
            .IgnoreQueryFilters()
            .Where(v => v.ProductoId == productoId && v.EsTecnica);
        if (!incluirEliminada)
            query = query.Where(v => !v.Eliminado);

        return query.OrderByDescending(v => v.Id).FirstOrDefaultAsync();
    }

    public Task<ProductoVariante?> GetBySkuAsync(string sku) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.Sku == sku);

    public Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.CodigoBarras == codigoBarras);

    public Task<ProductoVariante?> GetByCombinacionAsync(
        int productoId,
        int? marcaId,
        int? modeloId,
        int? colorId,
        int? tallaId) =>
        Query().FirstOrDefaultAsync(v =>
            !v.Eliminado &&
            v.ProductoId == productoId &&
            v.MarcaId == marcaId &&
            v.ModeloId == modeloId &&
            v.ColorId == colorId &&
            v.TallaId == tallaId);

    public Task<List<ProductoVariante>> BuscarPorCodigoAsync(
        string skuNormalizado,
        string codigoBarrasNormalizado,
        CancellationToken cancellationToken = default) =>
        _context.ProductoVariantes
            .AsNoTracking()
            .Include(v => v.Producto).ThenInclude(p => p.Imagenes)
            .Include(v => v.Marca)
            .Include(v => v.Modelo)
            .Include(v => v.Color)
            .Include(v => v.Talla)
            .Where(v =>
                !v.Eliminado &&
                !v.Producto.Eliminado &&
                ((v.Sku != null && v.Sku == skuNormalizado) ||
                 (v.CodigoBarras != null && v.CodigoBarras == codigoBarrasNormalizado)))
            .OrderBy(v => v.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

    public Task<List<ProductoVariante>> BuscarPorTerminoAsync(
        string terminoNormalizado,
        bool soloConStock,
        int limite,
        CancellationToken cancellationToken = default,
        TipoInventario? tipoInventario = null)
    {
        var limiteSeguro = Math.Clamp(limite, 1, 30);
        var query = _context.ProductoVariantes
            .AsNoTracking()
            .Include(v => v.Producto).ThenInclude(p => p.Imagenes)
            .Include(v => v.Producto).ThenInclude(p => p.Categoria)
            .Include(v => v.Marca)
            .Include(v => v.Modelo)
            .Include(v => v.Color)
            .Include(v => v.Talla)
            .Where(v =>
                !v.Eliminado && v.Activo &&
                !v.Producto.Eliminado && v.Producto.Activo);

        if (tipoInventario.HasValue)
            query = query.Where(v => v.Producto.TipoInventario == tipoInventario.Value);
        if (soloConStock)
            query = query.Where(v => v.Cantidad > 0);

        query = query.Where(v =>
            v.Producto.Nombre.ToLower().Contains(terminoNormalizado) ||
            (v.Producto.Descripcion != null && v.Producto.Descripcion.ToLower().Contains(terminoNormalizado)) ||
            (v.Producto.Categoria != null && v.Producto.Categoria.Nombre.ToLower().Contains(terminoNormalizado)) ||
            (v.Sku != null && v.Sku.ToLower().Contains(terminoNormalizado)) ||
            (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(terminoNormalizado)) ||
            (v.Marca != null && v.Marca.Nombre.ToLower().Contains(terminoNormalizado)) ||
            (v.Modelo != null && v.Modelo.Nombre.ToLower().Contains(terminoNormalizado)) ||
            (v.Color != null && v.Color.Nombre.ToLower().Contains(terminoNormalizado)) ||
            (v.Talla != null && v.Talla.Nombre.ToLower().Contains(terminoNormalizado)));

        return query
            .OrderBy(v => v.Producto.Nombre)
            .ThenBy(v => v.Marca != null ? v.Marca.Nombre : string.Empty)
            .ThenBy(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty)
            .ThenBy(v => v.Color != null ? v.Color.Nombre : string.Empty)
            .ThenBy(v => v.Talla != null ? v.Talla.Nombre : string.Empty)
            .ThenBy(v => v.Sku)
            .ThenBy(v => v.Id)
            .Take(limiteSeguro)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ProductoVariante variante) =>
        _context.ProductoVariantes.AddAsync(variante).AsTask();

    public void Update(ProductoVariante variante) => _context.ProductoVariantes.Update(variante);

    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
}
