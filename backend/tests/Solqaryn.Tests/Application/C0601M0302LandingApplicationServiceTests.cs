using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0601M0302LandingApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private static C0601PublicReadContext ValidRoute() =>
        new("/propuesta", "sitio.example.test", Now, true);

    private static C0601PublicContentSnapshot ValidSnapshot() =>
        new("/propuesta", "sitio.example.test", "publicacion-v1", 7,
            Now.AddDays(-1), Now.AddDays(1),
            true, true, true, false, false, false, false, false);

    private sealed class RouteStub(
        Func<CancellationToken, ValueTask<C0601PublicReadContext?>> read)
        : IC0601VerifiedPublicReadContextSource
    {
        public ValueTask<C0601PublicReadContext?> ReadCurrentAsync(
            CancellationToken cancellationToken) => read(cancellationToken);
    }

    private sealed class ContentStub(
        Func<string, CancellationToken, ValueTask<C0601PublicContentSnapshot?>> read)
        : IC0601VerifiedPublicContentSource
    {
        public ValueTask<C0601PublicContentSnapshot?> ReadForRouteAsync(
            string route, CancellationToken cancellationToken) => read(route, cancellationToken);
    }

    private static C0601LandingApplicationService Create(
        C0601PublicReadContext? route = null,
        C0601PublicContentSnapshot? snapshot = null) =>
        new(new RouteStub(_ => ValueTask.FromResult<C0601PublicReadContext?>(route ?? ValidRoute())),
            new ContentStub((_, _) =>
                ValueTask.FromResult<C0601PublicContentSnapshot?>(snapshot ?? ValidSnapshot())));

    [Fact]
    public async Task ValidTrustedInputsAreOnlyAnInertCandidate()
    {
        var decision = await Create().PrepareAsync();
        Assert.Equal("PLATFORM_PUBLICATION_AND_LEGAL_GATES_STILL_REQUIRED", decision.Code);
        Assert.True(decision.IsCandidateForFurtherReview);
        Assert.False(decision.AuthorizesPublication);
        Assert.False(decision.AuthorizesPrivateAccess);
        Assert.False(decision.AuthorizesPayment);
        Assert.False(decision.CertifiesJwtMysqlOrHttp);
        Assert.False(decision.ContainsPublishedContent);
    }

    [Fact]
    public async Task ServerRouteIsOnlyLookupKeyAndNoClientHintIsAvailable()
    {
        string? observedRoute = null;
        var service = new C0601LandingApplicationService(
            new RouteStub(_ => ValueTask.FromResult<C0601PublicReadContext?>(ValidRoute())),
            new ContentStub((route, _) =>
            {
                observedRoute = route;
                return ValueTask.FromResult<C0601PublicContentSnapshot?>(ValidSnapshot());
            }));
        Assert.True((await service.PrepareAsync()).IsCandidateForFurtherReview);
        Assert.Equal("/propuesta", observedRoute);
    }

    [Fact]
    public async Task MissingOrUnverifiedServerRouteNeverCallsContentSource()
    {
        var calls = 0;
        foreach (C0601PublicReadContext? route in new[]
        {
            null,
            ValidRoute() with { ServerSourceVerified = false },
            ValidRoute() with { ServerRouteKey = "" }
        })
        {
            var service = new C0601LandingApplicationService(
                new RouteStub(_ => ValueTask.FromResult(route)),
                new ContentStub((_, _) =>
                {
                    calls++;
                    return ValueTask.FromResult<C0601PublicContentSnapshot?>(ValidSnapshot());
                }));
            var preview = await service.PrepareAsync();
            Assert.Equal("TRUSTED_SERVER_ROUTE_REQUIRED", preview.Code);
            Assert.False(preview.IsCandidateForFurtherReview);
        }
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task TenantBOrSpoofedHostDoesNotMatchVerifiedPlatformA()
    {
        var mismatched = await Create(
            route: ValidRoute() with { ServerCanonicalHost = "tenant-b.example.test" })
            .PrepareAsync();
        Assert.Equal("CANONICAL_ROUTE_HOST_MISMATCH", mismatched.Code);
        Assert.False(mismatched.IsCandidateForFurtherReview);

        var tenantPayload = await Create(
            snapshot: ValidSnapshot() with { ContainsTenantData = true }).PrepareAsync();
        Assert.Equal("UNSAFE_OR_UNAPPROVED_CONTENT", tenantPayload.Code);
        Assert.False(tenantPayload.AuthorizesPrivateAccess);
    }

    [Fact]
    public async Task PrivateRouteAndMarkupOrUnapprovedLegalCannotPass()
    {
        var privateRoute = "/dashboard";
        Assert.Equal("CANONICAL_ROUTE_HOST_MISMATCH", (await Create(
            ValidRoute() with { ServerRouteKey = privateRoute },
            ValidSnapshot() with { RouteKey = privateRoute }).PrepareAsync()).Code);
        Assert.Equal("UNSAFE_OR_UNAPPROVED_CONTENT", (await Create(
            snapshot: ValidSnapshot() with { ContainsExecutableMarkup = true }).PrepareAsync()).Code);
        Assert.Equal("UNSAFE_OR_UNAPPROVED_CONTENT", (await Create(
            snapshot: ValidSnapshot() with { ContainsUnapprovedLegalText = true }).PrepareAsync()).Code);
    }

    [Fact]
    public async Task StaleRevisionAndDraftRemainDenied()
    {
        Assert.Equal("VERSIONED_CONTENT_KEY_REQUIRED", (await Create(
            snapshot: ValidSnapshot() with { Revision = 0 }).PrepareAsync()).Code);
        Assert.Equal("PLATFORM_APPROVAL_REQUIRED", (await Create(
            snapshot: ValidSnapshot() with { Draft = true }).PrepareAsync()).Code);
    }

    [Fact]
    public async Task NoSnapshotIsNeverTreatedAsPublication()
    {
        var service = new C0601LandingApplicationService(
            new RouteStub(_ => ValueTask.FromResult<C0601PublicReadContext?>(ValidRoute())),
            new ContentStub((_, _) => ValueTask.FromResult<C0601PublicContentSnapshot?>(null)));
        var preview = await service.PrepareAsync();
        Assert.Equal("TRUSTED_SERVER_SOURCE_REQUIRED", preview.Code);
        Assert.False(preview.AuthorizesPublication);
    }

    [Fact]
    public async Task UnavailableRouteOrSnapshotProviderFailsClosed()
    {
        var badRoute = new C0601LandingApplicationService(
            new RouteStub(_ => throw new InvalidOperationException("route offline")),
            new ContentStub((_, _) => ValueTask.FromResult<C0601PublicContentSnapshot?>(ValidSnapshot())));
        Assert.Equal("VERIFIED_SERVER_ROUTING_UNAVAILABLE", (await badRoute.PrepareAsync()).Code);

        var badSnapshot = new C0601LandingApplicationService(
            new RouteStub(_ => ValueTask.FromResult<C0601PublicReadContext?>(ValidRoute())),
            new ContentStub((_, _) => throw new InvalidOperationException("source offline")));
        Assert.Equal("VERIFIED_PLATFORM_SNAPSHOT_UNAVAILABLE", (await badSnapshot.PrepareAsync()).Code);
    }

    [Fact]
    public async Task PreCancelledTokenPreventsAnyProviderCall()
    {
        var reads = 0;
        var service = new C0601LandingApplicationService(
            new RouteStub(_ =>
            {
                reads++;
                return ValueTask.FromResult<C0601PublicReadContext?>(ValidRoute());
            }),
            new ContentStub((_, _) => ValueTask.FromResult<C0601PublicContentSnapshot?>(ValidSnapshot())));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.PrepareAsync(new CancellationToken(true)));
        Assert.Equal(0, reads);
    }

    [Fact]
    public async Task CancelBetweenProvidersAvoidsContentFetch()
    {
        using var cts = new CancellationTokenSource();
        var contents = 0;
        var service = new C0601LandingApplicationService(
            new RouteStub(_ =>
            {
                cts.Cancel();
                return ValueTask.FromResult<C0601PublicReadContext?>(ValidRoute());
            }),
            new ContentStub((_, _) =>
            {
                contents++;
                return ValueTask.FromResult<C0601PublicContentSnapshot?>(ValidSnapshot());
            }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.PrepareAsync(cts.Token));
        Assert.Equal(0, contents);
    }

    [Fact]
    public void NullSourcesAreRejectedBeforeRequest()
    {
        Assert.Throws<ArgumentNullException>(() => new C0601LandingApplicationService(
            null!, new ContentStub((_, _) => ValueTask.FromResult<C0601PublicContentSnapshot?>(ValidSnapshot()))));
        Assert.Throws<ArgumentNullException>(() => new C0601LandingApplicationService(
            new RouteStub(_ => ValueTask.FromResult<C0601PublicReadContext?>(ValidRoute())), null!));
    }
}
