namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C15-03.M03.01: pure fail-closed tenant/branch/ownership guard.
/// The backend MUST create trusted context and persisted snapshots; browser
/// headers, route IDs, and this verdict never authorize EF query bypasses.
/// </summary>
public static class V5TenancySecurityDomainRules
{
    public enum Surface { BusinessRead, BranchRead, FileDownload, BackgroundJob, ControlPlane }

    public sealed record TrustedAuthority(
        int? AuthenticatedTenantId,
        int? AuthenticatedActorId,
        int? AuthenticatedBranchId,
        bool ActiveMembershipVerified,
        bool RequiredRoleVerified,
        bool PrivilegedServiceGrantVerified,
        bool AuditedServiceContextVerified);

    public sealed record PersistedResource(
        int? OwnerTenantId,
        int? ParentTenantId,
        int? OwnerBranchId,
        bool OwnershipReadFromPersistence,
        bool AccessRevoked,
        bool SignedLinkExpired);

    public sealed record Request(
        Surface Surface,
        bool BypassesTenantFilters,
        string? IdempotencyKey,
        bool AttemptsCrossTenantEnumeration);

    public readonly record struct Verdict(string Code)
    {
        // This is only a precondition; Application/API must enforce real auth and
        // Infrastructure must enforce query filters and transactional ownership.
        public bool RequiresServerEnforcement => Code == "READY_FOR_SERVER_SECURITY_ENFORCEMENT";
    }

    public static Verdict Evaluate(TrustedAuthority? authority, PersistedResource? resource, Request? request)
    {
        if (authority is null || resource is null || request is null ||
            authority.AuthenticatedTenantId is not > 0 ||
            authority.AuthenticatedActorId is not > 0 ||
            !authority.ActiveMembershipVerified || !authority.RequiredRoleVerified)
            return new Verdict("VERIFIED_SERVER_AUTHORITY_REQUIRED");

        if (!Enum.IsDefined(request.Surface))
            return new Verdict("UNKNOWN_SECURITY_SURFACE");

        // A helper must never treat IgnoreQueryFilters or absent trusted tenant
        // scope as an alternate authorization for any surface.
        if (request.BypassesTenantFilters || request.AttemptsCrossTenantEnumeration)
            return new Verdict("TENANT_QUERY_BYPASS_DENIED");

        var tenant = authority.AuthenticatedTenantId.Value;
        if (!resource.OwnershipReadFromPersistence ||
            resource.OwnerTenantId is not > 0 ||
            resource.ParentTenantId is not > 0 ||
            resource.OwnerTenantId != tenant ||
            resource.ParentTenantId != tenant)
            return new Verdict("RESOURCE_OWNER_MISMATCH");

        if (request.Surface == Surface.BranchRead)
        {
            if (authority.AuthenticatedBranchId is not > 0 ||
                resource.OwnerBranchId is not > 0 ||
                authority.AuthenticatedBranchId != resource.OwnerBranchId)
                return new Verdict("BRANCH_SCOPE_MISMATCH");
        }

        if (request.Surface == Surface.FileDownload &&
            (resource.AccessRevoked || resource.SignedLinkExpired))
            return new Verdict("FILE_ACCESS_REVOKED_OR_EXPIRED");

        if (request.Surface == Surface.BackgroundJob)
        {
            if (!authority.PrivilegedServiceGrantVerified ||
                !authority.AuditedServiceContextVerified ||
                string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
                request.IdempotencyKey.Length > 128 ||
                !request.IdempotencyKey.All(c => c >= '!' && c <= '~'))
                return new Verdict("SERVICE_JOB_AUTHORITY_REQUIRED");
        }

        if (request.Surface == Surface.ControlPlane &&
            (!authority.PrivilegedServiceGrantVerified || !authority.AuditedServiceContextVerified))
            return new Verdict("CONTROL_PLANE_AUTHORITY_REQUIRED");

        // No EF filters are disabled; no row, download, job or admin action
        // is performed by this pure rule.
        return new Verdict("READY_FOR_SERVER_SECURITY_ENFORCEMENT");
    }
}
