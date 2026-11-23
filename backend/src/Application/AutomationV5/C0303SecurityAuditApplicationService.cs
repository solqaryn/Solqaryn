using Rules = global::Solqaryn.Domain.Security.C0303SecurityAuditDomainRules;

namespace Solqaryn.Application.AutomationV5;

// Contracts MUST be implemented using authenticated server context and
// current relational RBAC, membership and resource ownership. No adapter or
// permission is installed by this microtask.
public interface IC0303VerifiedAuditEventReader
{
    ValueTask<C0303SecurityAuditApplicationService.ServerEvent?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}
public interface IC0303VerifiedAuditResourceReader
{
    ValueTask<C0303SecurityAuditApplicationService.ServerResource?> ReadCurrentAsync(
        int actorId, int tenantId, Rules.Action action, CancellationToken cancellationToken);
}

/// <summary>
/// C03-03.M03.02: fail-closed Application orchestration for a future authenticated
/// durable audit writer. A positive result is NOT authorization or persistence.
/// </summary>
public sealed class C0303SecurityAuditApplicationService
{
    private readonly IC0303VerifiedAuditEventReader _events;
    private readonly IC0303VerifiedAuditResourceReader _resources;

    public C0303SecurityAuditApplicationService(
        IC0303VerifiedAuditEventReader events, IC0303VerifiedAuditResourceReader resources)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public sealed record ClientIntent(string? CorrelationId,
        bool ClientTenantOverride = false, bool RequestsRawPayload = false,
        bool RequestsImmediateWrite = false);
    public sealed record ServerEvent(
        int ActorId, int? TenantId, int? MembershipTenantId,
        bool TrustedServerContext, bool SignedJwtVerified, bool MembershipActive,
        bool CurrentAuditRbacVerified, bool PermissionRevoked,
        bool TrustedAuditWriter, bool PlatformAuthorityVerified,
        bool ExplicitPlatformAuditGrant, bool TransactionalSinkVerified,
        Rules.Scope Scope, Rules.Action Action, Rules.Outcome Outcome);
    public sealed record ServerResource(
        int? PersistedTenantId, bool TrustedStore, bool CurrentOwnershipVerified);
    public readonly record struct Preparation(string Code, Rules.SafeEnvelope? Candidate)
    {
        public bool RequiresDurableAuthenticatedAdapter =>
            Code == "AUDIT_CANDIDATE_REQUIRES_ATOMIC_SERVER_PERSISTENCE";
        public bool AuthorizesAnyScope => false;
        public bool WritesAnyAuditOrBusinessData => false;
        public bool CertifiesLiveJwtHttpMysql => false;
    }

    public async ValueTask<Preparation> PrepareAsync(
        ClientIntent? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || !SafeCorrelation(request.CorrelationId))
            return Deny("SAFE_CORRELATION_REQUIRED");
        if (request.ClientTenantOverride || request.RequestsRawPayload ||
            request.RequestsImmediateWrite)
            return Deny("UNTRUSTED_CLIENT_AUDIT_CONTROL_DENIED");

        ServerEvent? actor;
        try { actor = await _events.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return Deny("AUTHENTICATED_AUDIT_SOURCE_UNAVAILABLE"); }
        cancellationToken.ThrowIfCancellationRequested();

        if (actor is null || actor.ActorId <= 0 || !actor.TrustedServerContext ||
            !actor.SignedJwtVerified || !actor.TrustedAuditWriter ||
            !actor.CurrentAuditRbacVerified || actor.PermissionRevoked)
            return Deny("LIVE_SIGNED_JWT_AND_RBAC_REQUIRED");

        int? persistedTenant = null;
        if (actor.Scope == Rules.Scope.Enterprise)
        {
            if (actor.TenantId is not > 0)
                return Deny("SERVER_TENANT_REQUIRED");
            ServerResource? resource;
            try
            {
                resource = await _resources.ReadCurrentAsync(
                    actor.ActorId, actor.TenantId.Value, actor.Action, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            { return Deny("AUDIT_RESOURCE_SOURCE_UNAVAILABLE"); }
            cancellationToken.ThrowIfCancellationRequested();
            if (resource is null || !resource.TrustedStore || !resource.CurrentOwnershipVerified)
                return Deny("CURRENT_PERSISTED_RESOURCE_OWNERSHIP_REQUIRED");
            persistedTenant = resource.PersistedTenantId;
        }

        var verdict = Rules.Evaluate(new Rules.Request(
            AuthenticatedActorId: actor.ActorId,
            ServerResolvedTenantId: actor.TenantId,
            CurrentMembershipTenantId: actor.MembershipTenantId,
            PersistedResourceTenantId: persistedTenant,
            MembershipActive: actor.MembershipActive,
            AuditWriterTrusted: actor.TrustedAuditWriter,
            CurrentAuditPermissionVerified: actor.CurrentAuditRbacVerified,
            PermissionRevoked: actor.PermissionRevoked,
            PlatformAuthorityVerified: actor.PlatformAuthorityVerified,
            ExplicitPlatformAuditGrantVerified: actor.ExplicitPlatformAuditGrant,
            TransactionalSinkVerified: actor.TransactionalSinkVerified,
            ClientTenantOverrideAttempted: false,
            Scope: actor.Scope, Action: actor.Action, Outcome: actor.Outcome,
            CorrelationId: request.CorrelationId));

        return verdict.NeedsAuthenticatedPersistenceAdapter
            ? new Preparation("AUDIT_CANDIDATE_REQUIRES_ATOMIC_SERVER_PERSISTENCE",
                verdict.Candidate)
            : Deny(verdict.Code);
    }

    private static Preparation Deny(string code) => new(code, null);
    private static bool SafeCorrelation(string? value) =>
        value is { Length: > 0 and <= 64 } &&
        value.All(c => c is >= 'A' and <= 'Z' or
                             >= 'a' and <= 'z' or
                             >= '0' and <= '9' or '-' or '_' or '.');
}
