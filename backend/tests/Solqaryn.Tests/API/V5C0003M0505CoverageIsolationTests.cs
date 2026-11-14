using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C00-03.M05.05: directed negative security regression of the real read-only
/// coverage projection. Fixtures model server-trusted bindings; no live JWT,
/// tenant database, Sheets CAS or independent A5 is asserted.
/// </summary>
public sealed class V5C0003M0505CoverageIsolationTests
{
    private const string Actor = "6aeacb7820ac8191b7d768a6f33dfa09";
    private const string Item = "C00-03.M03.02";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void TrustedSameTenantProjectionKeepsEvidenceUnverified()
    {
        var outcome = Project("allowed");
        Assert.True(outcome.Verdict.Accepted);
        var row = Assert.Single(outcome.Rows);
        Assert.Equal(Item, row.WorkItemId);
        Assert.Equal(Sha, row.ObservedHeadSha);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", row.Outstanding);
    }

    [Theory]
    [InlineData("unknown-actor", "UNAUTHORIZED_ACTOR")]
    [InlineData("missing-actor", "UNAUTHORIZED_ACTOR")]
    [InlineData("revoked-actor-scope", "SCOPE_DENIED")]
    [InlineData("other-scope", "SCOPE_DENIED")]
    [InlineData("cross-tenant", "TENANT_MISMATCH")]
    [InlineData("case-shifted-tenant", "TENANT_MISMATCH")]
    [InlineData("missing-scope-tenant", "TENANT_MISMATCH")]
    [InlineData("cross-scope-item", "WORK_ITEM_SCOPE_DENIED")]
    [InlineData("missing-item-binding", "WORK_ITEM_SCOPE_DENIED")]
    [InlineData("cross-scope-requirement", "REQUIREMENT_SCOPE_DENIED")]
    [InlineData("missing-requirement-binding", "REQUIREMENT_SCOPE_DENIED")]
    public void UntrustedOrCrossTenantBindingsFailClosedWithoutRows(
        string scenario, string expectedCode)
    {
        var outcome = Project(scenario);
        Assert.False(outcome.Verdict.Accepted);
        Assert.Equal(expectedCode, outcome.Verdict.Code);
        Assert.Empty(outcome.Rows);
        Assert.DoesNotContain("private-tenant-b-secret", outcome.Verdict.Code, StringComparison.Ordinal);
    }

    private static V5RequirementCoverageProjectionService.ProjectionResult Project(
        string scenario)
    {
        var actor = scenario == "unknown-actor" ? "retired-task" :
            scenario == "missing-actor" ? "" : Actor;
        var scope = scenario == "other-scope" ? "scope-b" : "scope-a";
        var tenant = scenario == "cross-tenant" ? "private-tenant-b-secret" :
            scenario == "case-shifted-tenant" ? "TENANT-A" : "tenant-a";

        var taskScopes = new Dictionary<string, IReadOnlySet<string>>
        {
            [Actor] = scenario == "revoked-actor-scope"
                ? new HashSet<string>()
                : new HashSet<string> { "scope-a" }
        };
        var scopeTenants = new Dictionary<string, string>
        {
            ["scope-a"] = "tenant-a"
        };
        var itemScopes = new Dictionary<string, string>
        {
            [Item] = scenario == "cross-scope-item" ? "scope-b" : "scope-a"
        };
        var requirementScopes = new Dictionary<string, string>
        {
            ["R-A"] = scenario == "cross-scope-requirement" ? "scope-b" : "scope-a"
        };

        if (scenario == "missing-scope-tenant") scopeTenants.Clear();
        if (scenario == "missing-item-binding") itemScopes.Clear();
        if (scenario == "missing-requirement-binding") requirementScopes.Clear();

        return new V5CoverageTenantProjectionService().Project(
            actor, scope, tenant,
            new[] { new V5RequirementCoverageRules.Entry(
                "R-A", Item, "V5 FINAL", "contract", "xunit", "unverified") },
            new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
            {
                [Item] = new(Item, "LISTO", Actor, "Matriz integrada", Sha)
            },
            new HashSet<string> { "R-A" },
            new HashSet<string> { Item },
            Sha,
            new HashSet<string> { Actor },
            taskScopes,
            scopeTenants,
            itemScopes,
            requirementScopes);
    }
}
