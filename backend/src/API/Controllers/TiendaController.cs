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
    private readonly ICatalogoPublicoService _catalogoPublicoService;

    public TiendaController(
        IProductoService productoService,
        ICategoriaService categoriaService,
        IPromocionPublicaService promocionPublicaService,
        IInventarioPublicoService inventarioPublicoService,
        ICatalogoPublicoService catalogoPublicoService)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _promocionPublicaService = promocionPublicaService;
        _inventarioPublicoService = inventarioPublicoService;
        _catalogoPublicoService = catalogoPublicoService ?? throw new ArgumentNullException(nameof(catalogoPublicoService));
    }

    [HttpGet("productos")]
    public async Task<IActionResult> GetProductos([FromQuery] ProductoPagedRequest request)
    {
        request.Activo = true;
        request.UsuarioIdScope = null;
        var resumen = await _catalogoPublicoService.BuscarAsync(request, HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResult<TiendaProductoResumenDto>>.Ok(resumen));
    }

    [HttpGet("productos/destacados")]
    public async Task<IActionResult> GetProductosDestacados([FromQuery] int limite = 4)
    {
        var resumen = await _catalogoPublicoService.ObtenerDestacadosAsync(limite, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<TiendaProductoResumenDto>>.Ok(resumen));
    }

    [HttpGet("productos/{slug}")]
    public async Task<IActionResult> GetProducto(string slug)
    {
        if (!PublicSlug.TryGetId(slug, out var id))
            return NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."));

        var detalle = await _catalogoPublicoService.ObtenerDetalleAsync(id, HttpContext.RequestAborted);
        return detalle is null
            ? NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."))
            : Ok(ApiResponse<ProductoCatalogoPublicoDto>.Ok(detalle));
    }

    [HttpPost("productos/contexto")]
    public async Task<IActionResult> GetProductosContexto([FromBody] ProductosContextoPublicoRequestDto? request)
    {
        var ids = request?.ProductoIds?.Where(id => id > 0).Distinct().Take(101).ToArray() ?? Array.Empty<int>();
        if (ids.Length > 100)
            return BadRequest(ApiResponse<List<ProductoCatalogoPublicoDto>>.Fail("El contexto supera el máximo de 100 productos."));
        if (ids.Length == 0)
            return Ok(ApiResponse<List<ProductoCatalogoPublicoDto>>.Ok(new List<ProductoCatalogoPublicoDto>()));

        var productos = await _catalogoPublicoService.ObtenerPorIdsAsync(ids, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<ProductoCatalogoPublicoDto>>.Ok(productos));
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


}
