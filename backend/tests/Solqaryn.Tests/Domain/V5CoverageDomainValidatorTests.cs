using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5CoverageDomainValidatorTests
{
    private static readonly IReadOnlySet<string> Keys = new HashSet<string> { "REQ-1", "REQ-2" };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string>
    {
        "C15-01.M03.01", "C15-01.M03.02"
    };

    private static V5CoverageDomainValidator.SourceSnapshot Source(
        string revision = "V5 FINAL") => new(revision, Keys, Catalog);

    private static V5RequirementCoverageRules.Entry Map(
        string key = "REQ-1", string id = "C15-01.M03.01",
        string revision = "V5 FINAL") =>
        new(key, id, revision, "docs:contract", "xunit:test", "git:sha");

    [Fact]
    public void AllKeysMapped_IsOnlyReadyForEvidenceReview_NotCertified()
    {
        var result = V5CoverageDomainValidator.Assess(Source(), new[]
        {
            Map(), Map("REQ-2", "C15-01.M03.02")
        });
        Assert.True(result.ReadyForEvidenceReview);
        Assert.Equal("READY_FOR_EVIDENCE_REVIEW", result.Code);
        Assert.Equal(2, result.MappingsExamined);
        Assert.Equal(0, result.RequirementsWithoutMapping);
    }

    [Fact]
    public void MissingRequirementIsNotReportedAsFullCoverage()
    {
        var result = V5CoverageDomainValidator.Assess(Source(), new[] { Map() });
        Assert.False(result.ReadyForEvidenceReview);
        Assert.Equal("REQUIREMENTS_UNMAPPED", result.Code);
        Assert.Equal(1, result.RequirementsWithoutMapping);
    }

    [Theory]
    [InlineData("OLD", "C15-01.M03.01", "SOURCE_VERSION_MISMATCH")]
    [InlineData("V5 FINAL", "C15-01.M03.99", "UNKNOWN_WORK_ITEM")]
    [InlineData("V5 FINAL", "wrong-id", "INVALID_WORK_ITEM_FORMAT")]
    public void RejectsUntrustedVersionOrUnknownCatalogItem(
        string revision, string id, string code)
    {
        var result = V5CoverageDomainValidator.Assess(Source(),
            new[] { Map(revision: revision, id: id) });
        Assert.Equal(code, result.Code);
    }

    [Fact]
    public void DuplicateMappingCannotInflateCoverage()
    {
        var entry = Map();
        var result = V5CoverageDomainValidator.Assess(Source(),
            new[] { entry, entry with { EvidenceReference = "another:proof" } });
        Assert.Equal("DUPLICATE_COVERAGE_MAPPING", result.Code);
        Assert.False(result.ReadyForEvidenceReview);
    }

    [Theory]
    [InlineData("docs:line\nINJECTED")]
    [InlineData("docs:line\rINJECTED")]
    public void ControlCharactersInEvidenceReferencesAreRejected(string reference)
    {
        var result = V5CoverageDomainValidator.Assess(Source(),
            new[] { Map() with { EvidenceReference = reference } });
        Assert.Equal("UNSAFE_REFERENCE", result.Code);
    }

    [Fact]
    public void EmptyInputOrUntrustedEmptyCatalogFailsClosed()
    {
        Assert.Equal("COVERAGE_EMPTY",
            V5CoverageDomainValidator.Assess(Source(),
                Array.Empty<V5RequirementCoverageRules.Entry>()).Code);
        Assert.Equal("SOURCE_CATALOG_EMPTY",
            V5CoverageDomainValidator.Assess(
                new("V5 FINAL", new HashSet<string>(), Catalog),
                new[] { Map() }).Code);
    }

    [Fact]
    public void UnknownRequirementOrMissingTestReferenceIsRejected()
    {
        Assert.Equal("UNKNOWN_REQUIREMENT",
            V5CoverageDomainValidator.Assess(Source(),
                new[] { Map("FOREIGN") }).Code);
        Assert.Equal("UNSAFE_REFERENCE",
            V5CoverageDomainValidator.Assess(Source(),
                new[] { Map() with { TestReference = " " } }).Code);
    }
}
