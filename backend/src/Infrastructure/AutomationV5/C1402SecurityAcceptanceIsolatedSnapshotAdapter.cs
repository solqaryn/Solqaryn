using Gate = Solqaryn.Application.AutomationV5.C1402SecurityAcceptanceAuthorizationGate;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C14-02.M03.04: inert, in-memory security acceptance evidence adapter.
/// This snapshot is not a real JWT, database, HTTP or audit authority.
/// No preflight result can authorize a read, release, restore or write.
/// </summary>
public sealed class C1402SecurityAcceptanceIsolatedSnapshotAdapter
{
    private readonly string _resourceKey;
    private readonly int _tenantId;
    private readonly int _actorId;
    private readonly string _sourceSha;
    private readonly Gate.VerifiedEvidence _captured;

    public C1402SecurityAcceptanceIsolatedSnapshotAdapter(
        string resourceKey, int tenantId, int actorId,
        string sourceSha, Gate.VerifiedEvidence evidence)
    {
        if (!SafeResourceKey(resourceKey))
            throw new ArgumentException("Safe resource identifier required.", nameof(resourceKey));
        if (tenantId <= 0) throw new ArgumentOutOfRangeException(nameof(tenantId));
        if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
        if (!ValidSha(sourceSha))
            throw new ArgumentException("Full source SHA required.", nameof(sourceSha));
        ArgumentNullException.ThrowIfNull(evidence);
        _resourceKey = resourceKey;
        _tenantId = tenantId;
        _actorId = actorId;
        _sourceSha = sourceSha;
        // Nested records contain only immutable scalar values; clone to
        // document the snapshot boundary instead of retaining a reader.
        _captured = evidence with
        {
            Authority = evidence.Authority is null ? null : evidence.Authority with { },
            Target = evidence.Target is null ? null : evidence.Target with { },
            Attempt = evidence.Attempt is null ? null : evidence.Attempt with { }
        };
    }

    public bool IsLiveProvider => false;
    public bool GrantsTenantOrResourceAccess => false;
    public bool GrantsInstitutionalWrite => false;
    public bool GrantsFileDownload => false;
    public bool GrantsJobExecution => false;
    public bool GrantsRestoreOrDeployment => false;
    public bool AllowsTenantFilterBypass => false;
    public bool CertifiesJwtHttpMySqlOrPentest => false;

    public ValueTask<Gate.Decision> EvaluateReadOnlyAsync(
        Gate.Request? request,
        string? observedResourceKey,
        int observedTenantId,
        int observedActorId,
        string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null)
            return Denied("ISOLATED_SECURITY_REQUEST_REQUIRED");
        if (!string.Equals(observedResourceKey, _resourceKey, StringComparison.Ordinal) ||
            !string.Equals(request.ResourceKey, _resourceKey, StringComparison.Ordinal))
            return Denied("ISOLATED_SECURITY_RESOURCE_MISMATCH");
        if (observedTenantId != _tenantId)
            return Denied("ISOLATED_SECURITY_TENANT_MISMATCH");
        if (observedActorId != _actorId)
            return Denied("ISOLATED_SECURITY_ACTOR_MISMATCH");
        if (!string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(request.ExpectedCommitSha, _sourceSha, StringComparison.Ordinal))
            return Denied("ISOLATED_SECURITY_SHA_MISMATCH");

        // A matching browser hint cannot launder a contradictory persisted
        // resource, tenant, actor, or evidence source into trusted authority.
        if (!string.Equals(_captured.PersistedResourceKey, _resourceKey, StringComparison.Ordinal) ||
            !string.Equals(_captured.ObservedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            _captured.Authority is null || _captured.Target is null ||
            _captured.Attempt is null ||
            _captured.Authority.ActorId != _actorId ||
            _captured.Authority.ServerTenantId != _tenantId ||
            _captured.Target.OwnerTenantId != _tenantId ||
            !string.Equals(_captured.Authority.EvidenceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(_captured.Attempt.EvidenceSha, _sourceSha, StringComparison.Ordinal))
            return Denied("ISOLATED_CAPTURED_SECURITY_EVIDENCE_MISMATCH");

        // Reuse Domain + Application validation including BOLA, revoked
        // resources, branch, host, signed links, RBAC and audit boundaries.
        // A nominal result STILL requires a separate authenticated adapter.
        return new Gate(new CapturedReadOnlyReader(_resourceKey, _captured))
            .CheckAsync(request, cancellationToken);
    }

    private sealed class CapturedReadOnlyReader : IC1402SecurityAcceptanceEvidenceReader
    {
        private readonly string _resourceKey;
        private readonly Gate.VerifiedEvidence _snapshot;
        public CapturedReadOnlyReader(string resourceKey, Gate.VerifiedEvidence snapshot)
        {
            _resourceKey = resourceKey;
            _snapshot = snapshot;
        }

        public ValueTask<Gate.VerifiedEvidence?> ReadAsync(
            string resourceKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(resourceKey, _resourceKey, StringComparison.Ordinal))
                return ValueTask.FromResult<Gate.VerifiedEvidence?>(null);
            return ValueTask.FromResult<Gate.VerifiedEvidence?>(_snapshot);
        }
    }

    private static ValueTask<Gate.Decision> Denied(string code) =>
        ValueTask.FromResult(new Gate.Decision(code));

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    private static bool SafeResourceKey(string? value) =>
        value is { Length: >= 1 and <= 96 } && value.All(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c is '-' or '_');
}
