using Solqaryn.Infrastructure.AutomationV5;
using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0102ReadThroughCapabilityProviderTests
{
    private const string Capability = "orders.read";

    private static Rules.VerifiedPolicy Policy(IReadOnlySet<string>? plan = null) =>
        new(11, 11, 7, 19, 3, 3, "pro", "retail",
            true, true, true, false, true, true, true,
            plan ?? new HashSet<string>(StringComparer.Ordinal) { Capability },
            new HashSet<string>(StringComparer.Ordinal) { Capability },
            new HashSet<string>(StringComparer.Ordinal) { Capability },
            new HashSet<string>(StringComparer.Ordinal) { Capability },
            new HashSet<string>(StringComparer.Ordinal) { "security.override" });

    private static Fence.VerifiedResourcePolicy Evidence(
        Rules.VerifiedPolicy? p = null) =>
        new(101, 11, 7, true, true, p ?? Policy());

    private sealed class Source : IC0102ReadThroughCapabilityEvidenceSource
    {
        public Fence.VerifiedResourcePolicy? Current = Evidence();
        public Exception? Failure;
        public int Reads;
        public ValueTask<Fence.VerifiedResourcePolicy?> ReadUntrustedAsync(
            int tenantId, int resourceId, CancellationToken cancellationToken)
        {
            Reads++;
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Current);
        }
    }

    [Fact]
    public async Task RechecksEveryCallAndNeverCachesAnAllow()
    {
        var src = new Source();
        var provider = new C0102ReadThroughCapabilityProvider(src);
        var a = await provider.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(a);
        Assert.False(a.OwnershipReadFromTrustedPersistence);
        Assert.False(a.CurrentCapabilityCatalogReadFromBackend);
        Assert.False(a.Policy!.ExplicitRelationalRbacGrant);
        Assert.False(a.Policy.PlanCurrent);
        Assert.False(a.Policy.RubroCurrent);
        src.Current = Evidence(Policy() with { PlanCode = "basic" });
        var b = await provider.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(b);
        Assert.Equal("basic", b.Policy!.PlanCode);
        Assert.Equal(2, src.Reads);
        Assert.False(provider.ReadsLiveMysql);
        Assert.False(provider.ReadsLiveJwtOrRbac);
        Assert.False(provider.WritesOrMigrates);
        Assert.False(provider.AuthorizesResourceAccess);
        Assert.False(provider.CachesAuthorization);
    }

    [Fact]
    public async Task ForeignTenantsResourcesAndActorsFailClosed()
    {
        var src = new Source();
        var bridge = new C0102ReadThroughCapabilityProvider(src);
        Assert.Null(await bridge.ReadCurrentAsync(22, 101, default));
        Assert.Null(await bridge.ReadCurrentAsync(11, 102, default));
        foreach (var snapshot in new[] {
            Evidence() with { OwnerTenantId = 22 },
            Evidence() with { VerifiedActorId = 0 },
            Evidence(Policy() with { TenantEmpresaId = 22 }),
            Evidence(Policy() with { ResourceEmpresaId = 22 }),
            Evidence(Policy() with { VerifiedActorId = 999 }),
            Evidence(Policy() with { EffectiveRoleId = 0 })
        })
        {
            src.Current = snapshot;
            Assert.Null(await bridge.ReadCurrentAsync(11, 101, default));
        }
    }

    [Fact]
    public async Task RevocationAndInvalidBranchesAreDenied()
    {
        var src = new Source();
        var bridge = new C0102ReadThroughCapabilityProvider(src);
        foreach (var p in new[] {
            Policy() with { PermissionRevoked = true },
            Policy() with { TenantActive = false },
            Policy() with { MembershipActive = false },
            Policy() with { RoleActive = false },
            Policy() with { ResourceBranchId = 0 },
            Policy() with { AuthorizedBranchId = 12, ResourceBranchId = 13 }
        })
        {
            src.Current = Evidence(p);
            Assert.Null(await bridge.ReadCurrentAsync(11, 101, default));
        }
    }

    [Fact]
    public async Task ReturnedCollectionsAreFrozenEvenWhenSourceMutates()
    {
        var mutable = new HashSet<string>(StringComparer.Ordinal) { Capability };
        var src = new Source { Current = Evidence(Policy(mutable)) };
        var bridge = new C0102ReadThroughCapabilityProvider(src);
        var first = await bridge.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(first);
        Assert.Contains(Capability, first.Policy!.PlanCapabilities!);
        mutable.Clear();
        mutable.Add("security.override");
        var keys = first.Policy.PlanCapabilities!;
        Assert.False(keys is HashSet<string>);
        if (keys is ISet<string> set)
        {
            try { set.Add("security.override"); }
            catch (NotSupportedException) { }
        }
        Assert.Contains(Capability, keys);
        Assert.DoesNotContain("security.override", keys);
        Assert.False(first.Policy.ExplicitRelationalRbacGrant);
    }

    [Fact]
    public async Task NonOrdinalSourceCannotExpandCapabilityName()
    {
        var nonOrdinal = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Capability };
        var src = new Source { Current = Evidence(Policy(nonOrdinal)) };
        var v = await new C0102ReadThroughCapabilityProvider(src).ReadCurrentAsync(11, 101, default);
        Assert.NotNull(v);
        Assert.Contains(Capability, v.Policy!.PlanCapabilities!);
        Assert.False(v.Policy.PlanCapabilities!.Contains("Orders.Read"));
        Assert.Contains("security.override", v.Policy.DeniedCapabilities!);
    }

    [Fact]
    public async Task ProviderErrorsAreOpaqueAndFailClosed()
    {
        var src = new Source { Failure = new InvalidOperationException("private-provider-token") };
        Assert.Null(await new C0102ReadThroughCapabilityProvider(src).ReadCurrentAsync(11, 101, default));
        Assert.Equal(1, src.Reads);
        src.Failure = null;
        src.Current = null;
        Assert.Null(await new C0102ReadThroughCapabilityProvider(src).ReadCurrentAsync(11, 101, default));
    }

    [Fact]
    public async Task CancellationBeforeAndDuringProviderCallPropagates()
    {
        var src = new Source();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var bridge = new C0102ReadThroughCapabilityProvider(src);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await bridge.ReadCurrentAsync(11, 101, cancellation.Token));
        Assert.Equal(0, src.Reads);
        src.Failure = new OperationCanceledException();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await bridge.ReadCurrentAsync(11, 101, default));
    }

    [Fact]
    public async Task MissingSourceAndInvalidIdsNeverTriggerProviderReads()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new C0102ReadThroughCapabilityProvider(null!));
        var src = new Source();
        var bridge = new C0102ReadThroughCapabilityProvider(src);
        Assert.Null(await bridge.ReadCurrentAsync(0, 101, default));
        Assert.Null(await bridge.ReadCurrentAsync(11, 0, default));
        Assert.Equal(0, src.Reads);
    }

    [Fact]
    public async Task NullCollectionPreservedAsNonAuthoritative()
    {
        var src = new Source {
            Current = Evidence(Policy() with { RubroCapabilities = null })
        };
        var v = await new C0102ReadThroughCapabilityProvider(src).ReadCurrentAsync(11, 101, default);
        Assert.NotNull(v);
        Assert.Null(v.Policy!.RubroCapabilities);
        Assert.False(v.Policy.PlanCurrent);
    }
}
