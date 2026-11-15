using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C05-04.M03.02: real Application/Domain tests with a bounded memory source.
/// No HTTP, EF/MySQL, Core Web Vitals or production access claims.
/// </summary>
public sealed class V5UiLightnessApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private sealed class FakeSource : IV5UiLightnessTrustedSnapshotSource
    {
        public V5UiLightnessTrustedSnapshot? Current { get; set; }
        public int Reads { get; private set; }

        public Task<V5UiLightnessTrustedSnapshot?> LoadAsync(
            int usuarioId, int empresaId, string resourceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            return Task.FromResult(Current);
        }
    }

    private static ContextoTenantActual Actor()
    {
        var user = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var empresa = new Empresa("A") { Id = 11 };
        var membership = new UsuarioEmpresa(7, 11, 19);
        var role = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        return IdentidadUniversalReglas.CrearContextoVerificado(
            user, empresa, membership, role);
    }

    private static V5UiLightnessTrustedSnapshot Snapshot(int tenant = 11)
        => new(
            new V5UiLightnessDomainRules.TrustedRequestContext(
                tenant, tenant, true, true, true,
                true, true, false, false, false,
                false, false, "r3", "r3"),
            new V5UiLightnessDomainRules.TrustedBuildEvidence(
                590 * 1024, 12 * 1024, Sha, Sha, true, false));

    [Fact]
    public async Task TrustedSnapshotReturnsExternalVerificationRequiredNotAccessGranted()
    {
        var source = new FakeSource { Current = Snapshot() };
        var result = await new V5UiLightnessApplicationService(source)
            .EvaluateCurrentAsync(Actor(), 11, "opaque-resource");
        Assert.Equal("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED", result.Code);
        Assert.True(result.NeedsIndependentRuntimeVerification);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task TenantBRequestIsDeniedBeforeAnySourceRead()
    {
        var source = new FakeSource { Current = Snapshot(22) };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new V5UiLightnessApplicationService(source)
                .EvaluateCurrentAsync(Actor(), 22, "opaque-resource"));
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task TrustedSourceReportingTenantBForActorAIsRejected()
    {
        var source = new FakeSource { Current = Snapshot(22) };
        var result = await new V5UiLightnessApplicationService(source)
            .EvaluateCurrentAsync(Actor(), 11, "opaque-resource");
        Assert.Equal("TENANT_CACHE_ISOLATION_REQUIRED", result.Code);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task MissingEvidenceNeverBecomesMeasuredSuccess()
    {
        var source = new FakeSource();
        var result = await new V5UiLightnessApplicationService(source)
            .EvaluateCurrentAsync(Actor(), 11, "opaque-resource");
        Assert.Equal("TRUSTED_EVIDENCE_REQUIRED", result.Code);
        Assert.False(result.NeedsIndependentRuntimeVerification);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task RevokedMembershipFromTrustedSnapshotIsDenied()
    {
        var source = new FakeSource
        {
            Current = Snapshot() with
            {
                Request = Snapshot().Request with { CurrentMembershipVerified = false }
            }
        };
        var result = await new V5UiLightnessApplicationService(source)
            .EvaluateCurrentAsync(Actor(), 11, "opaque-resource");
        Assert.Equal("BACKEND_AUTHORITY_REQUIRED", result.Code);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task MissingResourceKeyFailsBeforeSourceRead()
    {
        var source = new FakeSource { Current = Snapshot() };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new V5UiLightnessApplicationService(source)
                .EvaluateCurrentAsync(Actor(), 11, " "));
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task CancelledTokenFailsBeforeSourceRead()
    {
        var source = new FakeSource { Current = Snapshot() };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new V5UiLightnessApplicationService(source)
                .EvaluateCurrentAsync(Actor(), 11, "opaque-resource", cancellation.Token));
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task StaleRevisionIsNeverAccepted()
    {
        var source = new FakeSource
        {
            Current = Snapshot() with
            {
                Request = Snapshot().Request with { ExpectedResourceRevision = "r4" }
            }
        };
        var result = await new V5UiLightnessApplicationService(source)
            .EvaluateCurrentAsync(Actor(), 11, "opaque-resource");
        Assert.Equal("STALE_RESPONSE_REVISION_DENIED", result.Code);
    }
}
