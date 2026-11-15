using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1004CacheJobsAuthorizationGateTests
{
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class Source : IC1004VerifiedWorkEvidenceSource
    {
        public C1004CacheJobsApplicationService.VerifiedSnapshot? Evidence { get; set; } = Good();
        public Exception? Failure { get; set; }
        public int Reads { get; private set; }
        public ValueTask<C1004CacheJobsApplicationService.VerifiedSnapshot?> ReadVerifiedAsync(
            string workId, CancellationToken ct)
        {
            Reads++;
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Evidence);
        }
    }

    private static ContextoTenantActual Context(int actor = 10, int company = 100) =>
        ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(actor, company, 5), actor, company);
    private static C1004CacheJobsApplicationService.VerifiedSnapshot Good() =>
        new(Context(), 10, 100, 7, true, false, true, false, false,
            new HashSet<string> { "export.request", "cache.invalidate" },
            0, 3, Now.AddMinutes(30), Now);
    private static C1004CacheJobsApplicationService.Request Request() =>
        new("work-7", "export.request", "idem-12345678", Digest, 100, 7);

    [Fact]
    public async Task DatosValidosNuncaOtorganEfectosDeRuntime()
    {
        var source = new Source();
        var result = await new C1004CacheJobsAuthorizationGate(source).CheckAsync(Request());
        Assert.Equal("PRECONDITIONS_ONLY_EXTERNAL_CAS_REQUIRED", result.Code);
        Assert.False(result.CanEnqueue);
        Assert.False(result.CanWrite);
        Assert.False(result.CanAcquireLease);
        Assert.False(result.CanGrantProviderAccess);
        Assert.False(result.CanReadPrivateArtifact);
        Assert.False(result.CertifiesRuntimeSecurity);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task EntradaHostilRechazadaAntesDeLeerFuente()
    {
        var source = new Source();
        var gate = new C1004CacheJobsAuthorizationGate(source);
        Assert.Equal("REQUEST_INVALID", (await gate.CheckAsync(null)).Code);
        Assert.Equal("REQUEST_INVALID", (await gate.CheckAsync(Request() with { WorkId = "../evil" })).Code);
        Assert.Equal("REQUEST_INVALID", (await gate.CheckAsync(Request() with { Operation = "https://evil" })).Code);
        Assert.Equal("REQUEST_INVALID", (await gate.CheckAsync(Request() with { ExpectedVersion = 0 })).Code);
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task FuenteAusenteOConErrorFallaCerrado()
    {
        var source = new Source { Evidence = null };
        var gate = new C1004CacheJobsAuthorizationGate(source);
        Assert.Equal("VERIFIED_BACKEND_CONTEXT_REQUIRED", (await gate.CheckAsync(Request())).Code);
        source.Failure = new InvalidOperationException("private provider");
        Assert.Equal("VERIFIED_SOURCE_UNAVAILABLE", (await gate.CheckAsync(Request())).Code);
    }

    [Fact]
    public async Task ActorDistintoNoPuedeSuplantarContexto()
    {
        var source = new Source { Evidence = Good() with { VerifiedActorId = 11 } };
        Assert.Equal("ACTOR_OR_TENANT_MISMATCH",
            (await new C1004CacheJobsAuthorizationGate(source).CheckAsync(Request())).Code);
    }

    [Fact]
    public async Task TenantAjenoPersistidoSeDeniegaSinHint()
    {
        var source = new Source { Evidence = Good() with { PersistedResourceEmpresaId = 200 } };
        Assert.Equal("ACTOR_OR_TENANT_MISMATCH",
            (await new C1004CacheJobsAuthorizationGate(source).CheckAsync(
                Request() with { ClientTenantHint = null })).Code);
    }

    [Fact]
    public async Task HintClienteAjenoNoConcedeAutoridad()
    {
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            (await new C1004CacheJobsAuthorizationGate(new Source()).CheckAsync(
                Request() with { ClientTenantHint = 200 })).Code);
    }

    [Fact]
    public async Task MembresiaRevocadaYPermisosAusentesFallan()
    {
        var source = new Source { Evidence = Good() with { ActiveMembership = false } };
        var gate = new C1004CacheJobsAuthorizationGate(source);
        Assert.Equal("PERMISSION_OR_MEMBERSHIP_DENIED", (await gate.CheckAsync(Request())).Code);
        source.Evidence = Good() with { PermissionRevoked = true };
        Assert.Equal("PERMISSION_OR_MEMBERSHIP_DENIED", (await gate.CheckAsync(Request())).Code);
        source.Evidence = Good() with { ExplicitOperationPermission = false };
        Assert.Equal("PERMISSION_OR_MEMBERSHIP_DENIED", (await gate.CheckAsync(Request())).Code);
    }

    [Fact]
    public async Task CuentaServicioSinGrantIndependienteSeDeniega()
    {
        var source = new Source { Evidence = Good() with { IsServiceActor = true } };
        Assert.Equal("PERMISSION_OR_MEMBERSHIP_DENIED",
            (await new C1004CacheJobsAuthorizationGate(source).CheckAsync(Request())).Code);
    }

    [Fact]
    public async Task OperacionNoAutorizadaOVersionObsoletaFallan()
    {
        var gate = new C1004CacheJobsAuthorizationGate(new Source());
        Assert.Equal("OPERATION_NOT_ALLOWLISTED",
            (await gate.CheckAsync(Request() with { Operation = "backup.delete" })).Code);
        Assert.Equal("PERSISTED_VERSION_STALE",
            (await gate.CheckAsync(Request() with { ExpectedVersion = 6 })).Code);
    }

    [Fact]
    public async Task HuellaInvalidaDeadlineEIntentosAgotadosSeDeniegan()
    {
        var source = new Source();
        var gate = new C1004CacheJobsAuthorizationGate(source);
        Assert.Equal("REQUEST_PARAMETERS_INVALID",
            (await gate.CheckAsync(Request() with { PayloadSha256 = new string('z', 64) })).Code);
        source.Evidence = Good() with { DeadlineUtc = Now };
        Assert.Equal("TENANT_OR_POLICY_DENIED", (await gate.CheckAsync(Request())).Code);
        source.Evidence = Good() with { RetryAttempt = 3 };
        Assert.Equal("TENANT_OR_POLICY_DENIED", (await gate.CheckAsync(Request())).Code);
    }

    [Fact]
    public async Task CancelacionSePropagaSinLeerFuente()
    {
        var source = new Source();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C1004CacheJobsAuthorizationGate(source).CheckAsync(Request(), cts.Token));
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public void RequiereProveedorDeEvidenciaVerificada()
    {
        Assert.Throws<ArgumentNullException>(() => new C1004CacheJobsAuthorizationGate(null!));
    }
}
