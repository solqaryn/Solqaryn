using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;
using Xunit;

namespace Solqaryn.Tests.Application;
public sealed class C0804SectorModuleAuthorizationFenceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static Rules.VerifiedActorAndPolicy Policy() => new()
    {
        ActorId = "actor-7", VerifiedTenantId = "tenant-A", EffectiveRoleId = "role-9",
        AuthenticatedJwtVerifiedByBackend = true, CurrentMembershipVerified = true,
        CurrentExplicitRelationalRbacGrant = true, TrustedCapabilityCatalogLoaded = true,
        CurrentPlanAllowsCapability = true, CurrentRubroAllowsCapability = true,
        CurrentTenantAllowsCapability = true, TrustedPolicyRevisionVerified = true,
        TrustedResourceOwnershipVerified = true, ResourceOwnerTenantId = "tenant-A",
        CurrentCapabilityKey = "sector.orders", CurrentModuleRevision = 7
    };
    private static C0804SectorModuleAuthorizationFence.ActorEvidence Actor() =>
        new("tenant-A", true, true, true, true, Policy());
    private static C0804SectorModuleAuthorizationFence.ModuleEvidence Module() =>
        new("sector.orders", "tenant-A", Sha, Rules.State.Disabled, 7, true, true);
    private static C0804SectorModuleAuthorizationFence.Intent Intent() =>
        new("sector.orders", "tenant-A", Sha, Rules.Operation.Activate, 7, "request-001");

    private sealed class Actors(C0804SectorModuleAuthorizationFence.ActorEvidence? response, bool fail = false)
        : IC0804CurrentActorSource
    {
        public int Calls { get; private set; }
        public ValueTask<C0804SectorModuleAuthorizationFence.ActorEvidence?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (fail) throw new InvalidOperationException("internal identity error");
            return ValueTask.FromResult(response);
        }
    }
    private sealed class Modules(C0804SectorModuleAuthorizationFence.ModuleEvidence? response, bool fail = false)
        : IC0804TenantModuleSource
    {
        public int Calls { get; private set; }
        public string? SelectedTenant { get; private set; }
        public ValueTask<C0804SectorModuleAuthorizationFence.ModuleEvidence?> ReadAsync(
            string tenant, string exactKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            SelectedTenant = tenant;
            if (fail) throw new InvalidOperationException("internal persistence error");
            return ValueTask.FromResult(response);
        }
    }
    private static C0804SectorModuleAuthorizationFence Gate(
        Actors? a = null, Modules? m = null) =>
        new(a ?? new Actors(Actor()), m ?? new Modules(Module()));

    [Fact] public async Task PositivePreflightCannotGrantOrActivate()
    {
        var result = await Gate().InspectAsync(Intent());
        Assert.Equal("AUTHENTICATED_MODULE_CAS_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsFutureAuthenticatedCasAdapter);
        Assert.False(result.GrantsAccess);
        Assert.False(result.ActivatesModule);
        Assert.False(result.WritesToDatabase);
        Assert.False(result.AuthorizesRelease);
    }
    [Fact] public async Task MalformedClientIntentAndGlobalOverrideAvoidSources()
    {
        var a = new Actors(Actor());
        var svc = Gate(a);
        Assert.Equal("VALID_MODULE_INTENT_REQUIRED", (await svc.InspectAsync(null)).Code);
        Assert.Equal("VALID_MODULE_INTENT_REQUIRED",
            (await svc.InspectAsync(Intent() with { ExpectedSourceSha = "bad" })).Code);
        Assert.Equal("UNTRUSTED_CLIENT_PRIVILEGE_DENIED",
            (await svc.InspectAsync(Intent() with { ClientClaimsPermissions = true })).Code);
        Assert.Equal("UNTRUSTED_CLIENT_PRIVILEGE_DENIED",
            (await svc.InspectAsync(Intent() with { ClientRequestsGlobalMutation = true })).Code);
        Assert.Equal(0, a.Calls);
    }
    [Fact] public async Task ForeignClientTenantCannotSelectBackendResource()
    {
        var m = new Modules(Module());
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED",
            (await Gate(m: m).InspectAsync(Intent() with { TenantAssertion = "tenant-B" })).Code);
        Assert.Equal(0, m.Calls);
        Assert.True((await Gate(m: m).InspectAsync(Intent())).NeedsFutureAuthenticatedCasAdapter);
        Assert.Equal("tenant-A", m.SelectedTenant);
    }
    [Fact] public async Task UnverifiedOrRevokedPrincipalDeniesWithoutResourceRead()
    {
        var m = new Modules(Module());
        Assert.Equal("CURRENT_BACKEND_IDENTITY_AND_RBAC_REQUIRED",
            (await Gate(new Actors(null), m).InspectAsync(Intent())).Code);
        Assert.Equal("CURRENT_BACKEND_IDENTITY_AND_RBAC_REQUIRED",
            (await Gate(new Actors(Actor() with { CurrentJwtVerified = false }), m)
                .InspectAsync(Intent())).Code);
        Assert.Equal("CURRENT_BACKEND_IDENTITY_AND_RBAC_REQUIRED",
            (await Gate(new Actors(Actor() with { RbacGrantCurrent = false }), m)
                .InspectAsync(Intent())).Code);
        Assert.Equal("CURRENT_BACKEND_IDENTITY_AND_RBAC_REQUIRED",
            (await Gate(new Actors(Actor() with { Policy = Policy() with { UserOrTenantOrRoleRevoked = true } }), m)
                .InspectAsync(Intent())).Code);
        Assert.Equal(0, m.Calls);
    }
    [Fact] public async Task ForeignOrLegacyModuleIsNeverTrusted()
    {
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await Gate(m: new Modules(Module() with { TenantOwner = "tenant-B" }))
                .InspectAsync(Intent())).Code);
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await Gate(m: new Modules(Module() with { TenantOwner = null }))
                .InspectAsync(Intent())).Code);
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await Gate(m: new Modules(Module() with { PersistedTenantReadVerified = false }))
                .InspectAsync(Intent())).Code);
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await Gate(m: new Modules(Module() with { AuditFenceAvailable = false }))
                .InspectAsync(Intent())).Code);
    }
    [Fact] public async Task SourceRevisionAndKeyUseExactComparison()
    {
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await Gate(m: new Modules(Module() with { SourceSha = new string('a', 40) }))
                .InspectAsync(Intent())).Code);
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await Gate(m: new Modules(Module() with { ModuleKey = "Sector.Inventory" }))
                .InspectAsync(Intent())).Code);
        Assert.Equal("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED",
            (await Gate(m: new Modules(Module() with { Revision = 8 }))
                .InspectAsync(Intent())).Code);
    }
    [Fact] public async Task DomainRejectsStaleVersionAndImpossibleState()
    {
        Assert.Equal("STALE_MODULE_VERSION",
            (await Gate().InspectAsync(Intent() with { ExpectedRevision = 6 })).Code);
        Assert.Equal("ILLEGAL_MODULE_LIFECYCLE_TRANSITION",
            (await Gate(m: new Modules(Module() with { State = Rules.State.Enabled }))
                .InspectAsync(Intent())).Code);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED",
            (await Gate().InspectAsync(Intent() with { IdempotencyKey = "" })).Code);
    }
    [Fact] public async Task CatalogEntitlementAndDenyListMustRemainCurrent()
    {
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            (await Gate(new Actors(Actor() with
            { Policy = Policy() with { DenyListBlocksCapability = true } }))
            .InspectAsync(Intent())).Code);
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            (await Gate(new Actors(Actor() with
            { Policy = Policy() with { CurrentPlanAllowsCapability = false } }))
            .InspectAsync(Intent())).Code);
    }
    [Fact] public async Task ReaderFailuresReturnSafeErrors()
    {
        Assert.Equal("CURRENT_ACTOR_SOURCE_UNAVAILABLE",
            (await Gate(new Actors(Actor(), true)).InspectAsync(Intent())).Code);
        Assert.Equal("TENANT_MODULE_SOURCE_UNAVAILABLE",
            (await Gate(m: new Modules(Module(), true)).InspectAsync(Intent())).Code);
    }
    [Fact] public async Task CancellationPropagatesBeforeExternalReads()
    {
        var a = new Actors(Actor());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Gate(a).InspectAsync(Intent(), cts.Token));
        Assert.Equal(0, a.Calls);
    }
}
