namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C01-04.M03.01: pure fail-closed universal case-file domain guard.
/// Caller flags are not authentication. API/Application must verify JWT, live
/// membership, ownership in persistence and row-version concurrency.
/// This helper never authorizes SQL writes or external attachments.
/// </summary>
public static class V5UniversalCaseFileDomainRules
{
    public enum CaseAction { ReadTimeline, AddObservation, RecordDecision, LinkAttachment }

    public sealed record ServerAuthority(
        int? ActorId, int? TenantId, int? BranchId,
        bool Authenticated, bool MembershipActive,
        bool ExplicitPermissionCurrent, bool ScopeResolvedOnServer);

    public sealed record StoredCase(
        int? OwnerTenantId, int? OwnerBranchId, long Version,
        bool OwnershipVerifiedFromPersistence,
        bool UnattributedLegacy, bool Archived);

    public sealed record Command(
        CaseAction Action, int? ClientSelectedTenantId,
        long? ExpectedVersion, string? IdempotencyKey,
        string? ActorReason, bool AttachmentOwnershipVerified,
        bool AttachmentLinkExpired, bool IndependentDecisionReviewVerified);

    public readonly record struct Verdict(string Code)
    {
        public bool RequiresTrustedApplicationEnforcement =>
            Code == "READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT";

        public bool AuthorizesPersistence => false;
        public bool AuthorizesExternalMediaAccess => false;
    }

    public static Verdict Evaluate(ServerAuthority? authority, StoredCase? stored, Command? command)
    {
        if (authority is null || stored is null || command is null ||
            authority.ActorId is not > 0 || authority.TenantId is not > 0 ||
            !authority.Authenticated || !authority.MembershipActive ||
            !authority.ExplicitPermissionCurrent || !authority.ScopeResolvedOnServer)
            return new Verdict("SERVER_AUTHORITY_REQUIRED");

        if (!Enum.IsDefined(command.Action))
            return new Verdict("UNKNOWN_CASEFILE_ACTION");

        if (command.ClientSelectedTenantId is not null &&
            command.ClientSelectedTenantId != authority.TenantId)
            return new Verdict("CLIENT_TENANT_OVERRIDE_DENIED");

        if (!stored.OwnershipVerifiedFromPersistence ||
            stored.UnattributedLegacy || stored.OwnerTenantId is not > 0 ||
            stored.OwnerTenantId != authority.TenantId || stored.Version < 0)
            return new Verdict("CASEFILE_OWNER_OR_LEGACY_SCOPE_DENIED");

        if (stored.OwnerBranchId is not null &&
            (stored.OwnerBranchId is not > 0 ||
             authority.BranchId != stored.OwnerBranchId))
            return new Verdict("CASEFILE_BRANCH_MISMATCH");

        if (command.Action == CaseAction.ReadTimeline)
            return new Verdict("READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT");

        if (stored.Archived)
            return new Verdict("ARCHIVED_CASEFILE_IS_READ_ONLY");

        if (command.ExpectedVersion is null ||
            command.ExpectedVersion != stored.Version)
            return new Verdict("CASEFILE_VERSION_CONFLICT");

        if (!SafeIdempotencyKey(command.IdempotencyKey))
            return new Verdict("SCOPED_IDEMPOTENCY_KEY_REQUIRED");

        if (!SafeReason(command.ActorReason))
            return new Verdict("AUDIT_REASON_REQUIRED");

        if (command.Action == CaseAction.RecordDecision &&
            !command.IndependentDecisionReviewVerified)
            return new Verdict("DECISION_REVIEW_REQUIRED");

        if (command.Action == CaseAction.LinkAttachment &&
            (!command.AttachmentOwnershipVerified || command.AttachmentLinkExpired))
            return new Verdict("ATTACHMENT_ACCESS_DENIED");

        // The application adapter must still revalidate all snapshots and
        // atomically persist the timeline version + scoped idempotency receipt.
        return new Verdict("READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT");
    }

    private static bool SafeIdempotencyKey(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => c >= '!' && c <= '~');

    private static bool SafeReason(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 256 &&
        value.All(c => !char.IsControl(c));
}
