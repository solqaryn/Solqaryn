using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class C0102CapabilityApplicationPreflightTests
{
    private const string Capability = "orders.read";
    private static readonly IReadOnlySet<string> Allowed = new HashSet<string> { Capability };
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>();
    private readonly C0102CapabilityApplicationPreflight _sut = new();

    private static ContextoTenantActual Actor(int user = 10, int tenant = 100, int role = 5) =>
        ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(user, tenant, role), user, tenant);

    private static Rules.VerifiedPolicy Policy() => new(
        100, 100, 10, 5, 7, 7, "pro", "retail",
        true, true, true, false, true, true, true,
        Allowed, Allowed, Allowed, Allowed, Empty);

    private static C0102CapabilityApplicationPreflight.ClientIntent Intent() =>
        new(100, 7, Capability, false, false);

    private string Code(
        Rules.VerifiedPolicy? p = null,
        C0102CapabilityApplicationPreflight.ClientIntent? i = null) =>
        _sut.ReviewOnly(Actor(), p ?? Policy(), i ?? Intent()).Code;

    [Fact]
    public void ValidCapabilityStillNeverGrantsAccessOrDataEffects()
    {
        var result = _sut.ReviewOnly(Actor(), Policy(), Intent());
        Assert.Equal("VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION", result.Code);
        Assert.True(result.RequiresLiveBackendAuthorization);
        Assert.False(result.GrantsAccess);
        Assert.False(result.ReadsOrWritesResource);
        Assert.False(result.CreatesCapabilityGrant);
        Assert.False(result.MayOverrideTenant);
    }

    [Fact]
    public void NullIntentFailsClosed()
    {
        Assert.Equal("CAPABILITY_INTENT_REQUIRED",
            _sut.ReviewOnly(Actor(), Policy(), null).Code);
    }

    [Fact]
    public void MissingTrustedTenantOrPolicyFailsClosed()
    {
        Assert.Equal("TRUSTED_BACKEND_CONTEXT_AND_POLICY_REQUIRED",
            _sut.ReviewOnly(null, Policy(), Intent()).Code);
        Assert.Equal("TRUSTED_BACKEND_CONTEXT_AND_POLICY_REQUIRED",
            _sut.ReviewOnly(Actor(), null, Intent()).Code);
    }

    [Fact]
    public void ForgedOrMissingTenantIdIsOnlyARejectedConsistencyAssertion()
    {
        Assert.Equal("CLIENT_TENANT_CONSISTENCY_MISMATCH",
            Code(i: Intent() with { RequestedTenantEmpresaId = 200 }));
        Assert.Equal("CLIENT_TENANT_CONSISTENCY_MISMATCH",
            Code(i: Intent() with { RequestedTenantEmpresaId = 0 }));
    }

    [Fact]
    public void BranchCannotBeRemovedOrSwitchedByClient()
    {
        Assert.Equal("CLIENT_BRANCH_CONSISTENCY_MISMATCH",
            Code(i: Intent() with { RequestedBranchId = 8 }));
        Assert.Equal("CLIENT_BRANCH_CONSISTENCY_MISMATCH",
            Code(i: Intent() with { RequestedBranchId = null }));
        Assert.Equal("CLIENT_BRANCH_CONSISTENCY_MISMATCH",
            Code(i: Intent() with { RequestedBranchId = 0 }));
    }

    [Fact]
    public void TenantWideResourceCannotBeMadeBranchScopedByClient()
    {
        Assert.Equal("CLIENT_BRANCH_CONSISTENCY_MISMATCH",
            Code(p: Policy() with { ResourceBranchId = null }));
        var validTenantWide = _sut.ReviewOnly(
            Actor(), Policy() with { ResourceBranchId = null },
            Intent() with { RequestedBranchId = null });
        Assert.True(validTenantWide.RequiresLiveBackendAuthorization);
        Assert.False(validTenantWide.GrantsAccess);
    }

    [Fact]
    public void ClientCannotSupplyGrantOrTrustedPolicy()
    {
        Assert.Equal("CLIENT_POLICY_OR_GRANT_OVERRIDE_DENIED",
            Code(i: Intent() with { IncludesClientGrantOrPolicy = true }));
    }

    [Fact]
    public void PreflightNeverAllowsWriteExportOrModuleActivation()
    {
        Assert.Equal("NON_AUTHORIZING_PREFLIGHT_CANNOT_MUTATE_OR_EXPORT",
            Code(i: Intent() with { RequestsWriteExportOrActivation = true }));
    }

    [Fact]
    public void TrustedActorRoleAndOwnerMismatchesAreDeniedByDomain()
    {
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH",
            Code(p: Policy() with { VerifiedActorId = 11 }));
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH",
            Code(p: Policy() with { EffectiveRoleId = 99 }));
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH",
            Code(p: Policy() with { ResourceEmpresaId = 200 }));
    }

    [Fact]
    public void RevokedMembershipAndRoleFailClosed()
    {
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED",
            Code(p: Policy() with { MembershipActive = false }));
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED",
            Code(p: Policy() with { RoleActive = false }));
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED",
            Code(p: Policy() with { PermissionRevoked = true }));
    }

    [Fact]
    public void CommercialEntitlementCannotElevateMissingRelationalRbac()
    {
        Assert.Equal("EXPLICIT_RBAC_GRANT_REQUIRED",
            Code(p: Policy() with { ExplicitRelationalRbacGrant = false }));
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE",
            Code(p: Policy() with { PlanCurrent = false }));
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE",
            Code(p: Policy() with { TenantCapabilities = null }));
    }

    [Fact]
    public void DenyListAndUnknownOrNonCanonicalCapabilitiesAreRejected()
    {
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Code(p: Policy() with { DeniedCapabilities = Allowed }));
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Code(i: Intent() with { CapabilityKey = "orders.write" }));
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED",
            Code(i: Intent() with { CapabilityKey = "Orders.Read" }));
    }
}
