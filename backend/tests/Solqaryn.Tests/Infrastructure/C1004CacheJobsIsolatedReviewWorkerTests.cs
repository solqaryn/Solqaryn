using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1004CacheJobsIsolatedReviewWorkerTests
{
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string OtherDigest = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static ContextoTenantActual Actor() =>
        ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(10, 100, 5), 10, 100);

    private static C1004CacheJobsApplicationService.VerifiedSnapshot Snapshot() =>
        new(Actor(), 10, 100, 7, true, false, true, false, false,
            new HashSet<string>(StringComparer.Ordinal)
            { "export.request", "cache.invalidate" },
            0, 3, Now.AddMinutes(30), Now);

    private static C1004CacheJobsApplicationService.Request Request() =>
        new("work-7", "export.request", "idem-12345678", Digest, 100, 7);

    private static C1004CacheJobsIsolatedReviewWorker.Attempt Attempt(
        C1004CacheJobsApplicationService.Request? request = null) =>
        new(request ?? Request(), "work-7", 100, 10, 7);

    private static C1004CacheJobsIsolatedReviewWorker Worker(
        C1004CacheJobsApplicationService.VerifiedSnapshot? snapshot = null) =>
        new(new C1004CacheJobsIsolatedSnapshotAdapter(
            "work-7", 100, 10, 7, snapshot ?? Snapshot()));

    [Fact]
    public async Task ValidReviewDrainsBoundedQueueButNeverEnqueuesOrMutates()
    {
        var w = Worker();
        var reviews = await w.DrainReviewOnlyAsync(new[] { Attempt() });
        var first = Assert.Single(reviews);
        Assert.Equal(0, first.Position);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", first.Code);
        Assert.False(first.EnqueuedExternally);
        Assert.False(first.LeaseAcquired);
        Assert.False(first.MutatedOrRestoredData);
        Assert.False(first.ExposedPrivateArtifact);
        Assert.False(first.GrantsCrossTenantAccess);
        Assert.False(w.RunsExternalWorker);
        Assert.False(w.UsesDurableQueue);
        Assert.False(w.GrantsProviderAccess);
        Assert.False(w.CertifiesMySqlOrRestore);
    }

    [Fact]
    public async Task SameIdempotencyKeyAndPayloadAreNoOpWithinOneTenantAndWork()
    {
        var result = await Worker().DrainReviewOnlyAsync(
            new[] { Attempt(), Attempt() });
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result[0].Code);
        Assert.Equal("ISOLATED_DUPLICATE_NO_DISPATCH", result[1].Code);
    }

    [Fact]
    public async Task SameIdempotencyKeyWithDifferentDigestNeverBecomesSecondEffect()
    {
        var altered = Request() with { PayloadSha256 = OtherDigest };
        var result = await Worker().DrainReviewOnlyAsync(
            new[] { Attempt(), Attempt(altered) });
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result[0].Code);
        Assert.Equal("ISOLATED_IDEMPOTENCY_DIGEST_CONFLICT", result[1].Code);
    }

    [Fact]
    public async Task TenantBAndActorBDoNotJoinTenantAWork()
    {
        var foreignTenant = Attempt() with { ObservedTenantId = 200 };
        var foreignActor = Attempt() with { ObservedActorId = 20 };
        var result = await Worker().DrainReviewOnlyAsync(
            new[] { foreignTenant, foreignActor, Attempt() });
        Assert.Equal("ISOLATED_TENANT_MISMATCH", result[0].Code);
        Assert.Equal("ISOLATED_ACTOR_MISMATCH", result[1].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result[2].Code);
    }

    [Fact]
    public async Task StaleVersionCannotBeEnqueuedOrLeased()
    {
        var stale = Attempt() with { ObservedVersion = 6 };
        var changed = Attempt(Request() with { ExpectedVersion = 8 });
        var result = await Worker().DrainReviewOnlyAsync(new[] { stale, changed });
        Assert.All(result, x => Assert.Equal("ISOLATED_VERSION_MISMATCH", x.Code));
    }

    [Fact]
    public async Task RevocationOrMissingServiceGrantFailsClosed()
    {
        foreach (var policy in new[]
        {
            Snapshot() with { ActiveMembership = false },
            Snapshot() with { PermissionRevoked = true },
            Snapshot() with { ExplicitOperationPermission = false },
            Snapshot() with { IsServiceActor = true, ServiceGrantVerified = false }
        })
        {
            var result = await Worker(policy).DrainReviewOnlyAsync(new[] { Attempt() });
            Assert.Equal("PERMISSION_OR_MEMBERSHIP_DENIED", Assert.Single(result).Code);
        }
    }

    [Fact]
    public async Task FailedWorkerPolicyValidationDoesNotPoisonSubsequentAttempt()
    {
        var failed = Attempt(Request() with { Operation = "backup.restore" });
        var result = await Worker().DrainReviewOnlyAsync(new[] { failed, Attempt() });
        Assert.Equal("OPERATION_NOT_ALLOWLISTED", result[0].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result[1].Code);
    }

    [Fact]
    public async Task RetryExhaustionAndExpiredDeadlineRejectBeforeDispatch()
    {
        foreach (var policy in new[]
        {
            Snapshot() with { RetryAttempt = 3 },
            Snapshot() with { DeadlineUtc = Now }
        })
        {
            var result = await Worker(policy).DrainReviewOnlyAsync(new[] { Attempt() });
            Assert.Equal("TENANT_OR_POLICY_DENIED", Assert.Single(result).Code);
        }
    }

    [Fact]
    public async Task InvalidRequestAndPayloadCannotReserveIdempotencyKey()
    {
        var bad = Attempt(Request() with { PayloadSha256 = "bad" });
        var result = await Worker().DrainReviewOnlyAsync(new[] { bad, Attempt() });
        Assert.Equal("REQUEST_PARAMETERS_INVALID", result[0].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result[1].Code);
    }

    [Fact]
    public async Task NullEmptyOrOverCapacityBatchNeverProcessesExternalWork()
    {
        var w = Worker();
        Assert.Equal("ISOLATED_BATCH_SIZE_DENIED",
            Assert.Single(await w.DrainReviewOnlyAsync(null)).Code);
        Assert.Equal("ISOLATED_BATCH_SIZE_DENIED",
            Assert.Single(await w.DrainReviewOnlyAsync(
                Array.Empty<C1004CacheJobsIsolatedReviewWorker.Attempt?>())).Code);
        var many = Enumerable.Repeat<C1004CacheJobsIsolatedReviewWorker.Attempt?>(
            Attempt(), 33).ToArray();
        Assert.Equal("ISOLATED_BATCH_SIZE_DENIED",
            Assert.Single(await w.DrainReviewOnlyAsync(many)).Code);
        Assert.False(w.RunsExternalWorker);
    }

    [Fact]
    public async Task NullEntriesAreRejectedWithoutAbortingOtherIndependentReviews()
    {
        var batch = new C1004CacheJobsIsolatedReviewWorker.Attempt?[]
        { null, Attempt() };
        var result = await Worker().DrainReviewOnlyAsync(batch);
        Assert.Equal("ISOLATED_ATTEMPT_REQUIRED", result[0].Code);
        Assert.Equal("ISOLATED_PREFLIGHT_NO_DISPATCH", result[1].Code);
    }

    [Fact]
    public async Task CancellationNeverStartsQueueDispatchOrRestoration()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var w = Worker();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await w.DrainReviewOnlyAsync(new[] { Attempt() }, cts.Token));
        Assert.False(w.UsesDurableQueue);
    }
}
