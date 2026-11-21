using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0401PlanDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class C0401M0301PlanDomainRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 5, 0, 0, DateTimeKind.Utc);
    private static Rules.VerifiedActor Actor() => new(100, 7, 5, 11, true, true, true, true, false);
    private static Rules.PlanSnapshot Plan() => new(42, "PLAN-BASICO", 3, true, Now.AddDays(-10), Now.AddDays(10));
    private static Rules.SuscripcionSnapshot Subscription() => new(100, 42, 8, true, Now.AddDays(-2), Now.AddDays(2));
    private static Rules.CapacidadSnapshot Capacity() => new("ventas.crear", 10, false, 2, true, true, true, true);
    private static Rules.SolicitudUso Request() => new(100, 11, 8, 3, "ventas.crear", 3, false);
    private static Rules.Decision Eval(
        Rules.VerifiedActor? actor = null,
        Rules.PlanSnapshot? plan = null,
        Rules.SuscripcionSnapshot? subscription = null,
        Rules.CapacidadSnapshot? cap = null,
        Rules.SolicitudUso? request = null) =>
        Rules.Evaluar(actor ?? Actor(), plan ?? Plan(), subscription ?? Subscription(),
            cap ?? Capacity(), request ?? Request(), Now);

    [Fact]
    public void PositiveVerifiedPlanIsOnlyPreconditionNotAuthority()
    {
        var result = Eval();
        Assert.Equal("PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO", result.Codigo);
        Assert.False(result.AutorizaAcceso);
        Assert.False(result.AutorizaEscritura);
        Assert.False(result.AutorizaCobro);
        Assert.False(result.CertificaJwtMysqlOHttp);
    }

    [Fact]
    public void MissingVerifiedSnapshotsAndIdentityFailClosed()
    {
        Assert.Equal("CONTEXTO_SERVIDOR_VERIFICADO_REQUERIDO",
            Rules.Evaluar(null, Plan(), Subscription(), Capacity(), Request(), Now).Codigo);
        Assert.Equal("CONTEXTO_SERVIDOR_VERIFICADO_REQUERIDO",
            Rules.Evaluar(Actor(), null, Subscription(), Capacity(), Request(), Now).Codigo);
        Assert.Equal("CONTEXTO_SERVIDOR_VERIFICADO_REQUERIDO",
            Rules.Evaluar(Actor(), Plan(), null, Capacity(), Request(), Now).Codigo);
        Assert.Equal("CONTEXTO_SERVIDOR_VERIFICADO_REQUERIDO",
            Rules.Evaluar(Actor(), Plan(), Subscription(), null, Request(), Now).Codigo);
        Assert.Equal("CONTEXTO_SERVIDOR_VERIFICADO_REQUERIDO",
            Rules.Evaluar(Actor(), Plan(), Subscription(), Capacity(), null, Now).Codigo);
        Assert.Equal("CONTEXTO_SERVIDOR_VERIFICADO_REQUERIDO",
            Eval(actor: Actor() with { ActorId = 0 }).Codigo);
    }

    [Fact]
    public void AAuthenticatedCannotConsumeBTenantSubscriptionOrForgedTarget()
    {
        Assert.Equal("TENANT_SUSCRIPCION_NO_COINCIDE",
            Eval(subscription: Subscription() with { EmpresaId = 200 }).Codigo);
        Assert.Equal("TENANT_SUSCRIPCION_NO_COINCIDE",
            Eval(request: Request() with { EmpresaObjetivoId = 200 }).Codigo);
    }

    [Fact]
    public void RevokedRoleMembershipAndMissingExplicitRbacAreDenied()
    {
        foreach (var bad in new[]
        {
            Actor() with { TenantActivo = false },
            Actor() with { MembresiaActiva = false },
            Actor() with { RolActivo = false },
            Actor() with { PermisoRevocado = true },
            Actor() with { PermisoRelacionalExplicito = false }
        })
            Assert.Equal("RBAC_O_MEMBRESIA_DENEGADOS", Eval(actor: bad).Codigo);
    }

    [Fact]
    public void BranchMismatchOrInvalidBranchAreDenied()
    {
        Assert.Equal("SUCURSAL_RECURSO_NO_AUTORIZADA",
            Eval(request: Request() with { SucursalRecursoId = 12 }).Codigo);
        Assert.Equal("SUCURSAL_RECURSO_NO_AUTORIZADA",
            Eval(request: Request() with { SucursalRecursoId = 0 }).Codigo);
    }

    [Fact]
    public void PlanInactiveWrongOrNonCanonicalMustFail()
    {
        Assert.Equal("PLAN_NO_DISPONIBLE_O_DISTINTO", Eval(plan: Plan() with { Activo = false }).Codigo);
        Assert.Equal("PLAN_NO_DISPONIBLE_O_DISTINTO", Eval(plan: Plan() with { CodigoCanonico = "plan-basico" }).Codigo);
        Assert.Equal("PLAN_NO_DISPONIBLE_O_DISTINTO", Eval(subscription: Subscription() with { PlanId = 43 }).Codigo);
    }

    [Fact]
    public void ConcurrencyRequiresExactSubscriptionAndPlanRevision()
    {
        Assert.Equal("VERSION_O_REVISION_OBSOLETA",
            Eval(request: Request() with { VersionSuscripcionEsperada = 7 }).Codigo);
        Assert.Equal("VERSION_O_REVISION_OBSOLETA",
            Eval(request: Request() with { RevisionPlanEsperada = 2 }).Codigo);
    }

    [Fact]
    public void ExpiredSubscriptionPlanAndNonUtcEvidenceFailClosed()
    {
        Assert.Equal("PLAN_O_SUSCRIPCION_NO_VIGENTES",
            Eval(subscription: Subscription() with { FinUtc = Now }).Codigo);
        Assert.Equal("PLAN_O_SUSCRIPCION_NO_VIGENTES",
            Eval(plan: Plan() with { VigenteDesdeUtc = Now.AddSeconds(1) }).Codigo);
        Assert.Equal("EVIDENCIA_UTC_INVALIDA",
            Eval(plan: Plan() with { VigenteDesdeUtc = DateTime.SpecifyKind(Now.AddDays(-1), DateTimeKind.Unspecified) }).Codigo);
        Assert.Equal("EVIDENCIA_UTC_INVALIDA",
            Rules.Evaluar(Actor(), Plan(), Subscription(), Capacity(), Request(),
                DateTime.SpecifyKind(Now, DateTimeKind.Local)).Codigo);
    }

    [Fact]
    public void CommercialPriceTaxProrationAndChargesNeverAuthorized()
    {
        var result = Eval(request: Request() with { SolicitaPrecioImpuestoProrrateoOCobro = true });
        Assert.Equal("DECISION_COMERCIAL_O_COBRO_NO_AUTORIZADOS", result.Codigo);
        Assert.False(result.AutorizaCobro);
        Assert.False(result.AutorizaEscritura);
    }

    [Fact]
    public void CapabilityMustBeCanonicalAndIntersectAllFourSources()
    {
        Assert.Equal("CAPACIDAD_CANONICA_NO_VERIFICADA",
            Eval(request: Request() with { ClaveCapacidad = "Ventas.Crear" }).Codigo);
        Assert.Equal("CAPACIDAD_CANONICA_NO_VERIFICADA",
            Eval(cap: Capacity() with { Clave = "ventas.leer" }).Codigo);
        Assert.Equal("CAPACIDAD_NO_HABILITADA_EXPRESAMENTE",
            Eval(cap: Capacity() with { HabilitadaPorRol = false }).Codigo);
        Assert.Equal("CAPACIDAD_NO_HABILITADA_EXPRESAMENTE",
            Eval(cap: Capacity() with { HabilitadaPorPlan = false }).Codigo);
        Assert.Equal("CAPACIDAD_NO_HABILITADA_EXPRESAMENTE",
            Eval(cap: Capacity() with { HabilitadaPorTenant = false }).Codigo);
        Assert.Equal("CAPACIDAD_NO_HABILITADA_EXPRESAMENTE",
            Eval(cap: Capacity() with { HabilitadaPorRubro = false }).Codigo);
    }

    [Fact]
    public void UnlimitedMustBeExplicitAndBoundedLimitPositive()
    {
        Assert.Equal("LIMITE_O_ILIMITADO_AMBIGUO",
            Eval(cap: Capacity() with { Maximo = null }).Codigo);
        Assert.Equal("LIMITE_O_ILIMITADO_AMBIGUO",
            Eval(cap: Capacity() with { IlimitadoExplicito = true }).Codigo);
        Assert.Equal("LIMITE_O_ILIMITADO_AMBIGUO",
            Eval(cap: Capacity() with { Maximo = 0 }).Codigo);
        Assert.Equal("PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO",
            Eval(cap: Capacity() with { Maximo = null, IlimitadoExplicito = true }).Codigo);
    }

    [Fact]
    public void QuotaAtBoundaryAndLargeValuesNeverOverflowOrGrant()
    {
        Assert.Equal("CUOTA_INSUFICIENTE",
            Eval(cap: Capacity() with { ConsumoActual = 9 }, request: Request() with { UnidadesAdicionales = 2 }).Codigo);
        Assert.Equal("CONSUMO_O_INCREMENTO_INVALIDO",
            Eval(cap: Capacity() with { ConsumoActual = -1 }).Codigo);
        Assert.Equal("CONSUMO_O_INCREMENTO_INVALIDO",
            Eval(request: Request() with { UnidadesAdicionales = 0 }).Codigo);
        Assert.Equal("CUOTA_INSUFICIENTE",
            Eval(cap: Capacity() with { Maximo = long.MaxValue, ConsumoActual = long.MaxValue - 2 },
                request: Request() with { UnidadesAdicionales = long.MaxValue }).Codigo);
        Assert.Equal("PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO",
            Eval(cap: Capacity() with { ConsumoActual = 7 }).Codigo);
    }
}
