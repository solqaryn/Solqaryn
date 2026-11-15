using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class C0102CapabilityDomainRulesTests
{
    private const string Capability = "orders.read";
    private static readonly IReadOnlySet<string> Allowed = new HashSet<string> { Capability };
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>();

    private static ContextoTenantActual Actor(int actor = 10, int company = 100, int role = 5) =>
        ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(actor, company, role), actor, company);

    private static Rules.VerifiedPolicy Good() => new(
        100, 100, 10, 5, 7, 7, "pro", "retail",
        true, true, true, false, true, true, true,
        Allowed, Allowed, Allowed, Allowed, Empty);

    [Fact]
    public void InterseccionValidaSoloVerificaPrecondicionesSinConcederAcceso()
    {
        var result = Rules.Evaluate(Actor(), Capability, Good());
        Assert.Equal("VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION", result.Code);
        Assert.False(result.CanGrantAccess);
        Assert.False(result.CanWriteOrReadResource);
        Assert.False(result.CanOverrideTenant);
        Assert.False(result.CanCreateRuntimeGrant);
    }

    [Fact]
    public void NullContextPolicyOrCapabilityFailClosed()
    {
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED", Rules.Evaluate(null, Capability, Good()).Code);
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED", Rules.Evaluate(Actor(), Capability, null).Code);
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED", Rules.Evaluate(Actor(), "../cross-tenant", Good()).Code);
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED", Rules.Evaluate(Actor(), "Orders.Read", Good()).Code);
    }

    [Fact]
    public void TenantAyRecursoBSeRechaza()
    {
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH",
            Rules.Evaluate(Actor(), Capability, Good() with { ResourceEmpresaId = 200 }).Code);
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH",
            Rules.Evaluate(Actor(), Capability, Good() with { TenantEmpresaId = 200 }).Code);
    }

    [Fact]
    public void ActorYRolEfectivoDiferentesDelContextoSeRechazan()
    {
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH",
            Rules.Evaluate(Actor(), Capability, Good() with { VerifiedActorId = 11 }).Code);
        Assert.Equal("ACTOR_ROLE_OR_TENANT_MISMATCH",
            Rules.Evaluate(Actor(), Capability, Good() with { EffectiveRoleId = 999 }).Code);
    }

    [Fact]
    public void MembresiaRolYTenantInactivosFallan()
    {
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED",
            Rules.Evaluate(Actor(), Capability, Good() with { MembershipActive = false }).Code);
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED",
            Rules.Evaluate(Actor(), Capability, Good() with { RoleActive = false }).Code);
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED",
            Rules.Evaluate(Actor(), Capability, Good() with { TenantActive = false }).Code);
        Assert.Equal("MEMBERSHIP_ROLE_OR_TENANT_REVOKED",
            Rules.Evaluate(Actor(), Capability, Good() with { PermissionRevoked = true }).Code);
    }

    [Fact]
    public void SucursalDeOtroAmbitoNoSeAutoriza()
    {
        Assert.Equal("RESOURCE_BRANCH_NOT_AUTHORIZED",
            Rules.Evaluate(Actor(), Capability, Good() with { ResourceBranchId = 8 }).Code);
        Assert.Equal("RESOURCE_BRANCH_NOT_AUTHORIZED",
            Rules.Evaluate(Actor(), Capability, Good() with { AuthorizedBranchId = null }).Code);
    }

    [Fact]
    public void PlanValidoSinGrantRelacionalNuncaElevaRol()
    {
        Assert.Equal("EXPLICIT_RBAC_GRANT_REQUIRED",
            Rules.Evaluate(Actor(), Capability, Good() with { ExplicitRelationalRbacGrant = false }).Code);
    }

    [Fact]
    public void PlanRubroFueraDeVigenciaSeRechazan()
    {
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE",
            Rules.Evaluate(Actor(), Capability, Good() with { PlanCurrent = false }).Code);
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE",
            Rules.Evaluate(Actor(), Capability, Good() with { RubroCurrent = false }).Code);
    }

    [Fact]
    public void AusenciaDeCualquierMatrizRechazaSinFallback()
    {
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE",
            Rules.Evaluate(Actor(), Capability, Good() with { RoleCapabilities = null }).Code);
        Assert.Equal("SERVER_CAPABILITY_POLICY_INCOMPLETE",
            Rules.Evaluate(Actor(), Capability, Good() with { TenantCapabilities = null }).Code);
    }

    [Fact]
    public void InterseccionPlanRubroRolTenantExigeTodasLasCapacidades()
    {
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Rules.Evaluate(Actor(), Capability, Good() with { PlanCapabilities = Empty }).Code);
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Rules.Evaluate(Actor(), Capability, Good() with { RubroCapabilities = Empty }).Code);
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Rules.Evaluate(Actor(), Capability, Good() with { RoleCapabilities = Empty }).Code);
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Rules.Evaluate(Actor(), Capability, Good() with { TenantCapabilities = Empty }).Code);
    }

    [Fact]
    public void DenylistServerSideTienePrecedencia()
    {
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Rules.Evaluate(Actor(), Capability, Good() with { DeniedCapabilities = Allowed }).Code);
    }

    [Fact]
    public void ComparacionOrdinalNoConcedePorComparerInsensible()
    {
        IReadOnlySet<string> mixedCase = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "ORDERS.READ" };
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            Rules.Evaluate(Actor(), Capability, Good() with { PlanCapabilities = mixedCase }).Code);
    }

    [Fact]
    public void PlanYRubroInvalidosSeRechazan()
    {
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED",
            Rules.Evaluate(Actor(), Capability, Good() with { PlanCode = "../x" }).Code);
        Assert.Equal("CAPABILITY_EVIDENCE_REQUIRED",
            Rules.Evaluate(Actor(), Capability, Good() with { RubroCode = "" }).Code);
    }
}
