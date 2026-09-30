using Microsoft.AspNetCore.Http;

namespace Solqaryn.API.Observability;

public sealed record RequestDbPerformanceSnapshot(int QueryCount, double DurationMs);

public sealed class RequestPerformanceState
{
    private readonly object _sync = new();
    private int _queryCount;
    private double _dbDurationMs;

    public void RecordDatabaseCommand(TimeSpan duration)
    {
        lock (_sync)
        {
            _queryCount++;
            _dbDurationMs += Math.Max(0, duration.TotalMilliseconds);
        }
    }

    public RequestDbPerformanceSnapshot Snapshot()
    {
        lock (_sync)
        {
            return new RequestDbPerformanceSnapshot(_queryCount, _dbDurationMs);
        }
    }
}

public static class RequestPerformanceContext
{
    private static readonly object ItemKey = new();

    public static RequestPerformanceState GetOrCreate(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(ItemKey, out var existing) &&
            existing is RequestPerformanceState state)
        {
            return state;
        }

        state = new RequestPerformanceState();
        context.Items[ItemKey] = state;
        return state;
    }
}
