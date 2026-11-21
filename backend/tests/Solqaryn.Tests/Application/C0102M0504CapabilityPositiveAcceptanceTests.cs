using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C01-02.M05.04: positive tenant/branch/capability contract acceptance.
/// A positive *precondition* is never a JWT/RBAC permission or a data read.
/// </summary>
public sealed class C0102M0504CapabilityPositiveAcceptanceTests
{
    private static ContextoTenantActual Actor(int userId, int tenantId, int roleId) =>
        ContextoTenantActual.DesdeMembresia(
            new UsuarioEmpresa(userId, tenantId, roleId), userId, tenantId);

    private static Rules.VerifiedPolicy Policy(
        int userId, int tenantId, int roleId, int branchId, string capability)
    {
        IReadOnlySet<string> enabled = new HashSet<string>(StringComparer.Ordinal) { capability };
        IReadOnlySet<string> denied = new HashSet<string>(StringComparer.Ordinal);
        return new Rules.VerifiedPolicy(
            tenantId, tenantId, userId, roleId, branchId, branchId, "pro", "retail",
            true, true, true, false, true, true, true,
            enabled, enabled, enabled, enabled, denied);
    }

    [Theory]
    [InlineData(301, 101, 7, 5, "orders.read")]
    [InlineData(302, 202, 8, 9, "inventory.read")]
    public void ValidIndependentTenantContractRequiresLiveAuthorization(
        int userId, int tenantId, int roleId, int branchId, string capability)
    {
        var context = Actor(userId, tenantId, roleId);
        var policy = Policy(userId, tenantId, roleId, branchId, capability);
        var client = new C0102CapabilityApplicationPreflight.ClientIntent(
            tenantId, branchId, capability, false, false);

        var domain = Rules.Evaluate(context, capability, policy);
        var application = new C0102CapabilityApplicationPreflight()
            .ReviewOnly(context, policy, client);

        Assert.Equal("VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION", domain.Code);
        Assert.False(domain.CanGrantAccess);
        Assert.False(domain.CanWriteOrReadResource);
        Assert.Equal("VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION", application.Code);
        Assert.True(application.RequiresLiveBackendAuthorization);
        Assert.False(application.GrantsAccess);
        Assert.False(application.ReadsOrWritesResource);
        Assert.False(application.CreatesCapabilityGrant);
        Assert.False(application.MayOverrideTenant);
    }

    [Fact]
    public void TenantBPolicyCannotBeAcceptedForAuthenticatedTenantA()
    {
        var contextA = Actor(301, 101, 7);
        var policyB = Policy(302, 202, 8, 9, "orders.read");
        var requestA = new C0102CapabilityApplicationPreflight.ClientIntent(
            101, 9, "orders.read", false, false);
        var result = new C0102CapabilityApplicationPreflight()
            .ReviewOnly(contextA, policyB, requestA);
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH", result.Code);
        Assert.False(result.RequiresLiveBackendAuthorization);
        Assert.False(result.GrantsAccess);
    }

    [Fact]
    public void ExpiredPlanOrRevokedMembershipCannotBecomePositiveAcceptance()
    {
        var actor = Actor(301, 101, 7);
        var valid = Policy(301, 101, 7, 5, "orders.read");
        var intent = new C0102CapabilityApplicationPreflight.ClientIntent(
            101, 5, "orders.read", false, false);
        var sut = new C0102CapabilityApplicationPreflight();
        var expired = sut.ReviewOnly(actor, valid with { PlanCurrent = false }, intent);
        var revoked = sut.ReviewOnly(actor, valid with { MembershipActive = false }, intent);
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE", expired.Code);
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED", revoked.Code);
        Assert.False(expired.GrantsAccess);
        Assert.False(revoked.GrantsAccess);
        Assert.False(expired.RequiresLiveBackendAuthorization);
        Assert.False(revoked.RequiresLiveBackendAuthorization);
    }
}
