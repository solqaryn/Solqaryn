using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public sealed class ProductoCatalogoPublicoRepository : IProductoCatalogoPublicoRepository
{
    private readonly AppDbContext _context;

    public ProductoCatalogoPublicoRepository(AppDbContext context) => _context = context;

    public async Task<(List<ProductoCatalogoReadModel> Items, int TotalCount)> GetPagedAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = AplicarFiltros(BaseQuery(), request);
        var totalCount = await query.CountAsync(cancellationToken);
        query = AplicarOrden(query, request);

        var ids = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(producto => producto.Id)
            .ToListAsync(cancellationToken);

        var items = await CargarPorIdsAsync(ids, includeGalleries: false, cancellationToken);
        var orden = ids.Select((id, indice) => (id, indice)).ToDictionary(x => x.id, x => x.indice);
        return (items.OrderBy(item => orden.GetValueOrDefault(item.Id, int.MaxValue)).ToList(), totalCount);
    }

    public async Task<ProductoCatalogoReadModel?> GetByIdAsync(
        int id,
        bool includeGalleries,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0) return null;
        return (await CargarPorIdsAsync(new[] { id }, includeGalleries, cancellationToken)).SingleOrDefault();
    }

    public Task<List<ProductoCatalogoReadModel>> GetByIdsAsync(
        IEnumerable<int> ids,
        bool includeGalleries,
        CancellationToken cancellationToken = default) =>
        CargarPorIdsAsync(ids.Where(id => id > 0).Distinct().Take(100).ToArray(), includeGalleries, cancellationToken);

    private IQueryable<Producto> BaseQuery() =>
        _context.Productos.AsNoTracking().Where(producto => !producto.Eliminado);

    private static IQueryable<Producto> AplicarFiltros(IQueryable<Producto> query, ProductoPagedRequest request)
    {
        if (request.CategoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == request.CategoriaId.Value);
        if (request.ColorId.HasValue)
            query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ColorId == request.ColorId.Value));
        if (request.TallaId.HasValue)
            query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.TallaId == request.TallaId.Value));
        if (request.MarcaId.HasValue)
            query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.MarcaId == request.MarcaId.Value));
        if (request.ModeloId.HasValue)
            query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ModeloId == request.ModeloId.Value));
        if (request.Activo.HasValue)
            query = query.Where(p => p.Activo == request.Activo.Value);
        if (request.EsDestacado.HasValue)
            query = query.Where(p => p.EsDestacado == request.EsDestacado.Value);
        if (request.Agotado.HasValue)
            query = request.Agotado.Value
                ? query.Where(p => !p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0))
                : query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(search)
                || (p.Descripcion != null && p.Descripcion.ToLower().Contains(search))
                || (p.Categoria != null && p.Categoria.Nombre.ToLower().Contains(search))
                || p.Variantes.Any(v => !v.Eliminado &&
                    ((v.Sku != null && v.Sku.ToLower().Contains(search))
                    || (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(search))
                    || (v.Marca != null && v.Marca.Nombre.ToLower().Contains(search))
                    || (v.Modelo != null && v.Modelo.Nombre.ToLower().Contains(search))
                    || (v.Color != null && v.Color.Nombre.ToLower().Contains(search))
                    || (v.Talla != null && v.Talla.Nombre.ToLower().Contains(search)))));
        }

        return query;
    }

    private static IQueryable<Producto> AplicarOrden(IQueryable<Producto> query, PagedRequest request)
    {
        var desc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return request.SortBy?.ToLowerInvariant() switch
        {
            "marca" => desc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault())
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault()),
            "modelo" => desc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault())
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault()),
            "cantidad" => desc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Sum(v => v.Cantidad))
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Sum(v => v.Cantidad)),
            "precio" => desc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado && v.Activo).Select(v => v.Precio ?? 0m).DefaultIfEmpty().Min())
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado && v.Activo).Select(v => v.Precio ?? 0m).DefaultIfEmpty().Min()),
            "fechacreacion" => desc ? query.OrderByDescending(p => p.FechaCreacion) : query.OrderBy(p => p.FechaCreacion),
            _ => desc ? query.OrderByDescending(p => p.Nombre) : query.OrderBy(p => p.Nombre)
        };
    }

    private async Task<List<ProductoCatalogoReadModel>> CargarPorIdsAsync(
        IReadOnlyCollection<int> ids,
        bool includeGalleries,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new List<ProductoCatalogoReadModel>();

        var productos = await _context.Productos
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.Eliminado)
            .Select(p => new ProductoCatalogoReadModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                MarcaFallback = p.MarcaCatalogo != null ? p.MarcaCatalogo.Nombre : p.Marca,
                ModeloFallback = p.ModeloCatalogo != null ? p.ModeloCatalogo.Nombre : p.Modelo,
                CantidadFallback = p.Cantidad,
                PrecioFallback = p.Precio,
                Activo = p.Activo,
                EsDestacado = p.EsDestacado,
                FechaCreacion = p.FechaCreacion,
                ImagenPrincipalUrl = p.Imagenes
                    .Where(i => i.ProductoVarianteId == null && !string.IsNullOrEmpty(i.Url))
                    .OrderByDescending(i => i.EsPrincipal)
                    .ThenBy(i => i.Orden)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var variantes = await _context.ProductoVariantes
            .AsNoTracking()
            .Where(v => ids.Contains(v.ProductoId) && !v.Eliminado && v.Activo)
            .Select(v => new ProductoVarianteCatalogoReadModel
            {
                Id = v.Id,
                ProductoId = v.ProductoId,
                ModeloId = v.ModeloId,
                ModeloNombre = v.Modelo != null ? v.Modelo.Nombre : null,
                MarcaNombre = v.Marca != null ? v.Marca.Nombre : null,
                Sku = v.Sku,
                CantidadFallback = v.Cantidad,
                UmbralStockBajo = v.UmbralStockBajo,
                Precio = v.Precio ?? 0m,
                Imagenes = includeGalleries
                    ? new List<ProductoImagenPublicaDto>()
                    : v.Imagenes
                        .Where(i => !string.IsNullOrEmpty(i.Url))
                        .OrderByDescending(i => i.EsPrincipal)
                        .ThenBy(i => i.Orden)
                        .Take(1)
                        .Select(i => new ProductoImagenPublicaDto
                        {
                            Url = i.Url,
                            Orden = i.Orden,
                            EsPrincipal = i.EsPrincipal
                        })
                        .ToList()
            })
            .ToListAsync(cancellationToken);

        if (includeGalleries)
        {
            var imagenes = await _context.ProductoImagenes
                .AsNoTracking()
                .Where(i => ids.Contains(i.ProductoId) && !string.IsNullOrEmpty(i.Url))
                .OrderBy(i => i.ProductoId)
                .ThenBy(i => i.ProductoVarianteId)
                .ThenBy(i => i.Orden)
                .Select(i => new
                {
                    i.ProductoId,
                    i.ProductoVarianteId,
                    Imagen = new ProductoImagenPublicaDto
                    {
                        Url = i.Url,
                        Orden = i.Orden,
                        EsPrincipal = i.EsPrincipal
                    }
                })
                .ToListAsync(cancellationToken);

            var productoImagenes = imagenes
                .Where(i => i.ProductoVarianteId == null)
                .GroupBy(i => i.ProductoId)
                .ToDictionary(g => g.Key, g => g.Select(i => i.Imagen).ToList());
            var varianteImagenes = imagenes
                .Where(i => i.ProductoVarianteId.HasValue)
                .GroupBy(i => i.ProductoVarianteId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(i => i.Imagen).ToList());

            foreach (var producto in productos)
                producto.Imagenes = productoImagenes.GetValueOrDefault(producto.Id) ?? new List<ProductoImagenPublicaDto>();
            foreach (var variante in variantes)
                variante.Imagenes = varianteImagenes.GetValueOrDefault(variante.Id) ?? new List<ProductoImagenPublicaDto>();
        }
        else
        {
            foreach (var producto in productos)
            {
                if (!string.IsNullOrWhiteSpace(producto.ImagenPrincipalUrl))
                {
                    producto.Imagenes.Add(new ProductoImagenPublicaDto
                    {
                        Url = producto.ImagenPrincipalUrl,
                        Orden = 0,
                        EsPrincipal = true
                    });
                }
            }
        }

        var porProducto = variantes.GroupBy(v => v.ProductoId).ToDictionary(g => g.Key, g => g.OrderBy(v => v.Id).ToList());
        foreach (var producto in productos)
            producto.Variantes = porProducto.GetValueOrDefault(producto.Id) ?? new List<ProductoVarianteCatalogoReadModel>();

        return productos;
    }
}
