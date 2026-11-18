using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;

namespace Solqaryn.Tests.Domain;

/// <summary>
/// C14-04.M05.05: security/abuse-only regression of a pure DEV evidence rule.
/// None of these synthetic snapshots authenticates a caller or grants deployment.
/// </summary>
public sealed class C1404M0505ReleaseReadinessAbuseRegressionTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Rules.VerificationContext Trusted() => new(
        "trusted-v5-dev-authority", Sha, "tenant-A",
        new HashSet<string>(StringComparer.Ordinal) { "author", "reviewer" },
        new HashSet<string>(StringComparer.Ordinal) { "scope-lock", "backend-tests" },
        new HashSet<string>(StringComparer.Ordinal) { "dev-grant" });

    private static Rules.Candidate Packet() => new(
        "release-dev", Sha, Sha, "sha256:" + new string('a', 64),
        "dev", "dev-grant", "tenant-A", "author", "reviewer",
        "rollback:versioned", "stop-on-failure",
        new Rules.CheckResult[]
        {
            new("scope-lock", Sha, "completed", "success"),
            new("backend-tests", Sha, "completed", "success")
        },
        new Rules.ObservedCase[] { new("acceptance-1", "observed:in-dev", true) });

    private static void Denies(Rules.Candidate candidate, string code)
    {
        var verdict = Rules.Evaluate(Trusted(), candidate);
        Assert.Equal(code, verdict.Code);
        Assert.False(verdict.ReadyForExternalEvidenceReview);
    }

    [Fact]
    public void ValidInProcessPacketNeverBecomesDeploymentAuthority()
    {
        var verdict = Rules.Evaluate(Trusted(), Packet());
        Assert.Equal("READY_FOR_EXTERNAL_EVIDENCE_REVIEW", verdict.Code);
    }

    [Theory]
    [InlineData("qa")]
    [InlineData("prod")]
    [InlineData("DEV")]
    [InlineData("")]
    public void AnyNonDevEnvironmentFailsBeforeSecurityReview(string environment)
    {
        Denies(Packet() with { Environment = environment }, "DEV_GRANT_REQUIRED");
    }

    [Fact]
    public void GrantSpoofingAndTenantCrossingCannotBorrowSuccessEvidence()
    {
        Denies(Packet() with { DevGrantId = "untrusted-grant" }, "DEV_GRANT_REQUIRED");
        Denies(Packet() with { TenantScope = "tenant-B" }, "TENANT_SCOPE_MISMATCH");
        Denies(Packet() with { TenantScope = "tenant-a" }, "TENANT_SCOPE_MISMATCH");
    }

    [Fact]
    public void UnlistedActorAndReviewerAndSelfReviewAreAlwaysDenied()
    {
        Denies(Packet() with { AuthorTaskId = "retired-agent" }, "INDEPENDENT_REVIEW_REQUIRED");
        Denies(Packet() with { ReviewerTaskId = "retired-agent" }, "INDEPENDENT_REVIEW_REQUIRED");
        Denies(Packet() with { ReviewerTaskId = "author" }, "INDEPENDENT_REVIEW_REQUIRED");
    }

    [Theory]
    [InlineData("queued", "")]
    [InlineData("in_progress", "")]
    [InlineData("completed", "failure")]
    [InlineData("completed", "cancelled")]
    [InlineData("completed", "skipped")]
    public void FakeOrUnfinishedCIIsNotSuccess(string status, string conclusion)
    {
        var candidate = Packet() with
        {
            Checks = new Rules.CheckResult[]
            {
                new("scope-lock", Sha, status, conclusion),
                new("backend-tests", Sha, "completed", "success")
            }
        };
        Denies(candidate, "CHECK_NOT_SUCCESS_EXACT_SHA");
    }

    [Fact]
    public void CheckFromDifferentCommitCannotBeReused()
    {
        var checks = new Rules.CheckResult[]
        {
            new("scope-lock", OtherSha, "completed", "success"),
            new("backend-tests", Sha, "completed", "success")
        };
        Denies(Packet() with { Checks = checks }, "CHECK_NOT_SUCCESS_EXACT_SHA");
    }

    [Fact]
    public void ExtraFailedCheckCannotBeMaskedByTwoSuccessfulRequiredChecks()
    {
        var packet = Packet();
        var checks = packet.Checks.Concat(new Rules.CheckResult[]
        {
            new("optional-security", Sha, "completed", "failure")
        }).ToArray();
        Denies(packet with { Checks = checks }, "CHECK_NOT_SUCCESS_EXACT_SHA");
    }

    [Fact]
    public void DuplicateOrMissingCheckNamesAreRejected()
    {
        var packet = Packet();
        Denies(packet with { Checks = packet.Checks.Concat(packet.Checks).ToArray() },
            "DUPLICATE_OR_INVALID_CHECK");
        Denies(packet with { Checks = packet.Checks.Take(1).ToArray() },
            "REQUIRED_CHECK_MISSING");
    }

    [Fact]
    public void ForgedOrUnexecutedAcceptanceCannotBecomeEvidence()
    {
        var packet = Packet();
        Denies(packet with
        {
            AcceptanceCases = new Rules.ObservedCase[]
            {
                new("acceptance-1", "planned-only", false)
            }
        }, "ACCEPTANCE_NOT_OBSERVED");
        Denies(packet with
        {
            AcceptanceCases = new Rules.ObservedCase[]
            {
                new("acceptance-1", "observed:1", true),
                new("acceptance-1", "observed:2", true)
            }
        }, "ACCEPTANCE_NOT_OBSERVED");
    }

    [Fact]
    public void MalformedArtifactAndMissingStopConditionStillDenyWithGreenCI()
    {
        Denies(Packet() with { ArtifactDigest = "sha256:" + new string('b', 63) },
            "RELEASE_MANIFEST_MISMATCH");
        Denies(Packet() with { ManifestCommitSha = OtherSha },
            "RELEASE_MANIFEST_MISMATCH");
        Denies(Packet() with { StopCondition = "" },
            "ROLLBACK_AND_STOP_CONDITION_REQUIRED");
    }

    [Fact]
    public void UnverifiedSourceCannotAcceptOtherwiseCompleteEvidence()
    {
        var verdict = Rules.Evaluate(
            Trusted() with { SourceRevision = "line" + (char)10 + "break" }, Packet());
        Assert.Equal("UNVERIFIED_AUTHORITY", verdict.Code);
        Assert.False(verdict.ReadyForExternalEvidenceReview);
    }
}
