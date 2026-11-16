using Gate = Solqaryn.Application.AutomationV5.C0103DynamicConfigurationAuthorizationGate;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0103IsolatedCatalogSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Key = "ui.compactMode";

    private static Gate.CatalogSnapshot Snapshot(IReadOnlySet<string>? keys = null) =>
        new(11, Sha, 3, true, true,
            new Rules.CatalogKey(Key, Rules.ValueKind.Boolean,
                Rules.ConfigScope.Tenant, false, 0, null, null),
            keys ?? new HashSet<string>(StringComparer.Ordinal) { Key });

    private static C0103IsolatedCatalogSnapshotAdapter Adapter(
        Gate.CatalogSnapshot? captured = null) =>
        new(11, Key, 3, Sha, captured ?? Snapshot());

    [Fact]
    public async Task NominalSnapshotIsReadOnlyAndDoesNotGrantAuthority()
    {
        var adapter = Adapter();
        var result = await adapter.ReadAsync(11, Key, default);
        Assert.NotNull(result);
        Assert.Equal(11, result.OwnerTenantId);
        Assert.True(result.ApprovedKeys!.Contains(Key));
        Assert.False(adapter.ReadsOrWritesMysql);
        Assert.False(adapter.ReadsLiveJwtOrRbac);
        Assert.False(adapter.PerformsDurableCas);
        Assert.False(adapter.GrantsTenantOrPlatformWrite);
        Assert.False(adapter.PerformsMigrationOrCutover);
    }

    [Fact]
    public async Task CrossTenantReadIsDenied()
    {
        var adapter = Adapter();
        Assert.Null(await adapter.ReadAsync(22, Key, default));
        Assert.Null(await Adapter(Snapshot() with { OwnerTenantId = 22 })
            .ReadAsync(11, Key, default));
    }

    [Fact]
    public async Task CaseInsensitiveSetCannotBroadenExactKey()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Key };
        var adapter = Adapter(Snapshot(keys));
        Assert.Null(await adapter.ReadAsync(11, "UI.compactMode", default));
        keys.Add("jwt.override");
        var frozen = await adapter.ReadAsync(11, Key, default);
        Assert.NotNull(frozen);
        Assert.DoesNotContain("jwt.override", frozen.ApprovedKeys!);
    }

    [Fact]
    public async Task CanonicalKeyAndAllowlistAreBothStrict()
    {
        Assert.Null(await Adapter(Snapshot() with {
            Definition = Snapshot().Definition! with { CanonicalKey = "UI.compactMode" }
        }).ReadAsync(11, Key, default));
        Assert.Null(await Adapter(Snapshot(new HashSet<string>(StringComparer.Ordinal)
        { "UI.compactMode" })).ReadAsync(11, Key, default));
        Assert.Null(await Adapter(Snapshot() with { ApprovedKeys = null })
            .ReadAsync(11, Key, default));
    }

    [Fact]
    public async Task StaleVersionAndShaFailClosed()
    {
        Assert.Null(await Adapter(Snapshot() with { Version = 2 })
            .ReadAsync(11, Key, default));
        Assert.Null(await Adapter(Snapshot() with { SourceSha = new string('f', 40) })
            .ReadAsync(11, Key, default));
    }

    [Fact]
    public async Task UnverifiedBackendReadAndOwnershipFailClosed()
    {
        Assert.Null(await Adapter(Snapshot() with { BackendReadVerified = false })
            .ReadAsync(11, Key, default));
        Assert.Null(await Adapter(Snapshot() with { PersistedOwnerVerified = false })
            .ReadAsync(11, Key, default));
        Assert.Null(await Adapter(Snapshot() with { Definition = null })
            .ReadAsync(11, Key, default));
    }

    [Fact]
    public async Task CancelledCallPropagatesRatherThanReturningApproval()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Adapter().ReadAsync(11, Key, cts.Token));
    }

    [Fact]
    public void InvalidCaptureIdentityIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0103IsolatedCatalogSnapshotAdapter(0, Key, 3, Sha, Snapshot()));
        Assert.Throws<ArgumentException>(() =>
            new C0103IsolatedCatalogSnapshotAdapter(11, " ", 3, Sha, Snapshot()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0103IsolatedCatalogSnapshotAdapter(11, Key, 0, Sha, Snapshot()));
        Assert.Throws<ArgumentException>(() =>
            new C0103IsolatedCatalogSnapshotAdapter(11, Key, 3, "short", Snapshot()));
        Assert.Throws<ArgumentNullException>(() =>
            new C0103IsolatedCatalogSnapshotAdapter(11, Key, 3, Sha, null!));
    }

    private sealed class SyntheticActor : IC0103TrustedConfigurationActorSource
    {
        public ValueTask<Gate.ActorSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var authority = new Rules.TrustedAuthority(
                11, true, true, true, true, false, false, false);
            return ValueTask.FromResult<Gate.ActorSnapshot?>(
                new(7, 11, Sha, true, true, true, true, false, authority));
        }
    }

    [Fact]
    public async Task ApplicationGateCannotTurnSyntheticSnapshotIntoDurableGrant()
    {
        var gate = new Gate(new SyntheticActor(), Adapter());
        var request = new V5DynamicConfigurationApplicationPreflight.ClientIntent(
            Key, Rules.ValueKind.Boolean, Rules.ConfigScope.Tenant,
            11, "true", 3, Sha);
        var decision = await gate.InspectAsync(request);
        Assert.Equal("AUTHENTICATED_CONFIGURATION_TRANSACTION_AND_A5_REQUIRED", decision.Code);
        Assert.False(decision.GrantsConfigurationWrite);
        Assert.False(decision.CertifiesDurableCas);
        Assert.False(decision.AllowsCrossTenantRead);
        Assert.False(decision.AuthorizesQaOrProduction);
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED",
            (await gate.InspectAsync(request with { TenantAssertion = 22 })).Code);
    }
}
