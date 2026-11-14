using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-03.M05.01: real xUnit regressions of the pure coverage projection.
/// These tests do not represent HTTP, MySQL, an A5 review or an authenticated tenant session.
/// </summary>
public sealed class V5CoverageM05RuleRegressionTests
{
    private const string KnownItem = "C00-03.M03.02";
    private const string UnknownItem = "C99-99.M99.99";
    private const string ExactSha = "0123456789abcdef0123456789abcdef01234567";

    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { "R01", "R02" };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { KnownItem };
    private readonly V5RequirementCoverageProjectionService _sut = new();

    private static V5RequirementCoverageRules.Entry Mapping(string id = KnownItem, string key = "R01") =>
        new(key, id, "V5 FINAL", "contract:v5", "test:xunit", "evidence:pending");

    private static IReadOnlyDictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        Snapshot(string state = "LISTO", string observed = ExactSha, string expected = "Matriz de cobertura") =>
        new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [KnownItem] = new(KnownItem, state, "javier-task", expected, observed)
        };

    [Theory]
    [InlineData("PENDIENTE")]
    [InlineData("EN_PROGRESO")]
    [InlineData("VALIDANDO")]
    [InlineData("LISTO")]
    public void ValidStateNeverProvesExternalTestOrIndependentReview(string state)
    {
        var result = _sut.Project(new[] { Mapping() }, Snapshot(state), Requirements, Catalog, ExactSha);

        Assert.True(result.Verdict.Accepted);
        var row = Assert.Single(result.Rows);
        Assert.Equal(state, row.State);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", row.Outstanding);
        if (state != "LISTO")
            Assert.Contains("WORK_ITEM_NOT_LISTO", row.Outstanding);
        else
            Assert.DoesNotContain("WORK_ITEM_NOT_LISTO", row.Outstanding);
    }

    [Fact]
    public void UnknownCatalogIdIsRejectedEvenWhenAPlanSnapshotClaimsListo()
    {
        var observed = new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [UnknownItem] = new(UnknownItem, "LISTO", "another-task", "Fake closure", ExactSha)
        };

        var result = _sut.Project(new[] { Mapping(UnknownItem) }, observed, Requirements, Catalog, ExactSha);

        Assert.False(result.Verdict.Accepted);
        Assert.Equal("UNKNOWN_WORK_ITEM", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void OneInvalidEntryRejectsEntireBatchWithoutPartialCoverageRows()
    {
        var result = _sut.Project(
            new[] { Mapping(), Mapping(UnknownItem, "R02") },
            Snapshot(), Requirements, Catalog, ExactSha);

        Assert.Equal("UNKNOWN_WORK_ITEM", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void DuplicateMappingCannotInflateCoverage()
    {
        var result = _sut.Project(
            new[] { Mapping(), Mapping() }, Snapshot(), Requirements, Catalog, ExactSha);

        Assert.Equal("DUPLICATE_COVERAGE_MAPPING", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void MovedSourceShaCannotBePresentedAsCurrentSuccess()
    {
        var result = _sut.Project(
            new[] { Mapping() }, Snapshot(observed: new string('a', 40)),
            Requirements, Catalog, ExactSha);

        Assert.Equal("SOURCE_REF_MOVED", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void MissingExpectedResultRejectsInsteadOfPublishingMisleadingCoverage()
    {
        var result = _sut.Project(
            new[] { Mapping() }, Snapshot(expected: "   "),
            Requirements, Catalog, ExactSha);

        Assert.Equal("EXPECTED_RESULT_REQUIRED", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void MismatchedSnapshotIdentityRejectsDespiteValidDictionaryKey()
    {
        var observed = new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [KnownItem] = new(UnknownItem, "LISTO", "javier-task", "Matriz", ExactSha)
        };

        var result = _sut.Project(new[] { Mapping() }, observed, Requirements, Catalog, ExactSha);

        Assert.Equal("SNAPSHOT_MISSING", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void MissingEvidenceReferenceCannotBeAcceptedAsRuleCoverage()
    {
        var invalid = Mapping() with { EvidenceReference = " " };
        var result = _sut.Project(new[] { invalid }, Snapshot(), Requirements, Catalog, ExactSha);

        Assert.Equal("EVIDENCE_REQUIRED", result.Verdict.Code);
        Assert.Empty(result.Rows);
    }
}
