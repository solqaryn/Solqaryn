using Gate = Solqaryn.Application.AutomationV5.V5GlobalControlPlaneAuthorizationGate;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-04.M03.05: bounded isolated read-only control-plane work preview.
/// Not a hosted worker, external queue, credential source or access grant.
/// </summary>
public sealed class V5GlobalControlPlaneIsolatedDispatchWorker
{
    public const int MaximumBatchSize = 24;
    private readonly V5GlobalControlPlaneIsolatedSnapshotAdapter _adapter;

    public V5GlobalControlPlaneIsolatedDispatchWorker(
        V5GlobalControlPlaneIsolatedSnapshotAdapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public sealed record Ticket(
        string? IdempotencyKey,
        string? ClaimedActorId,
        string? ClaimedTenantId,
        string? ClaimedSourceSha,
        Gate.Request? Request,
        bool IsolatedBackendProvenanceClaim);

    public readonly record struct Receipt(string? IdempotencyKey, string Code)
    {
        public bool Dispatched => false;
        public bool GrantsAccess => false;
        public bool GrantsPlatformDirectory => false;
        public bool GrantsExceptionalLease => false;
        public bool MutatesInstitutionalData => false;
        public bool CertifiesAuthenticatedRuntime => false;
    }

    /// <summary>
    /// Preview only. Each result is a rejection or a requirement for a
    /// separately authenticated backend adapter; never a permission.
    /// Deduplication is in-memory, per invocation, not durable idempotency.
    /// </summary>
    public async ValueTask<IReadOnlyList<Receipt>> PreviewAsync(
        IReadOnlyList<Ticket?>? tickets,
        string? verifiedActorId, string? verifiedTenantId,
        string? verifiedSourceSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tickets is null) return new[] { new Receipt(null, "CONTROL_PLANE_BATCH_REQUIRED") };
        if (tickets.Count is 0 or > MaximumBatchSize)
            return new[] { new Receipt(null, "CONTROL_PLANE_BATCH_LIMIT_DENIED") };
        if (!SafeId(verifiedActorId) || !SafeId(verifiedTenantId) ||
            !CanonicalSha(verifiedSourceSha))
            return new[] { new Receipt(null, "VERIFIED_CONTROL_PLANE_BATCH_SCOPE_REQUIRED") };

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var receipts = new List<Receipt>(tickets.Count);
        foreach (var ticket in tickets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ticket is null || !SafeKey(ticket.IdempotencyKey) || ticket.Request is null)
            {
                receipts.Add(new Receipt(ticket?.IdempotencyKey, "CONTROL_PLANE_TICKET_INVALID"));
                continue;
            }
            if (!seen.Add(ticket.IdempotencyKey!))
            {
                receipts.Add(new Receipt(ticket.IdempotencyKey, "CONTROL_PLANE_TICKET_DUPLICATE"));
                continue;
            }
            if (!ticket.IsolatedBackendProvenanceClaim)
            {
                receipts.Add(new Receipt(ticket.IdempotencyKey, "CONTROL_PLANE_UNVERIFIED_TICKET_SOURCE"));
                continue;
            }
            if (!string.Equals(ticket.ClaimedActorId, verifiedActorId, StringComparison.Ordinal))
            {
                receipts.Add(new Receipt(ticket.IdempotencyKey, "CONTROL_PLANE_BATCH_ACTOR_MISMATCH"));
                continue;
            }
            if (!string.Equals(ticket.ClaimedTenantId, verifiedTenantId, StringComparison.Ordinal) ||
                !string.Equals(ticket.Request.RequestedTenantId, verifiedTenantId,
                    StringComparison.Ordinal))
            {
                receipts.Add(new Receipt(ticket.IdempotencyKey, "CONTROL_PLANE_BATCH_TENANT_MISMATCH"));
                continue;
            }
            if (!string.Equals(ticket.ClaimedSourceSha, verifiedSourceSha,
                    StringComparison.Ordinal) ||
                !string.Equals(ticket.Request.ExpectedSourceSha, verifiedSourceSha,
                    StringComparison.Ordinal))
            {
                receipts.Add(new Receipt(ticket.IdempotencyKey, "CONTROL_PLANE_BATCH_STALE_SHA"));
                continue;
            }
            if (ticket.Request.WriteOrExport || ticket.Request.SensitiveData ||
                ticket.Request.ClientAuthorityHint)
            {
                receipts.Add(new Receipt(ticket.IdempotencyKey, "CONTROL_PLANE_UNSAFE_TICKET_DENIED"));
                continue;
            }

            // Exercise the actual Application + Domain preflight through M03.04.
            // A positive code is still NOT an access grant.
            var result = await _adapter.EvaluateReadOnlyAsync(
                ticket.Request, verifiedActorId, verifiedTenantId, verifiedSourceSha,
                cancellationToken);
            receipts.Add(new Receipt(ticket.IdempotencyKey, result.Code));
        }

        return receipts;
    }

    private static bool SafeKey(string? value) =>
        value is { Length: >= 1 and <= 64 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool SafeId(string? value) =>
        value is { Length: >= 1 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool CanonicalSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => (c >= '0' && c <= '9') ||
                       (c >= 'a' && c <= 'f'));
}
