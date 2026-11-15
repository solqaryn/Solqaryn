namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C14-02.M03.01: fail-closed, pure preflight of security acceptance invariants.
/// Only a trusted backend adapter may supply verified authority and persisted
/// ownership. No verdict authorizes access, CI certification or release.
/// </summary>
public static class V5SecurityAcceptanceDomainRules
{
    public enum Surface
    {
        BusinessRead, BusinessWrite, BranchRead, HostResolve,
        FileDownload, BackgroundJob, CacheRead, BackupRestore
    }

    public sealed record VerifiedAuthority(
        int? ActorId,
        int? ServerTenantId,
        int? ServerBranchId,
        string? EvidenceSha,
        bool JwtVerified,
        bool TenantResolvedOnServer,
        bool ActiveMembershipVerified,
        bool RequiredPermissionVerified,
        bool PermissionSnapshotCurrent,
        bool ServiceActorVerified,
        bool ServiceGrantVerified);

    public sealed record PersistedTarget(
        int? OwnerTenantId,
        int? OwnerBranchId,
        int? BoundHostTenantId,
        bool OwnershipVerifiedFromStore,
        bool HostBindingVerified,
        bool AccessRevoked,
        bool SignedLinkExpired);

    public sealed record Attempt(
        Surface Surface,
        int? ClientTenantOverride,
        string? EvidenceSha,
        bool RequestsTenantFilterBypass,
        bool RequestsCrossTenantEnumeration,
        bool IdempotencyEvidenceVerified);

    public readonly record struct Verdict(string Code)
    {
        public bool RequiresTrustedIntegration =>
            Code == "READY_FOR_TRUSTED_SECURITY_INTEGRATION";

        // Authentication, resource access and releases are never granted
        // by a caller-provided snapshot or a pure domain helper.
        public bool AuthorizesResourceAccess => false;
        public bool AuthorizesRelease => false;
    }

    public static Verdict Evaluate(
        VerifiedAuthority? authority, PersistedTarget? target, Attempt? attempt)
    {
        if (authority is null || target is null || attempt is null ||
            authority.ActorId is not > 0 ||
            authority.ServerTenantId is not > 0 ||
            !authority.JwtVerified || !authority.TenantResolvedOnServer ||
            !authority.ActiveMembershipVerified || !authority.RequiredPermissionVerified ||
            !authority.PermissionSnapshotCurrent)
            return new Verdict("SERVER_VERIFIED_AUTHORITY_REQUIRED");

        if (!Enum.IsDefined(attempt.Surface))
            return new Verdict("UNKNOWN_SECURITY_SURFACE");

        if (!FullSha(authority.EvidenceSha) ||
            !string.Equals(authority.EvidenceSha, attempt.EvidenceSha,
                StringComparison.Ordinal))
            return new Verdict("EXACT_EVIDENCE_SHA_REQUIRED");

        if (attempt.RequestsTenantFilterBypass ||
            attempt.RequestsCrossTenantEnumeration)
            return new Verdict("TENANT_FILTER_OR_ENUMERATION_BYPASS_DENIED");

        if (attempt.ClientTenantOverride is not null &&
            attempt.ClientTenantOverride != authority.ServerTenantId)
            return new Verdict("CLIENT_TENANT_SPOOFING_DENIED");

        if (!target.OwnershipVerifiedFromStore ||
            target.OwnerTenantId is not > 0 ||
            target.OwnerTenantId != authority.ServerTenantId)
            return new Verdict("BOLA_RESOURCE_TENANT_MISMATCH");

        if (target.AccessRevoked)
            return new Verdict("RESOURCE_OR_PERMISSION_REVOKED");

        if (attempt.Surface == Surface.BranchRead ||
            (attempt.Surface == Surface.BusinessWrite && target.OwnerBranchId is not null))
        {
            if (authority.ServerBranchId is not > 0 ||
                target.OwnerBranchId is not > 0 ||
                authority.ServerBranchId != target.OwnerBranchId)
                return new Verdict("BRANCH_OWNER_MISMATCH");
        }

        if (attempt.Surface == Surface.HostResolve)
        {
            if (!target.HostBindingVerified ||
                target.BoundHostTenantId is not > 0 ||
                target.BoundHostTenantId != authority.ServerTenantId)
                return new Verdict("UNTRUSTED_OR_CROSS_TENANT_HOST");
        }

        if (attempt.Surface == Surface.FileDownload && target.SignedLinkExpired)
            return new Verdict("FILE_LINK_EXPIRED");

        if (attempt.Surface == Surface.BackgroundJob &&
            (!authority.ServiceActorVerified ||
             !authority.ServiceGrantVerified ||
             !attempt.IdempotencyEvidenceVerified))
            return new Verdict("JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED");

        if (attempt.Surface == Surface.BackupRestore)
            return new Verdict("SEPARATE_SANDBOX_RESTORE_AUTHORIZATION_REQUIRED");

        // This is not an observed HTTP/EF/MySQL security acceptance test:
        // a trusted API/application adapter must still enforce access,
        // prove tenant A/B negatives and submit real evidence for A0-A5.
        return new Verdict("READY_FOR_TRUSTED_SECURITY_INTEGRATION");
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
