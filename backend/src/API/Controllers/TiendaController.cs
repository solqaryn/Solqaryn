using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Solqaryn.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("AuthLogin")]
[Route("tienda")]
public sealed class TiendaController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IPromocionPublicaService _promocionPublicaService;
    private readonly IInventarioPublicoService _inventarioPublicoService;
    private readonly ICatalogoPublicoService _catalogoPublicoService;
    private readonly ITiendaBootstrapService _bootstrapService;
    private readonly ICostoEnvioService _costoEnvioService;

    public TiendaController(
        IProductoService productoService,
        ICategoriaService categoriaService,
        IPromocionPublicaService promocionPublicaService,
        IInventarioPublicoService inventarioPublicoService,
        ICatalogoPublicoService catalogoPublicoService,
        ITiendaBootstrapService bootstrapService,
        ICostoEnvioService costoEnvioService)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _promocionPublicaService = promocionPublicaService;
        _inventarioPublicoService = inventarioPublicoService;
        _catalogoPublicoService = catalogoPublicoService ?? throw new ArgumentNullException(nameof(catalogoPublicoService));
        _bootstrapService = bootstrapService ?? throw new ArgumentNullException(nameof(bootstrapService));
        _costoEnvioService = costoEnvioService ?? throw new ArgumentNullException(nameof(costoEnvioService));
    }

    [HttpGet("bootstrap")]
    [PublicHttpCache(PublicHttpCacheProfile.Bootstrap)]
    public async Task<IActionResult> GetBootstrap()
    {
        var bootstrap = await _bootstrapService.ObtenerAsync(HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(ApiResponse<TiendaBootstrapDto>.Ok(bootstrap));
    }

    [HttpGet("productos")]
    [PublicHttpCache(PublicHttpCacheProfile.Products)]
    public async Task<IActionResult> GetProductos([FromQuery] ProductoPagedRequest request)
    {
        request.Activo = true;
        request.UsuarioIdScope = null;
        var resumen = await _catalogoPublicoService.BuscarAsync(request, HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(ApiResponse<PagedResult<TiendaProductoResumenDto>>.Ok(resumen));
    }

    [HttpGet("productos/destacados")]
    [PublicHttpCache(PublicHttpCacheProfile.Products)]
    public async Task<IActionResult> GetProductosDestacados([FromQuery] int limite = 4)
    {
        var resumen = await _catalogoPublicoService.ObtenerDestacadosAsync(limite, HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(ApiResponse<List<TiendaProductoResumenDto>>.Ok(resumen));
    }

    [HttpGet("productos/{slug}")]
    [PublicHttpCache(PublicHttpCacheProfile.Products)]
    public async Task<IActionResult> GetProducto(string slug)
    {
        if (!PublicSlug.TryGetId(slug, out var id))
            return NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."));

        var detalle = await _catalogoPublicoService.ObtenerDetalleAsync(id, HttpContext?.RequestAborted ?? CancellationToken.None);
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

        var productos = await _catalogoPublicoService.ObtenerPorIdsAsync(ids, HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(ApiResponse<List<ProductoCatalogoPublicoDto>>.Ok(productos));
    }

    [HttpGet("categorias")]
    [PublicHttpCache(PublicHttpCacheProfile.Categories)]
    public async Task<IActionResult> GetCategorias()
    {
        var categorias = await _bootstrapService.ObtenerCategoriasAsync(
            HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(ApiResponse<List<CategoriaCatalogoPublicoDto>>.Ok(categorias));
    }

    [HttpGet("categorias/{slug}")]
    [PublicHttpCache(PublicHttpCacheProfile.Categories)]
    public async Task<IActionResult> GetCategoria(string slug)
    {
        if (!PublicSlug.TryGetId(slug, out var id))
            return NotFound(ApiResponse<CategoriaCatalogoPublicoDto>.Fail("Categoria no encontrada."));

        var categorias = await _bootstrapService.ObtenerCategoriasAsync(
            HttpContext?.RequestAborted ?? CancellationToken.None);
        var categoria = categorias.SingleOrDefault(item => item.Id == id);
        return categoria is null
            ? NotFound(ApiResponse<CategoriaCatalogoPublicoDto>.Fail("Categoria no encontrada."))
            : Ok(ApiResponse<CategoriaCatalogoPublicoDto>.Ok(categoria));
    }

    /// <summary>
    /// Recalcula precio, variante y stock exclusivamente desde el catálogo vigente.
    /// El cliente nunca envía importes. La tarifa se resuelve por cobertura y tenant público verificado.
    /// Esta cotización no reserva inventario ni crea un pedido ERP.
    /// </summary>
    [HttpPost("checkout/validar")]
    public async Task<IActionResult> ValidarCheckout([FromBody] ValidarCheckoutTiendaDto dto)
    {
        var resultado = await new Solqaryn.Application.Services.CheckoutTiendaService(
            _productoService, _promocionPublicaService, _inventarioPublicoService, _costoEnvioService).ValidarAsync(dto);
        if (resultado.Datos is null)
        {
            var fallo = ApiResponse<CheckoutTiendaValidadoDto>.Fail(resultado.Mensaje);
            return resultado.EsConflicto ? Conflict(fallo) : BadRequest(fallo);
        }
        return Ok(ApiResponse<CheckoutTiendaValidadoDto>.Ok(resultado.Datos, resultado.Mensaje));
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
