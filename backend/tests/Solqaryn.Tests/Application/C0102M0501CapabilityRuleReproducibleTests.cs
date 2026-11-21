using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>C01-02.M05.01: reproducible non-authorizing capability rule matrix.</summary>
public sealed class C0102M0501CapabilityRuleReproducibleTests
{
    private const string Capability = "orders.read";
    private static readonly IReadOnlySet<string> Allowed =
        new HashSet<string>(StringComparer.Ordinal) { Capability };
    private static readonly IReadOnlySet<string> Empty =
        new HashSet<string>(StringComparer.Ordinal);

    private static ContextoTenantActual Actor() =>
        ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(301, 101, 7), 301, 101);

    private static C0102CapabilityDomainRules.VerifiedPolicy Policy() => new(
        101, 101, 301, 7, 5, 5, "pro", "retail",
        true, true, true, false, true, true, true,
        Allowed, Allowed, Allowed, Allowed, Empty);

    [Fact]
    public void ExactServerIntersectionChecksPreconditionsWithoutGrantingAccess()
    {
        var r = C0102CapabilityDomainRules.Evaluate(Actor(), Capability, Policy());
        Assert.Equal("VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION", r.Code);
        Assert.False(r.CanGrantAccess);
        Assert.False(r.CanWriteOrReadResource);
        Assert.False(r.CanCreateRuntimeGrant);
        Assert.False(r.CanOverrideTenant);
    }

    [Theory]
    [InlineData("resource_tenant_b", "ACTOR_ROLE_OR_TENANT_MISMATCH")]
    [InlineData("policy_tenant_b", "ACTOR_ROLE_OR_TENANT_MISMATCH")]
    [InlineData("actor_mismatch", "ACTOR_ROLE_OR_TENANT_MISMATCH")]
    [InlineData("role_mismatch", "ACTOR_ROLE_OR_TENANT_MISMATCH")]
    [InlineData("branch_b", "RESOURCE_BRANCH_NOT_AUTHORIZED")]
    [InlineData("membership_revoked", "MEMBERSHIP_ROLE_OR_TENANT_REVOKED")]
    [InlineData("role_revoked", "MEMBERSHIP_ROLE_OR_TENANT_REVOKED")]
    [InlineData("grant_revoked", "MEMBERSHIP_ROLE_OR_TENANT_REVOKED")]
    [InlineData("rbac_missing", "EXPLICIT_RBAC_GRANT_REQUIRED")]
    [InlineData("plan_expired", "SERVER_CAPABILITY_POLICY_INCOMPLETE")]
    [InlineData("rubro_expired", "SERVER_CAPABILITY_POLICY_INCOMPLETE")]
    [InlineData("plan_denies", "CAPABILITY_NOT_ENTITLED_OR_DISABLED")]
    [InlineData("role_denies", "CAPABILITY_NOT_ENTITLED_OR_DISABLED")]
    [InlineData("explicit_deny", "CAPABILITY_NOT_ENTITLED_OR_DISABLED")]
    public void AdversarialPolicyMutationFailsClosed(string scenario, string expected)
    {
        var p = Policy();
        var changed = scenario switch
        {
            "resource_tenant_b" => p with { ResourceEmpresaId = 202 },
            "policy_tenant_b" => p with { TenantEmpresaId = 202 },
            "actor_mismatch" => p with { VerifiedActorId = 302 },
            "role_mismatch" => p with { EffectiveRoleId = 8 },
            "branch_b" => p with { ResourceBranchId = 6 },
            "membership_revoked" => p with { MembershipActive = false },
            "role_revoked" => p with { RoleActive = false },
            "grant_revoked" => p with { PermissionRevoked = true },
            "rbac_missing" => p with { ExplicitRelationalRbacGrant = false },
            "plan_expired" => p with { PlanCurrent = false },
            "rubro_expired" => p with { RubroCurrent = false },
            "plan_denies" => p with { PlanCapabilities = Empty },
            "role_denies" => p with { RoleCapabilities = Empty },
            "explicit_deny" => p with { DeniedCapabilities = Allowed },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var r = C0102CapabilityDomainRules.Evaluate(Actor(), Capability, changed);
        Assert.Equal(expected, r.Code);
        Assert.False(r.CanGrantAccess);
        Assert.False(r.CanWriteOrReadResource);
        Assert.False(r.CanOverrideTenant);
        Assert.False(r.CanCreateRuntimeGrant);
    }

    [Theory]
    [InlineData("Orders.Read")]
    [InlineData("../orders.read")]
    [InlineData("orders read")]
    [InlineData("orders/read")]
    public void NonCanonicalKeysFailClosed(string key)
    {
        var r = C0102CapabilityDomainRules.Evaluate(Actor(), key, Policy());
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED", r.Code);
        Assert.False(r.CanGrantAccess);
    }

    [Fact]
    public void CapabilityLength96IsAcceptedBut97IsRejected()
    {
        var valid96 = new string('a', 91) + ".read";
        var invalid97 = new string('a', 92) + ".read";
        Assert.Equal(96, valid96.Length);
        Assert.Equal(97, invalid97.Length);
        IReadOnlySet<string> only96 = new HashSet<string>(StringComparer.Ordinal) { valid96 };
        var p = Policy() with
        {
            PlanCapabilities = only96, RubroCapabilities = only96,
            RoleCapabilities = only96, TenantCapabilities = only96
        };
        var positive = C0102CapabilityDomainRules.Evaluate(Actor(), valid96, p);
        var negative = C0102CapabilityDomainRules.Evaluate(Actor(), invalid97, p);
        Assert.Equal("VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION", positive.Code);
        Assert.False(positive.CanGrantAccess);
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED", negative.Code);
        Assert.False(negative.CanGrantAccess);
    }

    [Fact]
    public void CaseInsensitiveCollectionsMustNotBypassOrdinalIdentity()
    {
        IReadOnlySet<string> unsafeSet =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ORDERS.READ" };
        var r = C0102CapabilityDomainRules.Evaluate(
            Actor(), Capability, Policy() with { TenantCapabilities = unsafeSet });
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED", r.Code);
        Assert.False(r.CanGrantAccess);
    }

    [Fact]
    public void MissingTrustedPolicyIsNeverSuccess()
    {
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE",
            C0102CapabilityDomainRules.Evaluate(
                Actor(), Capability, Policy() with { RubroCapabilities = null }).Code);
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED",
            C0102CapabilityDomainRules.Evaluate(null, Capability, Policy()).Code);
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED",
            C0102CapabilityDomainRules.Evaluate(Actor(), Capability, null).Code);
    }
}
