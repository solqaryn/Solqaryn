using Microsoft.Extensions.Logging;
using Moq;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5CoverageAuditServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { "R01", "R02" };
    private static readonly IReadOnlySet<string> WorkItems = new HashSet<string> { "C00-03.M03.06" };

    private readonly Mock<ILogger<V5CoverageAuditService>> _logger = new();
    private V5CoverageAuditService Service => new(_logger.Object);

    private static V5RequirementCoverageRules.Entry Valid(string key = "R01") =>
        new(key, "C00-03.M03.06", "V5 FINAL", "internal:contract",
            "internal:test", "internal:evidence");

    private string LoggedText() =>
        string.Join(" | ", _logger.Invocations.Select(x =>
            string.Join(" ", x.Arguments.Select(a => a?.ToString()))));

    [Fact]
    public void KnownMapping_ProducesStructuredAuditWithoutRawReferences()
    {
        var result = Service.ValidateAndAudit(Valid(), Requirements, WorkItems, Sha);
        Assert.True(result.Accepted);
        Assert.Contains("WorkItemDigest", LoggedText());
        Assert.Contains(Sha, LoggedText());
        Assert.DoesNotContain("internal:contract", LoggedText());
        Assert.DoesNotContain("internal:test", LoggedText());
        Assert.DoesNotContain("R01", LoggedText());
        Assert.DoesNotContain("C00-03.M03.06", LoggedText());
    }

    [Fact]
    public void RejectedUntrustedInput_NeverLeaksPrivateValues()
    {
        const string secret = "password=do-not-disclose-12345";
        var rejected = Service.ValidateAndAudit(Valid(secret), Requirements, WorkItems, Sha);
        Assert.False(rejected.Accepted);
        Assert.Equal("UNKNOWN_REQUIREMENT", rejected.Code);
        Assert.Contains("UNKNOWN_REQUIREMENT", LoggedText());
        Assert.DoesNotContain(secret, LoggedText());
        Assert.DoesNotContain("internal:evidence", LoggedText());
    }

    [Fact]
    public void InvalidCommitSha_IsDeniedAndNotLogged()
    {
        const string rawToken = "token=never-log-this";
        var result = Service.ValidateAndAudit(Valid(), Requirements, WorkItems, rawToken);
        Assert.Equal("CHECK_SHA_MISMATCH", result.Code);
        Assert.DoesNotContain(rawToken, LoggedText());
        Assert.DoesNotContain("WorkItemDigest", LoggedText());
    }

    [Fact]
    public void DuplicateBatch_IsDeniedWithoutLoggingSensitiveMappings()
    {
        var result = Service.ValidateBatchAndAudit(
            new[] { Valid(), Valid() with { EvidenceReference = "secret-evidence-value" } },
            Requirements, WorkItems, Sha);
        Assert.False(result.Accepted);
        Assert.Equal("DUPLICATE_COVERAGE_MAPPING", result.Code);
        Assert.DoesNotContain("secret-evidence-value", LoggedText());
    }

    [Fact]
    public void ValidBatch_LogsCountWithoutMappingContents()
    {
        var result = Service.ValidateBatchAndAudit(
            new[] { Valid("R01"), Valid("R02") }, Requirements, WorkItems, Sha);
        Assert.True(result.Accepted);
        Assert.Contains("Count=2", LoggedText());
        Assert.DoesNotContain("R01", LoggedText());
        Assert.DoesNotContain("R02", LoggedText());
    }

    [Fact]
    public void FailureSanitizer_RejectsUnlistedCodesAndUnverifiedSha()
    {
        const string privateMessage = "sql-password=really-secret";
        Service.RecordSafeFailure(privateMessage, "bad-sha");
        Assert.Contains("UNCLASSIFIED_FAILURE", LoggedText());
        Assert.Contains("UNVERIFIED", LoggedText());
        Assert.DoesNotContain(privateMessage, LoggedText());
    }

    [Fact]
    public void KnownFailureCategory_IsRetainedWithoutPrivateExceptionText()
    {
        Service.RecordSafeFailure("CI_FAILED", Sha);
        Assert.Contains("CI_FAILED", LoggedText());
        Assert.Contains(Sha, LoggedText());
    }
}
