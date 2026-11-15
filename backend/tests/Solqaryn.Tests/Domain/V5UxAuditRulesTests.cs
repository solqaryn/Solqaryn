using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5UxAuditRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Surfaces = new HashSet<string> { "portal", "builder" };
    private static readonly IReadOnlySet<string> Criteria = new HashSet<string> { "p95-latency", "contrast-ratio" };

    private static V5UxAuditRules.Observation Example() =>
        new("portal", "p95-latency", "tenant-A", Sha, "ci:evidence", 95m, 150m, "MAX");

    private static V5UxAuditRules.Verdict Validate(V5UxAuditRules.Observation x) =>
        V5UxAuditRules.Validate(x, Surfaces, Criteria, "tenant-A", Sha);

    [Fact]
    public void ValidMeasuredSnapshotIsOnlyReadyForExternalEvidenceReview()
    {
        var result = Validate(Example());
        Assert.True(result.ReadyForEvidenceReview);
        Assert.Equal("READY_FOR_EVIDENCE_REVIEW", result.Code);
    }

    [Theory]
    [InlineData("tenant-B", "TENANT_SCOPE_MISMATCH")]
    [InlineData("", "TENANT_SCOPE_MISMATCH")]
    public void CrossTenantOrMissingTenantContextFailsClosed(string tenant, string expected)
    {
        Assert.Equal(expected, Validate(Example() with { TenantScope = tenant }).Code);
    }

    [Fact]
    public void UnknownSurfaceCriterionOrChangedSourceIsRejected()
    {
        Assert.Equal("UNKNOWN_SURFACE",
            Validate(Example() with { SurfaceId = "other-tenant" }).Code);
        Assert.Equal("UNKNOWN_CRITERION",
            Validate(Example() with { CriterionKey = "unapproved" }).Code);
        Assert.Equal("SOURCE_SHA_MISMATCH",
            Validate(Example() with { SourceCommitSha = new string('a', 40) }).Code);
    }

    [Fact]
    public void AboveMaximumBudgetDoesNotBecomeAnAuditPass()
    {
        var result = Validate(Example() with { ObservedValue = 151m });
        Assert.False(result.ReadyForEvidenceReview);
        Assert.Equal("BUDGET_EXCEEDED", result.Code);
    }

    [Fact]
    public void MinimumContrastRatioCanBeCheckedWithoutAuthorizingPublication()
    {
        var valid = Example() with
        {
            CriterionKey = "contrast-ratio", Comparison = "MIN",
            ObservedValue = 4.6m, BudgetValue = 4.5m
        };
        Assert.True(Validate(valid).ReadyForEvidenceReview);
        Assert.Equal("BUDGET_EXCEEDED",
            Validate(valid with { ObservedValue = 3.5m }).Code);
    }

    [Fact]
    public void MalformedMetricOrComparisonIsRejected()
    {
        Assert.Equal("INVALID_MEASUREMENT",
            Validate(Example() with { BudgetValue = 0m }).Code);
        Assert.Equal("INVALID_MEASUREMENT",
            Validate(Example() with { ObservedValue = -1m }).Code);
        Assert.Equal("UNKNOWN_COMPARISON",
            Validate(Example() with { Comparison = "EQUAL" }).Code);
    }

    [Fact]
    public void UnsafeOrMissingEvidenceCannotBePresentedAsVerified()
    {
        Assert.Equal("EVIDENCE_REQUIRED_OR_UNSAFE",
            Validate(Example() with { EvidenceReference = " " }).Code);
        Assert.Equal("EVIDENCE_REQUIRED_OR_UNSAFE",
            Validate(Example() with { EvidenceReference = "abc\nlog-injection" }).Code);
    }

    [Fact]
    public void InvalidExpectedCommitIsNotAccepted()
    {
        var result = V5UxAuditRules.Validate(Example(), Surfaces, Criteria, "tenant-A", "short");
        Assert.Equal("SOURCE_SHA_MISMATCH", result.Code);
    }
}
