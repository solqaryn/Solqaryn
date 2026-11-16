using System.Collections.Generic;
using System.Linq;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Read-only port for one current, isolated visual-review snapshot.
/// A real tenant/JWT authority and durable provider are deliberately absent.
/// </summary>
public interface IC1304VisualReviewSnapshotReadPort
{
    Task<C1304VisualReviewSnapshotAdapter.Outcome> ReadAsync(
        int tenantId, string key, string expectedSha, CancellationToken cancellationToken);
}

/// <summary>Adapter to the already existing in-process, authority-rechecking snapshot store.</summary>
public sealed class C1304VisualReviewSnapshotReadPort : IC1304VisualReviewSnapshotReadPort
{
    private readonly C1304VisualReviewSnapshotAdapter _adapter;

    public C1304VisualReviewSnapshotReadPort(C1304VisualReviewSnapshotAdapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public Task<C1304VisualReviewSnapshotAdapter.Outcome> ReadAsync(
        int tenantId, string key, string expectedSha, CancellationToken cancellationToken) =>
        _adapter.ReadAsync(tenantId, key, expectedSha, cancellationToken);
}

/// <summary>
/// C13-04.M03.05: bounded, sequential, in-process review of synthetic snapshots.
/// No hosted worker, queue, real provider, MySQL, HTTP, grants or promotion.
/// </summary>
public sealed class C1304VisualReviewBoundedWorker
{
    public const int MaximumBatch = 8;
    private readonly IC1304VisualReviewSnapshotReadPort _port;

    public C1304VisualReviewBoundedWorker(IC1304VisualReviewSnapshotReadPort port) =>
        _port = port ?? throw new ArgumentNullException(nameof(port));

    public sealed record Ticket(int TenantId, string? Key, string? SourceSha,
        bool RequestsPublication = false);

    public readonly record struct Review(string Code, int ItemsChecked)
    {
        public bool GrantsJwtOrRbac => false;
        public bool GrantsTenantAccess => false;
        public bool PersistsDurably => false;
        public bool CertifiesBrowserOrAccessibility => false;
        public bool StartsHostedWorker => false;
        public bool AuthorizesQaOrProduction => false;
    }

    private static Review Deny(string code) => new(code, 0);
    private static bool ValidKey(string? s) =>
        s is { Length: >= 3 and <= 64 } &&
        s.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
    private static bool ValidSha(string? s) =>
        s is { Length: 40 } &&
        s.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    public async Task<Review> InspectBatchAsync(
        IReadOnlyList<Ticket>? untrusted, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (untrusted is null) return Deny("VISUAL_REVIEW_INVALID_BATCH");

        // Snapshot caller-owned collection once, before any asynchronous authority read.
        // Do not trust Count alone: reject lying, changing, or throwing enumerators.
        Ticket[] batch;
        try
        {
            var count = untrusted.Count;
            if (count is < 1 or > MaximumBatch) return Deny("VISUAL_REVIEW_INVALID_BATCH");
            var captured = new List<Ticket>(count);
            foreach (var ticket in untrusted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (captured.Count >= count) return Deny("VISUAL_REVIEW_INVALID_BATCH");
                captured.Add(ticket);
            }
            if (captured.Count != count) return Deny("VISUAL_REVIEW_INVALID_BATCH");
            batch = captured.ToArray();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("VISUAL_REVIEW_INVALID_BATCH");
        }

        if (batch[0] is null) return Deny("VISUAL_REVIEW_INVALID_TICKET");
        var tenant = batch[0].TenantId;
        var sha = batch[0].SourceSha;
        var distinct = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ticket in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ticket is null || ticket.TenantId <= 0 ||
                !ValidKey(ticket.Key) || !ValidSha(ticket.SourceSha) ||
                ticket.RequestsPublication)
                return Deny("VISUAL_REVIEW_INVALID_TICKET");
            if (ticket.TenantId != tenant ||
                !string.Equals(ticket.SourceSha, sha, StringComparison.Ordinal))
                return Deny("VISUAL_REVIEW_MIXED_TENANT_OR_SOURCE");
            if (!distinct.Add(ticket.Key!))
                return Deny("VISUAL_REVIEW_DUPLICATE_TICKET");
        }

        // Every read rechecks the current trusted source via the existing adapter.
        // Never return the surfaces or imply browser A5/real provider certification.
        foreach (var ticket in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C1304VisualReviewSnapshotAdapter.Outcome result;
            try
            {
                result = await _port.ReadAsync(
                    tenant, ticket.Key!, sha!, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return Deny("VISUAL_REVIEW_SOURCE_UNAVAILABLE");
            }
            if (result.Code != "SNAPSHOT_STAGED_BROWSER_A5_REQUIRED" ||
                result.Revision <= 0 || result.Data is null ||
                result.Data.Revision != result.Revision ||
                result.Data.TenantId != tenant ||
                !string.Equals(result.Data.Key, ticket.Key, StringComparison.Ordinal) ||
                !string.Equals(result.Data.SourceSha, sha, StringComparison.Ordinal))
                return Deny("VISUAL_REVIEW_SOURCE_REJECTED");
        }

        return new("VISUAL_REVIEW_STAGED_BROWSER_A5_REQUIRED", batch.Length);
    }
}
