using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;
using Fence = Solqaryn.Application.AutomationV5.C0804SectorModuleAuthorizationFence;
using Boundary = Solqaryn.Application.AutomationV5.C0804SectorModuleReadOnlyIntegrationBoundary;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0804SectorModuleReadOnlyIntegrationBoundaryTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Tenant = "tenant-A";
    private const string ModuleKey = "sector.orders";

    private static Rules.VerifiedActorAndPolicy Policy() => new()
    {
        ActorId = "actor-7", VerifiedTenantId = Tenant, EffectiveRoleId = "role-9",
        AuthenticatedJwtVerifiedByBackend = true, CurrentMembershipVerified = true,
        CurrentExplicitRelationalRbacGrant = true, TrustedCapabilityCatalogLoaded = true,
        CurrentPlanAllowsCapability = true, CurrentRubroAllowsCapability = true,
        CurrentTenantAllowsCapability = true, TrustedPolicyRevisionVerified = true,
        TrustedResourceOwnershipVerified = true, ResourceOwnerTenantId = Tenant,
        CurrentCapabilityKey = ModuleKey, CurrentModuleRevision = 7
    };

    private static Fence.Intent Intent(Rules.Operation op = Rules.Operation.Inspect) =>
        new(ModuleKey, Tenant, Sha, op, 7, "request-001");

    private sealed class Actors(Fence.ActorEvidence? response) : IC0804CurrentActorSource
    {
        public int Calls { get; private set; }
        public ValueTask<Fence.ActorEvidence?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(response);
        }
    }

    private sealed class Modules(Fence.ModuleEvidence? response) : IC0804TenantModuleSource
    {
        public int Calls { get; private set; }
        public ValueTask<Fence.ModuleEvidence?> ReadAsync(string tenant, string module, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(response);
        }
    }

    private sealed class Readiness(Boundary.ProviderSnapshot? response, bool fail = false)
        : IC0804SectorModuleReadinessSource
    {
        public int Calls { get; private set; }
        public string? TenantRead { get; private set; }
        public string? ModuleRead { get; private set; }
        public ValueTask<Boundary.ProviderSnapshot?> ReadAsync(
            string tenant, string module, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            TenantRead = tenant;
            ModuleRead = module;
            if (fail) throw new InvalidOperationException("SECRET_PROVIDER_CREDENTIALS");
            return ValueTask.FromResult(response);
        }
    }

    private static Fence.ActorEvidence Actor() => new(Tenant, true, true, true, true, Policy());
    private static Fence.ModuleEvidence Module() =>
        new(ModuleKey, Tenant, Sha, Rules.State.Disabled, 7, true, true);
    private static Boundary.ProviderSnapshot Snapshot() =>
        new(Tenant, ModuleKey, Sha, 7, true, true);
    private static (Boundary boundary, Actors actors, Modules modules, Readiness readiness)
        Create(Fence.ActorEvidence? actor = null, Fence.ModuleEvidence? module = null,
            Boundary.ProviderSnapshot? snapshot = null, bool fail = false)
    {
        var a = new Actors(actor ?? Actor());
        var m = new Modules(module ?? Module());
        var r = new Readiness(snapshot ?? Snapshot(), fail);
        return (new Boundary(new Fence(a, m), r), a, m, r);
    }

    [Fact] public async Task TrustedReadOnlyProbeReturnsOpaqueNonAuthorizingReadiness()
    {
        var (boundary, actors, modules, readiness) = Create();
        var result = await boundary.ProbeAsync(Intent());
        Assert.Equal("READ_ONLY_PROVIDER_READY_NON_AUTHORIZING", result.Code);
        Assert.Equal(1, actors.Calls);
        Assert.Equal(1, modules.Calls);
        Assert.Equal(1, readiness.Calls);
        Assert.Equal(Tenant, readiness.TenantRead);
        Assert.Equal(ModuleKey, readiness.ModuleRead);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.EnablesModule);
        Assert.False(result.EnqueuesWorker);
        Assert.False(result.CallsMutatingProvider);
        Assert.False(result.WritesDatabase);
        Assert.False(result.CertifiesRuntimeProvider);
        Assert.False(result.AuthorizesRelease);
    }

    [Fact] public async Task MutationOrForgedDispatchNeverCallsAnySource()
    {
        var (boundary, actors, modules, readiness) = Create();
        Assert.Equal("READ_ONLY_INSPECTION_REQUIRED",
            (await boundary.ProbeAsync(Intent(Rules.Operation.Activate))).Code);
        Assert.Equal("UNTRUSTED_DISPATCH_OR_PRIVILEGE_DENIED",
            (await boundary.ProbeAsync(Intent(), clientRequestsDispatch: true)).Code);
        Assert.Equal("UNTRUSTED_DISPATCH_OR_PRIVILEGE_DENIED",
            (await boundary.ProbeAsync(Intent() with { ClientClaimsPermissions = true })).Code);
        Assert.Equal(0, actors.Calls);
        Assert.Equal(0, modules.Calls);
        Assert.Equal(0, readiness.Calls);
    }

    [Fact] public async Task TenantBForgeryAndRevocationNeverReachProvider()
    {
        var (boundary, _, _, readiness) = Create();
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED",
            (await boundary.ProbeAsync(Intent() with { TenantAssertion = "tenant-B" })).Code);
        Assert.Equal(0, readiness.Calls);
        var revoked = Create(actor: Actor() with { Policy = Policy() with { UserOrTenantOrRoleRevoked = true } });
        Assert.Equal("CURRENT_BACKEND_IDENTITY_AND_RBAC_REQUIRED",
            (await revoked.boundary.ProbeAsync(Intent())).Code);
        Assert.Equal(0, revoked.readiness.Calls);
    }

    [Fact] public async Task UnverifiedTenantSnapshotAndStaleRevisionFailClosed()
    {
        var fake = Create(module: Module() with { PersistedTenantReadVerified = false });
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await fake.boundary.ProbeAsync(Intent())).Code);
        Assert.Equal(0, fake.readiness.Calls);
        var stale = Create(module: Module() with { Revision = 9 });
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await stale.boundary.ProbeAsync(Intent())).Code);
        Assert.Equal(0, stale.readiness.Calls);
    }

    [Fact] public async Task CrossTenantAndNonReadOnlyProviderResponsesDenied()
    {
        var foreign = Create(snapshot: Snapshot() with { TenantId = "tenant-B" });
        Assert.Equal("UNTRUSTED_PROVIDER_SNAPSHOT_DENIED",
            (await foreign.boundary.ProbeAsync(Intent())).Code);
        var unsafeTransport = Create(snapshot: Snapshot() with { UsesReadOnlyTransport = false });
        Assert.Equal("UNTRUSTED_PROVIDER_SNAPSHOT_DENIED",
            (await unsafeTransport.boundary.ProbeAsync(Intent())).Code);
        var oldSha = Create(snapshot: Snapshot() with { SourceSha = new string('a', 40) });
        Assert.Equal("UNTRUSTED_PROVIDER_SNAPSHOT_DENIED",
            (await oldSha.boundary.ProbeAsync(Intent())).Code);
    }

    [Fact] public async Task UnavailableAndNotReadyProviderReturnOnlyOpaqueCodes()
    {
        var failure = Create(fail: true);
        Assert.Equal("READ_ONLY_PROVIDER_UNAVAILABLE",
            (await failure.boundary.ProbeAsync(Intent())).Code);
        var notReady = Create(snapshot: Snapshot() with { ProviderReportsReady = false });
        var result = await notReady.boundary.ProbeAsync(Intent());
        Assert.Equal("READ_ONLY_PROVIDER_NOT_READY", result.Code);
        Assert.False(result.CertifiesRuntimeProvider);
    }

    [Fact] public async Task CancellationPropagatesWithoutAnyExternalRead()
    {
        var (boundary, actors, modules, readiness) = Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await boundary.ProbeAsync(Intent(), cancellationToken: cancellation.Token));
        Assert.Equal(0, actors.Calls);
        Assert.Equal(0, modules.Calls);
        Assert.Equal(0, readiness.Calls);
    }

    [Fact] public async Task InvalidIntentOrConstructorCannotStartProbe()
    {
        var (boundary, actors, _, readiness) = Create();
        Assert.Equal("READ_ONLY_INSPECTION_REQUIRED", (await boundary.ProbeAsync(null)).Code);
        Assert.Equal("VALID_MODULE_INTENT_REQUIRED",
            (await boundary.ProbeAsync(Intent() with { ExpectedSourceSha = "bad" })).Code);
        Assert.Equal(0, readiness.Calls);
        Assert.Equal(0, actors.Calls);
        Assert.Throws<ArgumentNullException>(() => new Boundary(null!, readiness));
    }
}
