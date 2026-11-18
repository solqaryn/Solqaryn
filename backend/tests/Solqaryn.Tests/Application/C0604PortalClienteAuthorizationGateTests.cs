using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.C0604PortalClienteAuthorizationGate;

namespace Solqaryn.Tests.Application;

public sealed class C0604PortalClienteAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly DateTime Now = new(2026, 11, 15, 13, 0, 0, DateTimeKind.Utc);

    private sealed class Reader : IC0604PortalAuthorizationEvidenceReader
    {
        public Gate.Evidence? Current { get; set; } = Good();
        public int Calls { get; private set; }
        public ValueTask<Gate.Evidence?> ReadCurrentAsync(
            string resourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(Current);
        }
    }

    private static C0604PortalClienteApplicationService.VerifiedSnapshot Snapshot()
    {
        var customer = new Cliente { Id = 101, EmpresaId = 10, Nombre = "A1", Activo = true };
        var account = new TiendaCuentaCliente
        {
            Id = 501, ClienteId = 101, Activa = true,
            CorreoNormalizado = "synthetic@example.invalid", PasswordHash = "synthetic-hash"
        };
        var session = new TiendaSesionCliente
        {
            Id = 701, CuentaClienteId = 501,
            TokenHash = "synthetic-digest", ExpiraUtc = Now.AddMinutes(20)
        };
        return new(10, 101, 501, customer, account, session,
            10, 101, 3, Now, true, true, true, false, true);
    }
    private static Gate.Evidence Good() =>
        new(Sha, "portal-actor-7", 10, 101, 501, true,
            true, true, true, false, true, Snapshot());
    private static Gate.Request Request() =>
        new("case_1", Sha, 3, 10, false, false, false);

    [Fact]
    public async Task CoherentEvidenceNeverGrantsPrivateReadOrJwt()
    {
        var reader = new Reader();
        var result = await new Gate(reader).EvaluateAsync(Request());
        Assert.Equal("AUTHENTICATED_CUSTOMER_RUNTIME_ADAPTER_REQUIRED", result.Code);
        Assert.False(result.GrantsPrivateRead);
        Assert.False(result.GrantsCrossTenantRead);
        Assert.False(result.GrantsWrite);
        Assert.False(result.IssuesJwt);
        Assert.False(result.AuthorizesOrder);
        Assert.False(result.CertifiesLiveAuthentication);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task InvalidRequestsAndClientRoleOverrideFailBeforeReader()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        Assert.Equal("PORTAL_AUTH_REQUEST_INVALID", (await gate.EvaluateAsync(null)).Code);
        Assert.Equal("PORTAL_AUTH_REQUEST_INVALID",
            (await gate.EvaluateAsync(Request() with { ResourceId = "../case" })).Code);
        Assert.Equal("PORTAL_AUTH_REQUEST_INVALID",
            (await gate.EvaluateAsync(Request() with { ExpectedVersion = 0 })).Code);
        Assert.Equal("PORTAL_AUTH_SOURCE_SHA_INVALID",
            (await gate.EvaluateAsync(Request() with { ExpectedSourceSha = "bad" })).Code);
        Assert.Equal("PORTAL_AUTH_CLIENT_AUTHORITY_DENIED",
            (await gate.EvaluateAsync(Request() with { ClientSuppliedRole = true })).Code);
        Assert.Equal("PORTAL_AUTH_CLIENT_AUTHORITY_DENIED",
            (await gate.EvaluateAsync(Request() with { AttemptsWriteOrExport = true })).Code);
        Assert.Equal("PORTAL_AUTH_CLIENT_AUTHORITY_DENIED",
            (await gate.EvaluateAsync(Request() with { IncludesSensitiveTelemetry = true })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task MissingAndStaleBackendEvidenceAreRejected()
    {
        var reader = new Reader { Current = null };
        var gate = new Gate(reader);
        Assert.Equal("PORTAL_AUTH_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good() with { ObservedSourceSha = OtherSha };
        Assert.Equal("PORTAL_AUTH_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task UntrustedActorAndTenantHintsAreDenied()
    {
        var reader = new Reader { Current = Good() with { VerifiedActorId = null } };
        var gate = new Gate(reader);
        Assert.Equal("PORTAL_AUTH_ACTOR_OR_TENANT_UNVERIFIED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good();
        Assert.Equal("PORTAL_AUTH_CLIENT_TENANT_OVERRIDE_DENIED",
            (await gate.EvaluateAsync(Request() with { ClientTenantHint = 20 })).Code);
        reader.Current = Good() with { VerifiedEmpresaId = 0 };
        Assert.Equal("PORTAL_AUTH_ACTOR_OR_TENANT_UNVERIFIED",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    [InlineData("\u2003")]
    [InlineData("\u2028")]
    [InlineData("actor\u200B7")]
    [InlineData("actor\u200D7")]
    [InlineData("user name")]
    public async Task WhitespaceAndInvisibleActorIdentifiersFailClosed(string actorId)
    {
        var reader = new Reader { Current = Good() with { VerifiedActorId = actorId } };
        var result = await new Gate(reader).EvaluateAsync(Request());
        Assert.Equal("PORTAL_AUTH_ACTOR_OR_TENANT_UNVERIFIED", result.Code);
        Assert.False(result.GrantsPrivateRead);
        Assert.False(result.GrantsCrossTenantRead);
        Assert.False(result.GrantsWrite);
        Assert.False(result.IssuesJwt);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task ActorIdentifierLengthBoundaryPreservesValidIdentity()
    {
        var reader = new Reader { Current = Good() with { VerifiedActorId = new string('x', 128) } };
        var gate = new Gate(reader);
        Assert.Equal("AUTHENTICATED_CUSTOMER_RUNTIME_ADAPTER_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good() with { VerifiedActorId = new string('x', 129) };
        Assert.Equal("PORTAL_AUTH_ACTOR_OR_TENANT_UNVERIFIED",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task RevocationJwtAndTokenProofRemainMandatory()
    {
        var reader = new Reader { Current = Good() with { PermissionRevoked = true } };
        var gate = new Gate(reader);
        Assert.Equal("PORTAL_AUTH_CURRENT_BACKEND_PERMISSION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good() with { JwtVerifiedOnServer = false };
        Assert.Equal("PORTAL_AUTH_CURRENT_BACKEND_PERMISSION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good() with { TokenHashProofVerified = false };
        Assert.Equal("PORTAL_AUTH_CURRENT_BACKEND_PERMISSION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good() with { PrivateReadPermissionCurrent = false };
        Assert.Equal("PORTAL_AUTH_CURRENT_BACKEND_PERMISSION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task ActorAndPersistedTenantSnapshotsCannotDisagree()
    {
        var reader = new Reader
        { Current = Good() with { Snapshot = Snapshot() with { ServerVerifiedEmpresaId = 20 } } };
        var gate = new Gate(reader);
        Assert.Equal("PORTAL_AUTH_BACKEND_SNAPSHOT_INCONSISTENT",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good() with { Snapshot = Snapshot() with { SessionTokenProofVerified = false } };
        Assert.Equal("PORTAL_AUTH_BACKEND_SNAPSHOT_INCONSISTENT",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task ResourceOwnershipAndVersionAreCheckedByRealDomainRules()
    {
        var reader = new Reader
        { Current = Good() with { Snapshot = Snapshot() with { PersistedResourceEmpresaId = 20 } } };
        var gate = new Gate(reader);
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good();
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await gate.EvaluateAsync(Request() with { ExpectedVersion = 2 })).Code);
    }

    [Fact]
    public async Task ExpiredSessionAndInvalidAuditEvidenceFailClosed()
    {
        var snap = Snapshot();
        snap.PersistedSession!.ExpiraUtc = Now;
        var reader = new Reader { Current = Good() with { Snapshot = snap } };
        var gate = new Gate(reader);
        Assert.Equal("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED",
            (await gate.EvaluateAsync(Request())).Code);
        reader.Current = Good() with { AuditEvidenceAvailable = false };
        Assert.Equal("PORTAL_AUTH_CURRENT_BACKEND_PERMISSION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task CancellationAndMissingReaderPropagate()
    {
        Assert.Throws<ArgumentNullException>(() => new Gate(null!));
        var reader = new Reader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Gate(reader).EvaluateAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
