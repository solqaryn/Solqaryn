using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1304VisualReviewAuthorityAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Now =
        new(2026, 11, 16, 1, 30, 0, TimeSpan.Zero);

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Source : IC1304VisualReviewAuthoritySource
    {
        public C1304VisualReviewAuthorityAdapter.Authority? Current = Good();
        public Exception? Failure;
        public int Reads;
        public Task<C1304VisualReviewAuthorityAdapter.Authority?> ReadCurrentAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            if (Failure is not null) throw Failure;
            return Task.FromResult(Current);
        }
    }

    private static C1304VisualReviewAuthorityAdapter.Authority Good() => new(
        7, 11, true, true, true, false, true, Sha, Now.AddMinutes(5),
        new HashSet<string>(StringComparer.Ordinal) { "private-shell" });

    private static V5VisualConsistencyDomainRules.Observation ValidEvidence() =>
        new("private-shell", "ERROR", 11, 390, Sha, "ci:chromium:run-123",
            true, true, true, false, 4.8m, 4.5m);

    private static V5VisualConsistencyApplicationService.Request Request(
        params V5VisualConsistencyDomainRules.Observation?[] evidence) =>
        new(11, evidence.Length == 0 ?
            new V5VisualConsistencyDomainRules.Observation?[] { ValidEvidence() } :
            evidence);

    private static (C1304VisualReviewAuthorityAdapter Adapter, Source Reader) Setup()
    {
        var reader = new Source();
        return (new(reader, new V5VisualConsistencyApplicationService(), new Clock()), reader);
    }

    [Fact]
    public async Task CurrentAuthorityOnlyAllowsExternalBrowserPreflightNotRelease()
    {
        var (adapter, reader) = Setup();
        var result = await adapter.PrepareAsync(11, Sha, Request());
        Assert.Equal(1, reader.Reads);
        Assert.Equal("EXTERNAL_BROWSER_AND_A5_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsChecked);
        Assert.True(result.ReadyForIndependentBrowserReview);
        Assert.False(result.GrantsLiveTenantAccess);
        Assert.False(result.GrantsJwtOrRbac);
        Assert.False(result.CertifiesAccessibility);
        Assert.False(result.PersistsEvidence);
        Assert.False(result.AuthorizesQaOrProduction);
    }

    [Fact]
    public async Task TenantBAndClientMismatchAreDeniedBeforeDomainReview()
    {
        var (adapter, reader) = Setup();
        reader.Current = Good() with { TenantId = 22 };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
        reader.Current = Good();
        Assert.Equal("INVALID_REQUEST_SCOPE",
            (await adapter.PrepareAsync(11, Sha,
                Request() with { ClientRequestedTenantId = 22 })).Code);
    }

    [Fact]
    public async Task NullBackendAndInvalidIdentityFailClosed()
    {
        var (adapter, reader) = Setup();
        reader.Current = null;
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
        reader.Current = Good() with { ActorId = 0 };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
        reader.Current = Good() with { IdentityVerified = false };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
    }

    [Fact]
    public async Task MembershipPermissionRevocationAndUntrustedSourceDeny()
    {
        var (adapter, reader) = Setup();
        foreach (var bad in new[] {
            Good() with { MembershipCurrent = false },
            Good() with { ExplicitReviewPermission = false },
            Good() with { RoleRevoked = true },
            Good() with { SourceAuthenticated = false }
        })
        {
            reader.Current = bad;
            Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
                (await adapter.PrepareAsync(11, Sha, Request())).Code);
        }
    }

    [Fact]
    public async Task ExpiredOrExcessivelyLongAuthorityWindowIsDenied()
    {
        var (adapter, reader) = Setup();
        reader.Current = Good() with { ValidUntilUtc = Now };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
        reader.Current = Good() with { ValidUntilUtc = Now.AddMinutes(16) };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
        reader.Current = Good() with { ValidUntilUtc = Now.AddMinutes(15) };
        Assert.Equal("EXTERNAL_BROWSER_AND_A5_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
    }

    [Fact]
    public async Task SourceShaIsOrdinalAndCannotBeClientForged()
    {
        var (adapter, reader) = Setup();
        reader.Current = Good() with { SourceSha = Sha.ToUpperInvariant() };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
        reader.Current = Good();
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, new string('a', 40), Request())).Code);
        Assert.Equal("INVALID_REQUEST_SCOPE",
            (await adapter.PrepareAsync(11, "not-a-sha", Request())).Code);
    }

    [Fact]
    public async Task ServerOnlySurfaceAllowlistCannotBeEmptyOrUnsafe()
    {
        var (adapter, reader) = Setup();
        reader.Current = Good() with { ServerApprovedSurfaces = new HashSet<string>() };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
        reader.Current = Good() with {
            ServerApprovedSurfaces = new HashSet<string> { "private-shell\nunsafe" }
        };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.PrepareAsync(11, Sha, Request())).Code);
    }

    [Fact]
    public async Task CrossTenantOrSensitiveObservationStillFailsDomainGate()
    {
        var (adapter, _) = Setup();
        Assert.Equal("CROSS_TENANT_VISUAL_EVIDENCE_DENIED",
            (await adapter.PrepareAsync(11, Sha,
                Request(ValidEvidence() with { TenantId = 22 }))).Code);
        Assert.Equal("SENSITIVE_DATA_IN_EVIDENCE",
            (await adapter.PrepareAsync(11, Sha,
                Request(ValidEvidence() with { ContainsSensitiveData = true }))).Code);
    }

    [Fact]
    public async Task BatchOverCapacityDoesNotProvideABypass()
    {
        var (adapter, _) = Setup();
        var evidence = Enumerable.Repeat<V5VisualConsistencyDomainRules.Observation?>(
            ValidEvidence(), 65).ToArray();
        var result = await adapter.PrepareAsync(11, Sha,
            new V5VisualConsistencyApplicationService.Request(11, evidence));
        Assert.Equal("OBSERVATION_CAPACITY_INVALID", result.Code);
    }

    [Fact]
    public async Task AuthorityFailureReturnsOpaqueCodeWithoutPayload()
    {
        var (adapter, reader) = Setup();
        reader.Failure = new InvalidOperationException("private source token");
        var result = await adapter.PrepareAsync(11, Sha, Request());
        Assert.Equal("CURRENT_AUTHORITY_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("private", result.Code);
        Assert.Equal(0, result.ItemsChecked);
    }

    [Fact]
    public async Task CancellationIsPropagatedInsteadOfReportedAsAuthorization()
    {
        var (adapter, reader) = Setup();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adapter.PrepareAsync(11, Sha, Request(), cancellation.Token));
        Assert.Equal(0, reader.Reads);
    }

    [Fact]
    public async Task InvalidClientInputsDoNotReadAuthority()
    {
        var (adapter, reader) = Setup();
        Assert.Equal("INVALID_REQUEST_SCOPE",
            (await adapter.PrepareAsync(0, Sha, Request())).Code);
        Assert.Equal("INVALID_REQUEST_SCOPE",
            (await adapter.PrepareAsync(11, Sha, null)).Code);
        Assert.Equal(0, reader.Reads);
    }
}
