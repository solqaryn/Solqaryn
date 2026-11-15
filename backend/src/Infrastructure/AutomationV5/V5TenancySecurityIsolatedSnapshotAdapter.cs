using Gate = Solqaryn.Application.AutomationV5.V5TenancySecurityAuthorizationGate;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-03.M03.04: isolated snapshot adapter for negative tenancy/security tests.
/// This is NOT an EF provider, HTTP authentication source, permission grant,
/// production reader or a way to disable tenant query filters.
/// </summary>
public sealed class V5TenancySecurityIsolatedSnapshotAdapter
{
    private readonly int _resourceId;
    private readonly int _tenantId;
    private readonly int _actorId;
    private readonly string _sourceSha;
    private readonly Gate.VerifiedEvidence _captured;

    public V5TenancySecurityIsolatedSnapshotAdapter(
        int resourceId, int tenantId, int actorId,
        string sourceSha, Gate.VerifiedEvidence evidence)
    {
        if (resourceId <= 0) throw new ArgumentOutOfRangeException(nameof(resourceId));
        if (tenantId <= 0) throw new ArgumentOutOfRangeException(nameof(tenantId));
        if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
        if (!FullSha(sourceSha)) throw new ArgumentException("Full source SHA required.", nameof(sourceSha));
        ArgumentNullException.ThrowIfNull(evidence);

        _resourceId = resourceId;
        _tenantId = tenantId;
        _actorId = actorId;
        _sourceSha = sourceSha;

        // Every nested authority/resource member is a scalar/immutable record.
        // Capture the evidence at construction; never expose a mutable provider.
        _captured = evidence with
        {
            Authority = evidence.Authority is null ? null : evidence.Authority with { },
            Resource = evidence.Resource is null ? null : evidence.Resource with { }
        };
    }

    public bool IsLiveEvidenceProvider => false;
    public bool GrantsAccess => false;
    public bool GrantsFileDownload => false;
    public bool GrantsBackgroundJob => false;
    public bool GrantsControlPlane => false;
    public bool GrantsInstitutionalWrite => false;
    public bool AllowsPersistence => false;
    public bool AllowsTenantFilterBypass => false;
    public bool CertifiesHttpJwtOrMySql => false;

    public ValueTask<Gate.Result> EvaluateReadOnlyAsync(
        Gate.Request? request,
        int observedResourceId,
        int observedTenantId,
        int observedActorId,
        string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null)
            return Denied("ISOLATED_SECURITY_REQUEST_REQUIRED");
        if (observedResourceId != _resourceId ||
            request.ResourceId != _resourceId)
            return Denied("ISOLATED_SECURITY_RESOURCE_MISMATCH");
        if (observedTenantId != _tenantId)
            return Denied("ISOLATED_SECURITY_TENANT_MISMATCH");
        if (observedActorId != _actorId)
            return Denied("ISOLATED_SECURITY_ACTOR_MISMATCH");
        if (!string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(request.ExpectedSourceSha, _sourceSha, StringComparison.Ordinal))
            return Denied("ISOLATED_SECURITY_SHA_MISMATCH");

        // A caller cannot make its observed identifiers true merely by
        // matching the request: the captured evidence must also agree.
        if (_captured.VerifiedResourceId != _resourceId ||
            _captured.VerifiedTenantId != _tenantId ||
            _captured.VerifiedActorId != _actorId ||
            !string.Equals(_captured.ObservedSourceSha, _sourceSha, StringComparison.Ordinal))
            return Denied("ISOLATED_SECURITY_CAPTURED_EVIDENCE_MISMATCH");

        // Reuse actual Application + Domain gates for roles, membership,
        // branch, file expiry, service grants, idempotency and revocation.
        // A successful preflight STILL requires an authenticated execution
        // adapter and never produces a read/write/job/tenant-bypass grant.
        return new Gate(new CapturedReadOnlyReader(_captured))
            .EvaluateAsync(request, cancellationToken);
    }

    private sealed class CapturedReadOnlyReader : IV5TenancySecurityVerifiedAuthorizationEvidenceReader
    {
        private readonly Gate.VerifiedEvidence _snapshot;
        public CapturedReadOnlyReader(Gate.VerifiedEvidence snapshot) => _snapshot = snapshot;

        public ValueTask<Gate.VerifiedEvidence?> ReadVerifiedAsync(
            int resourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Gate.VerifiedEvidence?>(_snapshot);
        }
    }

    private static ValueTask<Gate.Result> Denied(string code) =>
        ValueTask.FromResult(new Gate.Result(code));

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}
