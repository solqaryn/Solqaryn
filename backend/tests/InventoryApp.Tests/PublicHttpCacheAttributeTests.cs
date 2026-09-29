using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace InventoryApp.Tests;

public sealed class PublicHttpCacheAttributeTests
{
    [Fact]
    public async Task Products_Get_EmiteEtagYCachePublicoCorto()
    {
        var (context, filter) = CreateContext(
            new OkObjectResult(new { success = true, data = new[] { 1, 2 } }),
            PublicHttpCacheProfile.Products);

        IActionResult? executed = null;
        await filter.OnResultExecutionAsync(context, () =>
        {
            executed = context.Result;
            return Task.FromResult(new ResultExecutedContext(
                context,
                context.Filters,
                context.Result,
                context.Controller));
        });

        Assert.Same(context.Result, executed);
        Assert.Equal("public, max-age=5, s-maxage=15, must-revalidate", context.HttpContext.Response.Headers[HeaderNames.CacheControl].ToString());
        Assert.StartsWith("W/\"", context.HttpContext.Response.Headers[HeaderNames.ETag].ToString(), StringComparison.Ordinal);
        Assert.Contains(HeaderNames.AcceptEncoding, context.HttpContext.Response.Headers[HeaderNames.Vary].ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IfNoneMatch_Coincidente_ConvierteRespuestaEn304()
    {
        var payload = new { success = true, data = new[] { "a", "b" } };
        var (firstContext, firstFilter) = CreateContext(new OkObjectResult(payload), PublicHttpCacheProfile.Categories);
        await ExecuteAsync(firstContext, firstFilter);
        var etag = firstContext.HttpContext.Response.Headers[HeaderNames.ETag].ToString();

        var (secondContext, secondFilter) = CreateContext(new OkObjectResult(payload), PublicHttpCacheProfile.Categories);
        secondContext.HttpContext.Request.Headers[HeaderNames.IfNoneMatch] = etag;

        await ExecuteAsync(secondContext, secondFilter);

        var result = Assert.IsType<StatusCodeResult>(secondContext.Result);
        Assert.Equal(StatusCodes.Status304NotModified, result.StatusCode);
        Assert.Equal(etag, secondContext.HttpContext.Response.Headers[HeaderNames.ETag].ToString());
    }

    [Fact]
    public async Task ErrorPublico_NoPuedeQuedarEnCachePublica()
    {
        var (context, filter) = CreateContext(
            new NotFoundObjectResult(new { success = false }),
            PublicHttpCacheProfile.Products);

        await ExecuteAsync(context, filter);

        Assert.Equal("private, no-store, max-age=0", context.HttpContext.Response.Headers[HeaderNames.CacheControl].ToString());
        Assert.False(context.HttpContext.Response.Headers.ContainsKey(HeaderNames.ETag));
    }

    [Fact]
    public void Tienda_SoloMarcaGetsPublicosYNoCheckoutNiContexto()
    {
        AssertPublicCache(nameof(TiendaController.GetBootstrap));
        AssertPublicCache(nameof(TiendaController.GetProductos));
        AssertPublicCache(nameof(TiendaController.GetProductosDestacados));
        AssertPublicCache(nameof(TiendaController.GetProducto));
        AssertPublicCache(nameof(TiendaController.GetCategorias));
        AssertPublicCache(nameof(TiendaController.GetCategoria));

        AssertNoPublicCache(nameof(TiendaController.GetProductosContexto));
        AssertNoPublicCache(nameof(TiendaController.ValidarCheckout));
    }

    [Fact]
    public void IdentidadPublica_EstaMarcada_PeroAdministracionNo()
    {
        AssertPublicCache(typeof(EmpresaConfiguracionController), nameof(EmpresaConfiguracionController.GetPublica));
        AssertNoPublicCache(typeof(EmpresaConfiguracionController), nameof(EmpresaConfiguracionController.Get));
        AssertPublicCache(typeof(TemaVisualController), nameof(TemaVisualController.Get));
        AssertPublicCache(typeof(WhatsAppController), nameof(WhatsAppController.GetPublicoAsync));
        AssertNoPublicCache(typeof(WhatsAppController), nameof(WhatsAppController.IniciarAsync));
    }

    private static async Task ExecuteAsync(ResultExecutingContext context, PublicHttpCacheAttribute filter)
    {
        await filter.OnResultExecutionAsync(context, () =>
            Task.FromResult(new ResultExecutedContext(
                context,
                context.Filters,
                context.Result,
                context.Controller)));
    }

    private static (ResultExecutingContext Context, PublicHttpCacheAttribute Filter) CreateContext(
        IActionResult result,
        PublicHttpCacheProfile profile)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IOptions<JsonOptions>>(Options.Create(new JsonOptions()))
            .BuildServiceProvider();

        var actionContext = new ActionContext
        {
            HttpContext = httpContext
        };
        var filters = new List<IFilterMetadata>();
        var controller = new object();
        var context = new ResultExecutingContext(actionContext, filters, result, controller);
        return (context, new PublicHttpCacheAttribute(profile));
    }

    private static void AssertPublicCache(string methodName) =>
        AssertPublicCache(typeof(TiendaController), methodName);

    private static void AssertNoPublicCache(string methodName) =>
        AssertNoPublicCache(typeof(TiendaController), methodName);

    private static void AssertPublicCache(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {controllerType.Name}.{methodName} no encontrado.");
        Assert.NotNull(method.GetCustomAttributes(typeof(PublicHttpCacheAttribute), inherit: true).SingleOrDefault());
    }

    private static void AssertNoPublicCache(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {controllerType.Name}.{methodName} no encontrado.");
        Assert.Empty(method.GetCustomAttributes(typeof(PublicHttpCacheAttribute), inherit: true));
    }
}
