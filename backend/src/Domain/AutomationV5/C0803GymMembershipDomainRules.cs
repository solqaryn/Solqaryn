namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C08-03.M03.01: deterministic server-side domain validation for a future
/// gym membership access decision. Snapshots must be read from trusted server
/// sources; a client-provided claim, QR or selected tenant is never authority.
/// This rule neither opens doors nor grants JWT/RBAC, payment or database writes.
/// </summary>
public static class C0803GymMembershipDomainRules
{
    public enum MembershipState
    {
        Draft = 1,
        Pending = 2,
        Active = 3,
        Suspended = 4,
        Cancelled = 5,
        Expired = 6
    }

    public sealed record VerifiedActor(
        int ActorId,
        int TenantEmpresaId,
        int AuthorizedBranchId,
        bool JwtVerified,
        bool SaaSMembershipActive,
        bool GymPermissionGrantedByServer,
        bool PermissionRevoked);

    public sealed record MembershipSnapshot(
        int TenantEmpresaId,
        int MemberId,
        int PlanId,
        int BranchId,
        MembershipState State,
        long Version,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        bool PlanActive,
        bool SuspensionActive,
        bool CredentialRevoked,
        bool ErpPaymentReconciled);

    public sealed record AccessRequest(
        int RequestedTenantId,
        int MemberId,
        int PlanId,
        int BranchId,
        long ExpectedVersion,
        DateTime ServerNowUtc,
        bool DeviceOffline,
        bool ClientClaimsPaid);

    public sealed record Decision(string Code, bool CandidateForBackendRevalidation)
    {
        // Domain-only evaluation must never become an authorization capability.
        public bool AuthorizesDoorOpening => false;
        public bool AuthorizesJwtOrRbac => false;
        public bool AuthorizesPayment => false;
        public bool CertifiesHttpMysqlOrHardware => false;
    }

    private static Decision Deny(string code) => new(code, false);

    public static Decision Evaluate(
        VerifiedActor? actor,
        MembershipSnapshot? membership,
        AccessRequest? request)
    {
        if (actor is null || membership is null || request is null ||
            actor.ActorId <= 0 || actor.TenantEmpresaId <= 0 ||
            actor.AuthorizedBranchId <= 0 ||
            membership.TenantEmpresaId <= 0 || membership.MemberId <= 0 ||
            membership.PlanId <= 0 || membership.BranchId <= 0 ||
            request.RequestedTenantId <= 0 || request.MemberId <= 0 ||
            request.PlanId <= 0 || request.BranchId <= 0)
            return Deny("VERIFIED_SERVER_SNAPSHOTS_REQUIRED");

        if (!actor.JwtVerified || !actor.SaaSMembershipActive ||
            !actor.GymPermissionGrantedByServer || actor.PermissionRevoked)
            return Deny("JWT_MEMBERSHIP_RBAC_NOT_AUTHORIZED");

        if (actor.TenantEmpresaId != membership.TenantEmpresaId ||
            actor.TenantEmpresaId != request.RequestedTenantId ||
            actor.AuthorizedBranchId != membership.BranchId ||
            actor.AuthorizedBranchId != request.BranchId ||
            membership.MemberId != request.MemberId ||
            membership.PlanId != request.PlanId)
            return Deny("TENANT_BRANCH_MEMBER_PLAN_MISMATCH");

        if (!Enum.IsDefined(membership.State) ||
            membership.State != MembershipState.Active)
            return Deny("MEMBERSHIP_NOT_ACTIVE");

        if (membership.Version <= 0 || request.ExpectedVersion != membership.Version)
            return Deny("VERSION_STALE_OR_INVALID");

        if (request.ServerNowUtc.Kind != DateTimeKind.Utc ||
            membership.StartsAtUtc.Kind != DateTimeKind.Utc ||
            membership.EndsAtUtc.Kind != DateTimeKind.Utc ||
            membership.EndsAtUtc <= membership.StartsAtUtc)
            return Deny("UTC_PERIOD_NOT_VERIFIED");

        // [start, end): the exact expiration instant is excluded.
        if (request.ServerNowUtc < membership.StartsAtUtc ||
            request.ServerNowUtc >= membership.EndsAtUtc)
            return Deny("MEMBERSHIP_EXPIRED_OR_NOT_STARTED");

        if (!membership.PlanActive || membership.SuspensionActive ||
            membership.CredentialRevoked)
            return Deny("PLAN_SUSPENSION_OR_CREDENTIAL_REVOKED");

        // Neither an offline device nor a payment assertion from a client is
        // evidence of an authorized entry or an ERP-settled membership.
        if (request.DeviceOffline || request.ClientClaimsPaid ||
            !membership.ErpPaymentReconciled)
            return Deny("TRUSTED_ERP_AND_ONLINE_REVALIDATION_REQUIRED");

        // Application must still re-read JWT, relational permission, scope,
        // membership, version and billing atomically at the actual decision.
        return new Decision("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED", true);
    }
}
