using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0102IsolatedCapabilityPolicySnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Key = "orders.read";

    private static Rules.VerifiedPolicy Policy(IReadOnlySet<string>? keys = null) =>
        new(11, 11, 7, 19, 3, 3, "pro", "retail",
            true, true, true, false, true, true, true,
            keys ?? new HashSet<string>(StringComparer.Ordinal) { Key },
            new HashSet<string>(StringComparer.Ordinal) { Key },
            new HashSet<string>(StringComparer.Ordinal) { Key },
            new HashSet<string>(StringComparer.Ordinal) { Key },
            new HashSet<string>(StringComparer.Ordinal));

    private static Fence.VerifiedResourcePolicy Evidence(
        Rules.VerifiedPolicy? policy = null) =>
        new(101, 11, 7, true, true, policy ?? Policy());

    private static C0102IsolatedCapabilityPolicySnapshotAdapter Adapter(
        Fence.VerifiedResourcePolicy? snapshot = null) =>
        new(11, 101, 3, Sha, snapshot ?? Evidence());

    [Fact]
    public async Task NominalSnapshotCannotClaimLivePermission()
    {
        var a = Adapter();
        var v = await a.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(v);
        Assert.Equal(11, v.OwnerTenantId);
        Assert.False(v.OwnershipReadFromTrustedPersistence);
        Assert.False(v.CurrentCapabilityCatalogReadFromBackend);
        Assert.False(v.Policy!.ExplicitRelationalRbacGrant);
        Assert.False(v.Policy.PlanCurrent);
        Assert.False(a.ReadsLiveMysql);
        Assert.False(a.ReadsLiveJwtOrRbac);
        Assert.False(a.AuthorizesResourceAccess);
        Assert.False(a.PerformsDurableCas);
        Assert.False(a.WritesOrMigrates);
    }

    [Fact]
    public async Task TenantAAndTenantBDifferenceIsNeverSelectable()
    {
        Assert.Null(await Adapter().ReadCurrentAsync(22, 101, default));
        Assert.Null(await Adapter(Evidence() with { OwnerTenantId = 22 })
            .ReadCurrentAsync(11, 101, default));
        Assert.Null(await Adapter(Evidence(Policy() with { TenantEmpresaId = 22 }))
            .ReadCurrentAsync(11, 101, default));
    }

    [Fact]
    public async Task ResourceAndPolicyOwnerMustMatch()
    {
        Assert.Null(await Adapter().ReadCurrentAsync(11, 102, default));
        Assert.Null(await Adapter(Evidence() with { ResourceId = 102 })
            .ReadCurrentAsync(11, 101, default));
        Assert.Null(await Adapter(Evidence(Policy() with { ResourceEmpresaId = 22 }))
            .ReadCurrentAsync(11, 101, default));
        Assert.Null(await Adapter(Evidence() with { VerifiedActorId = 0 })
            .ReadCurrentAsync(11, 101, default));
    }

    [Fact]
    public async Task CaseInsensitiveSourceDoesNotExpandOrdinalCapability()
    {
        var source = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Key };
        var a = Adapter(Evidence(Policy(source)));
        var v = await a.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(v);
        Assert.Contains(Key, v.Policy!.PlanCapabilities!);
        Assert.DoesNotContain("Orders.Read", v.Policy.PlanCapabilities!);
    }

    [Fact]
    public async Task SourceSetMutationAfterCaptureIsNotObserved()
    {
        var source = new HashSet<string>(StringComparer.Ordinal) { Key };
        var a = Adapter(Evidence(Policy(source)));
        source.Add("security.override");
        source.Clear();
        var v = await a.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(v);
        Assert.Contains(Key, v.Policy!.PlanCapabilities!);
        Assert.DoesNotContain("security.override", v.Policy.PlanCapabilities!);
    }

    [Fact]
    public async Task NullPolicyAndNullCapabilityCollectionsStayNonAuthoritative()
    {
        var a = Adapter(Evidence() with { Policy = null });
        Assert.Null(await a.ReadCurrentAsync(11, 101, default));
        var b = Adapter(Evidence(Policy() with { PlanCapabilities = null }));
        var v = await b.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(v);
        Assert.Null(v.Policy!.PlanCapabilities);
        Assert.False(v.Policy.ExplicitRelationalRbacGrant);
    }

    [Fact]
    public void InvalidCaptureIsRejectedBeforeQuery()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0102IsolatedCapabilityPolicySnapshotAdapter(0, 101, 3, Sha, Evidence()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0102IsolatedCapabilityPolicySnapshotAdapter(11, 0, 3, Sha, Evidence()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0102IsolatedCapabilityPolicySnapshotAdapter(11, 101, 0, Sha, Evidence()));
        Assert.Throws<ArgumentException>(() =>
            new C0102IsolatedCapabilityPolicySnapshotAdapter(11, 101, 3, "short", Evidence()));
        Assert.Throws<ArgumentNullException>(() =>
            new C0102IsolatedCapabilityPolicySnapshotAdapter(11, 101, 3, Sha, null!));
    }

    [Fact]
    public async Task CancellationPropagatesWithoutReturningAnyEvidence()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Adapter().ReadCurrentAsync(11, 101, cts.Token));
    }

    [Fact]
    public async Task SyntheticSnapshotIsAlwaysDowngradedEvenWithForgedPositiveFlags()
    {
        var forged = Evidence() with
        {
            OwnershipReadFromTrustedPersistence = true,
            CurrentCapabilityCatalogReadFromBackend = true,
            Policy = Policy() with { ExplicitRelationalRbacGrant = true, PlanCurrent = true }
        };
        var v = await Adapter(forged).ReadCurrentAsync(11, 101, default);
        Assert.NotNull(v);
        Assert.False(v.OwnershipReadFromTrustedPersistence);
        Assert.False(v.CurrentCapabilityCatalogReadFromBackend);
        Assert.False(v.Policy!.ExplicitRelationalRbacGrant);
        Assert.False(v.Policy.PlanCurrent);
    }
}
