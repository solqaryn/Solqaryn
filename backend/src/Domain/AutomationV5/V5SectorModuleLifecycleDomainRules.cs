using System;
using System.Linq;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C08-04.M03.01: inert sector module lifecycle/domain preflight. It cannot
/// create an entitlement, activate a module, change SQL/EF or grant RBAC.
/// Every input is a trusted backend snapshot; Application/API must enforce
/// JWT, fresh relational permissions, optimistic CAS and audit separately.
/// </summary>
public static class V5SectorModuleLifecycleDomainRules
{
    public enum State { Disabled = 1, Enabled = 2, Suspended = 3, Retired = 4 }
    public enum Operation { Inspect = 1, Activate = 2, Suspend = 3, Resume = 4, Retire = 5 }

    public sealed record VerifiedActorAndPolicy
    {
        public string? ActorId { get; init; }
        public string? VerifiedTenantId { get; init; }
        public string? EffectiveRoleId { get; init; }
        public bool AuthenticatedJwtVerifiedByBackend { get; init; }
        public bool CurrentMembershipVerified { get; init; }
        public bool CurrentExplicitRelationalRbacGrant { get; init; }
        public bool UserOrTenantOrRoleRevoked { get; init; }
        public bool TrustedCapabilityCatalogLoaded { get; init; }
        public bool CurrentPlanAllowsCapability { get; init; }
        public bool CurrentRubroAllowsCapability { get; init; }
        public bool CurrentTenantAllowsCapability { get; init; }
        public bool DenyListBlocksCapability { get; init; }
        public bool TrustedPolicyRevisionVerified { get; init; }
        public bool TrustedResourceOwnershipVerified { get; init; }
        public string? ResourceOwnerTenantId { get; init; }
        public string? CurrentCapabilityKey { get; init; }
        public long CurrentModuleRevision { get; init; }
        public bool PlatformGlobalGrantRequested { get; init; }
    }

    public sealed record ProposedTransition
    {
        public string? RequestedTenantId { get; init; }
        public string? CapabilityKey { get; init; }
        public string? ServerAllowedCapabilityKey { get; init; }
        public State CurrentState { get; init; }
        public Operation Operation { get; init; }
        public long ExpectedRevision { get; init; }
        public string? IdempotencyKey { get; init; }
        public bool ClientControlsCatalogOrPermission { get; init; }
        public bool ClientControlsTenantOrResource { get; init; }
        public bool BypassesAuditOrConcurrencyFence { get; init; }
        public bool RequestsSchemaOrProductionMutation { get; init; }
        public bool RequestsAutomaticCrossTenantFallback { get; init; }
        public bool RetainsHistoryAndOwnership { get; init; }
    }

    public readonly record struct Verdict(string Code)
    {
        public bool AuthorizesModuleActivation => false;
        public bool AuthorizesAccess => false;
        public bool WritesToDatabase => false;
        public bool RequiresApplicationEnforcement =>
            Code == "VERIFIED_PRECONDITIONS_REQUIRE_SERVER_ENFORCEMENT";
    }

    private static bool SafeId(string? value) =>
        value is { Length: >= 2 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool SafeCapability(string? key) =>
        key is { Length: >= 3 and <= 96 } &&
        key.Contains('.') &&
        key.All(c => (c >= 'a' && c <= 'z') ||
                     (c >= '0' && c <= '9') || c is '-' or '_' or '.');

    private static bool CanTransition(State state, Operation op) =>
        op switch
        {
            Operation.Inspect => true,
            Operation.Activate => state == State.Disabled,
            Operation.Suspend => state == State.Enabled,
            Operation.Resume => state == State.Suspended,
            Operation.Retire => state is State.Disabled or State.Suspended,
            _ => false
        };

    public static Verdict Evaluate(VerifiedActorAndPolicy? policy, ProposedTransition? request)
    {
        if (policy is null || request is null ||
            !SafeId(policy.ActorId) || !SafeId(policy.VerifiedTenantId) ||
            !SafeId(policy.EffectiveRoleId) ||
            !policy.AuthenticatedJwtVerifiedByBackend ||
            !policy.CurrentMembershipVerified || policy.UserOrTenantOrRoleRevoked)
            return new("TRUSTED_ACTIVE_ACTOR_AND_MEMBERSHIP_REQUIRED");

        if (!Enum.IsDefined(request.CurrentState) ||
            !Enum.IsDefined(request.Operation))
            return new("UNKNOWN_MODULE_STATE_OR_OPERATION");

        if (!policy.TrustedCapabilityCatalogLoaded ||
            !policy.TrustedPolicyRevisionVerified ||
            !policy.TrustedResourceOwnershipVerified ||
            !policy.CurrentExplicitRelationalRbacGrant ||
            policy.PlatformGlobalGrantRequested)
            return new("TRUSTED_POLICY_AND_EXPLICIT_RBAC_REQUIRED");

        if (!SafeId(policy.ResourceOwnerTenantId) ||
            !SafeId(request.RequestedTenantId) ||
            !string.Equals(policy.ResourceOwnerTenantId,
                policy.VerifiedTenantId, StringComparison.Ordinal) ||
            !string.Equals(request.RequestedTenantId,
                policy.VerifiedTenantId, StringComparison.Ordinal))
            return new("CROSS_TENANT_RESOURCE_DENIED");

        if (!SafeCapability(policy.CurrentCapabilityKey) ||
            !SafeCapability(request.CapabilityKey) ||
            !SafeCapability(request.ServerAllowedCapabilityKey) ||
            !string.Equals(request.CapabilityKey, policy.CurrentCapabilityKey,
                StringComparison.Ordinal) ||
            !string.Equals(request.CapabilityKey, request.ServerAllowedCapabilityKey,
                StringComparison.Ordinal))
            return new("UNALLOWLISTED_OR_MISMATCHED_CAPABILITY");

        if (!policy.CurrentPlanAllowsCapability ||
            !policy.CurrentRubroAllowsCapability ||
            !policy.CurrentTenantAllowsCapability ||
            policy.DenyListBlocksCapability)
            return new("CAPABILITY_NOT_ENTITLED_OR_DISABLED");

        if (request.ClientControlsCatalogOrPermission ||
            request.ClientControlsTenantOrResource ||
            request.BypassesAuditOrConcurrencyFence ||
            request.RequestsSchemaOrProductionMutation ||
            request.RequestsAutomaticCrossTenantFallback ||
            !request.RetainsHistoryAndOwnership)
            return new("UNAUTHORIZED_CLIENT_OVERRIDE_OR_DESTRUCTIVE_CHANGE");

        if (policy.CurrentModuleRevision <= 0 ||
            request.ExpectedRevision <= 0 ||
            policy.CurrentModuleRevision != request.ExpectedRevision)
            return new("STALE_MODULE_VERSION");

        if (request.Operation != Operation.Inspect && !SafeId(request.IdempotencyKey))
            return new("IDEMPOTENCY_KEY_REQUIRED");

        if (!CanTransition(request.CurrentState, request.Operation))
            return new("ILLEGAL_MODULE_LIFECYCLE_TRANSITION");

        // Explicitly not an authorization decision. Trusted Application/API
        // must verify backend evidence, execute transactional CAS with a fence,
        // check tenant A/B and audit before applying any module mutation.
        return new("VERIFIED_PRECONDITIONS_REQUIRE_SERVER_ENFORCEMENT");
    }
}
