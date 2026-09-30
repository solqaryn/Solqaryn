using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Solqaryn.API.Observability;

public sealed class ObservabilityOptions
{
    public int SlowRequestThresholdMs { get; set; } = 2000;
    public int ErrorAlertStatusCode { get; set; } = 500;
    public bool EnableAlertLogs { get; set; } = true;
    public bool EnablePerformanceBaseline { get; set; } = false;
    public int PerformanceApiHotTargetMs { get; set; } = 500;
    public int PerformanceIdentityCategoryTargetMs { get; set; } = 300;
}

public sealed record ObservabilitySnapshot(long RequestCount, long ErrorCount, long SlowRequestCount);

public sealed class RequestObservability : IDisposable
{
    public const string MeterName = "Solqaryn.API";
    public const string MeterVersion = "1.0.0";

    private readonly Meter _meter = new(MeterName, MeterVersion);
    private readonly Counter<long> _requests;
    private readonly Counter<long> _errors;
    private readonly Counter<long> _slowRequests;
    private readonly Histogram<double> _durationMs;
    private long _requestCount;
    private long _errorCount;
    private long _slowRequestCount;

    public RequestObservability()
    {
        _requests = _meter.CreateCounter<long>("solqaryn.http.server.requests", unit: "{request}", description: "Total HTTP requests handled by the API.");
        _errors = _meter.CreateCounter<long>("solqaryn.http.server.errors", unit: "{request}", description: "HTTP requests that completed with a 5xx response.");
        _slowRequests = _meter.CreateCounter<long>("solqaryn.http.server.slow_requests", unit: "{request}", description: "HTTP requests that exceeded the configured latency threshold.");
        _durationMs = _meter.CreateHistogram<double>("solqaryn.http.server.duration", unit: "ms", description: "HTTP server request duration in milliseconds.");
    }

    public void Record(string method, int statusCode, double durationMs, bool isSlow)
    {
        var normalizedMethod = NormalizeMethod(method);
        var statusClass = StatusClass(statusCode);
        TagList tags = new()
        {
            { "http.request.method", normalizedMethod },
            { "http.response.status_class", statusClass }
        };

        _requests.Add(1, tags);
        _durationMs.Record(Math.Max(0, durationMs), tags);
        Interlocked.Increment(ref _requestCount);

        if (statusCode >= 500)
        {
            _errors.Add(1, tags);
            Interlocked.Increment(ref _errorCount);
        }

        if (isSlow)
        {
            _slowRequests.Add(1, tags);
            Interlocked.Increment(ref _slowRequestCount);
        }
    }

    public ObservabilitySnapshot Snapshot() => new(
        Interlocked.Read(ref _requestCount),
        Interlocked.Read(ref _errorCount),
        Interlocked.Read(ref _slowRequestCount));

    public static string NormalizeMethod(string? method) => method?.Trim().ToUpperInvariant() switch
    {
        "GET" => "GET",
        "POST" => "POST",
        "PUT" => "PUT",
        "PATCH" => "PATCH",
        "DELETE" => "DELETE",
        "OPTIONS" => "OPTIONS",
        "HEAD" => "HEAD",
        _ => "OTHER"
    };

    private static string StatusClass(int statusCode) => statusCode switch
    {
        >= 100 and <= 599 => $"{statusCode / 100}xx",
        _ => "other"
    };

    public void Dispose() => _meter.Dispose();
}
