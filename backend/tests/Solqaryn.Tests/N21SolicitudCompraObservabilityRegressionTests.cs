using Solqaryn.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N21SolicitudCompraObservabilityRegressionTests
{
    [Fact]
    public async Task Correlation_id_valido_se_propaga_a_trace_items_y_respuesta()
    {
        const string esperado = "req-N21_ABC:2026.08";
        string? observadoEnPipeline = null;

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = $"  {esperado}  ";
        var middleware = new CorrelationIdMiddleware(
            next: ctx =>
            {
                observadoEnPipeline = ctx.TraceIdentifier;
                Assert.Equal(esperado, ctx.Items[CorrelationIdMiddleware.ItemKey]);
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(esperado, observadoEnPipeline);
        Assert.Equal(esperado, context.TraceIdentifier);
        Assert.Equal(esperado, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Theory]
    [InlineData("cliente/no-confiable")]
    [InlineData("cliente con espacios")]
    public async Task Correlation_id_con_caracteres_no_permitidos_se_reemplaza_por_valor_seguro(string provided)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;
        var middleware = CrearMiddlewareTerminal();

        await middleware.InvokeAsync(context);

        AssertGeneradoSeguro(context.TraceIdentifier);
        Assert.NotEqual(provided, context.TraceIdentifier);
        Assert.Equal(context.TraceIdentifier, context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(context.TraceIdentifier, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Correlation_id_mayor_a_64_caracteres_falla_cerrado()
    {
        var provided = new string('A', 65);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;
        var middleware = CrearMiddlewareTerminal();

        await middleware.InvokeAsync(context);

        AssertGeneradoSeguro(context.TraceIdentifier);
        Assert.NotEqual(provided, context.TraceIdentifier);
    }

    [Fact]
    public async Task Sin_correlation_id_el_middleware_genera_un_identificador_seguro()
    {
        var context = new DefaultHttpContext();
        var middleware = CrearMiddlewareTerminal();

        await middleware.InvokeAsync(context);

        AssertGeneradoSeguro(context.TraceIdentifier);
        Assert.Equal(context.TraceIdentifier, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    private static CorrelationIdMiddleware CrearMiddlewareTerminal() =>
        new(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

    private static void AssertGeneradoSeguro(string correlationId)
    {
        Assert.Equal(32, correlationId.Length);
        Assert.All(correlationId, c => Assert.True(char.IsAsciiHexDigit(c)));
    }
}
