namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C14-04.M03.05. A trusted DEV inbox exposes bounded review candidates.
/// An actual provider must authenticate its source; no implementation or
/// scheduler is registered here. This contract cannot grant a deployment.
/// </summary>
public interface IV5ReleaseReadinessReviewInbox
{
    ValueTask<V5ReleaseReadinessBoundedReviewWorker.Batch?> ReadCurrentBatchAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Manually invoked, one-shot bounded read-only integration into the existing
/// authenticated release snapshot adapter. No queue acknowledgement, retry,
/// dead-letter write, provider transport, timer or persistent deduplication.
/// </summary>
public sealed class V5ReleaseReadinessBoundedReviewWorker
{
    private readonly IV5ReleaseReadinessReviewInbox _inbox;
    private readonly V5ReleaseReadinessIsolatedSnapshotAdapter _inspector;

    public V5ReleaseReadinessBoundedReviewWorker(
        IV5ReleaseReadinessReviewInbox inbox,
        V5ReleaseReadinessIsolatedSnapshotAdapter inspector)
    {
        _inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
    }

    public sealed record WorkItem(
        string? WorkItemId, string? IdempotencyKey, string? ReleaseId,
        string? ExactCommitSha);

    public sealed record Batch(
        bool SourceAuthenticated, string? Environment,
        IReadOnlyList<WorkItem?>? PendingItems);

    public readonly record struct Outcome(string Code, int ItemsInspected)
    {
        public bool NeedsIndependentExternalReview =>
            Code == "EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED";
        public bool GrantsQaOrProduction => false;
        public bool AuthorizesDeployment => false;
        public bool AcknowledgesQueueItems => false;
        public bool CommitsDurableIdempotency => false;
        public bool CertifiesRestore => false;
        public bool ExposesTenantData => false;
    }

    private static bool LowerSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool SafeKey(string? value, int maxLength) =>
        value is { Length: >= 4 } && value.Length <= maxLength &&
        value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_');

    private static bool SafeRelease(string? value) =>
        value is { Length: >= 3 and <= 64 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.') &&
        !value.Contains("..", StringComparison.Ordinal);

    public async Task<Outcome> ProcessOnceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Batch? batch;
        try
        {
            batch = await _inbox.ReadCurrentBatchAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUTHENTICATED_REVIEW_INBOX_UNAVAILABLE", 0);
        }

        var items = batch?.PendingItems;
        if (batch is null || !batch.SourceAuthenticated ||
            !string.Equals(batch.Environment, "dev", StringComparison.Ordinal) ||
            items is null || items.Count is < 1 or > 16)
            return new("AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED", 0);

        // Prevalidate the ENTIRE batch before a single privileged read:
        // no item can cause partial processing after a duplicate or bad SHA.
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (item is null || !SafeKey(item.WorkItemId, 64) ||
                !SafeKey(item.IdempotencyKey, 128) ||
                !SafeRelease(item.ReleaseId) || !LowerSha(item.ExactCommitSha))
                return new("INVALID_READ_ONLY_WORK_ITEM", 0);
            if (!ids.Add(item.WorkItemId!) || !keys.Add(item.IdempotencyKey!))
                return new("DUPLICATE_WORK_ITEM_IN_BATCH", 0);
        }

        var inspected = 0;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            V5ReleaseReadinessIsolatedSnapshotAdapter.Verdict verdict;
            try
            {
                verdict = await _inspector.InspectAsync(
                    new V5ReleaseReadinessIsolatedSnapshotAdapter.Request(
                        item!.ReleaseId, item.ExactCommitSha), cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return new("READ_ONLY_SNAPSHOT_INSPECTION_UNAVAILABLE", inspected);
            }
            inspected++;
            if (!verdict.NeedsExternalAuthenticatedReview)
                return new("REVIEW_EVIDENCE_DENIED", inspected);
        }

        // No release, queue acknowledgement, transport, durable replay guard,
        // cross-run exactly-once guarantee, restoration or production activity.
        return new("EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED", inspected);
    }
}
