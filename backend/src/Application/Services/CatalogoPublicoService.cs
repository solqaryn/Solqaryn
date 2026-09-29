using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;

namespace InventoryApp.Application.Services;

public sealed class CatalogoPublicoService : ICatalogoPublicoService
{
    private readonly IProductoCatalogoPublicoRepository _repository;
    private readonly IPromocionPublicaService _promociones;
    private readonly IInventarioPublicoService _inventario;

    public CatalogoPublicoService(
        IProductoCatalogoPublicoRepository repository,
        IPromocionPublicaService promociones,
        IInventarioPublicoService inventario)
    {
        _repository = repository;
        _promociones = promociones;
        _inventario = inventario;
    }

    public async Task<PagedResult<TiendaProductoResumenDto>> BuscarAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Activo = true;
        request.UsuarioIdScope = null;

        if (request.SoloOfertas == true)
            return await BuscarOfertasAsync(request, cancellationToken);

        var (items, totalCount) = await _repository.GetPagedAsync(request, cancellationToken);
        var mapeados = await MapearLoteAsync(items, cancellationToken);

        return new PagedResult<ProductoCatalogoPublicoDto>
        {
            Items = mapeados,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    private async Task<PagedResult<TiendaProductoResumenDto>> BuscarOfertasAsync(
        ProductoPagedRequest request,
        CancellationToken cancellationToken)
    {
        var idsOrdenados = await _repository.GetOrderedIdsAsync(request, cancellationToken);
        var inicio = (request.Page - 1) * request.PageSize;
        var finExclusivo = inicio + request.PageSize;
        var totalOfertas = 0;
        var pagina = new List<TiendaProductoResumenDto>(request.PageSize);

        foreach (var loteIds in idsOrdenados.Chunk(50))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var modelos = await _repository.GetSummariesByIdsAsync(loteIds, cancellationToken);
            var porId = modelos.ToDictionary(modelo => modelo.Id);
            var ordenados = loteIds.Where(porId.ContainsKey).Select(id => porId[id]).ToList();
            var mapeados = await MapearResumenLoteAsync(ordenados, cancellationToken);

            foreach (var producto in mapeados)
            {
                var tieneOferta = producto.OfertaActiva || producto.Modelos.Any(modelo => modelo.OfertaActiva);
                if (!tieneOferta) continue;

                if (totalOfertas >= inicio && totalOfertas < finExclusivo)
                    pagina.Add(producto);
                totalOfertas++;
            }
        }

        return new PagedResult<ProductoCatalogoPublicoDto>
        {
            Items = pagina,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalOfertas
        };
    }

    public async Task<List<TiendaProductoResumenDto>> ObtenerDestacadosAsync(
        int limite,
        CancellationToken cancellationToken = default)
    {
        var request = new ProductoPagedRequest
        {
            Page = 1,
            PageSize = Math.Clamp(limite, 1, 4),
            Activo = true,
            EsDestacado = true,
            UsuarioIdScope = null,
            SortBy = "FechaCreacion",
            SortDirection = "desc"
        };

        var (items, _) = await _repository.GetPagedAsync(request, cancellationToken);
        return await MapearLoteAsync(items.Where(item => item.Activo && item.EsDestacado).ToList(), cancellationToken);
    }

    public async Task<ProductoCatalogoPublicoDto?> ObtenerDetalleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, includeGalleries: true, cancellationToken);
        if (item is null || !item.Activo)
            return null;

        var mapeados = await MapearLoteAsync(new[] { item }, cancellationToken);
        return mapeados.SingleOrDefault();
    }

    public async Task<List<ProductoCatalogoPublicoDto>> ObtenerPorIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var normalizados = ids.Where(id => id > 0).Distinct().Take(100).ToArray();
        if (normalizados.Length == 0)
            return new List<ProductoCatalogoPublicoDto>();

        var items = await _repository.GetByIdsAsync(normalizados, includeGalleries: false, cancellationToken);
        var orden = normalizados.Select((id, indice) => (id, indice)).ToDictionary(x => x.id, x => x.indice);
        var mapeados = await MapearLoteAsync(items.Where(item => item.Activo).ToList(), cancellationToken);
        return mapeados.OrderBy(item => orden.GetValueOrDefault(item.Id, int.MaxValue)).ToList();
    }

    private async Task<List<TiendaProductoResumenDto>> MapearResumenLoteAsync(
        IReadOnlyCollection<ProductoCatalogoResumenReadModel> productos,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (productos.Count == 0)
            return new List<TiendaProductoResumenDto>();

        var variantesIds = productos
            .SelectMany(producto => producto.Variantes)
            .Select(variante => variante.Id)
            .Distinct()
            .ToArray();

        var inventario = await _inventario.ObtenerPorVariantesAsync(variantesIds);
        var ahoraUtc = DateTime.UtcNow;
        var tareas = productos.Select(producto => MapearResumenProductoAsync(producto, ahoraUtc, inventario));
        return (await Task.WhenAll(tareas)).ToList();
    }

    private async Task<TiendaProductoResumenDto> MapearResumenProductoAsync(
        ProductoCatalogoResumenReadModel producto,
        DateTime ahoraUtc,
        IReadOnlyDictionary<int, InventarioPublicoVarianteDto> inventario)
    {
        var variantes = producto.Variantes.OrderBy(variante => variante.ModeloNombre).ThenBy(variante => variante.Id).ToList();

        int CantidadDisponible(ProductoVarianteCatalogoResumenReadModel variante) =>
            inventario.TryGetValue(variante.Id, out var resumen)
                ? Math.Max(0, resumen.CantidadDisponible)
                : Math.Max(0, variante.CantidadFallback);

        bool StockBajo(ProductoVarianteCatalogoResumenReadModel variante) =>
            inventario.TryGetValue(variante.Id, out var resumen)
                ? resumen.TieneStockBajo
                : variante.CantidadFallback > 0 && variante.CantidadFallback <= Math.Max(0, variante.UmbralStockBajo);

        var cantidadPublica = variantes.Count > 0
            ? variantes.Sum(CantidadDisponible)
            : Math.Max(0, producto.CantidadFallback);

        var preciosVariantes = variantes.Where(variante => variante.Precio > 0).Select(variante => variante.Precio).ToList();
        var precioPublico = Math.Max(0m, preciosVariantes.Count > 0 ? preciosVariantes.Min() : producto.PrecioFallback);
        var ofertaProducto = precioPublico > 0
            ? await _promociones.ResolverAsync(producto.Id, producto.CategoriaId, precioPublico, ahoraUtc)
            : null;

        var modelos = new List<TiendaProductoVarianteResumenDto>(variantes.Count);
        foreach (var variante in variantes)
        {
            var cantidad = CantidadDisponible(variante);
            var precioNormal = Math.Max(0m, variante.Precio);
            var oferta = precioNormal > 0
                ? await _promociones.ResolverAsync(producto.Id, producto.CategoriaId, precioNormal, ahoraUtc)
                : null;

            modelos.Add(new TiendaProductoVarianteResumenDto
            {
                ProductoVarianteId = variante.Id,
                ModeloId = variante.ModeloId,
                ModeloNombre = variante.ModeloNombre,
                MarcaNombre = variante.MarcaNombre,
                Sku = variante.Sku,
                Precio = precioNormal,
                PrecioOferta = oferta?.PrecioOferta,
                OfertaActiva = oferta is not null,
                OfertaNombre = oferta?.Nombre,
                Ahorro = oferta?.Ahorro ?? 0,
                PorcentajeAhorro = oferta?.PorcentajeAhorro ?? 0,
                CantidadDisponible = cantidad,
                EstaAgotado = cantidad <= 0,
                EstadoDisponibilidad = EstadoDisponibilidad(cantidad, StockBajo(variante))
            });
        }

        return new TiendaProductoResumenDto
        {
            Id = producto.Id,
            Slug = PublicSlug.Create(producto.Nombre, producto.Id),
            Nombre = producto.Nombre,
            DescripcionResumen = producto.DescripcionResumen,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.CategoriaNombre,
            Precio = precioPublico,
            PrecioOferta = ofertaProducto?.PrecioOferta,
            OfertaActiva = ofertaProducto is not null,
            OfertaNombre = ofertaProducto?.Nombre,
            Ahorro = ofertaProducto?.Ahorro ?? 0,
            PorcentajeAhorro = ofertaProducto?.PorcentajeAhorro ?? 0,
            CantidadDisponible = cantidadPublica,
            EstaAgotado = cantidadPublica <= 0,
            EstadoDisponibilidad = cantidadPublica <= 0
                ? "Agotado"
                : modelos.Any(modelo => modelo.EstadoDisponibilidad == "Últimas unidades")
                    ? "Últimas unidades"
                    : "Disponible",
            EsDestacado = producto.EsDestacado,
            FechaCreacion = producto.FechaCreacion,
            ImagenPrincipalUrl = producto.ImagenPrincipalUrl,
            Modelos = modelos
        };
    }

    private async Task<List<ProductoCatalogoPublicoDto>> MapearLoteAsync(
        IReadOnlyCollection<ProductoCatalogoReadModel> productos,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (productos.Count == 0)
            return new List<ProductoCatalogoPublicoDto>();

        var variantesIds = productos
            .SelectMany(producto => producto.Variantes)
            .Select(variante => variante.Id)
            .Distinct()
            .ToArray();

        var inventario = await _inventario.ObtenerPorVariantesAsync(variantesIds);
        var ahoraUtc = DateTime.UtcNow;
        var tareas = productos.Select(producto => MapearProductoAsync(producto, ahoraUtc, inventario));
        return (await Task.WhenAll(tareas)).ToList();
    }

    private async Task<ProductoCatalogoPublicoDto> MapearProductoAsync(
        ProductoCatalogoReadModel producto,
        DateTime ahoraUtc,
        IReadOnlyDictionary<int, InventarioPublicoVarianteDto> inventario)
    {
        var variantes = producto.Variantes.OrderBy(v => v.ModeloNombre).ThenBy(v => v.Id).ToList();

        int CantidadDisponible(ProductoVarianteCatalogoReadModel variante) =>
            inventario.TryGetValue(variante.Id, out var resumen)
                ? Math.Max(0, resumen.CantidadDisponible)
                : Math.Max(0, variante.CantidadFallback);

        bool StockBajo(ProductoVarianteCatalogoReadModel variante) =>
            inventario.TryGetValue(variante.Id, out var resumen)
                ? resumen.TieneStockBajo
                : variante.CantidadFallback > 0 && variante.CantidadFallback <= Math.Max(0, variante.UmbralStockBajo);

        var cantidadPublica = variantes.Count > 0
            ? variantes.Sum(CantidadDisponible)
            : Math.Max(0, producto.CantidadFallback);

        var preciosVariantes = variantes.Where(v => v.Precio > 0).Select(v => v.Precio).ToList();
        var precioPublico = Math.Max(0m, preciosVariantes.Count > 0 ? preciosVariantes.Min() : producto.PrecioFallback);

        var ofertaProducto = precioPublico > 0
            ? await _promociones.ResolverAsync(producto.Id, producto.CategoriaId, precioPublico, ahoraUtc)
            : null;

        var imagenesProducto = producto.Imagenes
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .OrderBy(i => i.Orden)
            .GroupBy(i => i.Url)
            .Select(grupo => grupo.First())
            .ToList();

        var modelos = new List<ModeloCatalogoPublicoDto>(variantes.Count);
        foreach (var variante in variantes)
        {
            var cantidad = CantidadDisponible(variante);
            var precioNormal = Math.Max(0m, variante.Precio);
            var oferta = precioNormal > 0
                ? await _promociones.ResolverAsync(producto.Id, producto.CategoriaId, precioNormal, ahoraUtc)
                : null;
            var imagenesEspecificas = variante.Imagenes
                .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                .OrderBy(i => i.Orden)
                .GroupBy(i => i.Url)
                .Select(grupo => grupo.First())
                .ToList();

            modelos.Add(new ModeloCatalogoPublicoDto
            {
                ProductoVarianteId = variante.Id,
                ModeloId = variante.ModeloId,
                ModeloNombre = variante.ModeloNombre,
                MarcaNombre = variante.MarcaNombre,
                Sku = variante.Sku,
                Precio = precioNormal,
                PrecioOferta = oferta?.PrecioOferta,
                OfertaActiva = oferta is not null,
                OfertaNombre = oferta?.Nombre,
                OfertaInicioUtc = oferta?.VigenteDesdeUtc,
                OfertaFinUtc = oferta?.VigenteHastaUtc,
                Ahorro = oferta?.Ahorro ?? 0,
                PorcentajeAhorro = oferta?.PorcentajeAhorro ?? 0,
                CantidadDisponible = cantidad,
                EstaAgotado = cantidad <= 0,
                EstadoDisponibilidad = EstadoDisponibilidad(cantidad, StockBajo(variante)),
                Imagenes = imagenesEspecificas.Count > 0 ? imagenesEspecificas : imagenesProducto
            });
        }

        var marcas = variantes.Select(v => v.MarcaNombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var modelosNombres = variantes.Select(v => v.ModeloNombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var skus = variantes.Select(v => v.Sku).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new ProductoCatalogoPublicoDto
        {
            Id = producto.Id,
            Slug = PublicSlug.Create(producto.Nombre, producto.Id),
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.CategoriaNombre,
            MarcaNombre = marcas.Count > 0 ? string.Join(" / ", marcas!) : producto.MarcaFallback,
            ModeloNombre = modelosNombres.Count > 0 ? string.Join(" / ", modelosNombres!) : producto.ModeloFallback,
            Precio = precioPublico,
            PrecioOferta = ofertaProducto?.PrecioOferta,
            OfertaActiva = ofertaProducto is not null,
            OfertaNombre = ofertaProducto?.Nombre,
            OfertaInicioUtc = ofertaProducto?.VigenteDesdeUtc,
            OfertaFinUtc = ofertaProducto?.VigenteHastaUtc,
            Ahorro = ofertaProducto?.Ahorro ?? 0,
            PorcentajeAhorro = ofertaProducto?.PorcentajeAhorro ?? 0,
            CantidadDisponible = cantidadPublica,
            EstaAgotado = cantidadPublica <= 0,
            EstadoDisponibilidad = cantidadPublica <= 0
                ? "Agotado"
                : modelos.Any(modelo => modelo.EstadoDisponibilidad == "Últimas unidades")
                    ? "Últimas unidades"
                    : "Disponible",
            Sku = skus.Count == 1 ? skus[0] : null,
            Activo = producto.Activo,
            EsDestacado = producto.EsDestacado,
            FechaCreacion = producto.FechaCreacion,
            ImagenPrincipalUrl = producto.ImagenPrincipalUrl,
            Imagenes = imagenesProducto,
            Modelos = modelos
        };
    }

    private static bool NoVacio(string? valor) => !string.IsNullOrWhiteSpace(valor);

    private static string EstadoDisponibilidad(int cantidad, bool stockBajo) =>
        cantidad <= 0 ? "Agotado" : stockBajo ? "Últimas unidades" : "Disponible";
}
