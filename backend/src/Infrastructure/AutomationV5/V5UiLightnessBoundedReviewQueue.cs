using Solqaryn.Domain.Security;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C05-04.M03.05: isolated, bounded, explicitly pumped in-memory queue for
/// synthetic UI-lightness preflight. No hosted worker, durable provider, DI,
/// live cache, HTTP access, database writes or authenticated access grant.
/// </summary>
public sealed class V5UiLightnessBoundedReviewQueue
{
    private readonly V5UiLightnessIsolatedSnapshotAdapter _adapter;
    private readonly int _tenantId;
    private readonly string _sourceSha;
    private readonly int _capacity;
    private readonly int _maxLifetimeKeys;
    private readonly object _sync = new();
    private readonly Queue<WorkItem> _queue = new();
    private readonly Dictionary<string, WorkItem> _seen = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _singleConsumer = new(1, 1);

    private sealed record WorkItem(ContextoTenantActual Actor, int TenantId,
        string ResourceId, string SourceSha, string Key, int ActorId);

    public readonly record struct Outcome(string Code)
    {
        public bool GrantsAccess => false;
        public bool IsLiveWorker => false;
        public bool HasDurableIdempotency => false;
        public bool WritesDatabase => false;
        public bool CertifiesCoreWebVitals => false;
    }

    public V5UiLightnessBoundedReviewQueue(
        V5UiLightnessIsolatedSnapshotAdapter adapter,
        int trustedTenantId, string trustedSourceSha,
        int capacity = 4, int maxLifetimeKeys = 32)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        if (trustedTenantId <= 0) throw new ArgumentOutOfRangeException(nameof(trustedTenantId));
        if (!FullSha(trustedSourceSha)) throw new ArgumentException(
            "Full lowercase source SHA required.", nameof(trustedSourceSha));
        if (capacity is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (maxLifetimeKeys < capacity || maxLifetimeKeys > 256)
            throw new ArgumentOutOfRangeException(nameof(maxLifetimeKeys));
        _tenantId = trustedTenantId;
        _sourceSha = trustedSourceSha;
        _capacity = capacity;
        _maxLifetimeKeys = maxLifetimeKeys;
    }

    public int PendingCount
    {
        get { lock (_sync) return _queue.Count; }
    }

    public Outcome Enqueue(
        ContextoTenantActual actor, int requestedTenantId,
        string? resourceId, string? observedSourceSha,
        string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(actor);
        if (requestedTenantId <= 0)
            return new("INVALID_TENANT_SCOPE");
        // Tenant A cannot submit tenant B before any queue mutation.
        actor.ExigirEmpresa(requestedTenantId);
        if (requestedTenantId != _tenantId)
            return new("ISOLATED_QUEUE_TENANT_SCOPE_MISMATCH");
        if (!SafeId(resourceId, 128) || !SafeId(idempotencyKey, 100))
            return new("INVALID_RESOURCE_OR_IDEMPOTENCY_KEY");
        if (!FullSha(observedSourceSha) ||
            !string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal))
            return new("ISOLATED_QUEUE_SOURCE_SHA_MISMATCH");

        lock (_sync)
        {
            if (_seen.TryGetValue(idempotencyKey!, out var old))
                return new(old.TenantId == requestedTenantId &&
                    old.ActorId == actor.UsuarioId &&
                    string.Equals(old.ResourceId, resourceId, StringComparison.Ordinal) &&
                    string.Equals(old.SourceSha, observedSourceSha, StringComparison.Ordinal)
                    ? "REPLAY_REQUIRES_DURABLE_IDEMPOTENCY"
                    : "IDEMPOTENCY_KEY_CONFLICT_DENIED");

            if (_seen.Count >= _maxLifetimeKeys)
                return new("IDEMPOTENCY_REGISTRY_FULL_FAIL_CLOSED");
            if (_queue.Count >= _capacity)
                return new("BOUNDED_QUEUE_FULL");
            var item = new WorkItem(actor, requestedTenantId,
                resourceId!, observedSourceSha!, idempotencyKey!, actor.UsuarioId);
            _seen.Add(idempotencyKey!, item);
            _queue.Enqueue(item);
            return new("QUEUED_FOR_ISOLATED_REVIEW");
        }
    }

    public async Task<Outcome> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        // Cancellation before processing never dequeues or reports success.
        cancellationToken.ThrowIfCancellationRequested();
        await _singleConsumer.WaitAsync(cancellationToken);
        try
        {
            WorkItem work;
            lock (_sync)
            {
                if (_queue.Count == 0) return new("NO_PENDING_ISOLATED_REVIEW");
                work = _queue.Peek();
            }
            // Delegate to the existing real Application/Domain synthetic
            // preflight. On cancellation or error retain item, never ACK.
            var verdict = await _adapter.EvaluateReadOnlyAsync(
                work.Actor, work.ResourceId, work.SourceSha, cancellationToken);
            lock (_sync) _queue.Dequeue();
            return new(verdict.Code);
        }
        finally { _singleConsumer.Release(); }
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool SafeId(string? value, int max) =>
        value is not null && value.Length is > 0 && value.Length <= max &&
        value.All(c => (c >= '0' && c <= '9') ||
                       (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') || c is '_' or '-');
}
