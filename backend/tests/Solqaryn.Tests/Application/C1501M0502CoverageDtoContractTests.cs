using System.Text.Json;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// C15-01.M05.02: pruebas reales del contrato DTO Application; NO endpoint HTTP.
public sealed class C1501M0502CoverageDtoContractTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5CoverageApplicationService Service = new();
    private static V5RequirementCoverageRules.Entry Item(string req, string id) =>
        new(req, id, "V5 FINAL", "docs:contract", "xunit:case", "git:blob");

    private static V5CoverageApplicationService.Request Good()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal) { "REQ-001", "REQ-002" };
        var ids = new HashSet<string>(StringComparer.Ordinal) { "C15-01.M03.01", "C15-01.M03.02" };
        return new(Sha, Sha, "V5 FINAL",
            new V5CoverageDomainValidator.SourceSnapshot("V5 FINAL", keys, ids),
            keys, ids, new[]
            {
                Item("REQ-001", "C15-01.M03.01"),
                Item("REQ-002", "C15-01.M03.02")
            });
    }

    private static void AssertSafeDto(V5CoverageApplicationService.Result result, string code)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var dto = doc.RootElement;
        Assert.Equal(code, dto.GetProperty("Code").GetString());
        Assert.False(dto.GetProperty("AllowsInstitutionalWrite").GetBoolean());
        Assert.False(dto.GetProperty("CertifiesFullCoverage").GetBoolean());
        Assert.False(dto.GetProperty("GrantsA5OrEnvironmentPromotion").GetBoolean());
        Assert.True(dto.GetProperty("MappingsExamined").GetInt32() >= 0);
        Assert.True(dto.GetProperty("RequirementsWithoutMapping").GetInt32() >= 0);
        Assert.Equal(6, dto.EnumerateObject().Count());
    }

    [Fact]
    public void ValidCallerSnapshotStillNeedsAuthenticatedEvidence()
    {
        var result = Service.Prepare(Good());
        AssertSafeDto(result, "AUTHENTICATED_EVIDENCE_REQUIRED");
        Assert.Equal(2, result.MappingsExamined);
        Assert.Equal(0, result.RequirementsWithoutMapping);
    }

    [Fact]
    public void InvalidShaIsNotReflectedIntoSerializedResponse()
    {
        const string secret = "PRIVATE_TENANT_A_TOKEN_NEVER_RENDER";
        var result = Service.Prepare(Good() with { ExpectedHeadSha = secret });
        AssertSafeDto(result, "CHECK_SHA_MISMATCH");
        Assert.DoesNotContain(secret, JsonSerializer.Serialize(result), StringComparison.Ordinal);
        Assert.Equal(0, result.MappingsExamined);
    }

    [Fact]
    public void StaleRevisionAndDuplicateMappingReturnOpaqueErrors()
    {
        var good = Good();
        var stale = Service.Prepare(good with { ExpectedSourceRevision = "OLD_PRIVATE_SOURCE" });
        AssertSafeDto(stale, "SOURCE_REVISION_MISMATCH");
        Assert.DoesNotContain("OLD_PRIVATE_SOURCE", JsonSerializer.Serialize(stale), StringComparison.Ordinal);
        var item = good.Mappings.First();
        AssertSafeDto(Service.Prepare(good with { Mappings = new[] { item, item } }), "DUPLICATE_COVERAGE_MAPPING");
    }

    [Fact]
    public void CaseInsensitiveSourceCollectionsCannotAliasRequirementOrWorkItemIds()
    {
        var good = Good();
        var foldedRequirements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "req-001", "REQ-002"
        };
        var sourceRequirementAlias = good with
        {
            Source = good.Source with { ApprovedRequirementKeys = foldedRequirements },
            Mappings = new[]
            {
                Item("req-001", "C15-01.M03.01"),
                Item("REQ-002", "C15-01.M03.02")
            }
        };
        AssertSafeDto(Service.Prepare(sourceRequirementAlias), "CATALOG_SNAPSHOT_MISMATCH");

        var foldedWorkItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "c15-01.m03.01", "C15-01.M03.02"
        };
        var sourceWorkItemAlias = good with
        {
            Source = good.Source with { ExistingPlanItemIds = foldedWorkItems }
        };
        AssertSafeDto(Service.Prepare(sourceWorkItemAlias), "CATALOG_SNAPSHOT_MISMATCH");
    }

    [Fact]
    public void CapacityAndUnmappedRequirementCannotGrantWriteOrA5()
    {
        var good = Good();
        AssertSafeDto(Service.Prepare(good with { MaxMappings = 2341 }), "CAPACITY_EXCEEDED");
        AssertSafeDto(Service.Prepare(good with { MaxMappings = 1 }), "CAPACITY_EXCEEDED");
        AssertSafeDto(Service.Prepare(good with { Mappings = new[] { good.Mappings.First() } }),
            "REQUIREMENTS_UNMAPPED");
    }
}
