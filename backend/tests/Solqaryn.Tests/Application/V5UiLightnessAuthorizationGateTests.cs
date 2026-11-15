using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.V5UiLightnessAuthorizationGate;

namespace Solqaryn.Tests.Application;

public sealed class V5UiLightnessAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private sealed class Reader : IV5UiLightnessAuthorizationEvidenceReader
    {
        public Gate.Evidence? Current { get; set; } = Valid();
        public int Calls { get; private set; }
        public ValueTask<Gate.Evidence?> ReadAsync(
            int actorId, int tenantId, string resourceId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(Current);
        }
    }

    private sealed class SnapshotSource : IV5UiLightnessTrustedSnapshotSource
    {
        public V5UiLightnessTrustedSnapshot? Current { get; set; } = Snapshot();
        public int Calls { get; private set; }
        public Task<V5UiLightnessTrustedSnapshot?> LoadAsync(
            int actorId, int tenantId, string resourceId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(Current);
        }
    }

    private static ContextoTenantActual Actor()
    {
        var user = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var tenant = new Empresa("A") { Id = 11 };
        var membership = new UsuarioEmpresa(7, 11, 19);
        var role = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        return IdentidadUniversalReglas.CrearContextoVerificado(user, tenant, membership, role);
    }

    private static Gate.Request Request() =>
        new(11, "resource_41", Sha, null, false, false, false, false);

    private static Gate.Evidence Valid() =>
        new(Sha, 7, 11, "resource_41", true, true, true, false, true, true, true);

    private static V5UiLightnessTrustedSnapshot Snapshot() => new(
        new V5UiLightnessDomainRules.TrustedRequestContext(
            11, 11, true, true, true, true, true, false, false,
            false, false, false, "rev-3", "rev-3"),
        new V5UiLightnessDomainRules.TrustedBuildEvidence(
            590 * 1024, 12 * 1024, Sha, Sha, true, false));

    private static Gate Create(Reader reader, SnapshotSource source) =>
        new(reader, new V5UiLightnessApplicationService(source));

    [Fact]
    public async Task ValidEvidenceNeverGrantsRuntimeAccess()
    {
        var reader = new Reader();
        var source = new SnapshotSource();
        var result = await Create(reader, source).EvaluateAsync(Actor(), Request());
        Assert.Equal("AUTHENTICATED_LIGHTNESS_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresAuthenticatedAdapter);
        Assert.False(result.GrantsAccess);
        Assert.False(result.GrantsCrossTenantCacheRead);
        Assert.False(result.GrantsPrivatePreload);
        Assert.False(result.CertifiesCoreWebVitals);
        Assert.Equal(1, reader.Calls);
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task InvalidInputsAndShaFailBeforeReader()
    {
        var reader = new Reader();
        var gate = Create(reader, new SnapshotSource());
        Assert.Equal("LIGHTNESS_AUTH_REQUEST_REQUIRED", (await gate.EvaluateAsync(Actor(), null)).Code);
        Assert.Equal("LIGHTNESS_TENANT_OR_RESOURCE_INVALID",
            (await gate.EvaluateAsync(Actor(), Request() with { TenantId = 0 })).Code);
        Assert.Equal("LIGHTNESS_TENANT_OR_RESOURCE_INVALID",
            (await gate.EvaluateAsync(Actor(), Request() with { ResourceId = "../tenant-b" })).Code);
        Assert.Equal("LIGHTNESS_SOURCE_SHA_INVALID",
            (await gate.EvaluateAsync(Actor(), Request() with { ExpectedSourceSha = "invalid" })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ClientBypassPrivatePreloadAndTelemetryAreDenied()
    {
        var reader = new Reader();
        var gate = Create(reader, new SnapshotSource());
        Assert.Equal("LIGHTNESS_CLIENT_SECURITY_BYPASS_DENIED",
            (await gate.EvaluateAsync(Actor(), Request() with { AttemptsTenantCacheBypass = true })).Code);
        Assert.Equal("LIGHTNESS_CLIENT_SECURITY_BYPASS_DENIED",
            (await gate.EvaluateAsync(Actor(), Request() with { AttemptsPrivatePreload = true })).Code);
        Assert.Equal("LIGHTNESS_CLIENT_SECURITY_BYPASS_DENIED",
            (await gate.EvaluateAsync(Actor(), Request() with { SuppressesUnauthorizedResponse = true })).Code);
        Assert.Equal("LIGHTNESS_CLIENT_SECURITY_BYPASS_DENIED",
            (await gate.EvaluateAsync(Actor(), Request() with { IncludesSensitiveTelemetry = true })).Code);
        Assert.Equal("LIGHTNESS_CLIENT_TENANT_OVERRIDE_DENIED",
            (await gate.EvaluateAsync(Actor(), Request() with { ClientTenantId = 22 })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task TenantAActorCannotQueryTenantBEvenForPreflight()
    {
        var reader = new Reader();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(reader, new SnapshotSource()).EvaluateAsync(Actor(), Request() with { TenantId = 22 }));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task StaleShaAndMissingEvidenceFailClosed()
    {
        var reader = new Reader { Current = null };
        var source = new SnapshotSource();
        var gate = Create(reader, source);
        Assert.Equal("LIGHTNESS_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { SourceSha = "abcdef0123456789abcdef0123456789abcdef01" };
        Assert.Equal("LIGHTNESS_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task RevokedMembershipAndGrantsMustBeCurrent()
    {
        var reader = new Reader();
        var gate = Create(reader, new SnapshotSource());
        reader.Current = Valid() with { PermissionRevoked = true };
        Assert.Equal("LIGHTNESS_BACKEND_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { CurrentMembershipVerified = false };
        Assert.Equal("LIGHTNESS_BACKEND_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { CurrentRbacPermissionVerified = false };
        Assert.Equal("LIGHTNESS_BACKEND_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { BackendAuthenticationVerified = false };
        Assert.Equal("LIGHTNESS_BACKEND_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
    }

    [Fact]
    public async Task ActorTenantAndPersistedOwnerMismatchesAreDenied()
    {
        var reader = new Reader();
        var gate = Create(reader, new SnapshotSource());
        reader.Current = Valid() with { ActorId = 8 };
        Assert.Equal("LIGHTNESS_PERSISTED_OWNER_MISMATCH",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { TenantId = 22 };
        Assert.Equal("LIGHTNESS_PERSISTED_OWNER_MISMATCH",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { ResourceId = "resource_42" };
        Assert.Equal("LIGHTNESS_PERSISTED_OWNER_MISMATCH",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { PersistedResourceOwnershipVerified = false };
        Assert.Equal("LIGHTNESS_PERSISTED_OWNER_MISMATCH",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
    }

    [Fact]
    public async Task CrossTenantCacheAndMissingRevocationInvalidationAreRejected()
    {
        var reader = new Reader();
        var gate = Create(reader, new SnapshotSource());
        reader.Current = Valid() with { TenantScopedCacheVerified = false };
        Assert.Equal("LIGHTNESS_TENANT_CACHE_ISOLATION_REQUIRED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        reader.Current = Valid() with { RevocationInvalidationVerified = false };
        Assert.Equal("LIGHTNESS_TENANT_CACHE_ISOLATION_REQUIRED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
    }

    [Fact]
    public async Task StaleRevisionAndUnverifiedBuildCannotBecomeSuccess()
    {
        var source = new SnapshotSource { Current = Snapshot() with
        { Request = Snapshot().Request with { ExpectedResourceRevision = "rev-4" } } };
        var gate = Create(new Reader(), source);
        Assert.Equal("STALE_RESPONSE_REVISION_DENIED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        source.Current = Snapshot() with
        { Build = Snapshot().Build with { ProducedByVerifiedBuildArtifact = false } };
        Assert.Equal("VERIFIED_BUNDLE_MEASUREMENT_REQUIRED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
        source.Current = Snapshot() with
        { Build = Snapshot().Build with { InitialBundleBytes = 800 * 1024 } };
        Assert.Equal("BUNDLE_BUDGET_EXCEEDED",
            (await gate.EvaluateAsync(Actor(), Request())).Code);
    }

    [Fact]
    public async Task CancelledTokenAndNullServicesFailBeforeIo()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Gate(null!, new V5UiLightnessApplicationService(new SnapshotSource())));
        Assert.Throws<ArgumentNullException>(() => new Gate(new Reader(), null!));
        var reader = new Reader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Create(reader, new SnapshotSource()).EvaluateAsync(Actor(), Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
