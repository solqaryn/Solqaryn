using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;
using Fence = Solqaryn.Application.AutomationV5.C0804SectorModuleAuthorizationFence;
using Boundary = Solqaryn.Application.AutomationV5.C0804SectorModuleReadOnlyIntegrationBoundary;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

/// <summary>Real Application/Infrastructure integration, synthetic sources only; never live authorization.</summary>
public sealed class C0804SectorModuleRealAdapterContractIntegrationTests
{
    private const string Tenant = "tenant-a";
    private const string Module = "sector.retail";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Rules.VerifiedActorAndPolicy Policy() => new()
    {
        ActorId = "actor-a", VerifiedTenantId = Tenant, EffectiveRoleId = "role-viewer",
        AuthenticatedJwtVerifiedByBackend = true, CurrentMembershipVerified = true,
        CurrentExplicitRelationalRbacGrant = true, TrustedCapabilityCatalogLoaded = true,
        CurrentPlanAllowsCapability = true, CurrentRubroAllowsCapability = true,
        CurrentTenantAllowsCapability = true, TrustedPolicyRevisionVerified = true,
        TrustedResourceOwnershipVerified = true, ResourceOwnerTenantId = Tenant,
        CurrentCapabilityKey = Module, CurrentModuleRevision = 4
    };
    private static Fence.ActorEvidence Actor(Rules.VerifiedActorAndPolicy? p = null) =>
        new(Tenant, true, true, true, true, p ?? Policy());
    private static Fence.ModuleEvidence Captured() =>
        new(Module, Tenant, Sha, Rules.State.Disabled, 4, true, true);
    private static Fence.Intent Intent() =>
        new(Module, Tenant, Sha, Rules.Operation.Inspect, 4, "request-c0804-001");

    private sealed class CurrentActor(Fence.ActorEvidence? actor) : IC0804CurrentActorSource
    {
        public int Calls { get; private set; }
        public ValueTask<Fence.ActorEvidence?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Calls++; return ValueTask.FromResult(actor);
        }
    }
    private sealed class ReadyProvider : IC0804SectorModuleReadinessSource
    {
        public int Calls { get; private set; }
        public ValueTask<Boundary.ProviderSnapshot?> ReadAsync(string tenant, string module, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Calls++;
            return ValueTask.FromResult<Boundary.ProviderSnapshot?>(new(tenant, module, Sha, 4, true, true));
        }
    }
    private static (Boundary boundary, CurrentActor actor, ReadyProvider provider)
        Create(Fence.ActorEvidence? actor = null, Fence.ModuleEvidence? captured = null)
    {
        var a = new CurrentActor(actor ?? Actor());
        var real = new C0804IsolatedTenantModuleSnapshotAdapter(Tenant, Module, 4, Sha, captured ?? Captured());
        var p = new ReadyProvider();
        return (new Boundary(new Fence(a, real), p), a, p);
    }

    [Fact] public async Task RealAdapterCannotCertifyProviderEvenWithSyntheticTrustedActor()
    {
        var (b, a, p) = Create(); var r = await b.ProbeAsync(Intent());
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED", r.Code);
        Assert.Equal(1, a.Calls); Assert.Equal(0, p.Calls);
        Assert.False(r.CertifiesRuntimeProvider); Assert.False(r.GrantsTenantAccess);
        Assert.False(r.EnablesModule); Assert.False(r.WritesDatabase);
    }
    [Fact] public async Task TenantBForgeryNeverCallsProvider()
    {
        var (b, a, p) = Create();
        var r = await b.ProbeAsync(Intent() with { TenantAssertion = "tenant-b" });
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED", r.Code);
        Assert.Equal(1, a.Calls); Assert.Equal(0, p.Calls);
    }
    [Fact] public async Task ClientPrivilegeAndDispatchNeverReadActorOrProvider()
    {
        var (b, a, p) = Create();
        Assert.Equal("UNTRUSTED_DISPATCH_OR_PRIVILEGE_DENIED",
            (await b.ProbeAsync(Intent() with { ClientClaimsPermissions = true })).Code);
        Assert.Equal("UNTRUSTED_DISPATCH_OR_PRIVILEGE_DENIED",
            (await b.ProbeAsync(Intent(), clientRequestsDispatch: true)).Code);
        Assert.Equal(0, a.Calls); Assert.Equal(0, p.Calls);
    }
    [Fact] public async Task RevokedActorCannotContactProvider()
    {
        var (b, a, p) = Create(actor: Actor(Policy() with { UserOrTenantOrRoleRevoked = true }));
        Assert.Equal("CURRENT_BACKEND_IDENTITY_AND_RBAC_REQUIRED", (await b.ProbeAsync(Intent())).Code);
        Assert.Equal(1, a.Calls); Assert.Equal(0, p.Calls);
    }
    [Fact] public async Task StaleRevisionCannotBecomeTrustedThroughRealAdapter()
    {
        var (b, a, p) = Create(captured: Captured() with { Revision = 3 });
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED", (await b.ProbeAsync(Intent())).Code);
        Assert.Equal(1, a.Calls); Assert.Equal(0, p.Calls);
    }
    [Fact] public async Task PreCancellationMakesNoExternalCalls()
    {
        var (b, a, p) = Create(); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await b.ProbeAsync(Intent(), cancellationToken: cts.Token));
        Assert.Equal(0, a.Calls); Assert.Equal(0, p.Calls);
    }
}
