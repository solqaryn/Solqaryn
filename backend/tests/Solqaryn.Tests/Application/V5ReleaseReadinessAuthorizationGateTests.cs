using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5ReleaseReadinessAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Rules.VerificationContext Authority() =>
        new("trusted-dev", Sha, "tenant-A",
            new HashSet<string> { "author", "reviewer" },
            new HashSet<string> { "scope-lock", "backend-tests" },
            new HashSet<string> { "grant-dev" });

    private static Rules.Candidate Candidate() =>
        new("release-1", Sha, Sha, "sha256:" + new string('a', 64),
            "dev", "grant-dev", "tenant-A", "author", "reviewer",
            "rollback:revert", "stop:on-failure",
            new[] {
                new Rules.CheckResult("scope-lock", Sha, "completed", "success"),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            },
            new[] { new Rules.ObservedCase("AC-1", "dev:synthetic", true) });

    private static V5ReadinessVerifiedCaller Caller() =>
        new("reviewer", "tenant-A", "grant-dev", "dev", true, true, true);

    private sealed class Source : IV5ReadinessAuthenticatedCallerSource,
        IV5ReleaseReadinessVerifiedEvidenceSource
    {
        public V5ReadinessVerifiedCaller? Principal { get; set; } = Caller();
        public Rules.VerificationContext? Context { get; set; } = Authority();
        public Rules.Candidate? Release { get; set; } = Candidate();
        public int CallerReads { get; private set; }
        public int AuthorityReads { get; private set; }
        public int CandidateReads { get; private set; }
        public ValueTask<V5ReadinessVerifiedCaller?> GetAuthenticatedCallerAsync(
            CancellationToken cancellationToken)
        {
            CallerReads++;
            return ValueTask.FromResult(Principal);
        }
        public ValueTask<Rules.VerificationContext?> GetAuthorityAsync(
            CancellationToken cancellationToken)
        {
            AuthorityReads++;
            return ValueTask.FromResult(Context);
        }
        public ValueTask<Rules.Candidate?> GetCandidateAsync(
            string releaseId, CancellationToken cancellationToken)
        {
            CandidateReads++;
            return ValueTask.FromResult(Release);
        }
    }

    private static V5ReleaseReadinessApplicationService.Request Request() =>
        new("release-1", Sha, Sha);

    private static Task<V5ReleaseReadinessAuthorizationGate.Verdict> Evaluate(
        Source source, V5ReleaseReadinessApplicationService.Request? request = null) =>
        new V5ReleaseReadinessAuthorizationGate(source, source)
            .EvaluateAsync(request ?? Request());

    [Fact]
    public async Task ValidTrustedDevCallerCanRequestIndependentReviewOnly()
    {
        var source = new Source();
        var result = await Evaluate(source);
        Assert.Equal("INDEPENDENT_AUTHENTICATED_RELEASE_REVIEW_REQUIRED", result.Code);
        Assert.True(result.NeedsExternalIndependentReview);
        Assert.False(result.AllowsRelease);
        Assert.False(result.AllowsDeployment);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.GrantsTenantAccess);
        Assert.Equal(1, source.CandidateReads);
    }

    [Fact]
    public async Task MissingOrRevokedPrincipalNeverReadsAuthority()
    {
        var source = new Source { Principal = null };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED", (await Evaluate(source)).Code);
        source.Principal = Caller() with { IsMembershipActive = false };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED", (await Evaluate(source)).Code);
        source.Principal = Caller() with { IsAuthenticated = false };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED", (await Evaluate(source)).Code);
        source.Principal = Caller() with { HasReadinessReviewGrant = false };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED", (await Evaluate(source)).Code);
        Assert.Equal(0, source.AuthorityReads);
    }

    [Theory]
    [InlineData("qa")]
    [InlineData("PROD")]
    [InlineData("")]
    public async Task NonDevCallerIsDenied(string env)
    {
        var source = new Source { Principal = Caller() with { Environment = env } };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED", (await Evaluate(source)).Code);
        Assert.Equal(0, source.AuthorityReads);
    }

    [Fact]
    public async Task CrossTenantAndUnlistedCallerCannotReadCandidate()
    {
        var source = new Source { Principal = Caller() with { TenantScope = "tenant-B" } };
        Assert.Equal("VERIFIED_DEV_AUTHORITY_REQUIRED", (await Evaluate(source)).Code);
        source.Principal = Caller() with { TaskId = "not-allowlisted" };
        Assert.Equal("VERIFIED_DEV_AUTHORITY_REQUIRED", (await Evaluate(source)).Code);
        source.Principal = Caller() with { DevGrantId = "unapproved" };
        Assert.Equal("VERIFIED_DEV_AUTHORITY_REQUIRED", (await Evaluate(source)).Code);
        Assert.Equal(0, source.CandidateReads);
    }

    [Fact]
    public async Task CandidateCannotImpersonateReviewerAuthorTenantOrGrant()
    {
        var source = new Source { Release = Candidate() with { ReviewerTaskId = "author" } };
        Assert.Equal("CALLER_SCOPE_OR_INDEPENDENCE_MISMATCH", (await Evaluate(source)).Code);
        source.Release = Candidate() with { AuthorTaskId = "reviewer" };
        Assert.Equal("CALLER_SCOPE_OR_INDEPENDENCE_MISMATCH", (await Evaluate(source)).Code);
        source.Release = Candidate() with { TenantScope = "tenant-B" };
        Assert.Equal("CALLER_SCOPE_OR_INDEPENDENCE_MISMATCH", (await Evaluate(source)).Code);
        source.Release = Candidate() with { DevGrantId = "another" };
        Assert.Equal("CALLER_SCOPE_OR_INDEPENDENCE_MISMATCH", (await Evaluate(source)).Code);
    }

    [Fact]
    public async Task InvalidRequestShaFailsBeforeIdentityRead()
    {
        var source = new Source();
        Assert.Equal("RELEASE_SOURCE_SHA_MISMATCH",
            (await Evaluate(source, Request() with { ObservedSha = OtherSha })).Code);
        Assert.Equal("RELEASE_REQUEST_REQUIRED",
            (await Evaluate(source, Request() with { ReleaseId = "https://host" })).Code);
        Assert.Equal(0, source.CallerReads);
    }

    [Fact]
    public async Task MissingAuthorityOrCandidateFailsClosed()
    {
        var source = new Source { Context = null };
        Assert.Equal("VERIFIED_DEV_AUTHORITY_REQUIRED", (await Evaluate(source)).Code);
        source.Context = Authority();
        source.Release = null;
        Assert.Equal("VERIFIED_RELEASE_CANDIDATE_REQUIRED", (await Evaluate(source)).Code);
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("skipped")]
    [InlineData("failure")]
    public async Task NonPassCINeverEnablesReview(string conclusion)
    {
        var source = new Source { Release = Candidate() with
        {
            Checks = new[] {
                new Rules.CheckResult("scope-lock", Sha, "completed", conclusion),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            }
        } };
        Assert.Equal("CHECK_NOT_SUCCESS_EXACT_SHA", (await Evaluate(source)).Code);
    }

    [Fact]
    public async Task StaleShaAndOwnA5NeverAuthorizeReview()
    {
        var source = new Source { Release = Candidate() with { ManifestCommitSha = OtherSha } };
        Assert.Equal("RELEASE_MANIFEST_MISMATCH", (await Evaluate(source)).Code);
        source.Release = Candidate() with { ReviewerTaskId = "reviewer", AuthorTaskId = "reviewer" };
        Assert.Equal("CALLER_SCOPE_OR_INDEPENDENCE_MISMATCH", (await Evaluate(source)).Code);
    }

    [Fact]
    public void MissingTrustedDependenciesCannotConstructGate()
    {
        var source = new Source();
        Assert.Throws<ArgumentNullException>(() => new V5ReleaseReadinessAuthorizationGate(null!, source));
        Assert.Throws<ArgumentNullException>(() => new V5ReleaseReadinessAuthorizationGate(source, null!));
    }
}
