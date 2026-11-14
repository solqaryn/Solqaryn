using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-02.M05.05: negative security and isolation regressions for the real,
/// side-effect-free V5 authorization guard. Trusted maps here are test fixtures,
/// not a live tenant session, JWT, or proof of persistence.
/// </summary>
public sealed class V5C0002M0505NegativeIsolationTests
{
    private const string Author = "6af16bb9492c8191b8e8c7d606f11507";
    private const string Reviewer = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string RetiredTask = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string ItemA = "C00-02.M05.05";
    private const string ItemB = "C00-03.M05.05";
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Author, Reviewer };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { ItemA, ItemB };
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Scopes =
        new Dictionary<string, IReadOnlySet<string>>
        {
            [Author] = new HashSet<string> { "scope-a" },
            [Reviewer] = new HashSet<string> { "scope-a", "scope-b" }
        };
    private static readonly IReadOnlyDictionary<string, string> ScopeTenants =
        new Dictionary<string, string>
        {
            ["scope-a"] = "tenant-a",
            ["scope-b"] = "tenant-b"
        };
    private static readonly IReadOnlyDictionary<string, string> ItemScopes =
        new Dictionary<string, string>
        {
            [ItemA] = "scope-a",
            [ItemB] = "scope-b"
        };

    private readonly V5WorkItemTenantAuthorizationGuard _guard = new();

    private V5WorkItemRules.Decision Authorize(
        V5WorkItemTenantAuthorizationGuard.Operation op,
        string actor = Author, string scope = "scope-a", string tenant = "tenant-a",
        string id = ItemA, string state = "PENDIENTE", string? owner = null)
        => _guard.Authorize(
            new(actor, scope, tenant, new V5WorkItemRules.Snapshot(id, state, owner), op),
            Actors, Scopes, ScopeTenants, Catalog, ItemScopes);

    [Fact]
    public void ForeignCatalogItemIsRejectedAcrossEveryOperationEvenWhenIdExists()
    {
        foreach (var operation in Enum.GetValues<V5WorkItemTenantAuthorizationGuard.Operation>())
        {
            var denied = Authorize(operation, id: ItemB);
            Assert.False(denied.Allowed);
            Assert.Equal("ITEM_SCOPE_MISMATCH", denied.Code);
        }
        Assert.True(Authorize(V5WorkItemTenantAuthorizationGuard.Operation.Read,
            actor: Reviewer, scope: "scope-b", tenant: "tenant-b", id: ItemB).Allowed);
    }

    [Theory]
    [InlineData("tenant-b")]
    [InlineData("Tenant-a")]
    [InlineData("tenant-a ")]
    [InlineData("")]
    public void TenantKeyFromAnotherContextOrVariantNeverMatches(string tenant)
    {
        foreach (var operation in Enum.GetValues<V5WorkItemTenantAuthorizationGuard.Operation>())
            Assert.Equal("TENANT_MISMATCH", Authorize(operation, tenant: tenant).Code);
    }

    [Fact]
    public void CallerCannotEscalateToAnotherScopeOrRetiredActor()
    {
        foreach (var operation in Enum.GetValues<V5WorkItemTenantAuthorizationGuard.Operation>())
        {
            Assert.Equal("SCOPE_DENIED", Authorize(operation, scope: "scope-b", tenant: "tenant-b", id: ItemB).Code);
            Assert.Equal("UNAUTHORIZED_ACTOR", Authorize(operation, actor: RetiredTask).Code);
        }
    }

    [Fact]
    public void MissingTrustedItemBindingRejectsValidCatalogAndActor()
    {
        var request = new V5WorkItemTenantAuthorizationGuard.AccessRequest(
            Author, "scope-a", "tenant-a",
            new V5WorkItemRules.Snapshot(ItemA, "PENDIENTE", null),
            V5WorkItemTenantAuthorizationGuard.Operation.Claim);
        var denied = _guard.Authorize(request, Actors, Scopes, ScopeTenants, Catalog,
            new Dictionary<string, string>());
        Assert.Equal("ITEM_SCOPE_MISMATCH", denied.Code);
    }

    [Fact]
    public void SameTaskCannotReviewItsOwnClosedWork()
    {
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            Authorize(V5WorkItemTenantAuthorizationGuard.Operation.ReviewA5,
                state: "LISTO", owner: Author).Code);
        Assert.True(Authorize(V5WorkItemTenantAuthorizationGuard.Operation.ReviewA5,
            actor: Reviewer, state: "LISTO", owner: Author).Allowed);
    }
}
