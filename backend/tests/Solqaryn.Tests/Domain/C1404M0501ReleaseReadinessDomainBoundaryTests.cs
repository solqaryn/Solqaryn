using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;

namespace Solqaryn.Tests.Domain;

/// <summary>
/// C14-04.M05.01: executable, in-process DEV readiness boundary regression.
/// Synthetic evidence is deliberately never a deployment or A5 authorization.
/// </summary>
public sealed class C1404M0501ReleaseReadinessDomainBoundaryTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly string Digest = "sha256:" + new string('a', 64);

    private static Rules.VerificationContext Trusted() => new(
        "v5-trusted-source", Sha, "tenant-A",
        new HashSet<string>(StringComparer.Ordinal) { "javier-author", "javier-reviewer" },
        new HashSet<string>(StringComparer.Ordinal) { "scope-lock", "backend-tests" },
        new HashSet<string>(StringComparer.Ordinal) { "dev-grant" });

    private static Rules.Candidate Packet() => new(
        "release-dev", Sha, Sha, Digest, "dev", "dev-grant", "tenant-A",
        "javier-author", "javier-reviewer", "rollback:verified", "stop-on-any-failure",
        new Rules.CheckResult[]
        {
            new("scope-lock", Sha, "completed", "success"),
            new("backend-tests", Sha, "completed", "success")
        },
        new Rules.ObservedCase[]
        {
            new("case-dev-a", "dev:observed-test-run", true)
        });

    private static Rules.Verdict Evaluate(Rules.Candidate candidate) =>
        Rules.Evaluate(Trusted(), candidate);

    [Fact]
    public void CompleteSyntheticPacketIsOnlyReadyForExternalEvidenceReview()
    {
        var verdict = Evaluate(Packet());
        Assert.Equal("READY_FOR_EXTERNAL_EVIDENCE_REVIEW", verdict.Code);
        Assert.True(verdict.ReadyForExternalEvidenceReview);
    }

    [Fact]
    public void TrustSourceLengthBoundary512Versus513IsExact()
    {
        var allowed = Rules.Evaluate(Trusted() with { SourceRevision = new string('r', 512) }, Packet());
        Assert.Equal("READY_FOR_EXTERNAL_EVIDENCE_REVIEW", allowed.Code);
        var blocked = Rules.Evaluate(Trusted() with { SourceRevision = new string('r', 513) }, Packet());
        Assert.Equal("UNVERIFIED_AUTHORITY", blocked.Code);
        Assert.False(blocked.ReadyForExternalEvidenceReview);
    }

    [Theory]
    [InlineData("qa")]
    [InlineData("PROD")]
    [InlineData("Dev")]
    public void NeverPromotesNonDevEnvironment(string environment)
    {
        var verdict = Evaluate(Packet() with { Environment = environment });
        Assert.Equal("DEV_GRANT_REQUIRED", verdict.Code);
        Assert.False(verdict.ReadyForExternalEvidenceReview);
    }

    [Fact]
    public void ForgedTenantOrReviewerCannotReuseSuccessEvidence()
    {
        Assert.Equal("TENANT_SCOPE_MISMATCH",
            Evaluate(Packet() with { TenantScope = "tenant-B" }).Code);
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Evaluate(Packet() with { ReviewerTaskId = "javier-author" }).Code);
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Evaluate(Packet() with { ReviewerTaskId = "unauthorized-task" }).Code);
    }

    [Fact]
    public void CaseSensitiveExpectedShaAndManifestMustBothMatch()
    {
        Assert.Equal("RELEASE_MANIFEST_MISMATCH",
            Evaluate(Packet() with { CommitSha = OtherSha }).Code);
        Assert.Equal("RELEASE_MANIFEST_MISMATCH",
            Evaluate(Packet() with { ManifestCommitSha = OtherSha }).Code);
        Assert.Equal("RELEASE_MANIFEST_MISMATCH",
            Evaluate(Packet() with { CommitSha = Sha.ToUpperInvariant(),
                ManifestCommitSha = Sha.ToUpperInvariant() }).Code);
    }

    [Theory]
    [InlineData("queued", "")]
    [InlineData("in_progress", "")]
    [InlineData("completed", "cancelled")]
    [InlineData("completed", "skipped")]
    [InlineData("completed", "failure")]
    public void RequiredCheckNotCompletedSuccessAlwaysFails(string status, string conclusion)
    {
        var candidate = Packet() with
        {
            Checks = new Rules.CheckResult[]
            {
                new("scope-lock", Sha, status, conclusion),
                new("backend-tests", Sha, "completed", "success")
            }
        };
        Assert.Equal("CHECK_NOT_SUCCESS_EXACT_SHA", Evaluate(candidate).Code);
    }

    [Fact]
    public void DuplicateAndMissingChecksNeverCountAsRequiredChecks()
    {
        var packet = Packet();
        var duplicate = packet with { Checks = packet.Checks.Concat(packet.Checks.Take(1)).ToArray() };
        Assert.Equal("DUPLICATE_OR_INVALID_CHECK", Evaluate(duplicate).Code);
        var missing = packet with { Checks = packet.Checks.Take(1).ToArray() };
        Assert.Equal("REQUIRED_CHECK_MISSING", Evaluate(missing).Code);
    }

    [Fact]
    public void AcceptanceMustHaveUniqueObservedIdentifiersAndSafeEvidence()
    {
        var packet = Packet();
        var repeated = packet with
        {
            AcceptanceCases = new Rules.ObservedCase[]
            {
                new("case-dev-a", "dev:observed-run-1", true),
                new("case-dev-a", "dev:observed-run-2", true)
            }
        };
        Assert.Equal("ACCEPTANCE_NOT_OBSERVED", Evaluate(repeated).Code);
        var notExecuted = packet with
        {
            AcceptanceCases = new Rules.ObservedCase[] { new("case-dev-a", "dev:planned", false) }
        };
        Assert.Equal("ACCEPTANCE_NOT_OBSERVED", Evaluate(notExecuted).Code);
        var unsafeEvidence = packet with
        {
            AcceptanceCases = new Rules.ObservedCase[] { new("case-dev-a", "unsafe" + (char)10 + "reference", true) }
        };
        Assert.Equal("ACCEPTANCE_NOT_OBSERVED", Evaluate(unsafeEvidence).Code);
    }

    [Fact]
    public void RollbackAndStopConditionAreMandatoryEvenForSuccessfulChecks()
    {
        Assert.Equal("ROLLBACK_AND_STOP_CONDITION_REQUIRED",
            Evaluate(Packet() with { RollbackReference = "" }).Code);
        Assert.Equal("ROLLBACK_AND_STOP_CONDITION_REQUIRED",
            Evaluate(Packet() with { StopCondition = "" }).Code);
    }
}
