using Gate = Solqaryn.Application.AutomationV5.C0202PlacementApplicationGate;
using DomainRules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-02.M03.04: reversible, single-attempt IN-MEMORY sandbox rehearsal of
/// a tenant placement candidate. It is not a repository, connection router,
/// migration executor, durable store, runtime authorization or production DI.
/// </summary>
public sealed class C0202PlacementSandboxRehearsalJournal
{
    private readonly object _gate = new();
    private readonly C0202PlacementIsolatedSnapshotAdapter _isolated;
    private readonly string _tenant;
    private readonly string _actor;
    private readonly string _sourceSha;
    private readonly string _originalBinding;
    private string _currentBinding;
    private long _currentRevision;
    private string? _stageKey;
    private string? _stageFingerprint;
    private bool _rolledBack;

    public C0202PlacementSandboxRehearsalJournal(
        C0202PlacementIsolatedSnapshotAdapter isolated,
        string verifiedSyntheticTenant, string verifiedSyntheticActor,
        string exactSourceSha, long initialRevision, string initialBinding)
    {
        _isolated = isolated ?? throw new ArgumentNullException(nameof(isolated));
        if (!SafeId(verifiedSyntheticTenant) || !SafeId(verifiedSyntheticActor) ||
            !SafeId(initialBinding) || !FullSha(exactSourceSha))
            throw new ArgumentException("Safe synthetic identity and SHA required.");
        if (initialRevision <= 0 || initialRevision == long.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(initialRevision));
        _tenant = verifiedSyntheticTenant;
        _actor = verifiedSyntheticActor;
        _sourceSha = exactSourceSha;
        _originalBinding = initialBinding;
        _currentBinding = initialBinding;
        _currentRevision = initialRevision;
    }

    public readonly record struct Outcome(string Code, long SandboxRevision)
    {
        public bool GrantsStorageConnection => false;
        public bool AuthorizesMigration => false;
        public bool AuthorizesCutover => false;
        public bool WritesMySqlOrEf => false;
        public bool IsDurable => false;
        public bool CertifiesProductionOrJwt => false;
    }

    public Outcome InspectSynthetic(string? tenant, string? actor)
    {
        lock (_gate)
        {
            if (!string.Equals(tenant, _tenant, StringComparison.Ordinal) ||
                !string.Equals(actor, _actor, StringComparison.Ordinal))
                return new("SANDBOX_TENANT_OR_ACTOR_DENIED", 0);
            return new("SANDBOX_SNAPSHOT_ONLY", _currentRevision);
        }
    }

    public async Task<Outcome> StageAsync(
        Gate.Request? request, string? observedTenant, string? observedActor,
        long observedOriginalRevision, string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.Intent != DomainRules.Intent.ProposeMigration ||
            !SafeId(request.TargetBindingAlias) || !SafeId(request.IdempotencyKey) ||
            !string.Equals(request.TenantId, _tenant, StringComparison.Ordinal) ||
            !string.Equals(observedTenant, _tenant, StringComparison.Ordinal) ||
            !string.Equals(observedActor, _actor, StringComparison.Ordinal) ||
            !string.Equals(request.SourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            request.ExpectedRevision != observedOriginalRevision)
            return new("SANDBOX_UNTRUSTED_COMMAND_DENIED", 0);

        // Existing Domain/Application/Infrastructure guard is consulted before
        // making a synthetic projection; its nominal result never grants I/O.
        var preflight = await _isolated.EvaluateReadOnlyAsync(
            request, observedTenant, observedActor, observedOriginalRevision,
            observedSourceSha, cancellationToken);
        if (!preflight.RequiresSeparateAuthenticatedStorageAdapter)
            return new("SANDBOX_PREFLIGHT_DENIED", 0);

        cancellationToken.ThrowIfCancellationRequested();
        var fingerprint = string.Join("|", request.TenantId, request.SourceSha,
            request.TargetBindingAlias, request.ExpectedRevision,
            (int)request.TargetMode, (int)request.Intent);
        lock (_gate)
        {
            if (_stageKey is not null)
            {
                if (string.Equals(_stageKey, request.IdempotencyKey, StringComparison.Ordinal))
                    return new(string.Equals(_stageFingerprint, fingerprint,
                        StringComparison.Ordinal)
                        ? (_rolledBack ? "SANDBOX_ALREADY_ROLLED_BACK" : "SANDBOX_ALREADY_STAGED")
                        : "SANDBOX_IDEMPOTENCY_CONFLICT", _currentRevision);
                return new("SANDBOX_SINGLE_ATTEMPT_ONLY", _currentRevision);
            }
            if (_currentRevision != request.ExpectedRevision ||
                _currentRevision == long.MaxValue ||
                string.Equals(_currentBinding, request.TargetBindingAlias,
                    StringComparison.Ordinal))
                return new("SANDBOX_STALE_OR_NOOP_PLACEMENT", _currentRevision);

            _currentBinding = request.TargetBindingAlias!;
            _currentRevision++;
            _stageKey = request.IdempotencyKey;
            _stageFingerprint = fingerprint;
            return new("SANDBOX_STAGE_RECORDED_NO_LIVE_EFFECT", _currentRevision);
        }
    }

    public async Task<Outcome> RollbackAsync(
        Gate.Request? originalRequest, string? observedTenant, string? observedActor,
        string? observedSourceSha, long expectedStagedRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (originalRequest is null ||
            !string.Equals(observedTenant, _tenant, StringComparison.Ordinal) ||
            !string.Equals(observedActor, _actor, StringComparison.Ordinal) ||
            !string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(originalRequest.TenantId, _tenant, StringComparison.Ordinal) ||
            !string.Equals(originalRequest.SourceSha, _sourceSha, StringComparison.Ordinal))
            return new("SANDBOX_ROLLBACK_IDENTITY_DENIED", 0);

        // Re-check immutable synthetic policy before restoring any projection.
        var proof = await _isolated.EvaluateReadOnlyAsync(originalRequest,
            observedTenant, observedActor, originalRequest.ExpectedRevision,
            observedSourceSha, cancellationToken);
        if (!proof.RequiresSeparateAuthenticatedStorageAdapter)
            return new("SANDBOX_ROLLBACK_PREFLIGHT_DENIED", 0);

        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_stageKey is null || !string.Equals(_stageKey,
                    originalRequest.IdempotencyKey, StringComparison.Ordinal))
                return new("SANDBOX_UNKNOWN_ROLLBACK_KEY", _currentRevision);
            if (_rolledBack)
                return new("SANDBOX_ROLLBACK_ALREADY_RECORDED", _currentRevision);
            if (_currentRevision != expectedStagedRevision ||
                _currentRevision == long.MaxValue)
                return new("SANDBOX_ROLLBACK_CAS_MISMATCH", _currentRevision);

            _currentBinding = _originalBinding;
            // Monotonic revision prevents ABA/stale expected revision reuse.
            _currentRevision++;
            _rolledBack = true;
            return new("SANDBOX_ROLLBACK_RECORDED_NO_LIVE_EFFECT", _currentRevision);
        }
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
    private static bool SafeId(string? value) =>
        value is { Length: >= 2 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}