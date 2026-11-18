using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Xunit;
using Adapter = Solqaryn.Application.AutomationV5.C0604IsolatedPortalPersistenceAdapter;
using Gate = Solqaryn.Application.AutomationV5.C0604PortalClienteAuthorizationGate;

namespace Solqaryn.Tests.Application;

public sealed class C0604IsolatedPortalPersistenceAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTime Now = new(2026, 11, 18, 19, 0, 0, DateTimeKind.Utc);

    private sealed class Source : IC0604TrustedPortalPersistenceReader
    {
        public Adapter.PersistedRead? Current { get; set; } = Good();
        public int Calls { get; private set; }
        public bool Throws { get; set; }
        public bool Cancels { get; set; }
        public ValueTask<Adapter.PersistedRead?> ReadAsync(
            string resourceId, CancellationToken cancellationToken)
        {
            Calls++;
            if (Cancels) throw new OperationCanceledException(cancellationToken);
            if (Throws) throw new InvalidOperationException("private provider detail");
            return ValueTask.FromResult(Current);
        }
    }

    private static Adapter.PersistedRead Good()
    {
        var customer = new Cliente { Id = 101, EmpresaId = 10, Nombre = "A1", Activo = true };
        var account = new TiendaCuentaCliente
        {
            Id = 501, ClienteId = 101, Activa = true,
            CorreoNormalizado = "test@example.invalid", PasswordHash = "synthetic"
        };
        var session = new TiendaSesionCliente
        {
            Id = 701, CuentaClienteId = 501, TokenHash = "synthetic",
            ExpiraUtc = Now.AddMinutes(20)
        };
        var snapshot = new C0604PortalClienteApplicationService.VerifiedSnapshot(
            10, 101, 501, customer, account, session, 10, 101, 3,
            Now, true, true, true, false, true);
        var evidence = new Gate.Evidence(Sha, "portal-actor-7", 10, 101, 501,
            true, true, true, true, false, true, snapshot);
        return new("case_1", Sha, 3, true, true, false, Now, evidence);
    }

    [Fact]
    public async Task CoherentIsolatedEvidenceStillNeverGrantsPrivateReadOrJwt()
    {
        var source = new Source();
        var adapter = new Adapter(source);
        var result = await new Gate(adapter).EvaluateAsync(
            new Gate.Request("case_1", Sha, 3, 10, false, false, false));
        Assert.Equal("AUTHENTICATED_CUSTOMER_RUNTIME_ADAPTER_REQUIRED", result.Code);
        Assert.False(result.GrantsPrivateRead);
        Assert.False(result.GrantsCrossTenantRead);
        Assert.False(result.GrantsWrite);
        Assert.False(result.IssuesJwt);
        Assert.False(result.CertifiesLiveAuthentication);
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task UnsafeResourceIsRejectedBeforeProviderAccess()
    {
        var source = new Source();
        var adapter = new Adapter(source);
        Assert.Null(await adapter.ReadCurrentAsync("../case_1", default));
        Assert.Null(await adapter.ReadCurrentAsync(new string('x', 129), default));
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task UntrustedOrRevokedPersistenceProofIsRejected()
    {
        var source = new Source { Current = Good() with { TrustedBackendProvenance = false } };
        var adapter = new Adapter(source);
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Current = Good() with { PersistedOwnershipChecked = false };
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Current = Good() with { PermissionRevoked = true };
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
    }

    [Fact]
    public async Task TenantAndOwnerMismatchAreRejectedBeforeAuthorization()
    {
        var source = new Source { Current = Good() with { Evidence = Good().Evidence! with { VerifiedEmpresaId = 20 } } };
        var adapter = new Adapter(source);
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Current = Good() with { Evidence = Good().Evidence! with { VerifiedCustomerId = 102 } };
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Current = Good() with { ResourceId = "other_case" };
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
    }

    [Fact]
    public async Task InvalidShaAndStaleVersionAreRejected()
    {
        var source = new Source { Current = Good() with { SourceSha = "not-a-sha" } };
        var adapter = new Adapter(source);
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Current = Good() with { SourceSha = new string('a', 40) };
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Current = Good() with { PersistedVersion = 4 };
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Current = Good() with { CapturedAtUtc = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified) };
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
    }

    [Fact]
    public async Task NullAndSourceErrorsFailClosedWithoutLeakingProviderDetails()
    {
        var source = new Source { Current = null };
        var adapter = new Adapter(source);
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
        source.Throws = true;
        Assert.Null(await adapter.ReadCurrentAsync("case_1", default));
    }

    [Fact]
    public async Task CancellationAndConstructorValidationArePreserved()
    {
        Assert.Throws<ArgumentNullException>(() => new Adapter(null!));
        var source = new Source();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Adapter(source).ReadCurrentAsync("case_1", cts.Token));
        Assert.Equal(0, source.Calls);
        source.Cancels = true;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Adapter(source).ReadCurrentAsync("case_1", default));
    }
}
