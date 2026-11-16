using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;
using Inspector = Solqaryn.Application.AutomationV5.V5ReleaseReadinessIsolatedSnapshotAdapter;
using Worker = Solqaryn.Application.AutomationV5.V5ReleaseReadinessBoundedReviewWorker;

namespace Solqaryn.Tests.Application;

public sealed class V5ReleaseReadinessBoundedReviewWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private sealed class Sources : IV5ReadinessAuthenticatedCallerSource,
        IV5ReleaseReadinessVerifiedEvidenceSource, IV5ReleaseReadinessSnapshotReader
    {
        public V5ReadinessVerifiedCaller? Principal =
            new("reviewer", "tenant-A", "grant-dev", "dev", true, true, true);
        public Rules.VerificationContext? Authority =
            new("trusted-dev", Sha, "tenant-A",
                new HashSet<string> { "author", "reviewer" },
                new HashSet<string> { "scope-lock", "backend-tests" },
                new HashSet<string> { "grant-dev" });
        public Rules.Candidate? Candidate = ValidCandidate();
        public Inspector.Snapshot? Snapshot = ValidSnapshot();
        public Exception? Error;
        public int Reads;
        public string? LastTenant;

        public ValueTask<V5ReadinessVerifiedCaller?> GetAuthenticatedCallerAsync(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Principal);
        }
        public ValueTask<Rules.VerificationContext?> GetAuthorityAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Authority);
        }
        public ValueTask<Rules.Candidate?> GetCandidateAsync(string id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Candidate);
        }
        public ValueTask<Inspector.Snapshot?> ReadForTenantAsync(
            string tenant, string release, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            LastTenant = tenant;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static Rules.Candidate ValidCandidate() =>
        new("release-1", Sha, Sha, "sha256:" + new string('a', 64),
            "dev", "grant-dev", "tenant-A", "author", "reviewer",
            "rollback:revert", "stop:on-failure",
            new[] {
                new Rules.CheckResult("scope-lock", Sha, "completed", "success"),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            },
            new[] { new Rules.ObservedCase("AC-1", "dev:synthetic", true) });

    private static Inspector.Snapshot ValidSnapshot() =>
        new("release-1", Sha, "tenant-A",
            "sha256:" + new string('a', 64), "rollback:revert",
            4, true, true, true, true);

    private sealed class Inbox : IV5ReleaseReadinessReviewInbox
    {
        public Worker.Batch? Current = ValidBatch();
        public Exception? Error;
        public int Reads;
        public ValueTask<Worker.Batch?> ReadCurrentBatchAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Current);
        }
    }

    private static Worker.WorkItem Item(string id = "job-0001",
        string key = "idem-0001") => new(id, key, "release-1", Sha);
    private static Worker.Batch ValidBatch(params Worker.WorkItem?[] items) =>
        new(true, "dev", items.Length == 0 ? new Worker.WorkItem?[] { Item() } : items);

    private static (Worker Work, Inbox Queue, Sources Auth) Setup()
    {
        var inbox = new Inbox();
        var source = new Sources();
        var adapter = new Inspector(source,
            new V5ReleaseReadinessAuthorizationGate(source, source), source);
        return (new Worker(inbox, adapter), inbox, source);
    }

    [Fact]
    public async Task OneValidItemOnlyRequiresExternalReviewNeverRelease()
    {
        var (worker, _, auth) = Setup();
        var result = await worker.ProcessOnceAsync();
        Assert.Equal("EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsInspected);
        Assert.Equal("tenant-A", auth.LastTenant);
        Assert.Equal(1, auth.Reads);
        Assert.True(result.NeedsIndependentExternalReview);
        Assert.False(result.GrantsQaOrProduction);
        Assert.False(result.AuthorizesDeployment);
        Assert.False(result.AcknowledgesQueueItems);
        Assert.False(result.CommitsDurableIdempotency);
        Assert.False(result.CertifiesRestore);
        Assert.False(result.ExposesTenantData);
    }

    [Fact]
    public async Task MultipleUniqueItemsProcessedSequentiallyNotAcknowledged()
    {
        var (worker, inbox, auth) = Setup();
        inbox.Current = ValidBatch(Item(), Item("job-0002", "idem-0002"));
        var result = await worker.ProcessOnceAsync();
        Assert.Equal(2, result.ItemsInspected);
        Assert.Equal(2, auth.Reads);
        Assert.False(result.AcknowledgesQueueItems);
    }

    [Fact]
    public async Task NullAndUnauthenticatedInboxDoNotCallInspector()
    {
        var (worker, inbox, auth) = Setup();
        inbox.Current = null;
        Assert.Equal("AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED",
            (await worker.ProcessOnceAsync()).Code);
        inbox.Current = ValidBatch() with { SourceAuthenticated = false };
        Assert.Equal("AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED",
            (await worker.ProcessOnceAsync()).Code);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task QaProdOrUppercaseDevDoNotEnableWork()
    {
        var (worker, inbox, auth) = Setup();
        foreach (var env in new[] { "qa", "PROD", "DEV", "" })
        {
            inbox.Current = ValidBatch() with { Environment = env };
            Assert.Equal("AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED",
                (await worker.ProcessOnceAsync()).Code);
        }
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task EmptyAndSeventeenItemsAreRejectedBeforeReading()
    {
        var (worker, inbox, auth) = Setup();
        inbox.Current = new(true, "dev", Array.Empty<Worker.WorkItem?>());
        Assert.Equal("AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED",
            (await worker.ProcessOnceAsync()).Code);
        inbox.Current = new(true, "dev",
            Enumerable.Range(1, 17).Select(i => Item($"job-{i:0000}", $"idem-{i:0000}")).ToArray());
        Assert.Equal("AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED",
            (await worker.ProcessOnceAsync()).Code);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task DuplicateIdOrIdempotencyKeyRejectsEntireBatch()
    {
        var (worker, inbox, auth) = Setup();
        inbox.Current = ValidBatch(Item(), Item("job-0001", "idem-0002"));
        Assert.Equal("DUPLICATE_WORK_ITEM_IN_BATCH",
            (await worker.ProcessOnceAsync()).Code);
        inbox.Current = ValidBatch(Item(), Item("job-0002", "idem-0001"));
        Assert.Equal("DUPLICATE_WORK_ITEM_IN_BATCH",
            (await worker.ProcessOnceAsync()).Code);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task InvalidShaTraversalAndMalformedIdentifierAreDenied()
    {
        var (worker, inbox, auth) = Setup();
        inbox.Current = ValidBatch(Item() with { ExactCommitSha = Sha.ToUpperInvariant() });
        Assert.Equal("INVALID_READ_ONLY_WORK_ITEM",
            (await worker.ProcessOnceAsync()).Code);
        inbox.Current = ValidBatch(Item() with { ReleaseId = "../prod" });
        Assert.Equal("INVALID_READ_ONLY_WORK_ITEM",
            (await worker.ProcessOnceAsync()).Code);
        inbox.Current = ValidBatch(Item() with { WorkItemId = "job\nsecret" });
        Assert.Equal("INVALID_READ_ONLY_WORK_ITEM",
            (await worker.ProcessOnceAsync()).Code);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task TenantBReviewerAndRevokedMembershipAreDeniedByRealGate()
    {
        var (worker, _, auth) = Setup();
        auth.Principal = auth.Principal! with { TenantScope = "tenant-B" };
        Assert.Equal("REVIEW_EVIDENCE_DENIED",
            (await worker.ProcessOnceAsync()).Code);
        auth.Principal = auth.Principal! with { TenantScope = "tenant-A", IsMembershipActive = false };
        Assert.Equal("REVIEW_EVIDENCE_DENIED",
            (await worker.ProcessOnceAsync()).Code);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task CancelledOrSkippedChecksCannotBecomePass()
    {
        var (worker, _, auth) = Setup();
        foreach (var status in new[] { "cancelled", "skipped", "failure" })
        {
            auth.Candidate = ValidCandidate() with {
                Checks = new[] {
                    new Rules.CheckResult("scope-lock", Sha, "completed", status),
                    new Rules.CheckResult("backend-tests", Sha, "completed", "success")
                }
            };
            Assert.Equal("REVIEW_EVIDENCE_DENIED",
                (await worker.ProcessOnceAsync()).Code);
        }
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task OwnReviewerCannotSelfApproveRelease()
    {
        var (worker, _, auth) = Setup();
        auth.Candidate = ValidCandidate() with { AuthorTaskId = "reviewer" };
        Assert.Equal("REVIEW_EVIDENCE_DENIED",
            (await worker.ProcessOnceAsync()).Code);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task InboxFailureIsOpaqueAndNoTenantIsRead()
    {
        var (worker, inbox, auth) = Setup();
        inbox.Error = new InvalidOperationException("private provider token");
        var result = await worker.ProcessOnceAsync();
        Assert.Equal("AUTHENTICATED_REVIEW_INBOX_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("token", result.Code);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public async Task InspectFailureIsOpaqueAndNeverPromotes()
    {
        var (worker, _, auth) = Setup();
        auth.Error = new InvalidOperationException("private auth header");
        var result = await worker.ProcessOnceAsync();
        Assert.Equal("READ_ONLY_SNAPSHOT_INSPECTION_UNAVAILABLE", result.Code);
        Assert.False(result.AuthorizesDeployment);
        Assert.Equal(0, result.ItemsInspected);
    }

    [Fact]
    public async Task CancellationStopsBeforeAnyExternalRead()
    {
        var (worker, inbox, auth) = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            worker.ProcessOnceAsync(cts.Token));
        Assert.Equal(0, inbox.Reads);
        Assert.Equal(0, auth.Reads);
    }

    [Fact]
    public void MissingDependenciesAreRejected()
    {
        var (_, inbox, auth) = Setup();
        var inspector = new Inspector(auth,
            new V5ReleaseReadinessAuthorizationGate(auth, auth), auth);
        Assert.Throws<ArgumentNullException>(() =>
            new Worker(null!, inspector));
        Assert.Throws<ArgumentNullException>(() =>
            new Worker(inbox, null!));
    }
}
