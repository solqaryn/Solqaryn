using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5ReleaseReadinessApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Rules.VerificationContext Authority() =>
        new("dev-v5-revision", Sha, "tenant-A",
            new HashSet<string> { "author", "reviewer" },
            new HashSet<string> { "scope-lock", "backend-tests" },
            new HashSet<string> { "grant-dev" });

    private static Rules.Candidate Candidate() =>
        new("release-1", Sha, Sha, "sha256:" + new string('a', 64),
            "dev", "grant-dev", "tenant-A", "author", "reviewer",
            "rollback:revert", "stop:on-failure",
            new[]
            {
                new Rules.CheckResult("scope-lock", Sha, "completed", "success"),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            },
            new[] { new Rules.ObservedCase("AC-1", "dev:observed", true) });

    private sealed class Source : IV5ReleaseReadinessVerifiedEvidenceSource
    {
        public Rules.VerificationContext? Context { get; set; } = Authority();
        public Rules.Candidate? Evidence { get; set; } = Candidate();
        public int AuthorityReads { get; private set; }
        public int CandidateReads { get; private set; }
        public ValueTask<Rules.VerificationContext?> GetAuthorityAsync(CancellationToken cancellationToken)
        {
            AuthorityReads++;
            return ValueTask.FromResult(Context);
        }
        public ValueTask<Rules.Candidate?> GetCandidateAsync(string releaseId,
            CancellationToken cancellationToken)
        {
            CandidateReads++;
            return ValueTask.FromResult(Evidence);
        }
    }

    private static V5ReleaseReadinessApplicationService.Request Request() =>
        new("release-1", Sha, Sha);

    [Fact]
    public async Task CompleteDevEvidenceOnlyRequestsExternalReviewNeverRelease()
    {
        var source = new Source();
        var result = await new V5ReleaseReadinessApplicationService(source).PrepareAsync(Request());
        Assert.Equal("INDEPENDENT_AUTHENTICATED_RELEASE_REVIEW_REQUIRED", result.Code);
        Assert.True(result.NeedsIndependentAuthenticatedReview);
        Assert.False(result.AllowsRelease);
        Assert.False(result.AllowsProductionDeployment);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesTenantIsolation);
        Assert.Equal(1, source.CandidateReads);
    }

    [Fact]
    public async Task MissingAndSpoofedRequestNeverReadsAuthority()
    {
        var source = new Source();
        var service = new V5ReleaseReadinessApplicationService(source);
        Assert.Equal("RELEASE_REQUEST_REQUIRED", (await service.PrepareAsync(null)).Code);
        Assert.Equal("RELEASE_REQUEST_REQUIRED",
            (await service.PrepareAsync(Request() with { ReleaseId = "https://bad" })).Code);
        Assert.Equal("RELEASE_SOURCE_SHA_MISMATCH",
            (await service.PrepareAsync(Request() with { ObservedSha = OtherSha })).Code);
        Assert.Equal("RELEASE_SOURCE_SHA_MISMATCH",
            (await service.PrepareAsync(Request() with { ExpectedSha = "bad" })).Code);
        Assert.Equal(0, source.AuthorityReads);
    }

    [Fact]
    public async Task MissingAndMismatchedAuthorityNeverReadsCandidate()
    {
        var source = new Source { Context = null };
        var service = new V5ReleaseReadinessApplicationService(source);
        Assert.Equal("VERIFIED_DEV_AUTHORITY_REQUIRED",
            (await service.PrepareAsync(Request())).Code);
        source.Context = Authority() with { ExpectedCommitSha = OtherSha };
        Assert.Equal("VERIFIED_DEV_AUTHORITY_REQUIRED",
            (await service.PrepareAsync(Request())).Code);
        Assert.Equal(0, source.CandidateReads);
    }

    [Fact]
    public async Task UnknownReleaseDenied()
    {
        var source = new Source { Evidence = Candidate() with { ReleaseId = "other" } };
        Assert.Equal("VERIFIED_RELEASE_CANDIDATE_REQUIRED",
            (await new V5ReleaseReadinessApplicationService(source).PrepareAsync(Request())).Code);
    }

    [Fact]
    public async Task TenantCrossingAndProductionEnvironmentDenied()
    {
        var source = new Source { Evidence = Candidate() with { TenantScope = "tenant-B" } };
        var service = new V5ReleaseReadinessApplicationService(source);
        Assert.Equal("TENANT_SCOPE_MISMATCH", (await service.PrepareAsync(Request())).Code);
        source.Evidence = Candidate() with { Environment = "PROD" };
        Assert.Equal("DEV_GRANT_REQUIRED", (await service.PrepareAsync(Request())).Code);
    }

    [Fact]
    public async Task CancelledAndStaleCINeverPass()
    {
        var source = new Source();
        var service = new V5ReleaseReadinessApplicationService(source);
        source.Evidence = Candidate() with
        {
            Checks = new[]
            {
                new Rules.CheckResult("scope-lock", Sha, "completed", "cancelled"),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            }
        };
        Assert.Equal("CHECK_NOT_SUCCESS_EXACT_SHA", (await service.PrepareAsync(Request())).Code);
        source.Evidence = Candidate() with
        {
            Checks = new[]
            {
                new Rules.CheckResult("scope-lock", OtherSha, "completed", "success"),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            }
        };
        Assert.Equal("CHECK_NOT_SUCCESS_EXACT_SHA", (await service.PrepareAsync(Request())).Code);
    }

    [Fact]
    public async Task OwnA5AndUnexecutedAcceptanceAreRejected()
    {
        var source = new Source { Evidence = Candidate() with { ReviewerTaskId = "author" } };
        var service = new V5ReleaseReadinessApplicationService(source);
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED", (await service.PrepareAsync(Request())).Code);
        source.Evidence = Candidate() with
        {
            AcceptanceCases = new[] { new Rules.ObservedCase("AC-1", "not-run", false) }
        };
        Assert.Equal("ACCEPTANCE_NOT_OBSERVED", (await service.PrepareAsync(Request())).Code);
    }

    [Fact]
    public void NullSourceCannotConstructService()
    {
        Assert.Throws<ArgumentNullException>(() => new V5ReleaseReadinessApplicationService(null!));
    }
}
