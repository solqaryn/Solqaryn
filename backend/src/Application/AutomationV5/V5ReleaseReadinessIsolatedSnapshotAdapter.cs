using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C14-04.M03.04: a read-only, isolated adapter boundary for trusted DEV
/// release-readiness snapshots. This interface is NOT registered with MySQL,
/// EF, cloud providers, restore jobs, QA or production.
/// </summary>
public interface IV5ReleaseReadinessSnapshotReader
{
    ValueTask<V5ReleaseReadinessIsolatedSnapshotAdapter.Snapshot?> ReadForTenantAsync(
        string verifiedTenantScope, string releaseId, CancellationToken cancellationToken);
}

public sealed class V5ReleaseReadinessIsolatedSnapshotAdapter
{
    private readonly IV5ReadinessAuthenticatedCallerSource _callers;
    private readonly V5ReleaseReadinessAuthorizationGate _gate;
    private readonly IV5ReleaseReadinessSnapshotReader _reader;

    public V5ReleaseReadinessIsolatedSnapshotAdapter(
        IV5ReadinessAuthenticatedCallerSource callers,
        V5ReleaseReadinessAuthorizationGate gate,
        IV5ReleaseReadinessSnapshotReader reader)
    {
        _callers = callers ?? throw new ArgumentNullException(nameof(callers));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public sealed record Request(string? ReleaseId, string? ExpectedCommitSha);

    /// <summary>
    /// Immutable metadata provided by an authenticated server-side reader.
    /// Flags are evidence assertions, never proof of a real database restoration.
    /// No payload, secrets or tenant resource contents are returned to the caller.
    /// </summary>
    public sealed record Snapshot(
        string? ReleaseId,
        string? CommitSha,
        string? TenantScope,
        string? ArtifactDigest,
        string? RollbackReference,
        long EvidenceVersion,
        bool IsImmutable,
        bool TenantOwnershipVerified,
        bool AuditChainVerified,
        bool ReadOnlyRollbackPlanVerified);

    public readonly record struct Verdict(string Code)
    {
        public bool NeedsExternalAuthenticatedReview =>
            Code == "READ_ONLY_SNAPSHOT_EXTERNAL_REVIEW_REQUIRED";
        public bool AllowsRelease => false;
        public bool AllowsDeployment => false;
        public bool AllowsInstitutionalWrite => false;
        public bool GrantsTenantAccess => false;
        public bool CertifiesDatabaseRestore => false;
        public bool ExposesSnapshotData => false;
    }

    public async Task<Verdict> InspectAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        if (request is null || !SafeReleaseId(request.ReleaseId) ||
            !LowercaseSha(request.ExpectedCommitSha))
            return Deny("INVALID_RELEASE_OR_SHA");

        cancellationToken.ThrowIfCancellationRequested();
        // Only server-verified identity can select a tenant partition.
        // There is deliberately no client-supplied tenant field in Request.
        var caller = await _callers.GetAuthenticatedCallerAsync(cancellationToken);
        if (caller is null || !caller.IsAuthenticated || !caller.IsMembershipActive ||
            !caller.HasReadinessReviewGrant ||
            !string.Equals(caller.Environment, "dev", StringComparison.Ordinal) ||
            !SafeTenant(caller.TenantScope))
            return Deny("AUTHENTICATED_DEV_REVIEWER_REQUIRED");

        // Re-evaluate full independent reviewer, authorization, source SHA and
        // required CI from the authenticated backend. A positive gate still
        // cannot grant access or authorize a deployment.
        var review = await _gate.EvaluateAsync(
            new V5ReleaseReadinessApplicationService.Request(
                request.ReleaseId, request.ExpectedCommitSha, request.ExpectedCommitSha),
            cancellationToken);
        if (!review.NeedsExternalIndependentReview)
            return Deny("TRUSTED_RELEASE_GATE_REQUIRED");

        // An isolated reader MUST use the verified tenant as a partition key.
        // This adapter does not provide a MySQL implementation or return raw data.
        var snapshot = await _reader.ReadForTenantAsync(
            caller.TenantScope, request.ReleaseId!, cancellationToken);
        if (snapshot is null ||
            !string.Equals(snapshot.ReleaseId, request.ReleaseId, StringComparison.Ordinal) ||
            !string.Equals(snapshot.CommitSha, request.ExpectedCommitSha, StringComparison.Ordinal) ||
            !string.Equals(snapshot.TenantScope, caller.TenantScope, StringComparison.Ordinal))
            return Deny("SNAPSHOT_TENANT_OR_SOURCE_MISMATCH");

        if (!Digest(snapshot.ArtifactDigest) ||
            string.IsNullOrWhiteSpace(snapshot.RollbackReference) ||
            snapshot.RollbackReference.Length > 256 ||
            snapshot.RollbackReference.Any(char.IsControl) ||
            snapshot.EvidenceVersion <= 0 ||
            !snapshot.IsImmutable || !snapshot.TenantOwnershipVerified ||
            !snapshot.AuditChainVerified || !snapshot.ReadOnlyRollbackPlanVerified)
            return Deny("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED");

        // Not a restoration, deployment, backup or tenant authorization.
        return new Verdict("READ_ONLY_SNAPSHOT_EXTERNAL_REVIEW_REQUIRED");
    }

    private static Verdict Deny(string code) => new(code);

    private static bool SafeReleaseId(string? value) =>
        value is { Length: >= 3 and <= 64 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_' or '.') &&
        !value.Contains("..", StringComparison.Ordinal);

    private static bool SafeTenant(string? value) =>
        value is { Length: >= 3 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool LowercaseSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool Digest(string? value) =>
        value is { Length: 71 } &&
        value.StartsWith("sha256:", StringComparison.Ordinal) &&
        value.Skip(7).All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}
