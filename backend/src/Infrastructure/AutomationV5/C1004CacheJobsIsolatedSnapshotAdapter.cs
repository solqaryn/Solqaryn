using Solqaryn.Application.AutomationV5;
using Gate = Solqaryn.Application.AutomationV5.C1004CacheJobsAuthorizationGate;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C10-04.M03.04: isolated read-only snapshot adapter for cache/job/export
/// negative tests. It does not connect to EF/MySQL/R2, obtain a persistent
/// lease, read an actual JWT or enqueue/restore anything.
/// </summary>
public sealed class C1004CacheJobsIsolatedSnapshotAdapter
{
    private readonly string _workId;
    private readonly int _tenantId;
    private readonly int _actorId;
    private readonly long _resourceVersion;
    private readonly C1004CacheJobsApplicationService.VerifiedSnapshot _captured;

    public C1004CacheJobsIsolatedSnapshotAdapter(
        string workId, int tenantId, int actorId, long resourceVersion,
        C1004CacheJobsApplicationService.VerifiedSnapshot verifiedSnapshot)
    {
        if (!SafeId(workId)) throw new ArgumentException("Valid work identifier required.", nameof(workId));
        if (tenantId <= 0) throw new ArgumentOutOfRangeException(nameof(tenantId));
        if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
        if (resourceVersion <= 0) throw new ArgumentOutOfRangeException(nameof(resourceVersion));
        ArgumentNullException.ThrowIfNull(verifiedSnapshot);
        _workId = workId;
        _tenantId = tenantId;
        _actorId = actorId;
        _resourceVersion = resourceVersion;

        // Never keep an externally mutable permission set. The captured
        // snapshot is only synthetic evidence for an isolated preflight test.
        _captured = verifiedSnapshot with
        {
            AllowedOperations = verifiedSnapshot.AllowedOperations is null
                ? null
                : new HashSet<string>(verifiedSnapshot.AllowedOperations, StringComparer.Ordinal)
        };
    }

    public bool IsLiveProvider => false;
    public bool CanEnqueue => false;
    public bool CanWrite => false;
    public bool CanAcquireLease => false;
    public bool CanGrantProviderAccess => false;
    public bool CanReadPrivateArtifact => false;
    public bool CanBypassTenantFilters => false;
    public bool CertifiesJwtMySqlOrR2 => false;

    public Task<Gate.Decision> EvaluateReadOnlyAsync(
        C1004CacheJobsApplicationService.Request? request,
        string? observedWorkId,
        int observedTenantId,
        int observedActorId,
        long observedResourceVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null)
            return Deny("ISOLATED_WORK_REQUEST_REQUIRED");
        if (!string.Equals(observedWorkId, _workId, StringComparison.Ordinal) ||
            !string.Equals(request.WorkId, _workId, StringComparison.Ordinal))
            return Deny("ISOLATED_WORK_ID_MISMATCH");
        if (observedTenantId != _tenantId)
            return Deny("ISOLATED_TENANT_MISMATCH");
        if (observedActorId != _actorId)
            return Deny("ISOLATED_ACTOR_MISMATCH");
        if (observedResourceVersion != _resourceVersion ||
            request.ExpectedVersion != _resourceVersion)
            return Deny("ISOLATED_VERSION_MISMATCH");

        // The snapshot must agree independently with every observed value.
        // A browser-provided tenant hint is never an authority source.
        if (_captured.VerifiedActorId != _actorId ||
            _captured.PersistedResourceEmpresaId != _tenantId ||
            _captured.PersistedVersion != _resourceVersion ||
            _captured.Context?.UsuarioId != _actorId ||
            _captured.Context.EmpresaId != _tenantId)
            return Deny("ISOLATED_CAPTURED_AUTHORITY_MISMATCH");

        // Delegate membership, permission, service-grant, allowlist, retry,
        // deadline, idempotency and payload checks to existing gates.
        // Even a positive verdict remains NON-AUTHORIZING.
        return new Gate(new CapturedSource(_workId, _captured))
            .CheckAsync(request, cancellationToken);
    }

    private sealed class CapturedSource : IC1004VerifiedWorkEvidenceSource
    {
        private readonly string _workId;
        private readonly C1004CacheJobsApplicationService.VerifiedSnapshot _snapshot;
        public CapturedSource(
            string workId, C1004CacheJobsApplicationService.VerifiedSnapshot snapshot)
        {
            _workId = workId;
            _snapshot = snapshot;
        }

        public ValueTask<C1004CacheJobsApplicationService.VerifiedSnapshot?> ReadVerifiedAsync(
            string workId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(workId, _workId, StringComparison.Ordinal))
                return ValueTask.FromResult<C1004CacheJobsApplicationService.VerifiedSnapshot?>(null);
            return ValueTask.FromResult<C1004CacheJobsApplicationService.VerifiedSnapshot?>(_snapshot);
        }
    }

    private static Task<Gate.Decision> Deny(string code) =>
        Task.FromResult(new Gate.Decision(code));

    private static bool SafeId(string? value) =>
        value is { Length: >= 1 and <= 128 } && value.All(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c is '-' or '_');
}
