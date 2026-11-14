using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solqaryn.API.Middleware;
using Solqaryn.API.Observability;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// G0-003.C: delimita la señal existente de conteo/tiempo de comandos DB
/// para dos rutas calientes de catálogo e identidad empresarial.
/// Instrumentación sintética: NO afirma mediciones reales de SQL/MySQL.
/// </summary>
public sealed class G0003CCatalogoEmpresaQueryMeasurementTests
{
    [Fact]
    public async Task CatalogoYEmpresa_RegistranQueriesPorPeticionSinArrastrarMetricaAjena()
    {
        using var observability = new RequestObservability();
        var logger = new MetricsLogger();
        var options = Options.Create(new ObservabilityOptions
        {
            EnablePerformanceBaseline = true,
            EnableAlertLogs = false,
            PerformanceIdentityCategoryTargetMs = 300,
            PerformanceApiHotTargetMs = 5000
        });

        RequestDelegate next = context =>
        {
            var count = context.Request.Path.StartsWithSegments("/tienda")
                ? 3
                : 1;
            var state = RequestPerformanceContext.GetOrCreate(context);
            for (var i = 0; i < count; i++)
                state.RecordDatabaseCommand(TimeSpan.FromMilliseconds(5));
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new RequestObservabilityMiddleware(
            next, observability, options, logger);

        await EjecutarAsync(
            "/tienda/categorias",
            "Solqaryn.API.Controllers.TiendaController.GetCategorias");
        await EjecutarAsync(
            "/empresa/configuracion-publica",
            "Solqaryn.API.Controllers.EmpresaConfiguracionController.GetPublica");

        Assert.Equal(2, logger.Records.Count);
        Assert.Equal(3, Assert.IsType<int>(logger.Records[0]["DbQueries"]));
        Assert.Equal(1, Assert.IsType<int>(logger.Records[1]["DbQueries"]));
        Assert.Equal(15, Assert.IsType<double>(logger.Records[0]["DbDurationMs"]));
        Assert.Equal(5, Assert.IsType<double>(logger.Records[1]["DbDurationMs"]));
        Assert.All(logger.Records, record =>
        {
            Assert.Equal(300, Assert.IsType<int>(record["TargetMs"]));
            Assert.Equal("GET", Assert.IsType<string>(record["Method"]));
            Assert.Equal(200, Assert.IsType<int>(record["StatusCode"]));
        });
        Assert.Equal(2, observability.Snapshot().RequestCount);

        async Task EjecutarAsync(string path, string endpointName)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = path;
            context.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(),
                endpointName));
            await middleware.InvokeAsync(context);
        }
    }

    private sealed class MetricsLogger : ILogger<RequestObservabilityMiddleware>
    {
        public List<Dictionary<string, object?>> Records { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Information ||
                state is not IEnumerable<KeyValuePair<string, object?>> values)
                return;

            Records.Add(values.ToDictionary(
                pair => pair.Key, pair => pair.Value,
                StringComparer.Ordinal));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
