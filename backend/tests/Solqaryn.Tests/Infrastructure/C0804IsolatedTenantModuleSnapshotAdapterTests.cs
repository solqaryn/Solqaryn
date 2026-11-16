using Fence = Solqaryn.Application.AutomationV5.C0804SectorModuleAuthorizationFence;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0804IsolatedTenantModuleSnapshotAdapterTests
{
    private const string Tenant = "tenant-11";
    private const string Key = "sector.retail";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Fence.ModuleEvidence Snapshot() => new(Key, Tenant, Sha, default, 3, true, true);
    private static C0804IsolatedTenantModuleSnapshotAdapter Create(Fence.ModuleEvidence? v = null) =>
        new(Tenant, Key, 3, Sha, v ?? Snapshot());

    [Fact] public async Task NominalCannotGrantLiveAccess()
    {
        var a = Create();
        var x = await a.ReadAsync(Tenant, Key, default);
        Assert.NotNull(x);
        Assert.False(x.PersistedTenantReadVerified);
        Assert.False(x.AuditFenceAvailable);
        Assert.False(a.ReadsLiveMysql);
        Assert.False(a.ReadsJwtOrRbac);
        Assert.False(a.ActivatesModule);
        Assert.False(a.PerformsDurableCas);
        Assert.False(a.WritesOrMigrates);
    }

    [Fact] public async Task TenantBRejected()
    {
        Assert.Null(await Create().ReadAsync("tenant-22", Key, default));
        Assert.Null(await Create(Snapshot() with { TenantOwner = "tenant-22" }).ReadAsync(Tenant, Key, default));
    }

    [Fact] public async Task ModuleKeyIsOrdinal()
    {
        Assert.Null(await Create().ReadAsync(Tenant, "Sector.Retail", default));
        Assert.Null(await Create(Snapshot() with { ModuleKey = "Sector.Retail" }).ReadAsync(Tenant, Key, default));
    }

    [Fact] public async Task RevisionMismatchRejected()
    {
        Assert.Null(await Create(Snapshot() with { Revision = 2 }).ReadAsync(Tenant, Key, default));
    }

    [Fact] public async Task ShaMismatchRejected()
    {
        Assert.Null(await Create(Snapshot() with { SourceSha = new string('f',40) }).ReadAsync(Tenant, Key, default));
    }

    [Fact] public async Task ForgedAuthorityFlagsAreAlwaysDowngraded()
    {
        var x = await Create(Snapshot() with { PersistedTenantReadVerified = true, AuditFenceAvailable = true })
            .ReadAsync(Tenant, Key, default);
        Assert.NotNull(x);
        Assert.False(x.PersistedTenantReadVerified);
        Assert.False(x.AuditFenceAvailable);
    }

    [Fact] public void InvalidConstructorInputsRejected()
    {
        Assert.Throws<ArgumentException>(() => new C0804IsolatedTenantModuleSnapshotAdapter("Tenant-11", Key, 3, Sha, Snapshot()));
        Assert.Throws<ArgumentException>(() => new C0804IsolatedTenantModuleSnapshotAdapter(Tenant, "sector retail", 3, Sha, Snapshot()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C0804IsolatedTenantModuleSnapshotAdapter(Tenant, Key, 0, Sha, Snapshot()));
        Assert.Throws<ArgumentException>(() => new C0804IsolatedTenantModuleSnapshotAdapter(Tenant, Key, 3, "bad", Snapshot()));
        Assert.Throws<ArgumentNullException>(() => new C0804IsolatedTenantModuleSnapshotAdapter(Tenant, Key, 3, Sha, null!));
    }

    [Fact] public async Task CancellationPropagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Create().ReadAsync(Tenant, Key, cts.Token));
    }

    [Fact] public async Task SyntheticSnapshotNeverCertifiesDatabaseOrCas()
    {
        var a = Create();
        var x = await a.ReadAsync(Tenant, Key, default);
        Assert.NotNull(x);
        Assert.False(x.PersistedTenantReadVerified && x.AuditFenceAvailable);
        Assert.False(a.PerformsDurableCas);
    }
}
