using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Isolated, in-memory CAS adapter for the V5 work-item state machine.
/// No institutional Sheets write, durable database, lease, or cross-process lock.
/// </summary>
public sealed class V5WorkItemMemoryAdapter
{
    public sealed record Result(bool Applied, string Code, V5WorkItemRules.Snapshot? Snapshot);

    private readonly object _gate = new();
    private readonly Dictionary<string, V5WorkItemRules.Snapshot> _items;
    private readonly Dictionary<(string Id, string Token), (string Operation, V5WorkItemRules.Snapshot Snapshot)> _requests = new();
    private readonly IReadOnlySet<string> _catalog;
    private readonly IReadOnlySet<string> _actors;
    private readonly int _maxRequests;

    public V5WorkItemMemoryAdapter(
        IEnumerable<V5WorkItemRules.Snapshot> initial,
        IReadOnlySet<string> catalog,
        IReadOnlySet<string> authorizedActors,
        int maxRequests = 1000)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(authorizedActors);
        if (maxRequests < 1)
            throw new ArgumentOutOfRangeException(nameof(maxRequests));

        _catalog = new HashSet<string>(catalog, StringComparer.Ordinal);
        _actors = new HashSet<string>(authorizedActors, StringComparer.Ordinal);
        _items = new Dictionary<string, V5WorkItemRules.Snapshot>(StringComparer.Ordinal);
        foreach (var item in initial)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!_catalog.Contains(item.Id) || !_items.TryAdd(item.Id, item))
                throw new ArgumentException("Invalid or duplicate initial work-item snapshot.", nameof(initial));
        }
        _maxRequests = maxRequests;
    }

    public Result Read(string id, string actor)
    {
        if (!_actors.Contains(actor))
            return new(false, "UNAUTHORIZED_ACTOR", null);
        lock (_gate)
            return _items.TryGetValue(id, out var item)
                ? new(true, "FOUND", item)
                : new(false, "UNKNOWN_WORK_ITEM", null);
    }

    public Result Claim(
        string id, string actor, string requestToken,
        bool queueDependenciesOk, bool queueDevelopmentEnabled,
        bool dagValidated, bool dagGateOpen)
        => Apply(id, actor, requestToken, "CLAIM", item =>
        {
            var d = V5WorkItemRules.CanClaim(item, actor, _catalog, _actors,
                queueDependenciesOk, queueDevelopmentEnabled, dagValidated, dagGateOpen);
            return (d, item with { State = "EN_PROGRESO", OwnerTaskId = actor });
        });

    public Result MarkValidating(
        string id, string actor, string requestToken, string candidateFingerprint)
        => Apply(id, actor, requestToken, "VALIDANDO", item =>
        {
            var d = V5WorkItemRules.CanMarkValidating(item, actor, candidateFingerprint);
            return (d, item with { State = "VALIDANDO" });
        });

    public Result CloseDev(
        string id, string actor, string requestToken, string commitSha,
        bool a0Passed, bool a1Passed, bool a2Passed,
        bool a3Passed, bool a4Passed, bool a5PendingRecorded)
        => Apply(id, actor, requestToken, "LISTO", item =>
        {
            var d = V5WorkItemRules.CanCloseDev(item, actor, commitSha,
                a0Passed, a1Passed, a2Passed, a3Passed, a4Passed, a5PendingRecorded);
            return (d, item with { State = "LISTO" });
        });

    private Result Apply(
        string id, string actor, string requestToken, string operation,
        Func<V5WorkItemRules.Snapshot,
            (V5WorkItemRules.Decision Decision, V5WorkItemRules.Snapshot Proposed)> proposal)
    {
        if (string.IsNullOrWhiteSpace(actor) || !_actors.Contains(actor))
            return new(false, "UNAUTHORIZED_ACTOR", null);
        if (string.IsNullOrWhiteSpace(id) || !_catalog.Contains(id))
            return new(false, "UNKNOWN_WORK_ITEM", null);
        if (string.IsNullOrWhiteSpace(requestToken))
            return new(false, "IDEMPOTENCY_TOKEN_REQUIRED", null);

        lock (_gate)
        {
            if (!_items.TryGetValue(id, out var current))
                return new(false, "MISSING_SNAPSHOT", null);

            var key = (id, requestToken);
            if (_requests.TryGetValue(key, out var old))
            {
                if (old.Operation == operation && old.Snapshot == current &&
                    current.OwnerTaskId == actor)
                    return new(true, "IDEMPOTENT_REPLAY", current);
                return new(false, "IDEMPOTENCY_CONFLICT", current);
            }

            var (decision, proposed) = proposal(current);
            if (!decision.Allowed)
                return new(false, decision.Code, current);
            if (_requests.Count >= _maxRequests)
                return new(false, "CAPACITY_REACHED", current);

            _items[id] = proposed;
            _requests.Add(key, (operation, proposed));
            return new(true, "APPLIED_IN_MEMORY", proposed);
        }
    }
}
