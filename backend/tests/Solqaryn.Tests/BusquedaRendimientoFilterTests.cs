using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Solqaryn.Tests;

public class BusquedaRendimientoFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_RegistraMetricasSinExponerTermino()
    {
        const string terminoSensible = "CLIENTE-SECRETO";
        var logger = new CapturadorLogger<MedirRendimientoBusquedaFilter>();
        var filtro = new MedirRendimientoBusquedaFilter(logger);
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "corr-test-2g"
        };
        httpContext.Request.Path = "/ventas/productos/buscar";
        httpContext.Request.QueryString = new QueryString($"?termino={terminoSensible}");

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var filtros = new List<IFilterMetadata>();
        var executingContext = new ActionExecutingContext(
            actionContext,
            filtros,
            new Dictionary<string, object?>(),
            new object());

        ActionExecutionDelegate next = () => Task.FromResult(new ActionExecutedContext(
            actionContext,
            filtros,
            new object())
        {
            Result = new OkObjectResult(ApiResponse<List<int>>.Ok(new List<int> { 1, 2, 3 }))
        });

        await filtro.OnActionExecutionAsync(executingContext, next);

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.DoesNotContain(terminoSensible, mensaje, StringComparison.Ordinal);
        Assert.Contains("Ruta=/ventas/productos/buscar", mensaje, StringComparison.Ordinal);
        Assert.Contains("LongitudTermino=15", mensaje, StringComparison.Ordinal);
        Assert.Contains("CantidadResultados=3", mensaje, StringComparison.Ordinal);
        Assert.Contains("EstadoHTTP=200", mensaje, StringComparison.Ordinal);
        Assert.Contains("CorrelationId=corr-test-2g", mensaje, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnActionExecutionAsync_RutaNoOperativa_NoGeneraLog()
    {
        var logger = new CapturadorLogger<MedirRendimientoBusquedaFilter>();
        var filtro = new MedirRendimientoBusquedaFilter(logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/health";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filtros = new List<IFilterMetadata>();
        var executingContext = new ActionExecutingContext(
            actionContext,
            filtros,
            new Dictionary<string, object?>(),
            new object());

        ActionExecutionDelegate next = () => Task.FromResult(new ActionExecutedContext(
            actionContext,
            filtros,
            new object())
        {
            Result = new OkResult()
        });

        await filtro.OnActionExecutionAsync(executingContext, next);

        Assert.Empty(logger.Mensajes);
    }

    private sealed class CapturadorLogger<T> : ILogger<T>
    {
        public List<string> Mensajes { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Mensajes.Add(formatter(state, exception));
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }
}
