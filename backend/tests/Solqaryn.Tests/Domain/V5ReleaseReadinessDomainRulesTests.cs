using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5ReleaseReadinessDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly string Digest = "sha256:" + new string('a', 64);

    private static V5ReleaseReadinessDomainRules.VerificationContext Authority() =>
        new("approved-v5-dev-snapshot", Sha, "tenant-A",
            new HashSet<string>(StringComparer.Ordinal) { "task-author", "task-reviewer" },
            new HashSet<string>(StringComparer.Ordinal) { "scope-lock", "backend-tests" },
            new HashSet<string>(StringComparer.Ordinal) { "grant-dev" });

    private static V5ReleaseReadinessDomainRules.Candidate Valid() =>
        new("release-1", Sha, Sha, Digest, "dev", "grant-dev",
            "tenant-A", "task-author", "task-reviewer",
            "rollback:v1", "stop-on-check-failure",
            new[]
            {
                new V5ReleaseReadinessDomainRules.CheckResult(
                    "scope-lock", Sha, "completed", "success"),
                new V5ReleaseReadinessDomainRules.CheckResult(
                    "backend-tests", Sha, "completed", "success")
            },
            new[]
            {
                new V5ReleaseReadinessDomainRules.ObservedCase(
                    "AC-1", "evidence:observed-in-dev", true)
            });

    private static string Check(
        V5ReleaseReadinessDomainRules.Candidate candidate) =>
        V5ReleaseReadinessDomainRules.Evaluate(Authority(), candidate).Code;

    [Fact]
    public void CompleteTrustedDevPacketIsOnlyReadyForExternalReview()
    {
        var verdict = V5ReleaseReadinessDomainRules.Evaluate(Authority(), Valid());
        Assert.True(verdict.ReadyForExternalEvidenceReview);
        Assert.Equal("READY_FOR_EXTERNAL_EVIDENCE_REVIEW", verdict.Code);
    }

    [Theory]
    [InlineData("qa")]
    [InlineData("PROD")]
    [InlineData("")]
    public void EnvironmentOutsideDevIsDenied(string environment) =>
        Assert.Equal("DEV_GRANT_REQUIRED", Check(Valid() with { Environment = environment }));

    [Fact]
    public void MissingGrantCannotBeBypassed() =>
        Assert.Equal("DEV_GRANT_REQUIRED", Check(Valid() with { DevGrantId = "unapproved" }));

    [Fact]
    public void DifferentTenantCannotBorrowAValidEvidencePacket() =>
        Assert.Equal("TENANT_SCOPE_MISMATCH", Check(Valid() with { TenantScope = "tenant-B" }));

    [Fact]
    public void ManifestShaAndDigestMustMatchTheTrustedRelease()
    {
        Assert.Equal("RELEASE_MANIFEST_MISMATCH",
            Check(Valid() with { ManifestCommitSha = OtherSha }));
        Assert.Equal("RELEASE_MANIFEST_MISMATCH",
            Check(Valid() with { CommitSha = OtherSha }));
        Assert.Equal("RELEASE_MANIFEST_MISMATCH",
            Check(Valid() with { ArtifactDigest = "not-a-digest" }));
    }

    [Fact]
    public void ReviewerCannotBeAuthorOrOutsideAllowlist()
    {
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Check(Valid() with { ReviewerTaskId = "task-author" }));
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Check(Valid() with { ReviewerTaskId = "unknown" }));
    }

    [Theory]
    [InlineData("in_progress", "")]
    [InlineData("completed", "cancelled")]
    [InlineData("completed", "skipped")]
    [InlineData("completed", "failure")]
    public void NonSuccessCIIsNeverACausalPass(string status, string conclusion)
    {
        var broken = Valid() with
        {
            Checks = new[]
            {
                new V5ReleaseReadinessDomainRules.CheckResult(
                    "scope-lock", Sha, status, conclusion),
                new V5ReleaseReadinessDomainRules.CheckResult(
                    "backend-tests", Sha, "completed", "success")
            }
        };
        Assert.Equal("CHECK_NOT_SUCCESS_EXACT_SHA", Check(broken));
    }

    [Fact]
    public void ChecksFromOtherShaCannotBeReused()
    {
        var broken = Valid() with
        {
            Checks = new[]
            {
                new V5ReleaseReadinessDomainRules.CheckResult(
                    "scope-lock", OtherSha, "completed", "success"),
                new V5ReleaseReadinessDomainRules.CheckResult(
                    "backend-tests", Sha, "completed", "success")
            }
        };
        Assert.Equal("CHECK_NOT_SUCCESS_EXACT_SHA", Check(broken));
    }

    [Fact]
    public void MissingOrDuplicateRequiredChecksAreRejected()
    {
        Assert.Equal("REQUIRED_CHECK_MISSING",
            Check(Valid() with { Checks = Valid().Checks.Take(1).ToArray() }));
        var duplicate = Valid().Checks.Concat(Valid().Checks.Take(1)).ToArray();
        Assert.Equal("DUPLICATE_OR_INVALID_CHECK",
            Check(Valid() with { Checks = duplicate }));
    }

    [Fact]
    public void WrittenButNotExecutedAcceptanceIsNotEvidence()
    {
        Assert.Equal("ACCEPTANCE_NOT_OBSERVED",
            Check(Valid() with
            {
                AcceptanceCases = new[]
                {
                    new V5ReleaseReadinessDomainRules.ObservedCase(
                        "AC-1", "future-only", false)
                }
            }));
        Assert.Equal("OBSERVED_ACCEPTANCE_REQUIRED",
            Check(Valid() with
            {
                AcceptanceCases = Array.Empty<V5ReleaseReadinessDomainRules.ObservedCase>()
            }));
    }

    [Fact]
    public void RollbackAndStopConditionMustBeExplicit() =>
        Assert.Equal("ROLLBACK_AND_STOP_CONDITION_REQUIRED",
            Check(Valid() with { StopCondition = "" }));

    [Fact]
    public void UnverifiedSourceCannotAuthorizeAnyCandidate() =>
        Assert.Equal("UNVERIFIED_AUTHORITY",
            V5ReleaseReadinessDomainRules.Evaluate(
                Authority() with { SourceRevision = "" }, Valid()).Code);
}
