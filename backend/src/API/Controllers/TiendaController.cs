using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InventoryApp.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("AuthLogin")]
[Route("tienda")]
public sealed class TiendaController : ControllerBase
{
    private const int MaxLineasCheckout = 50;
    private const int MaxUnidadesPorLinea = 999;
    private const int MaxLongitudIdentidadVariante = 200;
    private static readonly TimeSpan VigenciaValidacionCheckout = TimeSpan.FromMinutes(10);

    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IPromocionPublicaService _promocionPublicaService;
    private readonly IInventarioPublicoService _inventarioPublicoService;
    private readonly ICatalogoPublicoService? _catalogoPublicoService;

    public TiendaController(
        IProductoService productoService,
        ICategoriaService categoriaService,
        IPromocionPublicaService promocionPublicaService,
        IInventarioPublicoService inventarioPublicoService,
        ICatalogoPublicoService? catalogoPublicoService = null)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _promocionPublicaService = promocionPublicaService;
        _inventarioPublicoService = inventarioPublicoService;
        _catalogoPublicoService = catalogoPublicoService;
    }

    [HttpGet("productos")]
    public async Task<IActionResult> GetProductos([FromQuery] ProductoPagedRequest request)
    {
        request.Activo = true;
        request.UsuarioIdScope = null;

        if (_catalogoPublicoService is not null)
        {
            var ligero = await _catalogoPublicoService.BuscarAsync(request, HttpContext.RequestAborted);
            return Ok(ApiResponse<PagedResult<TiendaProductoResumenDto>>.Ok(ligero));
        }

        var resultado = await _productoService.GetPagedAsync(request);
        var ahoraUtc = DateTime.UtcNow;
        var productosActivos = resultado.Items.Where(producto => producto.Activo).ToList();
        var inventario = await _inventarioPublicoService.ObtenerPorVariantesAsync(
            productosActivos.SelectMany(producto => producto.Variantes)
                .Where(variante => variante.Activo)
                .Select(variante => variante.Id));
        var items = await Task.WhenAll(productosActivos
            .Select(producto => MapearProductoAsync(producto, ahoraUtc, inventario)));
        var catalogo = new PagedResult<TiendaProductoResumenDto>
        {
            Items = items.Select(ResumirProducto).ToList(),
            Page = resultado.Page,
            PageSize = resultado.PageSize,
            TotalCount = resultado.TotalCount
        };

        return Ok(ApiResponse<PagedResult<TiendaProductoResumenDto>>.Ok(catalogo));
    }

    [HttpGet("productos/destacados")]
    public async Task<IActionResult> GetProductosDestacados([FromQuery] int limite = 4)
    {
        if (_catalogoPublicoService is not null)
        {
            var ligeros = await _catalogoPublicoService.ObtenerDestacadosAsync(limite, HttpContext.RequestAborted);
            return Ok(ApiResponse<List<TiendaProductoResumenDto>>.Ok(ligeros));
        }

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

        var resultado = await _productoService.GetPagedAsync(request);
        var ahoraUtc = DateTime.UtcNow;
        var productosDestacados = resultado.Items
            .Where(producto => producto.Activo && producto.EsDestacado)
            .ToList();
        var inventario = await _inventarioPublicoService.ObtenerPorVariantesAsync(
            productosDestacados.SelectMany(producto => producto.Variantes)
                .Where(variante => variante.Activo)
                .Select(variante => variante.Id));
        var destacados = await Task.WhenAll(productosDestacados
            .Select(producto => MapearProductoAsync(producto, ahoraUtc, inventario)));

        return Ok(ApiResponse<List<TiendaProductoResumenDto>>.Ok(destacados.Select(ResumirProducto).ToList()));
    }

    [HttpGet("productos/{slug}")]
    public async Task<IActionResult> GetProducto(string slug)
    {
        if (!PublicSlug.TryGetId(slug, out var id))
            return NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."));

        if (_catalogoPublicoService is not null)
        {
            var ligero = await _catalogoPublicoService.ObtenerDetalleAsync(id, HttpContext.RequestAborted);
            return ligero is null
                ? NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."))
                : Ok(ApiResponse<ProductoCatalogoPublicoDto>.Ok(ligero));
        }

        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null || !producto.Activo)
            return NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."));

        var inventario = await _inventarioPublicoService.ObtenerPorVariantesAsync(
            producto.Variantes.Where(variante => variante.Activo).Select(variante => variante.Id));

        return Ok(ApiResponse<ProductoCatalogoPublicoDto>.Ok(
            await MapearProductoAsync(producto, DateTime.UtcNow, inventario)));
    }

    [HttpPost("productos/contexto")]
    public async Task<IActionResult> GetProductosContexto([FromBody] ProductosContextoPublicoRequestDto? request)
    {
        var ids = request?.ProductoIds?.Where(id => id > 0).Distinct().Take(101).ToArray() ?? Array.Empty<int>();
        if (ids.Length > 100)
            return BadRequest(ApiResponse<List<ProductoCatalogoPublicoDto>>.Fail("El contexto supera el máximo de 100 productos."));
        if (ids.Length == 0)
            return Ok(ApiResponse<List<ProductoCatalogoPublicoDto>>.Ok(new List<ProductoCatalogoPublicoDto>()));

        if (_catalogoPublicoService is not null)
        {
            var productos = await _catalogoPublicoService.ObtenerPorIdsAsync(ids, HttpContext.RequestAborted);
            return Ok(ApiResponse<List<ProductoCatalogoPublicoDto>>.Ok(productos));
        }

        var ahoraUtc = DateTime.UtcNow;
        var legacy = new List<ProductoCatalogoPublicoDto>();
        foreach (var id in ids)
        {
            var producto = await _productoService.GetByIdAsync(id);
            if (producto is null || !producto.Activo) continue;
            var inventario = await _inventarioPublicoService.ObtenerPorVariantesAsync(
                producto.Variantes.Where(variante => variante.Activo).Select(variante => variante.Id));
            legacy.Add(await MapearProductoAsync(producto, ahoraUtc, inventario));
        }
        return Ok(ApiResponse<List<ProductoCatalogoPublicoDto>>.Ok(legacy));
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias()
    {
        var categorias = await _categoriaService.GetActivasAsync();
        var resultado = categorias
            .Where(categoria => categoria.Activa)
            .OrderBy(categoria => categoria.Nombre)
            .Select(MapearCategoria)
            .ToList();

        return Ok(ApiResponse<List<CategoriaCatalogoPublicoDto>>.Ok(resultado));
    }

    [HttpGet("categorias/{slug}")]
    public async Task<IActionResult> GetCategoria(string slug)
    {
        if (!PublicSlug.TryGetId(slug, out var id))
            return NotFound(ApiResponse<CategoriaCatalogoPublicoDto>.Fail("Categoria no encontrada."));

        var categoria = await _categoriaService.GetByIdAsync(id);
        if (categoria is null || !categoria.Activa)
            return NotFound(ApiResponse<CategoriaCatalogoPublicoDto>.Fail("Categoria no encontrada."));

        return Ok(ApiResponse<CategoriaCatalogoPublicoDto>.Ok(MapearCategoria(categoria)));
    }

    /// <summary>
    /// Recalcula precio, variante y stock exclusivamente desde el catálogo vigente.
    /// El cliente nunca envía importes y esta validación no reserva inventario ni crea un pedido ERP.
    /// </summary>
    [HttpPost("checkout/validar")]
    public async Task<IActionResult> ValidarCheckout([FromBody] ValidarCheckoutTiendaDto dto)
    {
        if (dto?.Items is null || dto.Items.Count == 0)
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("El carrito está vacío."));

        if (dto.Items.Count > MaxLineasCheckout)
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("El carrito supera el máximo de líneas permitido."));

        if (dto.Items.Any(item =>
                item.ProductoId <= 0
                || item.ProductoVarianteId is <= 0
                || item.Unidades <= 0
                || item.Unidades > MaxUnidadesPorLinea
                || (item.ModeloNombre?.Length ?? 0) > MaxLongitudIdentidadVariante
                || (item.MarcaNombre?.Length ?? 0) > MaxLongitudIdentidadVariante))
        {
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("El carrito contiene una referencia o cantidad no válida."));
        }

        var agrupadas = dto.Items
            .GroupBy(item => new
            {
                item.ProductoId,
                item.ProductoVarianteId,
                ModeloId = item.ProductoVarianteId.HasValue ? null : item.ModeloId,
                ModeloNombre = item.ProductoVarianteId.HasValue || string.IsNullOrEmpty(item.ModeloNombre) ? null : item.ModeloNombre,
                MarcaNombre = item.ProductoVarianteId.HasValue || string.IsNullOrEmpty(item.MarcaNombre) ? null : item.MarcaNombre
            })
            .Select(grupo => new CheckoutTiendaItemRequestDto
            {
                ProductoId = grupo.Key.ProductoId,
                ProductoVarianteId = grupo.Key.ProductoVarianteId,
                ModeloId = grupo.Key.ModeloId,
                ModeloNombre = grupo.Key.ModeloNombre,
                MarcaNombre = grupo.Key.MarcaNombre,
                Unidades = grupo.Sum(item => item.Unidades)
            })
            .ToList();

        if (agrupadas.Any(item => item.Unidades > MaxUnidadesPorLinea))
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("La cantidad acumulada de un producto supera el máximo permitido."));

        var lineas = new List<CheckoutTiendaLineaDto>(agrupadas.Count);
        var ahoraUtc = DateTime.UtcNow;

        foreach (var solicitud in agrupadas)
        {
            var producto = await _productoService.GetByIdAsync(solicitud.ProductoId);
            if (producto is null || !producto.Activo)
                return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Uno de los productos ya no está disponible. Actualiza el carrito antes de continuar."));

            var variantesActivas = producto.Variantes.Where(variante => variante.Activo).ToList();
            int? productoVarianteId = null;
            int? modeloId = solicitud.ModeloId;
            int stock;
            decimal precio;
            string? modelo;
            string? sku;

            if (variantesActivas.Count > 0)
            {
                ProductoVarianteDto? varianteSeleccionada = null;

                if (solicitud.ProductoVarianteId.HasValue)
                {
                    varianteSeleccionada = variantesActivas.FirstOrDefault(variante =>
                        variante.Id == solicitud.ProductoVarianteId.Value
                        && variante.ProductoId == producto.Id);

                    if (varianteSeleccionada is null)
                        return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("La variante exacta seleccionada ya no está disponible. Actualiza el carrito antes de continuar."));
                }
                else
                {
                    var variantesModelo = variantesActivas
                        .Where(variante =>
                            variante.ModeloId == solicitud.ModeloId
                            && string.Equals(variante.ModeloNombre ?? string.Empty, solicitud.ModeloNombre ?? string.Empty, StringComparison.Ordinal)
                            && string.Equals(variante.MarcaNombre ?? string.Empty, solicitud.MarcaNombre ?? string.Empty, StringComparison.Ordinal))
                        .ToList();

                    if (variantesModelo.Count == 0)
                        return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Una variante seleccionada ya no está disponible. Actualiza el carrito antes de continuar."));

                    if (variantesModelo.Count > 1)
                        return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("La selección corresponde a más de una variante física. Actualiza el carrito y selecciona una variante exacta."));

                    varianteSeleccionada = variantesModelo[0];
                }

                productoVarianteId = varianteSeleccionada.Id;
                modeloId = varianteSeleccionada.ModeloId;
                var inventarioVariante = await _inventarioPublicoService.ObtenerPorVariantesAsync(
                    new[] { varianteSeleccionada.Id });
                stock = inventarioVariante.TryGetValue(varianteSeleccionada.Id, out var resumenInventario)
                    ? resumenInventario.CantidadDisponible
                    : Math.Max(0, varianteSeleccionada.Cantidad);
                precio = varianteSeleccionada.Precio;
                modelo = varianteSeleccionada.ModeloNombre;
                sku = varianteSeleccionada.Sku;
            }
            else
            {
                if (solicitud.ProductoVarianteId is not null
                    || solicitud.ModeloId is not null
                    || solicitud.ModeloNombre is not null
                    || solicitud.MarcaNombre is not null)
                {
                    return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("La variante seleccionada ya no existe. Actualiza el carrito antes de continuar."));
                }

                stock = Math.Max(0, producto.Cantidad);
                precio = producto.PrecioMinimo > 0 ? producto.PrecioMinimo : producto.Precio;
                modelo = producto.ModeloNombre ?? producto.Modelo;
                sku = null;
            }

            precio = Math.Max(0, precio);
            if (precio <= 0)
                return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Uno de los productos no tiene un precio público válido. Intenta nuevamente más tarde."));

            if (stock <= 0 || stock < solicitud.Unidades)
                return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Cambió la existencia disponible de uno de los productos. Actualiza el carrito antes de continuar."));

            var oferta = await _promocionPublicaService.ResolverAsync(
                producto.Id,
                producto.CategoriaId,
                precio,
                ahoraUtc);
            var precioVigente = oferta?.PrecioOferta ?? precio;

            lineas.Add(new CheckoutTiendaLineaDto
            {
                ProductoId = producto.Id,
                ProductoVarianteId = productoVarianteId,
                ModeloId = modeloId,
                Nombre = producto.Nombre,
                Modelo = modelo,
                Sku = sku,
                Unidades = solicitud.Unidades,
                StockDisponible = stock,
                PrecioUnitario = precioVigente,
                Total = precioVigente * solicitud.Unidades
            });
        }

        var subtotal = lineas.Sum(linea => linea.Total);
        var validado = new CheckoutTiendaValidadoDto
        {
            ValidacionId = Guid.NewGuid().ToString("N"),
            ExpiraUtc = ahoraUtc.Add(VigenciaValidacionCheckout),
            Subtotal = subtotal,
            Total = subtotal,
            Lineas = lineas
        };

        return Ok(ApiResponse<CheckoutTiendaValidadoDto>.Ok(validado, "Carrito validado contra el catálogo vigente."));
    }

    private static CategoriaCatalogoPublicoDto MapearCategoria(CategoriaDto categoria) => new()
    {
        Id = categoria.Id,
        Slug = PublicSlug.Create(categoria.Nombre, categoria.Id),
        Nombre = categoria.Nombre,
        Descripcion = categoria.Descripcion,
        TotalProductos = null
    };

    private async Task<ProductoCatalogoPublicoDto> MapearProductoAsync(
        ProductoDto producto,
        DateTime ahoraUtc,
        IReadOnlyDictionary<int, InventarioPublicoVarianteDto> inventario)
    {
        var variantesActivas = producto.Variantes.Where(v => v.Activo).ToList();
        var skusProducto = variantesActivas.Select(v => v.Sku).Where(sku => !string.IsNullOrWhiteSpace(sku)).Distinct().ToList();

        int CantidadDisponible(ProductoVarianteDto variante) =>
            inventario.TryGetValue(variante.Id, out var resumen)
                ? Math.Max(0, resumen.CantidadDisponible)
                : Math.Max(0, variante.Cantidad);

        bool StockBajo(ProductoVarianteDto variante) =>
            inventario.TryGetValue(variante.Id, out var resumen)
                ? resumen.TieneStockBajo
                : variante.TieneStockBajo;

        var cantidadPublica = variantesActivas.Count > 0
            ? variantesActivas.Sum(CantidadDisponible)
            : Math.Max(0, producto.Cantidad);
        var preciosVariantes = variantesActivas.Where(v => v.Precio > 0).Select(v => v.Precio).ToList();
        var precioPublico = preciosVariantes.Count > 0
            ? preciosVariantes.Min()
            : producto.PrecioMinimo > 0 ? producto.PrecioMinimo : producto.Precio;
        precioPublico = Math.Max(0, precioPublico);

        var ofertaProducto = precioPublico > 0
            ? await _promocionPublicaService.ResolverAsync(producto.Id, producto.CategoriaId, precioPublico, ahoraUtc)
            : null;

        var modelos = new List<ModeloCatalogoPublicoDto>();
        foreach (var v in variantesActivas.OrderBy(v => v.ModeloNombre).ThenBy(v => v.Id))
        {
            var imagenesEspecificas = v.Imagenes
                .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                .OrderBy(i => i.Orden)
                .Select(i => new ProductoImagenPublicaDto { Url = i.Url, Orden = i.Orden, EsPrincipal = i.EsPrincipal })
                .ToList();
            var imagenes = (imagenesEspecificas.Count > 0 ? imagenesEspecificas : producto.Imagenes
                    .OrderBy(i => i.Orden)
                    .Select(i => new ProductoImagenPublicaDto { Url = i.Url, Orden = i.Orden, EsPrincipal = i.EsPrincipal }))
                .GroupBy(i => i.Url)
                .Select(grupo => grupo.First())
                .ToList();
            var cantidad = CantidadDisponible(v);
            var precioNormal = Math.Max(0, v.Precio);
            var oferta = precioNormal > 0
                ? await _promocionPublicaService.ResolverAsync(producto.Id, producto.CategoriaId, precioNormal, ahoraUtc)
                : null;

            modelos.Add(new ModeloCatalogoPublicoDto
            {
                ProductoVarianteId = v.Id,
                ModeloId = v.ModeloId,
                ModeloNombre = v.ModeloNombre,
                MarcaNombre = v.MarcaNombre,
                Sku = v.Sku,
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
                EstadoDisponibilidad = EstadoDisponibilidad(cantidad, StockBajo(v)),
                Imagenes = imagenes
            });
        }

        return new ProductoCatalogoPublicoDto
        {
            Id = producto.Id,
            Slug = PublicSlug.Create(producto.Nombre, producto.Id),
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.CategoriaNombre,
            MarcaNombre = producto.MarcaNombre ?? producto.Marca,
            ModeloNombre = producto.ModeloNombre ?? producto.Modelo,
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
            Sku = skusProducto.Count == 1 ? skusProducto[0] : null,
            Activo = producto.Activo,
            EsDestacado = producto.EsDestacado,
            FechaCreacion = producto.FechaCreacion,
            ImagenPrincipalUrl = producto.ImagenPrincipalUrl,
            Imagenes = producto.Imagenes
                .OrderBy(imagen => imagen.Orden)
                .Select(imagen => new ProductoImagenPublicaDto
                {
                    Url = imagen.Url,
                    Orden = imagen.Orden,
                    EsPrincipal = imagen.EsPrincipal
                })
                .ToList(),
            Modelos = modelos
        };
    }

    private static TiendaProductoResumenDto ResumirProducto(ProductoCatalogoPublicoDto producto)
    {
        var descripcion = producto.Descripcion?.Trim();
        if (!string.IsNullOrEmpty(descripcion) && descripcion.Length > 180)
            descripcion = descripcion[..180];

        return new TiendaProductoResumenDto
        {
            Id = producto.Id,
            Slug = producto.Slug,
            Nombre = producto.Nombre,
            DescripcionResumen = descripcion,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.CategoriaNombre,
            MarcaNombre = producto.MarcaNombre,
            ModeloNombre = producto.ModeloNombre,
            Precio = producto.Precio,
            PrecioOferta = producto.PrecioOferta,
            OfertaActiva = producto.OfertaActiva,
            OfertaNombre = producto.OfertaNombre,
            Ahorro = producto.Ahorro,
            PorcentajeAhorro = producto.PorcentajeAhorro,
            CantidadDisponible = producto.CantidadDisponible,
            EstaAgotado = producto.EstaAgotado,
            EstadoDisponibilidad = producto.EstadoDisponibilidad,
            EsDestacado = producto.EsDestacado,
            FechaCreacion = producto.FechaCreacion,
            ImagenPrincipalUrl = producto.ImagenPrincipalUrl
                ?? producto.Imagenes.OrderByDescending(imagen => imagen.EsPrincipal).ThenBy(imagen => imagen.Orden)
                    .Select(imagen => imagen.Url).FirstOrDefault(),
            Modelos = producto.Modelos.Select(modelo => new TiendaProductoVarianteResumenDto
            {
                ProductoVarianteId = modelo.ProductoVarianteId,
                ModeloId = modelo.ModeloId,
                ModeloNombre = modelo.ModeloNombre,
                MarcaNombre = modelo.MarcaNombre,
                Sku = modelo.Sku,
                Precio = modelo.Precio,
                PrecioOferta = modelo.PrecioOferta,
                OfertaActiva = modelo.OfertaActiva,
                OfertaNombre = modelo.OfertaNombre,
                Ahorro = modelo.Ahorro,
                PorcentajeAhorro = modelo.PorcentajeAhorro,
                CantidadDisponible = modelo.CantidadDisponible,
                EstaAgotado = modelo.EstaAgotado,
                EstadoDisponibilidad = modelo.EstadoDisponibilidad
            }).ToList()
        };
    }

    private static string EstadoDisponibilidad(int cantidad, bool stockBajo) =>
        cantidad <= 0 ? "Agotado" : stockBajo ? "Últimas unidades" : "Disponible";

}
