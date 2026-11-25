using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5UniversalCaseFileDomainRules;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Trusted, current server identity/membership provider, not a request DTO or a JWT claim alone.
/// An implementation MUST reconcile membership and permission against its authoritative store.
/// </summary>
public interface IV5CaseFileTrustedActorSnapshotSource
{
    ValueTask<V5UniversalCaseFileIsolatedReadAdapter.ActorSnapshot?> ReadCurrentAsync(
        CancellationToken cancellationToken);

    // The requested action and resource are selectors, never authority. Legacy
    // providers cannot attest the exact resource/operation; deny by default.
    ValueTask<V5UniversalCaseFileIsolatedReadAdapter.ActorSnapshot?> ReadCurrentForActionAsync(
        int caseFileId, Rules.CaseAction action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<V5UniversalCaseFileIsolatedReadAdapter.ActorSnapshot?>(null);
    }
}

/// <summary>
/// Tenant-keyed, read-only case lookup. Implementations MUST filter by both tenantId
/// and caseFileId at the persistent source and must not infer ownership from an ID.
/// </summary>
public interface IV5CaseFileTenantScopedSnapshotSource
{
    ValueTask<V5UniversalCaseFileIsolatedReadAdapter.CaseSnapshot?> ReadAsync(
        int tenantId, int caseFileId, CancellationToken cancellationToken);
}

/// <summary>
/// C01-04.M03.04: isolated, read-only Infrastructure bridge to the existing
/// Application authorization preflight. No concrete database, DI registration, transaction,
/// mutation, JWT grant or public endpoint is installed by this component.
/// </summary>
public sealed class V5UniversalCaseFileIsolatedReadAdapter : IV5UniversalCaseFileVerifiedEvidenceReader
{
    private readonly IV5CaseFileTrustedActorSnapshotSource _actors;
    private readonly IV5CaseFileTenantScopedSnapshotSource _cases;

    public V5UniversalCaseFileIsolatedReadAdapter(
        IV5CaseFileTrustedActorSnapshotSource actors,
        IV5CaseFileTenantScopedSnapshotSource cases)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _cases = cases ?? throw new ArgumentNullException(nameof(cases));
    }

    /// <summary>
    /// Only authoritative backend sources may construct these snapshots.
    /// All flags must be freshly verified, not copied from client input.
    /// </summary>
    public sealed record ActorSnapshot(
        int ActorId, int TenantId, int? BranchId, string? SourceSha,
        bool ServerAuthenticationVerified, bool MembershipReadFromTrustedBackend,
        bool PermissionReadFromTrustedBackend, bool PermissionRevoked,
        bool MembershipActive, bool ExplicitPermissionGranted)
    {
        public int? VerifiedCaseFileId { get; init; }
        public Rules.CaseAction? VerifiedAction { get; init; }
    }

    public sealed record CaseSnapshot(
        int CaseFileId, int OwnerTenantId, int? OwnerBranchId,
        long Version, string? SourceSha, bool OwnershipVerifiedFromPersistence,
        bool UnattributedLegacy, bool Archived,
        bool IndependentDecisionReviewVerified,
        bool AttachmentOwnershipVerified, bool AttachmentLinkExpired);

    public ValueTask<V5UniversalCaseFileAuthorizationGate.VerifiedEvidence?> ReadAsync(
        int caseFileId, CancellationToken cancellationToken) =>
        ReadForActionAsync(caseFileId, Rules.CaseAction.ReadTimeline, cancellationToken);

    public async ValueTask<V5UniversalCaseFileAuthorizationGate.VerifiedEvidence?> ReadForActionAsync(
        int caseFileId, Rules.CaseAction action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Fail before touching either source for an invalid resource.
        if (caseFileId <= 0 || !Enum.IsDefined(action))
            return null;

        var actor = await _actors.ReadCurrentForActionAsync(caseFileId, action, cancellationToken);
        if (actor is null || actor.VerifiedCaseFileId != caseFileId ||
            actor.VerifiedAction != action || actor.ActorId <= 0 || actor.TenantId <= 0 ||
            actor.BranchId is <= 0 || !FullSha(actor.SourceSha) ||
            !actor.ServerAuthenticationVerified ||
            !actor.MembershipReadFromTrustedBackend ||
            !actor.PermissionReadFromTrustedBackend || actor.PermissionRevoked ||
            !actor.MembershipActive || !actor.ExplicitPermissionGranted)
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        // Trusted-source provenance does not prove an active membership or an
        // affirmative permission. These are checked before case-file I/O.
        // The tenant identifier originates from the trusted source, never from a URL/header.
        var stored = await _cases.ReadAsync(actor.TenantId, caseFileId, cancellationToken);
        if (stored is null || stored.CaseFileId != caseFileId ||
            stored.OwnerTenantId != actor.TenantId ||
            stored.OwnerBranchId is <= 0 || stored.Version < 0 ||
            !stored.OwnershipVerifiedFromPersistence || stored.UnattributedLegacy ||
            !FullSha(stored.SourceSha) ||
            !string.Equals(actor.SourceSha, stored.SourceSha, StringComparison.Ordinal) ||
            (stored.OwnerBranchId.HasValue &&
             actor.BranchId != stored.OwnerBranchId))
            return null;

        var authority = new Rules.ServerAuthority(
            actor.ActorId, actor.TenantId, actor.BranchId,
            actor.ServerAuthenticationVerified, actor.MembershipActive,
            actor.ExplicitPermissionGranted, true);
        var caseFile = new Rules.StoredCase(
            stored.OwnerTenantId, stored.OwnerBranchId, stored.Version,
            true, false, stored.Archived);

        // This is evidence for a SECOND, independent Application gate.
        // It does not authorize a write, read response, attachment, or transaction.
        return new V5UniversalCaseFileAuthorizationGate.VerifiedEvidence(
            actor.SourceSha, stored.CaseFileId, actor.ActorId, actor.TenantId,
            actor.ServerAuthenticationVerified, actor.MembershipReadFromTrustedBackend,
            actor.PermissionReadFromTrustedBackend, actor.PermissionRevoked,
            stored.OwnershipVerifiedFromPersistence, authority, caseFile,
            stored.IndependentDecisionReviewVerified,
            stored.AttachmentOwnershipVerified, stored.AttachmentLinkExpired)
        {
            VerifiedAction = actor.VerifiedAction
        };
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
