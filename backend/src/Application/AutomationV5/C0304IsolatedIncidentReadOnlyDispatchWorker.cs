using System.Collections.Generic;
using Rules = Solqaryn.Domain.AutomationV5.V5ContinuityIncidentDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C03-04.M03.05: bounded, isolated incident queue preview.
/// This is not a hosted worker, authenticated identity, data repository,
/// backup/restore runner, or authority to disclose incident evidence.
/// </summary>
public sealed class C0304IsolatedIncidentReadOnlyDispatchWorker
{
    public const int MaximumBatchSize = 24;

    public sealed record WorkTicket(
        string? IdempotencyKey,
        int ClaimedTenantId,
        int? ClaimedBranchId,
        string? ClaimedSourceSha,
        C0304IsolatedIncidentPersistenceProjectionAdapter.StoredMetadata? Metadata,
        bool IsolatedServerEvidenceConfirmed,
        Rules.IncidentAction Action);

    public readonly record struct PreviewReceipt(string? IdempotencyKey, string Code)
    {
        public bool Dispatched => false;
        public bool AuthorizesEvidenceDisclosure => false;
        public bool AuthorizesIncidentMutation => false;
        public bool AuthorizesBackupRestore => false;
        public bool CertifiesJwtOrPersistence => false;
    }

    /// <summary>
    /// Evaluates a single finite in-memory batch. No I/O, retry, lease, secret,
    /// hosted scheduling, authorization, side effect or persistent receipt.
    /// A positive preview still requires independent authenticated runtime gates.
    /// </summary>
    public ValueTask<IReadOnlyList<PreviewReceipt>> PreviewAsync(
        IReadOnlyList<WorkTicket?>? tickets,
        int verifiedTenantId,
        int? verifiedBranchId,
        string? verifiedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tickets is null)
            return One("INCIDENT_BATCH_REQUIRED");
        if (tickets.Count is 0 or > MaximumBatchSize)
            return One("INCIDENT_BATCH_LIMIT_DENIED");
        if (verifiedTenantId <= 0 ||
            verifiedBranchId is <= 0 ||
            !CanonicalSha(verifiedSourceSha))
            return One("VERIFIED_INCIDENT_WORKER_SCOPE_REQUIRED");

        var receipts = new List<PreviewReceipt>(tickets.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ticket in tickets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ticket is null || !SafeKey(ticket.IdempotencyKey))
            {
                receipts.Add(new PreviewReceipt(ticket?.IdempotencyKey, "INCIDENT_TICKET_INVALID"));
                continue;
            }
            if (!seen.Add(ticket.IdempotencyKey!))
            {
                receipts.Add(new PreviewReceipt(ticket.IdempotencyKey, "INCIDENT_TICKET_DUPLICATE"));
                continue;
            }
            if (!ticket.IsolatedServerEvidenceConfirmed)
            {
                receipts.Add(new PreviewReceipt(ticket.IdempotencyKey, "INCIDENT_UNVERIFIED_WORK_SOURCE"));
                continue;
            }
            if (ticket.Action != Rules.IncidentAction.ReadEvidence)
            {
                receipts.Add(new PreviewReceipt(ticket.IdempotencyKey, "INCIDENT_MUTATION_OR_RESTORE_NOT_DISPATCHED"));
                continue;
            }
            if (ticket.ClaimedTenantId != verifiedTenantId)
            {
                receipts.Add(new PreviewReceipt(ticket.IdempotencyKey, "INCIDENT_CROSS_TENANT_QUEUE_DENIED"));
                continue;
            }
            if (ticket.ClaimedBranchId != verifiedBranchId)
            {
                receipts.Add(new PreviewReceipt(ticket.IdempotencyKey, "INCIDENT_CROSS_BRANCH_QUEUE_DENIED"));
                continue;
            }
            if (!string.Equals(ticket.ClaimedSourceSha, verifiedSourceSha,
                    StringComparison.Ordinal))
            {
                receipts.Add(new PreviewReceipt(ticket.IdempotencyKey, "INCIDENT_STALE_QUEUE_SHA_DENIED"));
                continue;
            }

            var projection = C0304IsolatedIncidentPersistenceProjectionAdapter.Project(
                ticket.Metadata, verifiedTenantId, verifiedBranchId, verifiedSourceSha);
            var code = projection.Code == "AUTHENTICATED_INCIDENT_GATE_REQUIRED"
                ? "AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED"
                : projection.Code;
            receipts.Add(new PreviewReceipt(ticket.IdempotencyKey, code));
        }

        return ValueTask.FromResult<IReadOnlyList<PreviewReceipt>>(receipts);
    }

    private static ValueTask<IReadOnlyList<PreviewReceipt>> One(string code) =>
        ValueTask.FromResult<IReadOnlyList<PreviewReceipt>>(
            new[] { new PreviewReceipt(null, code) });

    private static bool SafeKey(string? value) =>
        value is { Length: >= 1 and <= 64 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool CanonicalSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => (c >= '0' && c <= '9') ||
                       (c >= 'a' && c <= 'f'));
}
