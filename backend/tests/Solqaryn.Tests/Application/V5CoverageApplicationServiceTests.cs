using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5CoverageApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5CoverageApplicationService Service = new();

    private static HashSet<string> Approved() => new(StringComparer.Ordinal)
    {
        "REQ-001", "REQ-002"
    };

    private static HashSet<string> PlanIds() => new(StringComparer.Ordinal)
    {
        "C15-01.M03.01", "C15-01.M03.02"
    };

    private static V5RequirementCoverageRules.Entry Map(
        string req = "REQ-001", string id = "C15-01.M03.01") =>
        new(req, id, "V5 FINAL", "docs:contract", "xunit:case", "git:blob");

    private static V5CoverageApplicationService.Request Request(
        params V5RequirementCoverageRules.Entry[] mappings) =>
        new(Sha, Sha, "V5 FINAL",
            new V5CoverageDomainValidator.SourceSnapshot(
                "V5 FINAL", Approved(), PlanIds()),
            Approved(), PlanIds(), mappings);

    [Fact]
    public void CompleteCallerSnapshotCannotAuthorizeWriteOrCertifyCoverage()
    {
        var result = Service.Prepare(Request(
            Map(), Map("REQ-002", "C15-01.M03.02")));
        Assert.Equal("AUTHENTICATED_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(2, result.MappingsExamined);
        Assert.Equal(0, result.RequirementsWithoutMapping);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesFullCoverage);
        Assert.False(result.GrantsA5OrEnvironmentPromotion);
    }

    [Fact]
    public void InvalidOrStaleGitShaFailsClosed()
    {
        var input = Request(Map(), Map("REQ-002", "C15-01.M03.02"));
        Assert.Equal("CHECK_SHA_MISMATCH",
            Service.Prepare(input with { ExpectedHeadSha = "short" }).Code);
        Assert.Equal("CHECK_SHA_MISMATCH",
            Service.Prepare(input with { ObservedHeadSha = new string('f', 40) }).Code);
        Assert.Equal("CHECK_SHA_MISMATCH",
            Service.Prepare(input with { ObservedHeadSha = new string('z', 40) }).Code);
    }

    [Fact]
    public void RevisionOrCatalogMismatchIsDenied()
    {
        var input = Request(Map(), Map("REQ-002", "C15-01.M03.02"));
        Assert.Equal("SOURCE_REVISION_MISMATCH",
            Service.Prepare(input with { ExpectedSourceRevision = "OLD" }).Code);
        Assert.Equal("CATALOG_SNAPSHOT_MISMATCH",
            Service.Prepare(input with
            {
                ExpectedApprovedRequirementKeys = new HashSet<string> { "REQ-001" }
            }).Code);
        Assert.Equal("CATALOG_SNAPSHOT_MISMATCH",
            Service.Prepare(input with
            {
                ExpectedPlanItemIds = new HashSet<string> { "C15-01.M03.01" }
            }).Code);
    }

    [Fact]
    public void MissingRequirementDoesNotBecomeCompletedCoverage()
    {
        var result = Service.Prepare(Request(Map()));
        Assert.Equal("REQUIREMENTS_UNMAPPED", result.Code);
        Assert.Equal(1, result.RequirementsWithoutMapping);
        Assert.False(result.CertifiesFullCoverage);
    }

    [Fact]
    public void DuplicateUnknownAndUnsafeMappingAreRejected()
    {
        var original = Map();
        Assert.Equal("DUPLICATE_COVERAGE_MAPPING",
            Service.Prepare(Request(original, original)).Code);
        Assert.Equal("UNKNOWN_WORK_ITEM",
            Service.Prepare(Request(Map(id: "C15-01.M03.99"))).Code);
        Assert.Equal("UNSAFE_REFERENCE",
            Service.Prepare(Request(Map() with
            {
                EvidenceReference = "git:sha\nunsafe"
            })).Code);
    }

    [Fact]
    public void OverLimitAndEmptySourceFailClosed()
    {
        var input = Request(Map(), Map("REQ-002", "C15-01.M03.02"));
        Assert.Equal("CAPACITY_EXCEEDED",
            Service.Prepare(input with { MaxMappings = 1 }).Code);
        Assert.Equal("CAPACITY_EXCEEDED",
            Service.Prepare(input with { MaxMappings = 2341 }).Code);
        Assert.Equal("CATALOG_SNAPSHOT_MISMATCH",
            Service.Prepare(input with
            {
                Source = new("V5 FINAL", new HashSet<string>(), PlanIds())
            }).Code);
        Assert.Equal("COVERAGE_EMPTY", Service.Prepare(Request()).Code);
    }
}
