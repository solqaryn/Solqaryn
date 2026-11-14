using Solqaryn.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Debe_reutilizar_identificador_seguro_y_exponerlo_en_respuesta()
    {
        string? traceIdentifierObservado = null;
        var middleware = new CorrelationIdMiddleware(
            context =>
            {
                traceIdentifierObservado = context.TraceIdentifier;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "ajuste-inventario:123_abc";

        await middleware.InvokeAsync(context);

        Assert.Equal("ajuste-inventario:123_abc", traceIdentifierObservado);
        Assert.Equal("ajuste-inventario:123_abc", context.TraceIdentifier);
        Assert.Equal("ajuste-inventario:123_abc", context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(
            "ajuste-inventario:123_abc",
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Theory]
    [InlineData("valor con espacios")]
    [InlineData("<script>")]
    public async Task Debe_reemplazar_identificador_inseguro(string provided)
    {
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;

        await middleware.InvokeAsync(context);

        Assert.NotEqual(provided, context.TraceIdentifier);
        Assert.Equal(32, context.TraceIdentifier.Length);
        Assert.All(context.TraceIdentifier, value => Assert.True(Uri.IsHexDigit(value)));
        Assert.Equal(
            context.TraceIdentifier,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Debe_reemplazar_identificador_que_excede_longitud_maxima()
    {
        var provided = new string('a', 65);
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;

        await middleware.InvokeAsync(context);

        Assert.NotEqual(provided, context.TraceIdentifier);
        Assert.Equal(32, context.TraceIdentifier.Length);
    }
}
