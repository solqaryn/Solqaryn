using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5WorkItemTenantAuthorizationGuardTests
{
    private const string Author = "6adc2e4bd8a48191ac7d8641ed95a1a3";
    private const string Reviewer = "6aeacb7820ac8191b7d768a6f33dfa09";
    private const string Foreign = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string Id = "C00-02.M03.03";
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Author, Reviewer };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { Id };
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ActorScopes =
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
    private readonly V5WorkItemTenantAuthorizationGuard _guard = new();

    private V5WorkItemRules.Decision Check(
        V5WorkItemTenantAuthorizationGuard.Operation operation,
        string actor = Author, string scope = "scope-a", string tenant = "tenant-a",
        string state = "PENDIENTE", string? owner = null, string id = Id)
        => _guard.Authorize(new(actor, scope, tenant,
            new V5WorkItemRules.Snapshot(id, state, owner), operation),
            Actors, ActorScopes, ScopeTenants, Catalog);

    [Fact]
    public void Read_UsesServerScopeAndTenantBinding()
    {
        Assert.True(Check(V5WorkItemTenantAuthorizationGuard.Operation.Read).Allowed);
        Assert.Equal("TENANT_MISMATCH",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.Read, tenant: "tenant-b").Code);
        Assert.Equal("SCOPE_DENIED",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.Read, scope: "scope-b", tenant: "tenant-b").Code);
        Assert.Equal("TENANT_MISMATCH",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.Read, tenant: "").Code);
    }

    [Fact]
    public void NoExternalExecutorOrUnlistedWorkItemCanPass()
    {
        Assert.Equal("UNAUTHORIZED_ACTOR",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.Claim, actor: Foreign).Code);
        Assert.Equal("INVALID_ID",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.Claim, id: "C99-99.M99.99").Code);
    }

    [Fact]
    public void ClaimIsFailClosedForAlreadyOwnedOrAdvancedItems()
    {
        Assert.True(Check(V5WorkItemTenantAuthorizationGuard.Operation.Claim).Allowed);
        Assert.Equal("CLAIM_CONFLICT",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.Claim, owner: Reviewer).Code);
        Assert.Equal("CLAIM_CONFLICT",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.Claim, state: "VALIDANDO").Code);
    }

    [Fact]
    public void OwnerOnlyCanValidateOrClose()
    {
        Assert.True(Check(V5WorkItemTenantAuthorizationGuard.Operation.Validate,
            state: "EN_PROGRESO", owner: Author).Allowed);
        Assert.True(Check(V5WorkItemTenantAuthorizationGuard.Operation.CloseDev,
            state: "VALIDANDO", owner: Author).Allowed);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.CloseDev,
                state: "VALIDANDO", owner: Reviewer).Code);
    }

    [Fact]
    public void A5RequiresDifferentAuthorizedActorInTheSameTenantScope()
    {
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.ReviewA5,
                state: "LISTO", owner: Author).Code);
        Assert.True(Check(V5WorkItemTenantAuthorizationGuard.Operation.ReviewA5,
            actor: Reviewer, state: "LISTO", owner: Author).Allowed);
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            Check(V5WorkItemTenantAuthorizationGuard.Operation.ReviewA5,
                actor: Reviewer, state: "LISTO", owner: Foreign).Code);
    }

    [Fact]
    public void MissingServerBindingsFailClosed()
    {
        var r = new V5WorkItemTenantAuthorizationGuard.AccessRequest(
            Author, "scope-a", "tenant-a",
            new V5WorkItemRules.Snapshot(Id, "PENDIENTE", null),
            V5WorkItemTenantAuthorizationGuard.Operation.Claim);
        Assert.Equal("SCOPE_DENIED", _guard.Authorize(
            r, Actors,
            new Dictionary<string, IReadOnlySet<string>>(),
            ScopeTenants, Catalog).Code);
        Assert.Equal("TENANT_MISMATCH", _guard.Authorize(
            r, Actors, ActorScopes,
            new Dictionary<string, string>(), Catalog).Code);
    }
}
