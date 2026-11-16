using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C13-04.M03.06: inert, structured audit boundary for a bounded synthetic visual review.
/// It never persists a record, emits caller-controlled text, or certifies browser A5.
/// </summary>
public sealed class C1304VisualReviewAuditSink
{
    public const int AuditEventId = 130406;
    private readonly ILogger<C1304VisualReviewAuditSink> _logger;
    private static readonly EventId AuditEvent = new(AuditEventId, "C1304VisualReview");

    private static readonly Action<ILogger, int, int, Exception?> Staged =
        LoggerMessage.Define<int, int>(LogLevel.Information, AuditEvent,
            "C1304 visual review staged for independent browser A5. TenantId={TenantId} ItemsChecked={ItemsChecked}");

    private static readonly Action<ILogger, int, Exception?> Rejected =
        LoggerMessage.Define<int>(LogLevel.Warning, AuditEvent,
            "C1304 visual review rejected or unavailable. TenantId={TenantId}");

    public C1304VisualReviewAuditSink(ILogger<C1304VisualReviewAuditSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public readonly record struct Receipt(string Code, bool Recorded)
    {
        public bool GrantsJwtOrRbac => false;
        public bool AuthorizesTenantAccess => false;
        public bool CertifiesBrowserOrAccessibility => false;
        public bool AuthorizesQaOrProduction => false;
        public bool PersistsDurably => false;
    }

    public Receipt Record(int tenantId, C1304VisualReviewBoundedWorker.Review review,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tenantId <= 0) return new("AUDIT_INVALID_TENANT", false);

        var staged = string.Equals(review.Code, "VISUAL_REVIEW_STAGED_BROWSER_A5_REQUIRED",
            StringComparison.Ordinal);
        if (staged && (review.ItemsChecked < 1 ||
                        review.ItemsChecked > C1304VisualReviewBoundedWorker.MaximumBatch))
            return new("AUDIT_INVALID_REVIEW", false);
        if (!staged && (review.ItemsChecked != 0 || !IsKnownRejection(review.Code)))
            return new("AUDIT_INVALID_REVIEW", false);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (staged)
                Staged(_logger, tenantId, review.ItemsChecked, null);
            else
                Rejected(_logger, tenantId, null);
            return new(staged ? "AUDIT_STAGED_NOT_CERTIFIED" : "AUDIT_REJECTED", true);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Do not leak logger exception, target, DSN or other provider details.
            return new("AUDIT_SINK_UNAVAILABLE", false);
        }
    }

    private static bool IsKnownRejection(string? code) =>
        code is "VISUAL_REVIEW_INVALID_BATCH"
            or "VISUAL_REVIEW_INVALID_TICKET"
            or "VISUAL_REVIEW_MIXED_TENANT_OR_SOURCE"
            or "VISUAL_REVIEW_DUPLICATE_TICKET"
            or "VISUAL_REVIEW_SOURCE_UNAVAILABLE"
            or "VISUAL_REVIEW_SOURCE_REJECTED";
}
