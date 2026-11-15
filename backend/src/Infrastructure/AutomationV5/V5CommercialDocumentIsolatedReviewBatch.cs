using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C07-04.M03.05: bounded non-hosted worker-like REVIEW only. Does not enqueue
/// a transaction, retain PII, issue an invoice, charge, export or contact PSP.
/// A nominal result demands a separately authenticated durable adapter.
/// </summary>
public sealed class V5CommercialDocumentIsolatedReviewBatch
{
    private readonly V5CommercialDocumentIsolatedReadAdapter _reader;
    public const int MaximumBatch = 16;
    public V5CommercialDocumentIsolatedReviewBatch(V5CommercialDocumentIsolatedReadAdapter reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Ticket(int DocumentId, Rules.Command? Command, string? SourceSha);
    public readonly record struct Receipt(int Position, string Code)
    {
        public bool GrantsPayment => false;
        public bool GrantsFiscalIssuance => false;
        public bool GrantsMutation => false;
        public bool GrantsExport => false;
        public bool GrantsTransaction => false;
        public bool CertifiesLiveJwtMySqlOrPsp => false;
    }
    public sealed record BatchResult(string Code, IReadOnlyList<Receipt> Receipts)
    {
        public bool IsDurable => false;
        public bool IsHostedWorker => false;
        public bool AuthorizesCommercialAction => false;
    }

    public async Task<BatchResult> InspectBatchAsync(
        IReadOnlyList<Ticket?>? tickets, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tickets is null || tickets.Count == 0 || tickets.Count > MaximumBatch)
            return new("REVIEW_BATCH_SIZE_REQUIRED", Array.Empty<Receipt>());

        // Keys are scoped to a single synthetic batch; this is NOT a durable
        // idempotency guarantee, nor is it suitable for payment or fiscal release.
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        var results = new List<Receipt>(tickets.Count);
        for (var i = 0; i < tickets.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ticket = tickets[i];
            if (ticket is null || ticket.DocumentId <= 0 ||
                ticket.Command is null || !FullSha(ticket.SourceSha) ||
                string.IsNullOrWhiteSpace(ticket.Command.IdempotencyKey) ||
                ticket.Command.IdempotencyKey.Length > 128 ||
                !FullDigest(ticket.Command.PayloadSha256))
            {
                results.Add(new(i, "REVIEW_TICKET_INVALID"));
                continue;
            }

            var key = ticket.Command.IdempotencyKey;
            var fingerprint = string.Join("|",
                ticket.DocumentId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ticket.SourceSha, (int)ticket.Command.RequestedOperation,
                ticket.Command.ExpectedVersion,
                ticket.Command.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ticket.Command.Currency, ticket.Command.PayloadSha256);
            if (keys.TryGetValue(key, out var prior))
            {
                results.Add(new(i, string.Equals(prior, fingerprint, StringComparison.Ordinal)
                    ? "REVIEW_DUPLICATE_NO_ACTION" : "REVIEW_IDEMPOTENCY_CONFLICT"));
                continue;
            }
            keys.Add(key, fingerprint);

            // Delegates current authority, tenant ownership and revocation to
            // the already-implemented isolated reader, which can only preflight.
            var decision = await _reader.InspectAsync(ticket.DocumentId,
                ticket.Command, ticket.SourceSha, cancellationToken);
            results.Add(new(i, decision.NeedsDurableAuthenticatedTransaction
                ? "REVIEW_NEEDS_SEPARATE_DURABLE_ADAPTER"
                : "REVIEW_DENIED_" + decision.Code));
        }

        return new("REVIEW_RESULTS_ONLY_NO_LIVE_EFFECT",
            results.AsReadOnly());
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
    private static bool FullDigest(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
}