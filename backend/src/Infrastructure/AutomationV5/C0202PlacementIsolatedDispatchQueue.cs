using Gate = Solqaryn.Application.AutomationV5.C0202PlacementApplicationGate;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-02.M03.05: single-attempt in-memory placement dispatch rehearsal.
/// Not a hosted worker, broker, durable queue, DB router, migration or live service.
/// </summary>
public sealed class C0202PlacementIsolatedDispatchQueue
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly C0202PlacementSandboxRehearsalJournal _journal;
    private readonly string _tenant;
    private readonly string _actor;
    private readonly string _sha;
    private string? _acceptedKey;
    private string? _fingerprint;

    public C0202PlacementIsolatedDispatchQueue(
        C0202PlacementSandboxRehearsalJournal journal,
        string verifiedSyntheticTenant, string verifiedSyntheticActor, string exactSourceSha)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        if (!SafeId(verifiedSyntheticTenant) || !SafeId(verifiedSyntheticActor) ||
            !FullSha(exactSourceSha))
            throw new ArgumentException("Safe synthetic identity and exact source SHA required.");
        _tenant = verifiedSyntheticTenant;
        _actor = verifiedSyntheticActor;
        _sha = exactSourceSha;
    }

    public readonly record struct Outcome(string Code, int SyntheticQueuedCount)
    {
        public bool StartsHostedWorker => false;
        public bool WritesDurableQueue => false;
        public bool SelectsDatabaseConnection => false;
        public bool AuthorizesMigrationOrCutover => false;
        public bool GrantsJwtOrRbac => false;
        public bool CertifiesMySqlOrProduction => false;
    }

    public Outcome InspectSynthetic(string? tenant, string? actor)
    {
        if (!string.Equals(tenant, _tenant, StringComparison.Ordinal) ||
            !string.Equals(actor, _actor, StringComparison.Ordinal))
            return new("QUEUE_INSPECTION_DENIED", 0);
        return new("QUEUE_SNAPSHOT_ONLY", _acceptedKey is null ? 0 : 1);
    }

    public async Task<Outcome> EnqueueSyntheticAsync(
        Gate.Request? request, string? observedTenant, string? observedActor,
        string? observedSha, long observedRevision, string? queueKey,
        bool clientProvidesEndpoint = false, bool requestsLiveWorker = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || !SafeId(queueKey) ||
            !string.Equals(queueKey, request.IdempotencyKey, StringComparison.Ordinal) ||
            !string.Equals(observedTenant, _tenant, StringComparison.Ordinal) ||
            !string.Equals(observedActor, _actor, StringComparison.Ordinal) ||
            !string.Equals(observedSha, _sha, StringComparison.Ordinal) ||
            !string.Equals(request.TenantId, _tenant, StringComparison.Ordinal) ||
            !string.Equals(request.SourceSha, _sha, StringComparison.Ordinal) ||
            request.ExpectedRevision != observedRevision ||
            clientProvidesEndpoint || requestsLiveWorker ||
            request.ClientSuppliesDsn || request.RequestsLiveCutoverOrDualWrite ||
            request.RequestsDestructiveSchema || request.CrossTenantFallback)
            return new("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED", 0);

        if (!SafeId(request.TargetBindingAlias))
            return new("QUEUE_INVALID_BINDING_DENIED", 0);

        var fingerprint = string.Join("|", request.TenantId, request.SourceSha,
            request.IdempotencyKey, request.TargetBindingAlias,
            request.ExpectedRevision, (int)request.Intent, (int)request.TargetMode);

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_acceptedKey is not null)
            {
                if (string.Equals(_acceptedKey, queueKey, StringComparison.Ordinal))
                    return new(
                        string.Equals(_fingerprint, fingerprint, StringComparison.Ordinal)
                            ? "QUEUE_IDEMPOTENT_REPLAY_NO_EFFECT" : "QUEUE_REPLAY_CONFLICT", 1);
                return new("QUEUE_SINGLE_ATTEMPT_CAPACITY", 1);
            }

            // All Domain/Application/Infrastructure checks must pass before an
            // inert projection. Nominal preflight grants no storage authority.
            var stage = await _journal.StageAsync(
                request, observedTenant, observedActor, observedRevision,
                observedSha, cancellationToken);
            if (stage.Code != "SANDBOX_STAGE_RECORDED_NO_LIVE_EFFECT")
                return new("QUEUE_PREFLIGHT_DENIED", 0);
            _acceptedKey = queueKey;
            _fingerprint = fingerprint;
            return new("QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER", 1);
        }
        finally { _mutex.Release(); }
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
    private static bool SafeId(string? value) =>
        value is { Length: >= 2 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}
