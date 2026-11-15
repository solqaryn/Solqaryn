using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0604PortalClienteApplicationServiceTests
{
    private static readonly DateTime Ahora = new(2026, 11, 15, 13, 0, 0, DateTimeKind.Utc);

    private sealed class Source : IC0604PortalVerifiedSnapshotSource
    {
        public C0604PortalClienteApplicationService.VerifiedSnapshot? Snapshot { get; set; } = Good();
        public int Reads { get; private set; }

        public ValueTask<C0604PortalClienteApplicationService.VerifiedSnapshot?> LoadVerifiedAsync(
            string resourceId, CancellationToken cancellationToken)
        {
            Reads++;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static C0604PortalClienteApplicationService.VerifiedSnapshot Good()
    {
        var customer = new Cliente { Id = 101, EmpresaId = 10, Nombre = "Cliente A1", Activo = true };
        var account = new TiendaCuentaCliente
        {
            Id = 501, ClienteId = 101, Activa = true,
            CorreoNormalizado = "a1@example.invalid", PasswordHash = "synthetic"
        };
        var session = new TiendaSesionCliente
        {
            Id = 701, CuentaClienteId = 501,
            TokenHash = "synthetic-token-digest", ExpiraUtc = Ahora.AddMinutes(20)
        };
        return new(10, 101, 501, customer, account, session, 10, 101, 3, Ahora,
            true, true, true, false, true);
    }

    private static C0604PortalClienteApplicationService.Request Request() =>
        new("case-1", 10, 3);

    [Fact]
    public async Task AcceptedShapeNeverGrantsPrivateDataOrJwt()
    {
        var source = new Source();
        var result = await new C0604PortalClienteApplicationService(source).PreflightAsync(Request());
        Assert.Equal("AUTHENTICATED_CUSTOMER_ADAPTER_REQUIRED", result.Code);
        Assert.False(result.AllowsPrivateRead);
        Assert.False(result.AllowsWrite);
        Assert.False(result.IssuesJwt);
        Assert.False(result.AuthorizesOrder);
        Assert.False(result.CertifiesTenantIsolation);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task BadInputsDoNotReachSource()
    {
        var source = new Source();
        var app = new C0604PortalClienteApplicationService(source);
        Assert.Equal("REQUEST_INVALID", (await app.PreflightAsync(null)).Code);
        Assert.Equal("REQUEST_INVALID", (await app.PreflightAsync(Request() with { ResourceId = "../b" })).Code);
        Assert.Equal("REQUEST_INVALID", (await app.PreflightAsync(Request() with { ExpectedVersion = 0 })).Code);
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task MissingAndUntrustedSnapshotDenied()
    {
        var source = new Source { Snapshot = null };
        var app = new C0604PortalClienteApplicationService(source);
        Assert.Equal("TRUSTED_BACKEND_SNAPSHOT_REQUIRED", (await app.PreflightAsync(Request())).Code);
        source.Snapshot = Good() with { ServerNowUtc = DateTime.SpecifyKind(Ahora, DateTimeKind.Unspecified) };
        Assert.Equal("TRUSTED_BACKEND_SNAPSHOT_REQUIRED", (await app.PreflightAsync(Request())).Code);
    }

    [Fact]
    public async Task CrossTenantAndCrossCustomerResourceDenied()
    {
        var source = new Source();
        var app = new C0604PortalClienteApplicationService(source);
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            (await app.PreflightAsync(Request() with { ClientTenantHint = 20 })).Code);
        source.Snapshot = Good() with { PersistedResourceEmpresaId = 20 };
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await app.PreflightAsync(Request() with { ClientTenantHint = null })).Code);
        source.Snapshot = Good() with { PersistedResourceCustomerId = 102 };
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await app.PreflightAsync(Request())).Code);
    }

    [Fact]
    public async Task CustomerLegacyWithoutOwnerDenied()
    {
        var snapshot = Good();
        snapshot.PersistedCustomer!.EmpresaId = null;
        var source = new Source { Snapshot = snapshot };
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await new C0604PortalClienteApplicationService(source).PreflightAsync(Request())).Code);
    }

    [Fact]
    public async Task StaleVersionNeverGrantsWrite()
    {
        var app = new C0604PortalClienteApplicationService(new Source());
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await app.PreflightAsync(Request() with { ExpectedVersion = 2 })).Code);
    }

    [Fact]
    public async Task RevokedOrExpiredSessionDenied()
    {
        var source = new Source { Snapshot = Good() with { PermissionRevoked = true } };
        var app = new C0604PortalClienteApplicationService(source);
        Assert.Equal("AUTHENTICATION_OR_CUSTOMER_PERMISSION_REQUIRED",
            (await app.PreflightAsync(Request())).Code);
        var expired = Good();
        expired.PersistedSession!.ExpiraUtc = Ahora;
        source.Snapshot = expired;
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await app.PreflightAsync(Request())).Code);
    }

    [Fact]
    public async Task InvalidJwtTokenProofAndMembershipDenied()
    {
        var source = new Source { Snapshot = Good() with { JwtVerifiedOnServer = false } };
        var app = new C0604PortalClienteApplicationService(source);
        Assert.Equal("AUTHENTICATION_OR_CUSTOMER_PERMISSION_REQUIRED",
            (await app.PreflightAsync(Request())).Code);
        source.Snapshot = Good() with { SessionTokenProofVerified = false };
        Assert.Equal("AUTHENTICATION_OR_CUSTOMER_PERMISSION_REQUIRED",
            (await app.PreflightAsync(Request())).Code);
        source.Snapshot = Good() with { MembershipActive = false };
        Assert.Equal("AUTHENTICATION_OR_CUSTOMER_PERMISSION_REQUIRED",
            (await app.PreflightAsync(Request())).Code);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C0604PortalClienteApplicationService(new Source())
                .PreflightAsync(Request(), cts.Token));
    }

    [Fact]
    public void SourceMustBeProvided()
    {
        Assert.Throws<ArgumentNullException>(() => new C0604PortalClienteApplicationService(null!));
    }
}
