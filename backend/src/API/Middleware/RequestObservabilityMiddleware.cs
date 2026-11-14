using System.Diagnostics;
using Solqaryn.API.Observability;
using Microsoft.Extensions.Options;

namespace Solqaryn.API.Middleware;

public sealed class RequestObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestObservability _observability;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;
    private readonly ObservabilityOptions _options;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        RequestObservability observability,
        IOptions<ObservabilityOptions> options,
        ILogger<RequestObservabilityMiddleware> logger)
    {
        _next = next;
        _observability = observability;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var activity = Activity.Current;
        var performanceState = RequestPerformanceContext.GetOrCreate(context);
        activity?.SetTag("service.name", "Solqaryn.API");

        if (_options.EnablePerformanceBaseline)
        {
            context.Response.OnStarting(() =>
            {
                var db = performanceState.Snapshot();
                var serverTiming = FormattableString.Invariant(
                    $"db;dur={db.DurationMs:F1};desc=\"queries={db.QueryCount}\"");
                context.Response.Headers.Append("Server-Timing", serverTiming);
                return Task.CompletedTask;
            });
        }

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var thresholdMs = Math.Clamp(_options.SlowRequestThresholdMs, 100, 60_000);
            var errorAlertStatusCode = Math.Clamp(_options.ErrorAlertStatusCode, 500, 599);
            var isSlow = stopwatch.Elapsed.TotalMilliseconds >= thresholdMs;
            var endpoint = context.GetEndpoint()?.DisplayName ?? "unmatched";
            var db = performanceState.Snapshot();

            _observability.Record(context.Request.Method, statusCode, stopwatch.Elapsed.TotalMilliseconds, isSlow);

            activity?.SetTag("http.response.status_code", statusCode);
            activity?.SetTag("app.request.slow", isSlow);
            activity?.SetTag("db.query.count", db.QueryCount);
            activity?.SetTag("db.duration.ms", db.DurationMs);

            if (_options.EnablePerformanceBaseline && !context.Request.Path.StartsWithSegments("/health"))
            {
                var targetMs = ResolveTargetMs(endpoint);
                var durationMs = stopwatch.Elapsed.TotalMilliseconds;
                var dbSharePct = durationMs > 0 ? Math.Min(100, Math.Max(0, db.DurationMs * 100 / durationMs)) : 0;
                var requestBytes = context.Request.ContentLength ?? -1;
                var responseBytes = context.Response.ContentLength ?? -1;

                _logger.LogInformation(
                    "PerformanceBaseline ApiRequest Method={Method} Endpoint={Endpoint} StatusCode={StatusCode} DurationMs={DurationMs:F1} DbQueries={DbQueries} DbDurationMs={DbDurationMs:F1} DbSharePct={DbSharePct:F1} RequestBytes={RequestBytes} ResponseBytes={ResponseBytes} TargetMs={TargetMs} TargetMet={TargetMet}",
                    RequestObservability.NormalizeMethod(context.Request.Method),
                    endpoint,
                    statusCode,
                    durationMs,
                    db.QueryCount,
                    db.DurationMs,
                    dbSharePct,
                    requestBytes,
                    responseBytes,
                    targetMs,
                    durationMs <= targetMs);
            }

            if (_options.EnableAlertLogs)
            {
                if (statusCode >= errorAlertStatusCode)
                {
                    _logger.LogError(
                        "ObservabilityAlert RequestError Method={Method} Endpoint={Endpoint} StatusCode={StatusCode} DurationMs={DurationMs:F1}",
                        RequestObservability.NormalizeMethod(context.Request.Method), endpoint, statusCode, stopwatch.Elapsed.TotalMilliseconds);
                }
                else if (isSlow)
                {
                    _logger.LogWarning(
                        "ObservabilityAlert SlowRequest Method={Method} Endpoint={Endpoint} StatusCode={StatusCode} DurationMs={DurationMs:F1} ThresholdMs={ThresholdMs}",
                        RequestObservability.NormalizeMethod(context.Request.Method), endpoint, statusCode, stopwatch.Elapsed.TotalMilliseconds, thresholdMs);
                }
            }
        }
    }

    private int ResolveTargetMs(string endpoint)
    {
        var identityOrCategory =
            endpoint.Contains("EmpresaConfiguracionController.GetPublica", StringComparison.Ordinal) ||
            endpoint.Contains("TiendaController.GetCategorias", StringComparison.Ordinal);

        return identityOrCategory
            ? Math.Clamp(_options.PerformanceIdentityCategoryTargetMs, 100, 10_000)
            : Math.Clamp(_options.PerformanceApiHotTargetMs, 100, 10_000);
    }
}
