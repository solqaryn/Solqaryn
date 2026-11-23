namespace Solqaryn.Domain.Security;

/// <summary>
/// C03-01.M03.01: deterministic 0/1/N domain resolution from current
/// server-verified memberships. Neither preference nor this decision is
/// authentication, JWT validation, RBAC grant or tenant authorization.
/// </summary>
public static class C0301LoginTenantResolutionDomainRules
{
    public sealed record Membership(
        int UserId,
        int TenantId,
        bool UserActive,
        bool TenantActive,
        bool MembershipActive,
        bool RoleActiveAndAssignableToTenant,
        bool CurrentServerRevalidationCompleted);

    public sealed record Request(
        int? AuthenticatedUserId,
        IReadOnlyCollection<Membership>? Memberships,
        int? LastPreferredTenantId,
        int? PreferenceOwnerUserId,
        bool IdentityVerifiedByServer,
        bool AccountLockedOrRevoked,
        bool PlatformAuthorityVerified,
        bool ExplicitPlatformConsoleGrantVerified,
        bool ClientTenantOverrideAttempted);

    public sealed record Decision(
        string Code,
        int? CandidateTenantId,
        IReadOnlyList<int> ValidTenantIds)
    {
        // No pure function can establish signed JWT, active SQL role, or issue
        // credentials: every candidate requires Application/API server checks.
        public bool GrantsTenantAccess => false;
        public bool GrantsPlatformAccess => false;
    }

    private static Decision Stop(string code) => new(code, null, Array.Empty<int>());

    public static Decision Resolve(Request? request)
    {
        if (request is null ||
            !request.IdentityVerifiedByServer ||
            request.AuthenticatedUserId is not > 0 ||
            request.AccountLockedOrRevoked)
            return Stop("TRUSTED_ACTIVE_IDENTITY_REQUIRED");

        if (request.ClientTenantOverrideAttempted)
            return Stop("CLIENT_TENANT_OVERRIDE_DENIED");

        if (request.Memberships is null)
            return Stop("FRESH_SERVER_MEMBERSHIP_EVIDENCE_REQUIRED");

        // A missing or inconsistent preference owner is not a fallback to a
        // different user's hint; reject the hint rather than the whole login.
        var preferred = request.PreferenceOwnerUserId == request.AuthenticatedUserId
            ? request.LastPreferredTenantId : null;

        if (request.Memberships.Any(m =>
            m.UserId != request.AuthenticatedUserId ||
            m.TenantId <= 0))
            return Stop("FOREIGN_OR_INVALID_MEMBERSHIP_SNAPSHOT");

        if (request.Memberships.GroupBy(m => m.TenantId).Any(g => g.Count() != 1))
            return Stop("DUPLICATE_TENANT_MEMBERSHIP_DENIED");

        var valid = request.Memberships
            .Where(m => m.UserActive && m.TenantActive &&
                        m.MembershipActive && m.RoleActiveAndAssignableToTenant &&
                        m.CurrentServerRevalidationCompleted)
            .Select(m => m.TenantId)
            .OrderBy(id => id)
            .ToArray();

        // Inactive or not-current membership rows NEVER select a tenant.
        if (valid.Length == 0)
        {
            if (request.PlatformAuthorityVerified &&
                request.ExplicitPlatformConsoleGrantVerified)
                return new Decision("SEPARATE_PLATFORM_LOGIN_REQUIRES_SERVER_GRANT", null, valid);
            return new Decision("NO_VALID_ENTERPRISE_MEMBERSHIP", null, valid);
        }

        if (valid.Length == 1)
            return new Decision("SINGLE_TENANT_REQUIRES_SERVER_ENFORCEMENT", valid[0], valid);

        if (preferred is > 0 && valid.Contains(preferred.Value))
            return new Decision("VALID_PREFERENCE_REQUIRES_SERVER_ENFORCEMENT", preferred, valid);

        return new Decision("EXPLICIT_TENANT_SELECTION_REQUIRED", null, valid);
    }
}
