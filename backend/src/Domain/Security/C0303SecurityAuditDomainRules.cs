namespace Solqaryn.Domain.Security;

/// <summary>
/// C03-03.M03.01: closed, payload-free security audit domain preflight.
/// Only the authenticated API/Application adapter may resolve actor, current
/// membership, resource owner, permission and transactional sink from trusted
/// server state. This type is never an authorization grant or a DB writer.
/// </summary>
public static class C0303SecurityAuditDomainRules
{
    public enum Scope { Enterprise = 1, Platform = 2 }
    public enum Action
    {
        AuthorizationGranted = 1,
        AuthorizationDenied = 2,
        CriticalChange = 3,
        ExceptionalAccessDenied = 4,
        AuditRead = 5
    }
    public enum Outcome { Allowed = 1, Denied = 2, Error = 3 }

    public sealed record Request(
        int? AuthenticatedActorId,
        int? ServerResolvedTenantId,
        int? CurrentMembershipTenantId,
        int? PersistedResourceTenantId,
        bool MembershipActive,
        bool AuditWriterTrusted,
        bool CurrentAuditPermissionVerified,
        bool PermissionRevoked,
        bool PlatformAuthorityVerified,
        bool ExplicitPlatformAuditGrantVerified,
        bool TransactionalSinkVerified,
        bool ClientTenantOverrideAttempted,
        Scope Scope,
        Action Action,
        Outcome Outcome,
        string? CorrelationId);

    /// <summary>
    /// Deliberately contains no arbitrary description, raw headers, JWT,
    /// request body, old/new values or exception text.
    /// </summary>
    public sealed record SafeEnvelope(
        int ActorId,
        int? TenantId,
        Scope Scope,
        Action Action,
        Outcome Outcome,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);

    public sealed record Result(string Code, SafeEnvelope? Candidate)
    {
        public bool NeedsAuthenticatedPersistenceAdapter =>
            Code == "DOMAIN_PREFLIGHT_ONLY_SERVER_ENFORCEMENT_REQUIRED";
        public bool AuthorizesAuditWrite => false;
    }

    public static Result Evaluate(Request? request)
    {
        static Result Deny(string code) => new(code, null);

        if (request is null)
            return Deny("MISSING_AUDIT_REQUEST");
        if (!Enum.IsDefined(request.Scope) ||
            !Enum.IsDefined(request.Action) ||
            !Enum.IsDefined(request.Outcome))
            return Deny("UNKNOWN_AUDIT_CODE");
        if (request.AuthenticatedActorId is not > 0 ||
            !request.AuditWriterTrusted)
            return Deny("TRUSTED_SERVER_ACTOR_REQUIRED");
        if (request.ClientTenantOverrideAttempted)
            return Deny("CLIENT_TENANT_OVERRIDE_DENIED");
        if (!ValidCorrelation(request.CorrelationId))
            return Deny("UNSAFE_CORRELATION_REJECTED");
        if (request.PermissionRevoked || !request.CurrentAuditPermissionVerified)
            return Deny("AUDIT_PERMISSION_REVOKED_OR_MISSING");

        if (request.Scope == Scope.Enterprise)
        {
            if (!request.MembershipActive ||
                request.ServerResolvedTenantId is not > 0 ||
                request.CurrentMembershipTenantId != request.ServerResolvedTenantId)
                return Deny("CURRENT_TENANT_MEMBERSHIP_REQUIRED");

            if (request.PersistedResourceTenantId is not > 0 ||
                request.PersistedResourceTenantId != request.ServerResolvedTenantId)
                return Deny("CROSS_TENANT_RESOURCE_DENIED");
        }
        else
        {
            // A null enterprise ID is NEVER an implicit platform grant.
            if (!request.PlatformAuthorityVerified ||
                !request.ExplicitPlatformAuditGrantVerified ||
                request.ServerResolvedTenantId is not null ||
                request.PersistedResourceTenantId is not null ||
                request.CurrentMembershipTenantId is not null)
                return Deny("EXPLICIT_PLATFORM_AUDIT_SCOPE_REQUIRED");
        }

        if (request.Action == Action.AuthorizationGranted &&
            request.Outcome != Outcome.Allowed ||
            request.Action == Action.AuthorizationDenied &&
            request.Outcome != Outcome.Denied ||
            request.Action == Action.ExceptionalAccessDenied &&
            request.Outcome != Outcome.Denied)
            return Deny("ACTION_OUTCOME_MISMATCH");

        // Exceptional GRANT, MFA, TTL, retention and export are not
        // implemented by this microtask. No grant is created here.
        if (request.Action == Action.CriticalChange &&
            !request.TransactionalSinkVerified)
            return Deny("STRICT_TRANSACTIONAL_AUDIT_REQUIRED");

        var safe = new SafeEnvelope(
            request.AuthenticatedActorId.Value,
            request.Scope == Scope.Enterprise ? request.ServerResolvedTenantId : null,
            request.Scope,
            request.Action,
            request.Outcome,
            request.CorrelationId!,
            DateTimeOffset.UtcNow);

        // A positive domain verdict is not authenticated HTTP authorization,
        // durable MySQL proof or permission to commit a business mutation.
        return new Result("DOMAIN_PREFLIGHT_ONLY_SERVER_ENFORCEMENT_REQUIRED", safe);
    }

    private static bool ValidCorrelation(string? value) =>
        !string.IsNullOrEmpty(value) &&
        value.Length <= 64 &&
        value.All(c =>
            c is >= 'A' and <= 'Z' or
                 >= 'a' and <= 'z' or
                 >= '0' and <= '9' ||
            c is '-' or '_' or '.');
}
