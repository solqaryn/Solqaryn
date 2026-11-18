using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

/// <summary>
/// C10-04.M05.01: xUnit boundaries on real isolated Domain/Application/Infrastructure
/// review code. No external job, durable claim, JWT, DB connection or restore.
/// </summary>
public sealed class C1004CacheJobsM0501RuleTests
{
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static C1004CacheJobsApplicationService.VerifiedSnapshot Snapshot() =>
        new(
            ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(10, 100, 5), 10, 100),
            10, 100, 7, true, false, true, false, false,
            new HashSet<string>(StringComparer.Ordinal) { "export.request", "cache.invalidate" },
            0, 3, Now.AddMinutes(30), Now);

    private static C1004CacheJobsIsolatedReviewWorker Worker(
        C1004CacheJobsApplicationService.VerifiedSnapshot? snapshot = null) =>
        new(new C1004CacheJobsIsolatedSnapshotAdapter(
            "work-7", 100, 10, 7, snapshot ?? Snapshot()));

    private static C1004CacheJobsApplicationService.Request Request() =>
        new("work-7", "export.request", "idem-12345678", Digest, 100, 7);

    private static C1004CacheJobsIsolatedReviewWorker.Attempt Attempt(
        C1004CacheJobsApplicationService.Request? request = null) =>
        new(request ?? Request(), "work-7", 100, 10, 7);

    [Fact]
    public async Task ThirtyTwoDistinctKeysRespectCapacityOrderAndNeverDispatch()
    {
        var attempts = Enumerable.Range(0, 32)
            .Select(i => (C1004CacheJobsIsolatedReviewWorker.Attempt?)
                Attempt(Request() with { IdempotencyKey = $"idem-m0501-{i:00000000}" }))
            .ToArray();
        var worker = Worker();
        var results = await worker.DrainReviewOnlyAsync(attempts);
        Assert.Equal(32, results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            Assert.Equal(i, results[i].Position);
            Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[i].Code);
            Assert.False(results[i].EnqueuedExternally);
            Assert.False(results[i].LeaseAcquired);
            Assert.False(results[i].MutatedOrRestoredData);
            Assert.False(results[i].ExposedPrivateArtifact);
            Assert.False(results[i].GrantsCrossTenantAccess);
        }
        Assert.False(worker.UsesDurableQueue);
        Assert.False(worker.GrantsProviderAccess);
        Assert.False(worker.CertifiesMySqlOrRestore);
    }

    [Fact]
    public async Task RepeatedKeyAtCapacityGivesOnePreflightAndThirtyOneNoOps()
    {
        var attempts = Enumerable.Repeat<C1004CacheJobsIsolatedReviewWorker.Attempt?>(
            Attempt(), 32).ToArray();
        var results = await Worker().DrainReviewOnlyAsync(attempts);
        Assert.Equal(32, results.Count);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[0].Code);
        for (var i = 1; i < 32; i++)
        {
            Assert.Equal(i, results[i].Position);
            Assert.Equal("ISOLATED_DUPLICATE_NO_DISPATCH", results[i].Code);
            Assert.False(results[i].MutatedOrRestoredData);
        }
    }

    [Fact]
    public async Task TenantBRejectionCannotConsumeTenantAIdempotencyKey()
    {
        var foreign = Attempt() with { ObservedTenantId = 200 };
        var results = await Worker().DrainReviewOnlyAsync(
            new C1004CacheJobsIsolatedReviewWorker.Attempt?[] { foreign, Attempt(), Attempt() });
        Assert.Equal("ISOLATED_TENANT_MISMATCH", results[0].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[1].Code);
        Assert.Equal("ISOLATED_DUPLICATE_NO_DISPATCH", results[2].Code);
        Assert.All(results, r => Assert.False(r.GrantsCrossTenantAccess));
    }

    [Fact]
    public async Task WrongWorkIdCannotReserveAValidWorkIdKey()
    {
        var foreign = Attempt(Request() with { WorkId = "work-other" });
        var results = await Worker().DrainReviewOnlyAsync(
            new C1004CacheJobsIsolatedReviewWorker.Attempt?[] { foreign, Attempt() });
        Assert.Equal("ISOLATED_WORK_ID_MISMATCH", results[0].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[1].Code);
    }

    [Fact]
    public async Task NullWorkRequestFailsClosedThenAllowsIndependentReview()
    {
        var missing = Attempt() with { Request = null };
        var results = await Worker().DrainReviewOnlyAsync(
            new C1004CacheJobsIsolatedReviewWorker.Attempt?[] { missing, Attempt() });
        Assert.Equal("ISOLATED_WORK_REQUEST_REQUIRED", results[0].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", results[1].Code);
        Assert.All(results, r => Assert.False(r.LeaseAcquired));
    }

    [Fact]
    public async Task TwoBatchesShowNoDurableIdempotencyOrRestoration()
    {
        var worker = Worker();
        var first = await worker.DrainReviewOnlyAsync(new[] { Attempt() });
        var second = await worker.DrainReviewOnlyAsync(new[] { Attempt() });
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", Assert.Single(first).Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", Assert.Single(second).Code);
        Assert.False(worker.UsesDurableQueue);
        Assert.False(worker.RunsExternalWorker);
        Assert.False(worker.CertifiesMySqlOrRestore);
    }

    [Fact]
    public async Task RevocationDeniesEveryReviewWithoutAnyEffect()
    {
        var worker = Worker(Snapshot() with { ActiveMembership = false });
        var results = await worker.DrainReviewOnlyAsync(
            new C1004CacheJobsIsolatedReviewWorker.Attempt?[] { Attempt(), Attempt() });
        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal("PERMISSION_OR_MEMBERSHIP_DENIED", r.Code);
            Assert.False(r.EnqueuedExternally);
            Assert.False(r.MutatedOrRestoredData);
        });
    }

    [Fact]
    public async Task ThirtyThreeRequestsRejectBeforeProcessingAnyReview()
    {
        var worker = Worker();
        var oversized = Enumerable.Repeat<C1004CacheJobsIsolatedReviewWorker.Attempt?>(
            Attempt(), 33).ToArray();
        var reject = Assert.Single(await worker.DrainReviewOnlyAsync(oversized));
        Assert.Equal(-1, reject.Position);
        Assert.Equal("ISOLATED_BATCH_SIZE_DENIED", reject.Code);
        Assert.False(reject.EnqueuedExternally);
        Assert.False(worker.GrantsProviderAccess);
    }
}
