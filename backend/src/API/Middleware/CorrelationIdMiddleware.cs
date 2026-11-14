using System.Diagnostics;

namespace Solqaryn.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";
    public const string TraceIdItemKey = "TraceId";

    private const int MaxCorrelationIdLength = 64;
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolverCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault());
        var traceId = Activity.Current?.TraceId.ToString();
        if (string.IsNullOrWhiteSpace(traceId))
            traceId = correlationId;

        context.Items[ItemKey] = correlationId;
        context.Items[TraceIdItemKey] = traceId;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [ItemKey] = correlationId,
            [TraceIdItemKey] = traceId
        }))
        {
            await _next(context);
        }
    }

    private static string ResolverCorrelationId(string? provided)
    {
        if (!string.IsNullOrWhiteSpace(provided))
        {
            var candidate = provided.Trim();
            if (candidate.Length <= MaxCorrelationIdLength && candidate.All(EsCaracterSeguro))
                return candidate;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool EsCaracterSeguro(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.' or ':';
}
