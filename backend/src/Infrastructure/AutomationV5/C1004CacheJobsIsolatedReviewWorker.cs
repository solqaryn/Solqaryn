using System.Threading.Channels;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C10-04.M03.05: bounded single-consumer in-memory REVIEW queue for cache/jobs/export.
/// This does not enqueue live work, acquire leases, connect to providers, restore
/// backups, export private artifacts or grant any execution permission.
/// A real worker needs its own authenticated provider, persistent CAS and audit.
/// </summary>
public sealed class C1004CacheJobsIsolatedReviewWorker
{
    private readonly C1004CacheJobsIsolatedSnapshotAdapter _adapter;
    public C1004CacheJobsIsolatedReviewWorker(C1004CacheJobsIsolatedSnapshotAdapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public sealed record Attempt(
        C1004CacheJobsApplicationService.Request? Request,
        string? ObservedWorkId,
        int ObservedTenantId,
        int ObservedActorId,
        long ObservedVersion);

    public readonly record struct Review(int Position, string Code)
    {
        public bool EnqueuedExternally => false;
        public bool LeaseAcquired => false;
        public bool MutatedOrRestoredData => false;
        public bool ExposedPrivateArtifact => false;
        public bool GrantsCrossTenantAccess => false;
    }

    public bool RunsExternalWorker => false;
    public bool UsesDurableQueue => false;
    public bool GrantsProviderAccess => false;
    public bool CertifiesMySqlOrRestore => false;

    public async Task<IReadOnlyList<Review>> DrainReviewOnlyAsync(
        IReadOnlyList<Attempt?>? batch, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batch is null || batch.Count is < 1 or > 32)
            return new[] { new Review(-1, "ISOLATED_BATCH_SIZE_DENIED") };

        // Bounded, ephemeral channel; all entries fit before the single consumer
        // starts. No persistent lease, retry daemon, scheduler or provider calls.
        var channel = Channel.CreateBounded<(int Index, Attempt? Value)>(
            new BoundedChannelOptions(32)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        for (var i = 0; i < batch.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!channel.Writer.TryWrite((i, batch[i])))
                throw new InvalidOperationException("Bounded queue rejected an in-range review.");
        }
        channel.Writer.TryComplete();

        var verdicts = new List<Review>(batch.Count);
        var seen = new Dictionary<(int TenantId, string WorkId, string Key), string>();
        await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
        {
            var item = entry.Value;
            if (item is null)
            {
                verdicts.Add(new(entry.Index, "ISOLATED_ATTEMPT_REQUIRED"));
                continue;
            }

            var decision = await _adapter.EvaluateReadOnlyAsync(
                item.Request, item.ObservedWorkId, item.ObservedTenantId,
                item.ObservedActorId, item.ObservedVersion, cancellationToken);
            if (decision.Code != "PRECONDITIONS_ONLY_EXTERNAL_CAS_REQUIRED")
            {
                verdicts.Add(new(entry.Index, decision.Code));
                continue;
            }

            // Idempotency key ownership is tenant + work specific. A retry with
            // identical digest is a no-op; changing payload under one key is
            // a conflict. All matching happens AFTER fresh policy inspection.
            var key = (
                item.ObservedTenantId,
                item.ObservedWorkId!,
                item.Request!.IdempotencyKey!);
            if (seen.TryGetValue(key, out var previousDigest))
            {
                verdicts.Add(new(entry.Index,
                    string.Equals(previousDigest, item.Request.PayloadSha256,
                        StringComparison.Ordinal)
                        ? "ISOLATED_DUPLICATE_NO_DISPATCH"
                        : "ISOLATED_IDEMPOTENCY_DIGEST_CONFLICT"));
                continue;
            }
            seen.Add(key, item.Request.PayloadSha256!);
            verdicts.Add(new(entry.Index, "ISOLATED_PREFLIGHT_NO_DISPATCH"));
        }

        return verdicts;
    }
}
