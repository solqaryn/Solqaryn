using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C14-04.M05.02: in-process Request/Result DTO contract and safe error tests.
/// No HTTP endpoint or real JWT/DB authentication is claimed.
/// </summary>
public sealed class C1404M0502ReadinessApplicationContractRegressionTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class FakeSource : IV5ReleaseReadinessVerifiedEvidenceSource
    {
        public int AuthorityReads { get; private set; }
        public int CandidateReads { get; private set; }
        public Rules.VerificationContext? Authority { get; set; } = new(
            "source-dev", Sha, "tenant-A",
            new HashSet<string>(StringComparer.Ordinal) { "author", "reviewer" },
            new HashSet<string>(StringComparer.Ordinal) { "scope-lock", "backend-tests" },
            new HashSet<string>(StringComparer.Ordinal) { "dev-grant" });

        public Rules.Candidate? Candidate { get; set; } = new(
            "release-dev", Sha, Sha, "sha256:" + new string('a', 64),
            "dev", "dev-grant", "tenant-A", "author", "reviewer",
            "rollback:revert", "stop-on-failure",
            new Rules.CheckResult[]
            {
                new("scope-lock", Sha, "completed", "success"),
                new("backend-tests", Sha, "completed", "success")
            },
            new Rules.ObservedCase[]
            {
                new("AC-dev", "observed:test", true)
            });

        public ValueTask<Rules.VerificationContext?> GetAuthorityAsync(CancellationToken cancellationToken)
        {
            AuthorityReads++;
            return ValueTask.FromResult(Authority);
        }

        public ValueTask<Rules.Candidate?> GetCandidateAsync(string releaseId, CancellationToken cancellationToken)
        {
            CandidateReads++;
            return ValueTask.FromResult(Candidate);
        }
    }

    private static V5ReleaseReadinessApplicationService.Request Request() =>
        new("release-dev", Sha, Sha);

    private static void NoPowers(V5ReleaseReadinessApplicationService.Result result)
    {
        Assert.False(result.AllowsRelease);
        Assert.False(result.AllowsProductionDeployment);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesTenantIsolation);
    }

    [Fact]
    public async Task CompleteSyntheticDtoContractStillCannotReleaseAnything()
    {
        var source = new FakeSource();
        var result = await new V5ReleaseReadinessApplicationService(source).PrepareAsync(Request());
        Assert.Equal("INDEPENDENT_AUTHENTICATED_RELEASE_REVIEW_REQUIRED", result.Code);
        Assert.True(result.NeedsIndependentAuthenticatedReview);
        NoPowers(result);
        Assert.Equal(1, source.AuthorityReads);
        Assert.Equal(1, source.CandidateReads);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("https://untrusted.invalid")]
    [InlineData("tenant@attacker")]
    public async Task InvalidReleaseIdReturnsSafeCodeWithoutReadingProvider(string releaseId)
    {
        var source = new FakeSource();
        var result = await new V5ReleaseReadinessApplicationService(source)
            .PrepareAsync(Request() with { ReleaseId = releaseId });
        Assert.Equal("RELEASE_REQUEST_REQUIRED", result.Code);
        Assert.False(result.NeedsIndependentAuthenticatedReview);
        NoPowers(result);
        Assert.Equal(0, source.AuthorityReads);
        Assert.Equal(0, source.CandidateReads);
    }

    [Fact]
    public async Task ReleaseIdBoundary128AcceptedBut129DeniedBeforeProviderRead()
    {
        var source = new FakeSource();
        source.Candidate = source.Candidate! with { ReleaseId = new string('r', 128) };
        var service = new V5ReleaseReadinessApplicationService(source);
        var result = await service.PrepareAsync(Request() with { ReleaseId = new string('r', 128) });
        Assert.Equal("INDEPENDENT_AUTHENTICATED_RELEASE_REVIEW_REQUIRED", result.Code);
        NoPowers(result);
        var denied = await service.PrepareAsync(Request() with { ReleaseId = new string('r', 129) });
        Assert.Equal("RELEASE_REQUEST_REQUIRED", denied.Code);
        NoPowers(denied);
        Assert.Equal(1, source.AuthorityReads);
        Assert.Equal(1, source.CandidateReads);
    }

    [Fact]
    public async Task ShaMismatchRejectsBeforeAuthorityAccess()
    {
        var source = new FakeSource();
        var service = new V5ReleaseReadinessApplicationService(source);
        foreach (var request in new[]
        {
            Request() with { ObservedSha = OtherSha },
            Request() with { ExpectedSha = "bad" },
            Request() with { ObservedSha = "" }
        })
        {
            var result = await service.PrepareAsync(request);
            Assert.Equal("RELEASE_SOURCE_SHA_MISMATCH", result.Code);
            NoPowers(result);
        }
        Assert.Equal(0, source.AuthorityReads);
        Assert.Equal(0, source.CandidateReads);
    }

    [Fact]
    public async Task MismatchedAuthorityNeverAllowsCandidateRead()
    {
        var source = new FakeSource();
        source.Authority = source.Authority! with { ExpectedCommitSha = OtherSha };
        var result = await new V5ReleaseReadinessApplicationService(source).PrepareAsync(Request());
        Assert.Equal("VERIFIED_DEV_AUTHORITY_REQUIRED", result.Code);
        NoPowers(result);
        Assert.Equal(1, source.AuthorityReads);
        Assert.Equal(0, source.CandidateReads);
    }

    [Fact]
    public async Task AbsentOrForgedCandidateAlwaysHasSafeTypedError()
    {
        var source = new FakeSource { Candidate = null };
        var service = new V5ReleaseReadinessApplicationService(source);
        var missing = await service.PrepareAsync(Request());
        Assert.Equal("VERIFIED_RELEASE_CANDIDATE_REQUIRED", missing.Code);
        NoPowers(missing);
        source.Candidate = new FakeSource().Candidate! with { ReleaseId = "other" };
        var forged = await service.PrepareAsync(Request());
        Assert.Equal("VERIFIED_RELEASE_CANDIDATE_REQUIRED", forged.Code);
        NoPowers(forged);
    }

    [Fact]
    public async Task ForgedTenantOrFailedRequiredCheckCannotBorrowSuccess()
    {
        var source = new FakeSource();
        var service = new V5ReleaseReadinessApplicationService(source);
        source.Candidate = source.Candidate! with { TenantScope = "tenant-B" };
        var tenantDenied = await service.PrepareAsync(Request());
        Assert.Equal("TENANT_SCOPE_MISMATCH", tenantDenied.Code);
        NoPowers(tenantDenied);
        source.Candidate = new FakeSource().Candidate! with
        {
            Checks = new Rules.CheckResult[]
            {
                new("scope-lock", Sha, "completed", "skipped"),
                new("backend-tests", Sha, "completed", "success")
            }
        };
        var ciDenied = await service.PrepareAsync(Request());
        Assert.Equal("CHECK_NOT_SUCCESS_EXACT_SHA", ciDenied.Code);
        NoPowers(ciDenied);
    }

    [Fact]
    public async Task PreCancelledTokenDoesNotReadExternalEvidenceSource()
    {
        var source = new FakeSource();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var service = new V5ReleaseReadinessApplicationService(source);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.PrepareAsync(Request(), cancel.Token));
        Assert.Equal(0, source.AuthorityReads);
        Assert.Equal(0, source.CandidateReads);
    }
}
