using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5CoverageTenantProjectionServiceTests
{
    private const string Actor = "6adc2e4bd8a48191ac7d8641ed95a1a3";
    private const string External = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string A = "C00-03.M03.03";
    private const string B = "C00-03.M03.02";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Actor };
    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { "R-A", "R-B" };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { A, B };
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Scopes =
        new Dictionary<string, IReadOnlySet<string>> { [Actor] = new HashSet<string> { "scope-a" } };
    private static readonly IReadOnlyDictionary<string, string> Tenants =
        new Dictionary<string, string> { ["scope-a"] = "tenant-a", ["scope-b"] = "tenant-b" };
    private static readonly IReadOnlyDictionary<string, string> WorkScopes =
        new Dictionary<string, string> { [A] = "scope-a", [B] = "scope-b" };
    private static readonly IReadOnlyDictionary<string, string> RequirementScopes =
        new Dictionary<string, string> { ["R-A"] = "scope-a", ["R-B"] = "scope-b" };
    private readonly V5CoverageTenantProjectionService _service = new();

    private static V5RequirementCoverageRules.Entry Entry(string id = A, string req = "R-A") =>
        new(req, id, "V5_FINAL", "contract", "xunit:test", "commit:proof");

    private static IReadOnlyDictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        Observations(string sha = Sha) =>
        new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [A] = new(A, "PENDIENTE", null, "Cobertura", sha),
            [B] = new(B, "LISTO", Actor, "Otra cobertura", sha)
        };

    private V5RequirementCoverageProjectionService.ProjectionResult Run(
        string actor = Actor, string scope = "scope-a", string tenant = "tenant-a",
        string head = Sha, V5RequirementCoverageRules.Entry[]? entries = null,
        IReadOnlyDictionary<string, string>? itemScopes = null,
        IReadOnlyDictionary<string, string>? requirementScopes = null)
        => _service.Project(actor, scope, tenant,
            entries ?? new[] { Entry() }, Observations(), Requirements, Catalog,
            head, Actors, Scopes, Tenants,
            itemScopes ?? WorkScopes, requirementScopes ?? RequirementScopes);

    [Fact]
    public void AuthorizedProjectionKeepsEvidenceUnverified()
    {
        var result = Run();
        Assert.True(result.Verdict.Accepted);
        Assert.Single(result.Rows);
        Assert.Equal(A, result.Rows[0].WorkItemId);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", result.Rows[0].Outstanding);
        Assert.Contains("WORK_ITEM_NOT_LISTO", result.Rows[0].Outstanding);
    }

    [Theory]
    [InlineData(External, "scope-a", "tenant-a", "UNAUTHORIZED_ACTOR")]
    [InlineData(Actor, "scope-b", "tenant-b", "SCOPE_DENIED")]
    [InlineData(Actor, "scope-a", "tenant-b", "TENANT_MISMATCH")]
    [InlineData(Actor, "scope-a", "", "TENANT_MISMATCH")]
    public void DeniesUnauthorizedActorScopeOrCrossTenant(
        string actor, string scope, string tenant, string code)
    {
        var result = Run(actor: actor, scope: scope, tenant: tenant);
        Assert.Equal(code, result.Verdict.Code);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void DeniesWorkItemOrRequirementFromAnotherScope()
    {
        Assert.Equal("WORK_ITEM_SCOPE_DENIED", Run(
            entries: new[] { Entry(B) }).Verdict.Code);
        Assert.Equal("REQUIREMENT_SCOPE_DENIED", Run(
            entries: new[] { Entry(A, "R-B") }).Verdict.Code);
    }

    [Fact]
    public void MissingTrustedBindingsAreDeniedBeforeProjection()
    {
        Assert.Equal("WORK_ITEM_SCOPE_DENIED", Run(
            itemScopes: new Dictionary<string, string>()).Verdict.Code);
        Assert.Equal("REQUIREMENT_SCOPE_DENIED", Run(
            requirementScopes: new Dictionary<string, string>()).Verdict.Code);
    }

    [Fact]
    public void StaleShaAndDuplicateMappingRemainRejected()
    {
        Assert.Equal("SOURCE_REF_MOVED", Run(head: new string('b', 40)).Verdict.Code);
        Assert.Equal("DUPLICATE_COVERAGE_MAPPING", Run(
            entries: new[] { Entry(), Entry() }).Verdict.Code);
        Assert.Equal("COVERAGE_EMPTY", Run(
            entries: Array.Empty<V5RequirementCoverageRules.Entry>()).Verdict.Code);
    }
}
