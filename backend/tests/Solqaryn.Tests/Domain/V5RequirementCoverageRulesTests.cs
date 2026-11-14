using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5RequirementCoverageRulesTests
{
    private static readonly IReadOnlySet<string> Requirements =
        new HashSet<string> { "R01", "R02", "R03" };
    private static readonly IReadOnlySet<string> WorkItems =
        new HashSet<string> { "C00-03.M03.01", "C00-03.M03.02" };

    private static V5RequirementCoverageRules.Entry Valid(
        string requirement = "R01",
        string workItem = "C00-03.M03.01") =>
        new(requirement, workItem, "V5 FINAL", "contract:readback",
            "test:positive-negative", "commit:exact-sha");

    [Fact]
    public void ValidCoverage_AcceptsKnownRequirementAndExistingWorkItem()
    {
        var result = V5RequirementCoverageRules.Validate(Valid(), Requirements, WorkItems);
        Assert.True(result.Accepted);
        Assert.Equal("OK", result.Code);
    }

    [Fact]
    public void MultipleRequirementsMayReferenceSameWorkItem_AndViceVersa()
    {
        var mappings = new[] {
            Valid("R01", "C00-03.M03.01"),
            Valid("R02", "C00-03.M03.01"),
            Valid("R01", "C00-03.M03.02")
        };
        Assert.True(V5RequirementCoverageRules.ValidateBatch(
            mappings, Requirements, WorkItems).Accepted);
    }

    [Theory]
    [InlineData("R99", "C00-03.M03.01", "UNKNOWN_REQUIREMENT")]
    [InlineData("R01", "C00-03.M06.07", "UNKNOWN_WORK_ITEM")]
    [InlineData("", "C00-03.M03.01", "UNKNOWN_REQUIREMENT")]
    public void RejectsUnknownRequirementsAndUnlistedWorkItemIds(
        string requirement, string item, string expected)
    {
        var result = V5RequirementCoverageRules.Validate(
            Valid(requirement, item), Requirements, WorkItems);
        Assert.False(result.Accepted);
        Assert.Equal(expected, result.Code);
    }

    [Theory]
    [InlineData("", "contract", "test", "evidence", "SOURCE_VERSION_REQUIRED")]
    [InlineData("V5", "", "test", "evidence", "CONTRACT_REQUIRED")]
    [InlineData("V5", "contract", "", "evidence", "TEST_REQUIRED")]
    [InlineData("V5", "contract", "test", "", "EVIDENCE_REQUIRED")]
    public void RejectsCoverageWithMissingCausalLinks(
        string version, string contract, string test, string evidence, string expected)
    {
        var entry = Valid() with {
            SourceVersion = version,
            ContractReference = contract,
            TestReference = test,
            EvidenceReference = evidence
        };
        Assert.Equal(expected, V5RequirementCoverageRules.Validate(
            entry, Requirements, WorkItems).Code);
    }

    [Fact]
    public void RepeatedMappingForSameRequirementWorkItemAndVersionIsRejected()
    {
        var entry = Valid();
        var repeated = entry with { EvidenceReference = "other-evidence" };
        var result = V5RequirementCoverageRules.ValidateBatch(
            new[] { entry, repeated }, Requirements, WorkItems);
        Assert.Equal("DUPLICATE_COVERAGE_MAPPING", result.Code);
    }

    [Fact]
    public void SameMappingMayAppearForDistinctSourceVersions()
    {
        var entry = Valid();
        var next = entry with { SourceVersion = "V5 FINAL v2" };
        Assert.True(V5RequirementCoverageRules.ValidateBatch(
            new[] { entry, next }, Requirements, WorkItems).Accepted);
    }

    [Fact]
    public void EmptyBatchDoesNotCreateAnyCoverageOrNewId()
    {
        Assert.True(V5RequirementCoverageRules.ValidateBatch(
            Array.Empty<V5RequirementCoverageRules.Entry>(),
            Requirements, WorkItems).Accepted);
    }
}
