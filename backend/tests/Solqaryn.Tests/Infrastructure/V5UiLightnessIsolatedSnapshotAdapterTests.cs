using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5UiLightnessIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static ContextoTenantActual Actor()
    {
        var user = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var tenant = new Empresa("A") { Id = 11 };
        var membership = new UsuarioEmpresa(7, 11, 19);
        var role = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        return IdentidadUniversalReglas.CrearContextoVerificado(
            user, tenant, membership, role);
    }

    private static V5UiLightnessTrustedSnapshot Snapshot() => new(
        new V5UiLightnessDomainRules.TrustedRequestContext(
            11, 11, true, true, true, true, true,
            false, false, false, false, false, "revision-3", "revision-3"),
        new V5UiLightnessDomainRules.TrustedBuildEvidence(
            590 * 1024, 12 * 1024, Sha, Sha, true, false));

    private static V5UiLightnessIsolatedSnapshotAdapter Adapter(
        V5UiLightnessTrustedSnapshot? snapshot = null) =>
        new(11, "resource_41", Sha, snapshot ?? Snapshot());

    [Fact]
    public async Task ValidCapturedEvidenceRequiresIndependentBrowserReview()
    {
        var a = Adapter();
        var verdict = await a.EvaluateReadOnlyAsync(Actor(), "resource_41", Sha);
        Assert.Equal("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED", verdict.Code);
        Assert.False(a.GrantsAccess);
        Assert.False(a.AllowsPersistence);
        Assert.False(a.CertifiesBrowserVitals);
        Assert.False(a.IsLiveProvider);
    }

    [Fact]
    public async Task TenantBRequestedByActorAIsRejected()
    {
        var a = new V5UiLightnessIsolatedSnapshotAdapter(22, "resource_41", Sha, Snapshot());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            a.EvaluateReadOnlyAsync(Actor(), "resource_41", Sha));
    }

    [Fact]
    public async Task CrossTenantCapturedEvidenceFailsClosed()
    {
        var snapshot = Snapshot();
        var a = Adapter(snapshot with
        {
            Request = snapshot.Request with { PersistedResourceTenantId = 22 }
        });
        Assert.Equal("TENANT_CACHE_ISOLATION_REQUIRED",
            (await a.EvaluateReadOnlyAsync(Actor(), "resource_41", Sha)).Code);
    }

    [Fact]
    public async Task WrongResourceAndInjectionLikeIdentifiersAreDenied()
    {
        var a = Adapter();
        Assert.Equal("ISOLATED_RESOURCE_SCOPE_MISMATCH",
            (await a.EvaluateReadOnlyAsync(Actor(), "resource_42", Sha)).Code);
        Assert.Equal("ISOLATED_RESOURCE_SCOPE_MISMATCH",
            (await a.EvaluateReadOnlyAsync(Actor(), "../tenantB", Sha)).Code);
        Assert.Equal("ISOLATED_RESOURCE_SCOPE_MISMATCH",
            (await a.EvaluateReadOnlyAsync(Actor(), null, Sha)).Code);
    }

    [Fact]
    public async Task StaleOrMissingSourceShaNeverBecomesPass()
    {
        var a = Adapter();
        Assert.Equal("ISOLATED_SOURCE_SHA_MISSING_OR_STALE",
            (await a.EvaluateReadOnlyAsync(Actor(), "resource_41", OtherSha)).Code);
        Assert.Equal("ISOLATED_SOURCE_SHA_MISSING_OR_STALE",
            (await a.EvaluateReadOnlyAsync(Actor(), "resource_41", null)).Code);
        var snapshot = Snapshot();
        var stale = Adapter(snapshot with
        {
            Build = snapshot.Build with { MeasuredCommitSha = OtherSha }
        });
        Assert.Equal("ISOLATED_SOURCE_SHA_MISSING_OR_STALE",
            (await stale.EvaluateReadOnlyAsync(Actor(), "resource_41", Sha)).Code);
    }

    [Fact]
    public async Task RevokedPermissionAndStaleResourceRevisionAreRejected()
    {
        var snapshot = Snapshot();
        var revoked = Adapter(snapshot with
        {
            Request = snapshot.Request with { CurrentMembershipVerified = false }
        });
        Assert.Equal("BACKEND_AUTHORITY_REQUIRED",
            (await revoked.EvaluateReadOnlyAsync(Actor(), "resource_41", Sha)).Code);
        var stale = Adapter(snapshot with
        {
            Request = snapshot.Request with { ExpectedResourceRevision = "revision-4" }
        });
        Assert.Equal("STALE_RESPONSE_REVISION_DENIED",
            (await stale.EvaluateReadOnlyAsync(Actor(), "resource_41", Sha)).Code);
    }

    [Fact]
    public async Task BundleErrorIsNotOverriddenBySnapshotAdapter()
    {
        var snapshot = Snapshot();
        var oversized = Adapter(snapshot with
        {
            Build = snapshot.Build with { InitialBundleBytes = 900 * 1024 }
        });
        Assert.Equal("BUNDLE_BUDGET_EXCEEDED",
            (await oversized.EvaluateReadOnlyAsync(Actor(), "resource_41", Sha)).Code);
    }

    [Fact]
    public async Task CancelledTokenPropagatesBeforeEvaluation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Adapter().EvaluateReadOnlyAsync(Actor(), "resource_41", Sha, cts.Token));
    }

    [Fact]
    public void InvalidConstructorParametersAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5UiLightnessIsolatedSnapshotAdapter(0, "resource_41", Sha, Snapshot()));
        Assert.Throws<ArgumentException>(() =>
            new V5UiLightnessIsolatedSnapshotAdapter(11, "../B", Sha, Snapshot()));
        Assert.Throws<ArgumentException>(() =>
            new V5UiLightnessIsolatedSnapshotAdapter(11, "resource_41", "invalid", Snapshot()));
        Assert.Throws<ArgumentNullException>(() =>
            new V5UiLightnessIsolatedSnapshotAdapter(11, "resource_41", Sha, null!));
    }
}
