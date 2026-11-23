using Rules = global::Solqaryn.Domain.AutomationV5.C0803GymMembershipDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Future backend adapter must obtain signed JWT, relational RBAC and tenant
/// identity from authenticated server context, never from a client request.
/// </summary>
public interface IC0803VerifiedGymActorSource
{
    ValueTask<C0803GymMembershipApplicationService.AuthenticatedContext?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Future provider MUST tenant-filter the latest membership/ERP state and
/// validate read permission; no unfiltered membership ID lookup is acceptable.
/// </summary>
public interface IC0803VerifiedGymMembershipSource
{
    ValueTask<C0803GymMembershipApplicationService.VerifiedMembership?> ReadCurrentAsync(
        int tenantId, int memberId, CancellationToken cancellationToken);
}

/// <summary>
/// C08-03.M03.02: Application use case for read-only membership eligibility
/// preflight. The result cannot open a door, issue credentials or charge users.
/// </summary>
public sealed class C0803GymMembershipApplicationService
{
    private readonly IC0803VerifiedGymActorSource _actors;
    private readonly IC0803VerifiedGymMembershipSource _memberships;
    private readonly TimeProvider _clock;

    public C0803GymMembershipApplicationService(
        IC0803VerifiedGymActorSource actors,
        IC0803VerifiedGymMembershipSource memberships,
        TimeProvider? clock = null)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _memberships = memberships ?? throw new ArgumentNullException(nameof(memberships));
        _clock = clock ?? TimeProvider.System;
    }

    public sealed record ClientIntent(
        int MemberId, int PlanId, int BranchId, long ExpectedVersion,
        bool ClientTenantOverride = false, bool ClaimsPaymentConfirmed = false,
        bool RequestsOfflineAccess = false, bool RequestsDoorOpening = false);

    public sealed record AuthenticatedContext(
        Rules.VerifiedActor? Actor,
        bool FromTrustedServer, bool SignedJwtValidated,
        bool CurrentMembershipAndRbacFromDatabase, bool CurrentTenantResolved);

    public sealed record VerifiedMembership(
        Rules.MembershipSnapshot? Snapshot,
        bool FromTrustedTenantScopedStore, bool CurrentVersionRead,
        bool CurrentErpReconciliationVerified);

    public readonly record struct Preparation(string Code)
    {
        public bool RequiresAtomicAuthenticatedBackendRecheck =>
            Code == "GYM_MEMBERSHIP_CANDIDATE_REQUIRES_LIVE_BACKEND_GATE";
        public bool AuthorizesDoorOrHardware => false;
        public bool AuthorizesJwtRbacOrPayments => false;
        public bool CertifiesHttpMysqlIntegration => false;
    }

    public async ValueTask<Preparation> PrepareAsync(
        ClientIntent? intent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || intent.MemberId <= 0 || intent.PlanId <= 0 ||
            intent.BranchId <= 0 || intent.ExpectedVersion <= 0)
            return Deny("INVALID_MEMBER_PLAN_BRANCH_OR_VERSION");
        if (intent.ClientTenantOverride || intent.ClaimsPaymentConfirmed ||
            intent.RequestsOfflineAccess || intent.RequestsDoorOpening)
            return Deny("UNTRUSTED_CLIENT_ACCESS_PAYMENT_OR_TENANT_REQUEST");

        AuthenticatedContext? context;
        try { context = await _actors.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return Deny("TRUSTED_GYM_ACTOR_SOURCE_UNAVAILABLE"); }
        cancellationToken.ThrowIfCancellationRequested();

        var actor = context?.Actor;
        if (actor is null || actor.ActorId <= 0 || actor.TenantEmpresaId <= 0 ||
            actor.AuthorizedBranchId <= 0 || !context!.FromTrustedServer ||
            !context.SignedJwtValidated || !context.CurrentMembershipAndRbacFromDatabase ||
            !context.CurrentTenantResolved || !actor.JwtVerified ||
            !actor.SaaSMembershipActive || !actor.GymPermissionGrantedByServer ||
            actor.PermissionRevoked)
            return Deny("SIGNED_JWT_CURRENT_TENANT_AND_RBAC_REQUIRED");

        // Prevent cross-branch probing before touching a private record.
        if (actor.AuthorizedBranchId != intent.BranchId)
            return Deny("CROSS_BRANCH_ACCESS_DENIED");

        VerifiedMembership? trusted;
        try
        {
            trusted = await _memberships.ReadCurrentAsync(
                actor.TenantEmpresaId, intent.MemberId, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return Deny("TRUSTED_GYM_MEMBERSHIP_SOURCE_UNAVAILABLE"); }
        cancellationToken.ThrowIfCancellationRequested();

        var member = trusted?.Snapshot;
        if (member is null || !trusted!.FromTrustedTenantScopedStore ||
            !trusted.CurrentVersionRead || !trusted.CurrentErpReconciliationVerified)
            return Deny("FRESH_TENANT_SCOPED_MEMBER_AND_ERP_PROOF_REQUIRED");

        // Never use a client clock, paid assertion or tenant header as authority.
        var request = new Rules.AccessRequest(
            actor.TenantEmpresaId, intent.MemberId, intent.PlanId,
            intent.BranchId, intent.ExpectedVersion, _clock.GetUtcNow().UtcDateTime,
            DeviceOffline: false, ClientClaimsPaid: false);
        var decision = Rules.Evaluate(actor, member, request);
        return decision.CandidateForBackendRevalidation
            ? new Preparation("GYM_MEMBERSHIP_CANDIDATE_REQUIRES_LIVE_BACKEND_GATE")
            : Deny(decision.Code);
    }

    private static Preparation Deny(string code) => new(code);
}
