using Xunit;
using Rules = global::Solqaryn.Domain.AutomationV5.C0401PlanDomainRules;
using Service = global::Solqaryn.Application.AutomationV5.C0401PlanApplicationService;
using global::Solqaryn.Application.AutomationV5;

namespace Solqaryn.Tests.Application;

public sealed class C0401M0302PlanApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 5, 0, 0, DateTimeKind.Utc);

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private static Service.AuthenticatedActor Actor() =>
        new(new Rules.VerifiedActor(100, 7, 5, 11, true, true, true, true, false),
            true, true, true, true);
    private static Service.ServerPlanContext Snapshot() =>
        new(new Rules.PlanSnapshot(42, "PLAN-BASICO", 3, true,
                Now.AddDays(-10), Now.AddDays(10)),
            new Rules.SuscripcionSnapshot(100, 42, 8, true,
                Now.AddDays(-2), Now.AddDays(2)),
            new Rules.CapacidadSnapshot("ventas.crear", 10, false, 2,
                true, true, true, true),
            true, true, true);
    private static Service.ClientIntent Intent() =>
        new(42, new Rules.SolicitudUso(100, 11, 8, 3, "ventas.crear", 3, false));

    private sealed class ActorSource : IC0401VerifiedPlanActorSource
    {
        public Service.AuthenticatedActor? Value { get; set; } = Actor();
        public Exception? Failure { get; set; }
        public Action? BeforeReturn { get; set; }
        public int Calls { get; private set; }

        public ValueTask<Service.AuthenticatedActor?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Failure is not null) throw Failure;
            BeforeReturn?.Invoke();
            return ValueTask.FromResult(Value);
        }
    }

    private sealed class PlanSource : IC0401VerifiedPlanSnapshotSource
    {
        public Service.ServerPlanContext? Value { get; set; } = Snapshot();
        public Exception? Failure { get; set; }
        public Action? BeforeReturn { get; set; }
        public int Calls { get; private set; }
        public int LastTenant { get; private set; }
        public int LastPlan { get; private set; }

        public ValueTask<Service.ServerPlanContext?> ReadForTenantAsync(
            int tenantId, int planIdHint, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            LastTenant = tenantId;
            LastPlan = planIdHint;
            if (Failure is not null) throw Failure;
            BeforeReturn?.Invoke();
            return ValueTask.FromResult(Value);
        }
    }

    private static Service Build(ActorSource actor, PlanSource plan) =>
        new(actor, plan, new Clock());

    [Fact]
    public async Task PositivePreflightNeverGrantsReadsWritesPaymentsOrQuota()
    {
        var a = new ActorSource(); var p = new PlanSource();
        var r = await Build(a, p).PrepareAsync(Intent());
        Assert.Equal("REAL_JWT_RBAC_MYSQL_TRANSACTION_STILL_REQUIRED", r.Code);
        Assert.True(r.RequiresLiveAtomicBackendGate);
        Assert.False(r.AuthorizesReadOrWrite);
        Assert.False(r.ReservesQuota);
        Assert.False(r.AuthorizesPayment);
        Assert.False(r.CertifiesJwtMysqlHttp);
        Assert.Equal(100, p.LastTenant);
        Assert.Equal(42, p.LastPlan);
        Assert.Equal(1, a.Calls);
        Assert.Equal(1, p.Calls);
    }

    [Fact]
    public async Task BadIntentFailsBeforeTrustedAdapterAccess()
    {
        var a = new ActorSource(); var p = new PlanSource();
        var svc = Build(a, p);
        foreach (var request in new Service.ClientIntent?[]
        {
            null, Intent() with { PlanIdHint = 0 },
            Intent() with { Solicitud = null },
            Intent() with { Solicitud = Intent().Solicitud! with { EmpresaObjetivoId = 0 } },
            Intent() with { Solicitud = Intent().Solicitud! with { UnidadesAdicionales = 0 } },
            Intent() with { Solicitud = Intent().Solicitud! with { VersionSuscripcionEsperada = 0 } }
        })
            Assert.Equal("CLIENT_INTENT_INVALID_OR_STALE", (await svc.PrepareAsync(request)).Code);
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, p.Calls);
    }

    [Fact]
    public async Task ClientClaimsPaymentsAndDirectWritesCannotReachProviders()
    {
        var a = new ActorSource(); var p = new PlanSource();
        var svc = Build(a, p);
        Assert.Equal("CLIENT_AUTHORITY_OR_PAYMENT_WRITE_DENIED",
            (await svc.PrepareAsync(Intent() with { DeclaraAutoridadDeCliente = true })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_PAYMENT_WRITE_DENIED",
            (await svc.PrepareAsync(Intent() with { PideReservaOEscrituraDirecta = true })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_PAYMENT_WRITE_DENIED",
            (await svc.PrepareAsync(Intent() with
            { Solicitud = Intent().Solicitud! with { SolicitaPrecioImpuestoProrrateoOCobro = true } })).Code);
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, p.Calls);
    }

    [Fact]
    public async Task UnsignedJwtRevokedRbacAndMembershipDenyBeforePrivatePlanLookup()
    {
        foreach (var invalid in new[]
        {
            Actor() with { JwtFirmadoYValidado = false },
            Actor() with { TenantResueltoEnServidor = false },
            Actor() with { RbacRelacionalFresco = false },
            Actor() with { MembresiaFresca = false },
            Actor() with { Scope = Actor().Scope with { PermisoRevocado = true } },
            Actor() with { Scope = Actor().Scope with { MembresiaActiva = false } },
            Actor() with { Scope = Actor().Scope with { PermisoRelacionalExplicito = false } }
        })
        {
            var a = new ActorSource { Value = invalid }; var p = new PlanSource();
            Assert.Equal("LIVE_SIGNED_JWT_RBAC_MEMBERSHIP_REQUIRED",
                (await Build(a, p).PrepareAsync(Intent())).Code);
            Assert.Equal(0, p.Calls);
        }
    }

    [Fact]
    public async Task ClientTenantBCannotSelectAuthenticatedTenantA()
    {
        var a = new ActorSource(); var p = new PlanSource();
        var client = Intent() with
        { Solicitud = Intent().Solicitud! with { EmpresaObjetivoId = 200 } };
        Assert.Equal("CLIENT_TENANT_HINT_CONFLICT",
            (await Build(a, p).PrepareAsync(client)).Code);
        Assert.Equal(0, p.Calls);
    }

    [Fact]
    public async Task MissingUnscopedWrongPlanOrCrossTenantSnapshotsDeny()
    {
        foreach (Service.ServerPlanContext? snapshot in new Service.ServerPlanContext?[]
        {
            null, Snapshot() with { ConsultaFiltradaPorTenant = false },
            Snapshot() with { CatalogoVerificadoEnServidor = false },
            Snapshot() with { VersionesFrescas = false },
            Snapshot() with { Plan = Snapshot().Plan with { PlanId = 999 } },
            Snapshot() with { Suscripcion = Snapshot().Suscripcion with { EmpresaId = 200 } }
        })
        {
            var p = new PlanSource { Value = snapshot };
            Assert.Equal("TRUSTED_TENANT_PLAN_SNAPSHOT_REQUIRED",
                (await Build(new ActorSource(), p).PrepareAsync(Intent())).Code);
            Assert.Equal(100, p.LastTenant);
        }
    }

    [Fact]
    public async Task DomainExpiryStaleVersionsQuotaAndBranchDenialsStayDenied()
    {
        var a = new ActorSource();
        var p = new PlanSource { Value = Snapshot() with
        { Suscripcion = Snapshot().Suscripcion with { FinUtc = Now } } };
        Assert.Equal("PLAN_O_SUSCRIPCION_NO_VIGENTES",
            (await Build(a, p).PrepareAsync(Intent())).Code);

        p.Value = Snapshot();
        Assert.Equal("VERSION_O_REVISION_OBSOLETA",
            (await Build(a, p).PrepareAsync(Intent() with
            { Solicitud = Intent().Solicitud! with { RevisionPlanEsperada = 999 } })).Code);
        Assert.Equal("SUCURSAL_RECURSO_NO_AUTORIZADA",
            (await Build(a, p).PrepareAsync(Intent() with
            { Solicitud = Intent().Solicitud! with { SucursalRecursoId = 12 } })).Code);

        p.Value = Snapshot() with
        { Capacidad = Snapshot().Capacidad with { ConsumoActual = 9 } };
        var denied = await Build(a, p).PrepareAsync(Intent() with
        { Solicitud = Intent().Solicitud! with { UnidadesAdicionales = 2 } });
        Assert.Equal("CUOTA_INSUFICIENTE", denied.Code);
        Assert.False(denied.RequiresLiveAtomicBackendGate);
        Assert.False(denied.AuthorizesReadOrWrite);
    }

    [Fact]
    public async Task SourceExceptionsFailClosedWithoutExposingDetails()
    {
        var a = new ActorSource { Failure = new InvalidOperationException("JWT secret details") };
        var p = new PlanSource();
        Assert.Equal("AUTHENTICATED_AUTHORITY_SOURCE_UNAVAILABLE",
            (await Build(a, p).PrepareAsync(Intent())).Code);
        Assert.Equal(0, p.Calls);
        var broken = new PlanSource { Failure = new InvalidOperationException("private database") };
        Assert.Equal("SERVER_PLAN_SOURCE_UNAVAILABLE",
            (await Build(new ActorSource(), broken).PrepareAsync(Intent())).Code);
    }

    [Fact]
    public async Task CancellationAfterNonCooperativeAwaitCannotReachNextSource()
    {
        using var cts = new CancellationTokenSource();
        var a = new ActorSource { BeforeReturn = () => cts.Cancel() };
        var p = new PlanSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            { await Build(a, p).PrepareAsync(Intent(), cts.Token); });
        Assert.Equal(0, p.Calls);
    }

    [Fact]
    public async Task CancellationAfterPlanProviderNeverReturnsPreflight()
    {
        using var cts = new CancellationTokenSource();
        var p = new PlanSource { BeforeReturn = () => cts.Cancel() };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            { await Build(new ActorSource(), p).PrepareAsync(Intent(), cts.Token); });
        Assert.Equal(1, p.Calls);
    }
}
