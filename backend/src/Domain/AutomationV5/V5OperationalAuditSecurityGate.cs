namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Pure fail-closed tenant/permission preflight for the V5 operational audit.
/// Inputs are untrusted until a server-side adapter independently authenticates
/// their origin. A positive preflight NEVER authorizes a Sheets mutation, CI
/// certification, workflow dispatch, or an external resource operation.
/// </summary>
public static class V5OperationalAuditSecurityGate
{
    public sealed record Context(
        string? PrincipalId,
        string? ServerResolvedTenantId,
        string? MembershipTenantId,
        string? TargetResourceTenantId,
        string? ClientRequestedTenantId,
        string? EvidenceSha,
        bool IsAuthenticated,
        bool TenantResolvedOnServer,
        bool MembershipActive,
        bool HasExplicitAuditPermission,
        bool PermissionRevoked,
        bool IsServiceActor,
        bool ServiceActorGrantVerified,
        bool RequestedQueryFilterBypass,
        bool RequestedInstitutionalWrite);

    public readonly record struct Decision(string Code)
    {
        // This is a request for further authenticated evidence, not a grant.
        public bool NeedsExternalAdapterVerification =>
            Code == "AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED";

        public bool AllowsInstitutionalWrite => false;
    }

    public static Decision Evaluate(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.RequestedInstitutionalWrite)
            return new Decision("INSTITUTIONAL_WRITE_OUT_OF_SCOPE");
        if (context.RequestedQueryFilterBypass)
            return new Decision("QUERY_FILTER_BYPASS_DENIED");
        if (!FullSha(context.EvidenceSha))
            return new Decision("EXACT_SHA_REQUIRED");

        if (!context.IsAuthenticated ||
            string.IsNullOrWhiteSpace(context.PrincipalId))
            return new Decision("AUTHENTICATION_REQUIRED");

        if (!context.TenantResolvedOnServer ||
            string.IsNullOrWhiteSpace(context.ServerResolvedTenantId))
            return new Decision("SERVER_VERIFIED_TENANT_REQUIRED");

        if (!string.IsNullOrWhiteSpace(context.ClientRequestedTenantId) &&
            !SameTenant(context.ClientRequestedTenantId,
                context.ServerResolvedTenantId))
            return new Decision("CLIENT_TENANT_OVERRIDE_DENIED");

        if (context.PermissionRevoked || !context.MembershipActive)
            return new Decision("REVOKED_OR_INACTIVE_MEMBERSHIP");

        if (!SameTenant(context.MembershipTenantId,
                context.ServerResolvedTenantId))
            return new Decision("TENANT_MEMBERSHIP_MISMATCH");

        if (!SameTenant(context.TargetResourceTenantId,
                context.ServerResolvedTenantId))
            return new Decision("CROSS_TENANT_OR_UNOWNED_RESOURCE_DENIED");

        if (!context.HasExplicitAuditPermission)
            return new Decision("EXPLICIT_AUDIT_PERMISSION_REQUIRED");

        if (context.IsServiceActor && !context.ServiceActorGrantVerified)
            return new Decision("SERVICE_ACTOR_GRANT_REQUIRED");

        // These flags are not proof of authentication. Verify JWT, current
        // membership, resource ownership, CI, CAS and A5 in a trusted adapter.
        return new Decision("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED");
    }

    private static bool SameTenant(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left, right, StringComparison.Ordinal);

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
