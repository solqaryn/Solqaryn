using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

/// <summary>
/// C10-04.M05.04: real .NET 10 positive acceptance of the existing isolated
/// review pipeline (Domain/Application/Infrastructure). A positive preflight
/// is deliberately NOT permission to enqueue, write, lease, restore or use R2.
/// </summary>
public sealed class C1004CacheJobsM0504PositiveAcceptanceTests
{
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string OtherDigest = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static C1004CacheJobsApplicationService.VerifiedSnapshot Snapshot() =>
        new(
            ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(10, 100, 5), 10, 100),
            10, 100, 7, true, false, true, false, false,
            new HashSet<string>(StringComparer.Ordinal) { "export.request", "cache.invalidate" },
            0, 3, Now.AddMinutes(30), Now);

    private static C1004CacheJobsApplicationService.Request Request(
        string operation = "export.request", string key = "idem-12345678") =>
        new("work-7", operation, key, Digest, 100, 7);

    private static C1004CacheJobsIsolatedReviewWorker.Attempt Attempt(
        C1004CacheJobsApplicationService.Request? request = null) =>
        new(request ?? Request(), "work-7", 100, 10, 7);

    private static C1004CacheJobsIsolatedReviewWorker Worker(
        C1004CacheJobsApplicationService.VerifiedSnapshot? snapshot = null) =>
        new(new C1004CacheJobsIsolatedSnapshotAdapter(
            "work-7", 100, 10, 7, snapshot ?? Snapshot()));

    private static void NoExternalAuthority(C1004CacheJobsIsolatedReviewWorker.Review review)
    {
        Assert.False(review.EnqueuedExternally);
        Assert.False(review.LeaseAcquired);
        Assert.False(review.MutatedOrRestoredData);
        Assert.False(review.ExposedPrivateArtifact);
        Assert.False(review.GrantsCrossTenantAccess);
    }

    [Theory]
    [InlineData("export.request")]
    [InlineData("cache.invalidate")]
    public async Task TwoAllowlistedOperationsAcceptAnIsolatedPositivePreflight(string operation)
    {
        var worker = Worker();
        var result = Assert.Single(await worker.DrainReviewOnlyAsync(
            new[] { Attempt(Request(operation)) }));
        Assert.Equal(0, result.Position);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result.Code);
        NoExternalAuthority(result);
        Assert.False(worker.RunsExternalWorker);
        Assert.False(worker.UsesDurableQueue);
        Assert.False(worker.GrantsProviderAccess);
        Assert.False(worker.CertifiesMySqlOrRestore);
    }

    [Fact]
    public async Task TwoIndependentPositiveOperationsPreserveOrderWithoutDispatch()
    {
        var results = await Worker().DrainReviewOnlyAsync(new[]
        {
            Attempt(Request("export.request", "idem-accept-0001")),
            Attempt(Request("cache.invalidate", "idem-accept-0002"))
        });
        Assert.Equal(2, results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            Assert.Equal(i, results[i].Position);
            Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[i].Code);
            NoExternalAuthority(results[i]);
        }
    }

    [Fact]
    public async Task LastPermittedRetryWithVerifiedServiceGrantRemainsOnlyPreflight()
    {
        var snapshot = Snapshot() with
        {
            RetryAttempt = 2,
            RetryLimit = 3,
            IsServiceActor = true,
            ServiceGrantVerified = true
        };
        var result = Assert.Single(await Worker(snapshot).DrainReviewOnlyAsync(
            new[] { Attempt() }));
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result.Code);
        NoExternalAuthority(result);
    }

    [Fact]
    public async Task PositivePreflightThenReplayAndDigestConflictNeverExecuteTwice()
    {
        var conflicting = Request() with { PayloadSha256 = OtherDigest };
        var results = await Worker().DrainReviewOnlyAsync(new[]
        {
            Attempt(), Attempt(), Attempt(conflicting)
        });
        Assert.Equal(new[]
        {
            "ISOLATED_PREFLIGHT_NO_DISPATCH",
            "ISOLATED_DUPLICATE_NO_DISPATCH",
            "ISOLATED_IDEMPOTENCY_DIGEST_CONFLICT"
        }, results.Select(r => r.Code).ToArray());
        Assert.All(results, NoExternalAuthority);
    }

    [Fact]
    public async Task ForeignTenantDeniedBeforeAnIndependentPositiveAcceptance()
    {
        var foreign = Attempt() with { ObservedTenantId = 200 };
        var results = await Worker().DrainReviewOnlyAsync(new[] { foreign, Attempt() });
        Assert.Equal("ISOLATED_TENANT_MISMATCH", results[0].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[1].Code);
        Assert.All(results, NoExternalAuthority);
    }

    [Fact]
    public async Task PositiveSnapshotDoesNotAcquireAGrantWhenCallerMutatesOriginalAllowlist()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        { "export.request", "cache.invalidate" };
        var worker = Worker(Snapshot() with { AllowedOperations = allowed });
        allowed.Add("backup.restore");
        var results = await worker.DrainReviewOnlyAsync(new[]
        {
            Attempt(Request("export.request", "idem-accept-0001")),
            Attempt(Request("backup.restore", "idem-accept-0002"))
        });
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[0].Code);
        Assert.Equal("OPERATION_NOT_ALLOWLISTED", results[1].Code);
        Assert.All(results, NoExternalAuthority);
    }

    [Fact]
    public async Task PreCancelledPositiveRequestNeverBecomesAnExternalOperation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var worker = Worker();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await worker.DrainReviewOnlyAsync(new[] { Attempt() }, cts.Token));
        Assert.False(worker.RunsExternalWorker);
        Assert.False(worker.UsesDurableQueue);
    }
}
