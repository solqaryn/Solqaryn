using System.Linq;
using Gate = Solqaryn.Application.AutomationV5.C0202PlacementApplicationGate;
using Rules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0202PlacementIsolatedDispatchQueueTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Tenant = "tenant-A";
    private const string Actor = "user-7";
    private static Rules.TrustedPlacementSnapshot Snapshot() => new()
    {
        VerifiedTenantId = Tenant, AuthenticatedActorId = Actor,
        JwtValidatedByBackend = true, CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true, CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true, AssignmentOwnerTenantId = Tenant,
        CurrentAssignmentRevision = 8, CurrentBindingAlias = "shared-cluster",
        BindingFromTrustedCatalog = true, UniqueActiveAssignmentVerified = true
    };
    private static Gate.Evidence Evidence() => new(
        Sha, Tenant, Tenant, "dedicated-tenant-A",
        true, true, true, true, true, true, true, true, true, true, Snapshot());
    private static Gate.Request Request() => new(
        Tenant, Sha, Rules.Intent.ProposeMigration, Rules.PlacementMode.Dedicated,
        "dedicated-tenant-A", 8, "idem-12345");
    private static C0202PlacementIsolatedDispatchQueue Queue(Gate.Evidence? e = null)
    {
        var adapter = new C0202PlacementIsolatedSnapshotAdapter(
            Tenant, Actor, Sha, 8, e ?? Evidence());
        var journal = new C0202PlacementSandboxRehearsalJournal(
            adapter, Tenant, Actor, Sha, 8, "shared-cluster");
        return new(journal, Tenant, Actor, Sha);
    }
    private static Task<C0202PlacementIsolatedDispatchQueue.Outcome> Enqueue(
        C0202PlacementIsolatedDispatchQueue q, Gate.Request? r = null,
        string tenant = Tenant, string actor = Actor, string sha = Sha,
        long rev = 8, string key = "idem-12345",
        bool endpoint = false, bool live = false,
        CancellationToken ct = default) =>
        q.EnqueueSyntheticAsync(r ?? Request(), tenant, actor, sha,
            rev, key, endpoint, live, ct);

    [Fact] public async Task SuccessfulRehearsalNeverGrantsLiveAuthority()
    {
        var q = Queue();
        var result = await Enqueue(q);
        Assert.Equal("QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER", result.Code);
        Assert.Equal(1, result.SyntheticQueuedCount);
        Assert.False(result.StartsHostedWorker);
        Assert.False(result.WritesDurableQueue);
        Assert.False(result.SelectsDatabaseConnection);
        Assert.False(result.AuthorizesMigrationOrCutover);
        Assert.False(result.GrantsJwtOrRbac);
        Assert.False(result.CertifiesMySqlOrProduction);
    }
    [Fact] public async Task TenantBAndActorBFailClosed()
    {
        var q = Queue();
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(q, tenant: "tenant-B")).Code);
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(q, actor: "user-8")).Code);
        Assert.Equal("QUEUE_INSPECTION_DENIED", q.InspectSynthetic("tenant-B", Actor).Code);
        Assert.Equal(0, q.InspectSynthetic(Tenant, Actor).SyntheticQueuedCount);
    }
    [Fact] public async Task StaleRevisionAndMismatchedShaCannotEnqueue()
    {
        var q = Queue();
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(q, rev: 7)).Code);
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(q, sha: new string('f', 40))).Code);
        Assert.Equal(0, q.InspectSynthetic(Tenant, Actor).SyntheticQueuedCount);
    }
    [Fact] public async Task ForgedEndpointLiveWorkerDsnAndCutoverAreDenied()
    {
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(Queue(), endpoint: true)).Code);
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(Queue(), live: true)).Code);
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(Queue(), Request() with { ClientSuppliesDsn = true })).Code);
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(Queue(), Request() with { RequestsLiveCutoverOrDualWrite = true })).Code);
    }
    [Fact] public async Task RevokedJwtRbacRestoreAndTenantBlockQueue()
    {
        foreach (var e in new[]
        {
            Evidence() with { CurrentJwtAndMembershipVerified = false },
            Evidence() with { FreshRelationalRbacVerified = false },
            Evidence() with { VerifiedSandboxBackupRestore = false },
            Evidence() with { Snapshot = Snapshot() with { TenantSuspendedOrRevoked = true } }
        })
            Assert.Equal("QUEUE_PREFLIGHT_DENIED", (await Enqueue(Queue(e))).Code);
    }
    [Fact] public async Task InvalidKeyAndClientBindingNeverStage()
    {
        var q = Queue();
        Assert.Equal("QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED",
            (await Enqueue(q, key: "wrong-key")).Code);
        Assert.Equal("QUEUE_INVALID_BINDING_DENIED",
            (await Enqueue(q, Request() with { TargetBindingAlias = "../dsn" })).Code);
        Assert.Equal(0, q.InspectSynthetic(Tenant, Actor).SyntheticQueuedCount);
    }
    [Fact] public async Task ReplayIsIdempotentButChangedIntentConflicts()
    {
        var q = Queue();
        Assert.Equal("QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER", (await Enqueue(q)).Code);
        Assert.Equal("QUEUE_IDEMPOTENT_REPLAY_NO_EFFECT", (await Enqueue(q)).Code);
        Assert.Equal("QUEUE_REPLAY_CONFLICT",
            (await Enqueue(q, Request() with { TargetMode = Rules.PlacementMode.Shared })).Code);
    }
    [Fact] public async Task OneAttemptRejectsNewIdempotencyKey()
    {
        var q = Queue();
        await Enqueue(q);
        Assert.Equal("QUEUE_SINGLE_ATTEMPT_CAPACITY",
            (await Enqueue(q, Request() with { IdempotencyKey = "idem-other" },
                key: "idem-other")).Code);
    }
    [Fact] public async Task ConcurrentDistinctKeysExactlyOneAccepted()
    {
        var q = Queue();
        var tasks = Enumerable.Range(0, 24).Select(i => Task.Run(async () =>
            await Enqueue(q, Request() with { IdempotencyKey = "idem-" + i },
                key: "idem-" + i)));
        var values = await Task.WhenAll(tasks);
        Assert.Single(values.Where(x => x.Code == "QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER"));
        Assert.Equal(1, q.InspectSynthetic(Tenant, Actor).SyntheticQueuedCount);
    }
    [Fact] public async Task CancellationDoesNotMutateQueue()
    {
        var q = Queue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Enqueue(q, ct: cts.Token));
        Assert.Equal(0, q.InspectSynthetic(Tenant, Actor).SyntheticQueuedCount);
    }
    [Fact] public void InvalidConstructorIdentityIsRejected()
    {
        var adapter = new C0202PlacementIsolatedSnapshotAdapter(
            Tenant, Actor, Sha, 8, Evidence());
        var journal = new C0202PlacementSandboxRehearsalJournal(
            adapter, Tenant, Actor, Sha, 8, "shared-cluster");
        Assert.Throws<ArgumentException>(() =>
            new C0202PlacementIsolatedDispatchQueue(journal, "../tenant", Actor, Sha));
        Assert.Throws<ArgumentException>(() =>
            new C0202PlacementIsolatedDispatchQueue(journal, Tenant, Actor, "bad"));
    }
}
