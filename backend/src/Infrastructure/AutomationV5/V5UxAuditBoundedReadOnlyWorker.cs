using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-04.M03.05: bounded synchronous, read-only worker over isolated UX audit
/// snapshots. This is not a HostedService, queue consumer, scheduler, JWT
/// verifier, database reader, browser measurement or institutional writer.
/// </summary>
public sealed class V5UxAuditBoundedReadOnlyWorker
{
    public sealed record Candidate(
        string? IdempotencyKey,
        V5UxAuditApplicationService.Request? Request,
        V5UxAuditAuthorizedApplicationService.TrustedAuthority? Authority,
        string? ObservedSourceSha,
        string? ObservedTenantScope);

    public sealed record ItemOutcome(int Index, string Code, int ObservationsReviewed);

    public sealed record BatchOutcome(
        string Code,
        int ItemsEvaluated,
        IReadOnlyList<ItemOutcome> Items)
    {
        public bool GrantsAccess => false;
        public bool AllowsPersistence => false;
        public bool AllowsInstitutionalWrite => false;
        public bool CertifiesExternalMeasurement => false;
        public bool AuthorizesProduction => false;
        public bool CertifiesA5 => false;
    }

    private static BatchOutcome Deny(string code) =>
        new(code, 0, Array.Empty<ItemOutcome>());

    public BatchOutcome EvaluateReadOnly(
        IReadOnlyCollection<Candidate>? candidates,
        CancellationToken cancellationToken = default)
    {
        if (candidates is null || candidates.Count is < 1 or > 32)
            return Deny("BATCH_CAPACITY_DENIED");

        if (cancellationToken.IsCancellationRequested)
            return Deny("CANCELLED_NO_WRITE");

        // Snapshot before iteration: no item can be appended while reviewing.
        var snapshot = candidates.ToArray();
        if (snapshot.Length is < 1 or > 32)
            return Deny("BATCH_CAPACITY_DENIED");

        var first = snapshot[0];
        if (first is null ||
            !FullSha(first.ObservedSourceSha) ||
            string.IsNullOrWhiteSpace(first.ObservedTenantScope))
            return Deny("VERIFIED_BATCH_CONTEXT_REQUIRED");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in snapshot)
        {
            if (cancellationToken.IsCancellationRequested)
                return Deny("CANCELLED_NO_WRITE");

            if (item is null || !ValidKey(item.IdempotencyKey) ||
                !seen.Add(item.IdempotencyKey!))
                return Deny("INVALID_OR_DUPLICATE_WORK_KEY");

            // Reject mixed tenants and commits before evaluating any item;
            // never merge measurements across security boundaries.
            if (!string.Equals(item.ObservedTenantScope,
                    first.ObservedTenantScope, StringComparison.Ordinal) ||
                !string.Equals(item.ObservedSourceSha,
                    first.ObservedSourceSha, StringComparison.Ordinal))
                return Deny("MIXED_TENANT_OR_SOURCE_BATCH");

            if (item.Request is null)
                return Deny("INVALID_WORK_SNAPSHOT");
        }

        var results = new List<ItemOutcome>(snapshot.Length);
        for (var index = 0; index < snapshot.Length; index++)
        {
            if (cancellationToken.IsCancellationRequested)
                return Deny("CANCELLED_NO_WRITE");

            var item = snapshot[index];
            V5UxAuditAuthorizedApplicationService.Result check;
            try
            {
                var adapter = new V5UxAuditIsolatedSnapshotAdapter(item.Request!);
                check = adapter.PrepareReadOnly(
                    item.Authority, item.ObservedSourceSha,
                    item.ObservedTenantScope);
            }
            catch (ArgumentException)
            {
                return Deny("INVALID_WORK_SNAPSHOT");
            }

            // Outcome reveals neither tenant, actor, SHA, raw evidence nor key.
            results.Add(new ItemOutcome(index, check.Code, check.ItemsReviewed));
        }

        return new BatchOutcome(
            results.All(r => r.Code == "EXTERNAL_EVIDENCE_AND_A5_REQUIRED")
                ? "STRUCTURAL_REVIEW_ONLY"
                : "SOME_ITEMS_DENIED",
            results.Count,
            Array.AsReadOnly(results.ToArray()));
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    private static bool ValidKey(string? value) =>
        value is { Length: > 0 and <= 64 } &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
