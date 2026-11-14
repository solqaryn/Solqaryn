using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5RequirementCoverageProjectionServiceTests
{
    private const string A = "C00-03.M03.02";
    private const string B = "C00-03.M03.03";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { "R01" };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { A, B };
    private readonly V5RequirementCoverageProjectionService _service = new();

    private static V5RequirementCoverageRules.Entry Mapping(string id = A) =>
        new("R01", id, "V5_FINAL", "docs/contract.md", "xunit:test", "git:sha");

    private static IReadOnlyDictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        Observations(string state = "PENDIENTE", string head = Sha) =>
        new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [A] = new(A, state, null, "Cobertura integrada", head),
            [B] = new(B, "LISTO", "javier-task", "Cobertura independiente", head)
        };

    [Fact]
    public void ProjectsExistingRequirementWithoutInventingAClosedOrTestedItem()
    {
        var mapping = Mapping();
        var result = _service.Project(new[] { mapping }, Observations(), Requirements, Catalog, Sha);
        Assert.True(result.Verdict.Accepted);
        Assert.Single(result.Rows);
        Assert.Equal(A, result.Rows[0].WorkItemId);
        Assert.Equal("PENDIENTE", result.Rows[0].State);
        Assert.Contains("WORK_ITEM_NOT_LISTO", result.Rows[0].Outstanding);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", result.Rows[0].Outstanding);
    }

    [Fact]
    public void ManyToManyMappingUsesOnlyExistingCatalogIdsAndPreservesSnapshot()
    {
        var result = _service.Project(
            new[] { Mapping(A), Mapping(B) }, Observations("LISTO"), Requirements, Catalog, Sha);
        Assert.True(result.Verdict.Accepted);
        Assert.Equal(2, result.Rows.Count);
        Assert.All(result.Rows, x => Assert.Equal("EVIDENCE_NOT_VERIFIED", Assert.Single(x.Outstanding)));
        Assert.Equal("javier-task", result.Rows[1].OwnerTaskId);
    }

    [Fact]
    public void DuplicateCoverageOrUnknownRequirementFailsClosed()
    {
        var duplicate = _service.Project(
            new[] { Mapping(), Mapping() }, Observations(), Requirements, Catalog, Sha);
        Assert.Equal("DUPLICATE_COVERAGE_MAPPING", duplicate.Verdict.Code);
        Assert.Empty(duplicate.Rows);

        var unknown = _service.Project(
            new[] { new V5RequirementCoverageRules.Entry("FOREIGN", A, "V5_FINAL", "contract", "test", "proof") },
            Observations(), Requirements, Catalog, Sha);
        Assert.Equal("UNKNOWN_REQUIREMENT", unknown.Verdict.Code);
        Assert.Empty(unknown.Rows);
    }

    [Fact]
    public void StaleShaMissingSnapshotOrUnknownStateRejectsTheEntireProjection()
    {
        Assert.Equal("CHECK_SHA_MISMATCH", _service.Project(
            new[] { Mapping() }, Observations(), Requirements, Catalog, "short").Verdict.Code);
        Assert.Equal("SOURCE_REF_MOVED", _service.Project(
            new[] { Mapping() }, Observations(head: new string('a', 40)),
            Requirements, Catalog, Sha).Verdict.Code);
        Assert.Equal("SNAPSHOT_MISSING", _service.Project(
            new[] { Mapping() },
            new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>(),
            Requirements, Catalog, Sha).Verdict.Code);
        Assert.Equal("INVALID_STATE", _service.Project(
            new[] { Mapping() }, Observations("PROMOVIDO"),
            Requirements, Catalog, Sha).Verdict.Code);
    }

    [Fact]
    public void EmptyMappingsCannotBePresentedAsSuccessfulCoverage()
    {
        var result = _service.Project(
            Array.Empty<V5RequirementCoverageRules.Entry>(),
            Observations(), Requirements, Catalog, Sha);
        Assert.Equal("COVERAGE_EMPTY", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }
}
