using Solqaryn.Domain.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5SectorModuleLifecycleDomainRulesTests
{
    private static Guard.VerifiedActorAndPolicy Policy() => new()
    {
        ActorId = "actor-A",
        VerifiedTenantId = "tenant-A",
        EffectiveRoleId = "admin-A",
        AuthenticatedJwtVerifiedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentExplicitRelationalRbacGrant = true,
        TrustedCapabilityCatalogLoaded = true,
        CurrentPlanAllowsCapability = true,
        CurrentRubroAllowsCapability = true,
        CurrentTenantAllowsCapability = true,
        TrustedPolicyRevisionVerified = true,
        TrustedResourceOwnershipVerified = true,
        ResourceOwnerTenantId = "tenant-A",
        CurrentCapabilityKey = "sector.stock",
        CurrentModuleRevision = 9
    };

    private static Guard.ProposedTransition Request(
        Guard.Operation op = Guard.Operation.Activate) => new()
    {
        RequestedTenantId = "tenant-A",
        CapabilityKey = "sector.stock",
        ServerAllowedCapabilityKey = "sector.stock",
        CurrentState = Guard.State.Disabled,
        Operation = op,
        ExpectedRevision = 9,
        IdempotencyKey = "idempotent-9",
        RetainsHistoryAndOwnership = true
    };

    private static string Code(Guard.VerifiedActorAndPolicy? p = null,
        Guard.ProposedTransition? r = null) =>
        Guard.Evaluate(p ?? Policy(), r ?? Request()).Code;

    [Fact]
    public void VerifiedActivationOnlyRequestsIndependentServerEnforcement()
    {
        var result = Guard.Evaluate(Policy(), Request());
        Assert.True(result.RequiresApplicationEnforcement);
        Assert.False(result.AuthorizesAccess);
        Assert.False(result.AuthorizesModuleActivation);
        Assert.False(result.WritesToDatabase);
    }

    [Fact]
    public void ValidSuspendResumeAndRetireStatesOnlyRequestServerEnforcement()
    {
        Assert.True(Guard.Evaluate(Policy(), Request(Guard.Operation.Suspend) with
        {
            CurrentState = Guard.State.Enabled
        }).RequiresApplicationEnforcement);
        Assert.True(Guard.Evaluate(Policy(), Request(Guard.Operation.Resume) with
        {
            CurrentState = Guard.State.Suspended
        }).RequiresApplicationEnforcement);
        Assert.True(Guard.Evaluate(Policy(), Request(Guard.Operation.Retire) with
        {
            CurrentState = Guard.State.Disabled
        }).RequiresApplicationEnforcement);
    }

    [Fact]
    public void UnauthorizedActorsAndRevokedMembershipFailClosed()
    {
        Assert.Equal("TRUSTED_ACTIVE_ACTOR_AND_MEMBERSHIP_REQUIRED",
            Guard.Evaluate(null, Request()).Code);
        Assert.Equal("TRUSTED_ACTIVE_ACTOR_AND_MEMBERSHIP_REQUIRED",
            Guard.Evaluate(Policy(), null).Code);
        Assert.Equal("TRUSTED_ACTIVE_ACTOR_AND_MEMBERSHIP_REQUIRED",
            Code(Policy() with { AuthenticatedJwtVerifiedByBackend = false }));
        Assert.Equal("TRUSTED_ACTIVE_ACTOR_AND_MEMBERSHIP_REQUIRED",
            Code(Policy() with { CurrentMembershipVerified = false }));
        Assert.Equal("TRUSTED_ACTIVE_ACTOR_AND_MEMBERSHIP_REQUIRED",
            Code(Policy() with { UserOrTenantOrRoleRevoked = true }));
    }

    [Fact]
    public void CapabilityDoesNotGrantRbacOrGlobalPlatformAuthority()
    {
        Assert.Equal("TRUSTED_POLICY_AND_EXPLICIT_RBAC_REQUIRED",
            Code(Policy() with { CurrentExplicitRelationalRbacGrant = false }));
        Assert.Equal("TRUSTED_POLICY_AND_EXPLICIT_RBAC_REQUIRED",
            Code(Policy() with { PlatformGlobalGrantRequested = true }));
        Assert.Equal("TRUSTED_POLICY_AND_EXPLICIT_RBAC_REQUIRED",
            Code(Policy() with { TrustedCapabilityCatalogLoaded = false }));
    }

    [Fact]
    public void TenantBAndForeignResourceNeverBorrowTenantAPermissions()
    {
        Assert.Equal("CROSS_TENANT_RESOURCE_DENIED",
            Code(r: Request() with { RequestedTenantId = "tenant-B" }));
        Assert.Equal("CROSS_TENANT_RESOURCE_DENIED",
            Code(Policy() with { ResourceOwnerTenantId = "tenant-B" }));
        Assert.Equal("TRUSTED_POLICY_AND_EXPLICIT_RBAC_REQUIRED",
            Code(Policy() with { TrustedResourceOwnershipVerified = false }));
    }

    [Fact]
    public void UnknownKeyAndCaseVariantCannotBypassExactAllowlist()
    {
        Assert.Equal("UNALLOWLISTED_OR_MISMATCHED_CAPABILITY",
            Code(r: Request() with { CapabilityKey = "sector.Stock" }));
        Assert.Equal("UNALLOWLISTED_OR_MISMATCHED_CAPABILITY",
            Code(r: Request() with { CapabilityKey = "unregistered" }));
        Assert.Equal("UNALLOWLISTED_OR_MISMATCHED_CAPABILITY",
            Code(r: Request() with { ServerAllowedCapabilityKey = "sector.billing" }));
    }

    [Fact]
    public void PlanRubroTenantAndDenyListRemainIndependentRestrictions()
    {
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Code(Policy() with { CurrentPlanAllowsCapability = false }));
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Code(Policy() with { CurrentRubroAllowsCapability = false }));
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Code(Policy() with { CurrentTenantAllowsCapability = false }));
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Code(Policy() with { DenyListBlocksCapability = true }));
    }

    [Fact]
    public void ClientOverridesAndUnsafeBypassesAlwaysFailClosed()
    {
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_DESTRUCTIVE_CHANGE",
            Code(r: Request() with { ClientControlsCatalogOrPermission = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_DESTRUCTIVE_CHANGE",
            Code(r: Request() with { ClientControlsTenantOrResource = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_DESTRUCTIVE_CHANGE",
            Code(r: Request() with { BypassesAuditOrConcurrencyFence = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_DESTRUCTIVE_CHANGE",
            Code(r: Request() with { RequestsSchemaOrProductionMutation = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_DESTRUCTIVE_CHANGE",
            Code(r: Request() with { RequestsAutomaticCrossTenantFallback = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_DESTRUCTIVE_CHANGE",
            Code(r: Request() with { RetainsHistoryAndOwnership = false }));
    }

    [Fact]
    public void VersionStaleZeroAndFutureAreNotCasProof()
    {
        Assert.Equal("STALE_MODULE_VERSION",
            Code(r: Request() with { ExpectedRevision = 8 }));
        Assert.Equal("STALE_MODULE_VERSION",
            Code(r: Request() with { ExpectedRevision = 10 }));
        Assert.Equal("STALE_MODULE_VERSION",
            Code(r: Request() with { ExpectedRevision = 0 }));
    }

    [Fact]
    public void StateTransitionMatrixIsFailClosed()
    {
        Assert.Equal("ILLEGAL_MODULE_LIFECYCLE_TRANSITION",
            Code(r: Request() with { CurrentState = Guard.State.Enabled }));
        Assert.Equal("ILLEGAL_MODULE_LIFECYCLE_TRANSITION",
            Code(r: Request(Guard.Operation.Resume) with { CurrentState = Guard.State.Retired }));
        Assert.Equal("ILLEGAL_MODULE_LIFECYCLE_TRANSITION",
            Code(r: Request(Guard.Operation.Retire) with { CurrentState = Guard.State.Enabled }));
        Assert.Equal("UNKNOWN_MODULE_STATE_OR_OPERATION",
            Code(r: Request() with { CurrentState = (Guard.State)99 }));
    }

    [Fact]
    public void MutationWithoutIdempotencyKeyCannotProceed()
    {
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED",
            Code(r: Request() with { IdempotencyKey = null }));
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED",
            Code(r: Request() with { IdempotencyKey = " " }));
    }

    [Fact]
    public void InspectionIsNotAuthorizationOrWrite()
    {
        var result = Guard.Evaluate(Policy(), Request(Guard.Operation.Inspect) with
        {
            IdempotencyKey = null
        });
        Assert.True(result.RequiresApplicationEnforcement);
        Assert.False(result.AuthorizesAccess);
        Assert.False(result.AuthorizesModuleActivation);
    }
}
