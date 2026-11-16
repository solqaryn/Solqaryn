using Microsoft.Extensions.Logging;
using System.Linq;
using Batch = Solqaryn.Infrastructure.AutomationV5.V5CommercialDocumentIsolatedReviewBatch;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C07-04.M03.06: bounded structured audit of non-authorizing document review.
/// Not a hosted worker, payment service, fiscal issuer or durable audit sink.
/// </summary>
public interface IC0704CurrentCommercialAuditSource
{
    ValueTask<V5CommercialDocumentSafeAuditPipeline.AuditEvidence?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

public sealed class V5CommercialDocumentSafeAuditPipeline
{
    private readonly IC0704CurrentCommercialAuditSource _authority;
    private readonly Batch _review;
    private readonly ILogger<V5CommercialDocumentSafeAuditPipeline> _logger;

    public V5CommercialDocumentSafeAuditPipeline(
        IC0704CurrentCommercialAuditSource authority, Batch review,
        ILogger<V5CommercialDocumentSafeAuditPipeline> logger)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _review = review ?? throw new ArgumentNullException(nameof(review));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public sealed record AuditEvidence(
        int ActorId, int TenantId, int ResourceTenantId,
        bool BackendIdentityVerified, bool MembershipActive,
        bool CurrentRoleVerified, bool GrantRevoked, bool SourceAuthenticated,
        string? InternalCorrelationKey, string? SourceSha);

    public sealed record AuditResult(string Code, int ReceiptCount, bool LoggerAccepted)
    {
        public bool GrantsTenantOrResourceAccess => false;
        public bool GrantsPaymentOrFiscalIssuance => false;
        public bool StartsHostedWorkerOrTransaction => false;
        public bool CertifiesDurableAudit => false;
    }

    private static bool ValidSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeCorrelation(string? value) =>
        value is { Length: >= 8 and <= 96 } &&
        value.All(ch => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-');

    public async ValueTask<AuditResult> ReviewAndAuditAsync(
        IReadOnlyList<Batch.Ticket?>? tickets, int expectedTenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expectedTenantId <= 0 || tickets is null ||
            tickets.Count == 0 || tickets.Count > Batch.MaximumBatch)
            return new("AUDIT_BATCH_INVALID", 0, false);
        if (tickets.Any(t => t is null || t.DocumentId <= 0 ||
            t.Command is null || !ValidSha(t.SourceSha)))
            return new("AUDIT_TICKET_INVALID", 0, false);

        AuditEvidence? current;
        try
        {
            current = await _authority.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            try { _logger.LogWarning("C0704_AUDIT_AUTHORITY_UNAVAILABLE"); }
            catch (Exception logError) when (logError is not OutOfMemoryException) { }
            return new("AUDIT_AUTHORITY_UNAVAILABLE", 0, false);
        }

        if (current is null || current.ActorId <= 0 ||
            current.TenantId <= 0 || current.ResourceTenantId != current.TenantId ||
            expectedTenantId != current.TenantId ||
            !current.BackendIdentityVerified || !current.MembershipActive ||
            !current.CurrentRoleVerified || current.GrantRevoked ||
            !current.SourceAuthenticated || !SafeCorrelation(current.InternalCorrelationKey) ||
            !ValidSha(current.SourceSha) ||
            tickets.Any(t => !string.Equals(t!.SourceSha, current.SourceSha,
                StringComparison.Ordinal)))
            return new("CURRENT_VERIFIED_COMMERCIAL_AUDIT_SCOPE_REQUIRED", 0, false);

        Batch.BatchResult result;
        try
        {
            result = await _review.InspectBatchAsync(tickets, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            try { _logger.LogWarning("C0704_AUDIT_REVIEW_UNAVAILABLE"); }
            catch (Exception logError) when (logError is not OutOfMemoryException) { }
            return new("AUDIT_REVIEW_UNAVAILABLE", 0, false);
        }

        // Never log actor, tenant, document ID, SHA, money, client-supplied
        // idempotency key, bearer token, exception or private payload.
        var complete = result.Receipts.Count == tickets.Count &&
            result.Receipts.All(r => r.Code == "REVIEW_NEEDS_SEPARATE_DURABLE_ADAPTER");
        var status = complete ? "REVIEW_ONLY" : "REVIEW_DENIED_OR_DUPLICATE";
        try
        {
            _logger.LogInformation(
                "C0704_SAFE_AUDIT event={EventCode} correlation={CorrelationKey} count={TicketCount} status={StatusCode}",
                "C0704_REVIEW", current.InternalCorrelationKey, result.Receipts.Count, status);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_LOG_SINK_UNAVAILABLE", 0, false);
        }
        return new(complete ? "AUDIT_REVIEW_ONLY_NO_LIVE_EFFECT" :
            "AUDIT_REVIEW_DENIED_NO_LIVE_EFFECT", result.Receipts.Count, true);
    }
}
