using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;
namespace Solqaryn.Tests.Application;
public sealed class C0501DesignTokenApplicationServiceTests
{
    private const string GuidKey = "2c85f621-314a-4f81-88c4-8054e61f812d";
    private static C0501DesignTokenDomainRules.TrustedContext Context() =>
        new(11, 101, true, true, true, false, 7, 9);
    private static C0501DesignTokenApplicationService.ActorSnapshot Actor() =>
        new(Context(), true, true, true, true);
    private static C0501DesignTokenApplicationService.CatalogSnapshot Catalog() =>
        new(101, "primary-color", 7,
            new HashSet<string>(StringComparer.Ordinal) { "primary-color" }, true, true);
    private static C0501DesignTokenApplicationService.ClientIntent Request() =>
        new(101, 9, 7, "primary-color", "COLOR", "#123AbC", GuidKey);

    private sealed class Actors(C0501DesignTokenApplicationService.ActorSnapshot? actor, bool fail = false)
        : IC0501DesignTokenActorReader
    {
        public int Calls { get; private set; }
        public ValueTask<C0501DesignTokenApplicationService.ActorSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (fail) throw new InvalidOperationException("actor offline");
            return ValueTask.FromResult(actor);
        }
    }
    private sealed class Catalogs(C0501DesignTokenApplicationService.CatalogSnapshot? catalog, bool fail = false)
        : IC0501DesignTokenCatalogReader
    {
        public int Calls { get; private set; }
        public int LastTenant { get; private set; }
        public ValueTask<C0501DesignTokenApplicationService.CatalogSnapshot?> ReadAsync(
            int tenant, string exactKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            LastTenant = tenant;
            if (fail) throw new InvalidOperationException("catalog offline");
            return ValueTask.FromResult(catalog);
        }
    }
    private static C0501DesignTokenApplicationService Service(Actors? actor = null, Catalogs? catalog = null) =>
        new(actor ?? new Actors(Actor()), catalog ?? new Catalogs(Catalog()));

    [Fact] public async Task AcceptedIntentStillGrantsNothing()
    {
        var result = await Service().PrepareAsync(Request());
        Assert.Equal("AUTHENTICATED_THEME_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedAdapter);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.AuthorizesThemeMutation);
        Assert.False(result.PersistsAnything);
        Assert.False(result.CertifiesAccessibility);
    }
    [Fact] public async Task InvalidIntentAndPlatformOverrideNeverRead()
    {
        var actors = new Actors(Actor());
        var svc = Service(actors);
        Assert.Equal("VALID_CLIENT_INTENT_REQUIRED", (await svc.PrepareAsync(null)).Code);
        Assert.Equal("VALID_CLIENT_INTENT_REQUIRED",
            (await svc.PrepareAsync(Request() with { TokenKey = "" })).Code);
        Assert.Equal("UNTRUSTED_MUTATION_DENIED",
            (await svc.PrepareAsync(Request() with { DirectWrite = true })).Code);
        Assert.Equal("UNTRUSTED_MUTATION_DENIED",
            (await svc.PrepareAsync(Request() with { PlatformCatalogMutation = true })).Code);
        Assert.Equal(0, actors.Calls);
    }
    [Fact] public async Task ClientTenantOverrideDoesNotReachCatalog()
    {
        var catalog = new Catalogs(Catalog());
        Assert.Equal("CLIENT_TENANT_MISMATCH",
            (await Service(catalog: catalog).PrepareAsync(Request() with { ClaimedTenantId = 202 })).Code);
        Assert.Equal(0, catalog.Calls);
        Assert.True((await Service(catalog: catalog).PrepareAsync(Request())).NeedsAuthenticatedAdapter);
        Assert.Equal(101, catalog.LastTenant);
    }
    [Fact] public async Task JwtMembershipRbacAndRevocationRequired()
    {
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Service(new Actors(null)).PrepareAsync(Request())).Code);
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Service(new Actors(Actor() with { JwtFresh = false })).PrepareAsync(Request())).Code);
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Service(new Actors(Actor() with { MembershipFresh = false })).PrepareAsync(Request())).Code);
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Service(new Actors(Actor() with { RbacFresh = false })).PrepareAsync(Request())).Code);
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Service(new Actors(Actor() with { Context = Context() with { RoleRevoked = true } }))
                .PrepareAsync(Request())).Code);
    }
    [Fact] public async Task ForeignCatalogAndUntrustedOwnershipDenied()
    {
        Assert.Equal("TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await Service(catalog: new Catalogs(Catalog() with { TenantId = 202 }))
                .PrepareAsync(Request())).Code);
        Assert.Equal("TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await Service(catalog: new Catalogs(Catalog() with { OwnerVerified = false }))
                .PrepareAsync(Request())).Code);
        Assert.Equal("TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await Service(catalog: new Catalogs(Catalog() with { TenantScopedBackendRead = false }))
                .PrepareAsync(Request())).Code);
    }
    [Fact] public async Task NonOrdinalCatalogComparerCannotBypassTokenKey()
    {
        var keyset = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PRIMARY-COLOR" };
        Assert.Equal("TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await Service(catalog: new Catalogs(Catalog() with { ExactKey = "PRIMARY-COLOR" }))
                .PrepareAsync(Request())).Code);
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN",
            (await Service(catalog: new Catalogs(Catalog() with { ApprovedKeys = keyset }))
                .PrepareAsync(Request())).Code);
    }
    [Fact] public async Task RevisionAndSessionMustMatch()
    {
        Assert.Equal("STALE_CATALOG_SNAPSHOT_RETRY_REQUIRED",
            (await Service(catalog: new Catalogs(Catalog() with { StoredRevision = 8 }))
                .PrepareAsync(Request())).Code);
        Assert.Equal("THEME_REVISION_CONFLICT",
            (await Service().PrepareAsync(Request() with { ExpectedRevision = 8 })).Code);
        Assert.Equal("STALE_OR_INVALID_SESSION_EPOCH",
            (await Service().PrepareAsync(Request() with { SessionEpoch = 8 })).Code);
    }
    [Fact] public async Task UnsafeCssAndInvalidIdempotencyDenied()
    {
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE",
            (await Service().PrepareAsync(Request() with { TokenValue = "url(javascript:alert(1))" })).Code);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE",
            (await Service().PrepareAsync(Request() with { TokenValue = "#FFF" })).Code);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED",
            (await Service().PrepareAsync(Request() with { IdempotencyKey = "not-a-guid" })).Code);
    }
    [Fact] public async Task ReaderFailuresAreSafeAndDoNotGrant()
    {
        Assert.Equal("BACKEND_AUTHORITY_UNAVAILABLE",
            (await Service(new Actors(Actor(), true)).PrepareAsync(Request())).Code);
        Assert.Equal("TRUSTED_CATALOG_UNAVAILABLE",
            (await Service(catalog: new Catalogs(Catalog(), true)).PrepareAsync(Request())).Code);
    }
    [Fact] public async Task CancellationIsNotSuppressed()
    {
        var a = new Actors(Actor());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Service(a).PrepareAsync(Request(), cts.Token));
        Assert.Equal(0, a.Calls);
    }
}
