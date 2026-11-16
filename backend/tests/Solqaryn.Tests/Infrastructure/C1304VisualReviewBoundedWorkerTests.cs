using System.Collections;
using System.Collections.Generic;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1304VisualReviewBoundedWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Source : IC1304VisualReviewAuthoritySource
    {
        public C1304VisualReviewAuthorityAdapter.Authority? Current =
            new(8, 11, true, true, true, false, true, Sha, Now.AddMinutes(5),
                new HashSet<string>(StringComparer.Ordinal) { "private-shell", "visual-audit" });

        public Task<C1304VisualReviewAuthorityAdapter.Authority?> ReadCurrentAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Current);
        }
    }

    private sealed class Port : IC1304VisualReviewSnapshotReadPort
    {
        public int Calls;
        public readonly List<(int TenantId, string Key, string Sha)> Seen = new();
        public Func<int, string, string, CancellationToken,
            Task<C1304VisualReviewSnapshotAdapter.Outcome>>? Handler;

        public Task<C1304VisualReviewSnapshotAdapter.Outcome> ReadAsync(
            int tenantId, string key, string expectedSha, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            Seen.Add((tenantId, key, expectedSha));
            return Handler?.Invoke(tenantId, key, expectedSha, cancellationToken)
                ?? Task.FromResult(Staged(tenantId, key, expectedSha));
        }
    }

    private static C1304VisualReviewSnapshotAdapter.Outcome Staged(
        int tenantId, string key, string sourceSha) =>
        new("SNAPSHOT_STAGED_BROWSER_A5_REQUIRED", 1,
            new C1304VisualReviewSnapshotAdapter.Snapshot(
                tenantId, key, sourceSha, 1, Now,
                Array.AsReadOnly(new[] { "private-shell" })));

    private static C1304VisualReviewBoundedWorker.Ticket T(
        string key = "review-one", int tenantId = 11, string sha = Sha,
        bool requestsPublication = false) =>
        new(tenantId, key, sha, requestsPublication);

    private sealed class LyingBatch : IReadOnlyList<C1304VisualReviewBoundedWorker.Ticket>
    {
        public int Count => 1;
        public C1304VisualReviewBoundedWorker.Ticket this[int index] => T();
        public IEnumerator<C1304VisualReviewBoundedWorker.Ticket> GetEnumerator()
        {
            yield return T();
            yield return T("review-two");
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public async Task ActualInProcessAdapterRechecksTrustedAuthorityForEachRead()
    {
        var source = new Source();
        var store = new C1304VisualReviewSnapshotAdapter(source, new Clock());
        var saved = await store.WriteAsync(11, "review-one", Sha, 0,
            new[] { "private-shell" });
        Assert.Equal("SNAPSHOT_STAGED_BROWSER_A5_REQUIRED", saved.Code);

        var worker = new C1304VisualReviewBoundedWorker(
            new C1304VisualReviewSnapshotReadPort(store));
        var good = await worker.InspectBatchAsync(new[] { T() });
        Assert.Equal("VISUAL_REVIEW_STAGED_BROWSER_A5_REQUIRED", good.Code);
        Assert.Equal(1, good.ItemsChecked);
        Assert.False(good.GrantsJwtOrRbac);
        Assert.False(good.GrantsTenantAccess);
        Assert.False(good.PersistsDurably);
        Assert.False(good.CertifiesBrowserOrAccessibility);
        Assert.False(good.StartsHostedWorker);
        Assert.False(good.AuthorizesQaOrProduction);

        source.Current = source.Current! with { RoleRevoked = true };
        var denied = await worker.InspectBatchAsync(new[] { T() });
        Assert.Equal("VISUAL_REVIEW_SOURCE_REJECTED", denied.Code);
        Assert.Equal(0, denied.ItemsChecked);
    }

    [Fact]
    public async Task PreflightRejectsInvalidPublicationAndMixedTenantBeforeAnyPortCall()
    {
        var port = new Port();
        var worker = new C1304VisualReviewBoundedWorker(port);
        foreach (var batch in new C1304VisualReviewBoundedWorker.Ticket[][]
        {
            new[] { T(requestsPublication: true) },
            new[] { T(tenantId: 0) },
            new[] { T(key: "../private") },
            new[] { T(sha: "not-a-sha") },
            new[] { T("review-one"), T("review-two", tenantId: 22) },
            new[] { T("review-one"), T("review-two", sha: Sha.ToUpperInvariant()) },
            new[] { T("review-one"), T("review-one") },
            Array.Empty<C1304VisualReviewBoundedWorker.Ticket>(),
            Enumerable.Range(0, C1304VisualReviewBoundedWorker.MaximumBatch + 1)
                .Select(i => T("review-" + i)).ToArray()
        })
        {
            var result = await worker.InspectBatchAsync(batch);
            Assert.NotEqual("VISUAL_REVIEW_STAGED_BROWSER_A5_REQUIRED", result.Code);
            Assert.Equal(0, result.ItemsChecked);
        }
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task LyingCountAndOversizedEnumerationAreRejectedBeforeAnyRead()
    {
        var port = new Port();
        var worker = new C1304VisualReviewBoundedWorker(port);
        var result = await worker.InspectBatchAsync(new LyingBatch());
        Assert.Equal("VISUAL_REVIEW_INVALID_BATCH", result.Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task MutableCallerListIsSnapshottedBeforeFirstAwait()
    {
        var port = new Port();
        var tickets = new List<C1304VisualReviewBoundedWorker.Ticket>
        {
            T("review-one"), T("review-two")
        };
        port.Handler = (tenant, key, sha, token) =>
        {
            if (key == "review-one") tickets[1] = T("review-two", tenantId: 22);
            return Task.FromResult(Staged(tenant, key, sha));
        };
        var result = await new C1304VisualReviewBoundedWorker(port)
            .InspectBatchAsync(tickets);
        Assert.Equal("VISUAL_REVIEW_STAGED_BROWSER_A5_REQUIRED", result.Code);
        Assert.Equal(2, result.ItemsChecked);
        Assert.Equal(2, port.Calls);
        Assert.All(port.Seen, seen => Assert.Equal(11, seen.TenantId));
        Assert.Equal(new[] { "review-one", "review-two" },
            port.Seen.Select(x => x.Key).ToArray());
    }

    [Fact]
    public async Task SourceFailureIsOpaqueAndStopsRemainingReads()
    {
        var port = new Port
        {
            Handler = (_, _, _, _) =>
                Task.FromException<C1304VisualReviewSnapshotAdapter.Outcome>(
                    new InvalidOperationException("SECRET_TENANT_DSN"))
        };
        var result = await new C1304VisualReviewBoundedWorker(port)
            .InspectBatchAsync(new[] { T("review-one"), T("review-two") });
        Assert.Equal("VISUAL_REVIEW_SOURCE_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("SECRET", result.Code);
        Assert.Equal(0, result.ItemsChecked);
        Assert.Equal(1, port.Calls);
    }

    [Fact]
    public async Task ForgedSuccessAndWrongTenantOrShaNeverCountAsReviewed()
    {
        var port = new Port
        {
            Handler = (_, key, _, _) =>
                Task.FromResult(Staged(22, key, Sha))
        };
        var worker = new C1304VisualReviewBoundedWorker(port);
        Assert.Equal("VISUAL_REVIEW_SOURCE_REJECTED",
            (await worker.InspectBatchAsync(new[] { T() })).Code);
        port.Handler = (tenant, key, _, _) =>
            Task.FromResult(Staged(tenant, key, Sha.ToUpperInvariant()));
        Assert.Equal("VISUAL_REVIEW_SOURCE_REJECTED",
            (await worker.InspectBatchAsync(new[] { T() })).Code);
        port.Handler = (_, _, _, _) =>
            Task.FromResult(new C1304VisualReviewSnapshotAdapter.Outcome(
                "SNAPSHOT_NOT_FOUND", 0, null));
        Assert.Equal("VISUAL_REVIEW_SOURCE_REJECTED",
            (await worker.InspectBatchAsync(new[] { T() })).Code);
    }

    [Fact]
    public async Task OutcomeRevisionMustEqualSnapshotRevisionBeforeAnySuccessfulReview()
    {
        var port = new Port();
        var worker = new C1304VisualReviewBoundedWorker(port);
        // Adversarial read port claims staged success while its outer revision
        // disagrees with the payload. Both mismatch directions fail closed.
        port.Handler = (tenant, key, sha, _) =>
        {
            var staged = Staged(tenant, key, sha);
            return Task.FromResult(staged with { Revision = staged.Revision + 1 });
        };
        var higherOutcome = await worker.InspectBatchAsync(new[] { T() });
        Assert.Equal("VISUAL_REVIEW_SOURCE_REJECTED", higherOutcome.Code);
        Assert.Equal(0, higherOutcome.ItemsChecked);

        port.Handler = (tenant, key, sha, _) =>
        {
            var staged = Staged(tenant, key, sha);
            return Task.FromResult(staged with { Data = staged.Data! with { Revision = staged.Revision + 1 } });
        };
        var higherPayload = await worker.InspectBatchAsync(new[] { T() });
        Assert.Equal("VISUAL_REVIEW_SOURCE_REJECTED", higherPayload.Code);
        Assert.Equal(0, higherPayload.ItemsChecked);
        Assert.Equal(2, port.Calls);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutAccessToPort()
    {
        var port = new Port();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new C1304VisualReviewBoundedWorker(port).InspectBatchAsync(
                new[] { T() }, cts.Token));
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task MidReadCancellationPropagatesAndCannotCountSuccess()
    {
        using var cts = new CancellationTokenSource();
        var port = new Port
        {
            Handler = (_, _, _, _) =>
                Task.FromException<C1304VisualReviewSnapshotAdapter.Outcome>(
                    new OperationCanceledException(cts.Token))
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new C1304VisualReviewBoundedWorker(port).InspectBatchAsync(
                new[] { T() }, cts.Token));
        Assert.Equal(1, port.Calls);
    }
}
