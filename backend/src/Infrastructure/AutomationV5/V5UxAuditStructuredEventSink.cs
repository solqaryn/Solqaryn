using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-04.M03.06: structured, opaque UX-audit preflight logging.
/// The receipt confirms only an enabled in-process ILogger call, never
/// durable delivery, external browser evidence, tenant authentication or A5.
/// </summary>
public sealed class V5UxAuditStructuredEventSink
{
    public const int AuditEventId = 150406;
    private static readonly EventId AuditEvent = new(AuditEventId, "C1504UxAuditPreflight");

    // Record checks IsEnabled once. Delegates must not recheck it: stateful
    // loggers can change their answer and produce a false-positive receipt.
    private static readonly Action<ILogger, int, Exception?> Staged =
        LoggerMessage.Define<int>(LogLevel.Information, AuditEvent,
            "C1504 UX preflight awaiting independent evidence. ItemsEvaluated={ItemsEvaluated}",
            new LogDefineOptions { SkipEnabledCheck = true });

    private static readonly Action<ILogger, int, Exception?> Denied =
        LoggerMessage.Define<int>(LogLevel.Warning, AuditEvent,
            "C1504 UX preflight denied or unavailable. ItemsEvaluated={ItemsEvaluated}",
            new LogDefineOptions { SkipEnabledCheck = true });

    private readonly ILogger<V5UxAuditStructuredEventSink> _logger;

    public V5UxAuditStructuredEventSink(ILogger<V5UxAuditStructuredEventSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public readonly record struct Receipt(string Code, bool Recorded)
    {
        public bool PersistsDurably => false;
        public bool CertifiesBrowserMeasurement => false;
        public bool CertifiesA5 => false;
        public bool GrantsAccess => false;
        public bool AllowsInstitutionalWrite => false;
        public bool AuthorizesProduction => false;
    }

    public Receipt Record(V5UxAuditBoundedReadOnlyWorker.BatchOutcome? outcome,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool staged;
        int count;
        try
        {
            if (!Classify(outcome, out staged, out count))
                return new("UX_AUDIT_INVALID_OUTCOME", false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new("UX_AUDIT_INVALID_OUTCOME", false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException &&
                                   ex is not OperationCanceledException)
        {
            // An adversarial IReadOnlyList cannot leak its exception or be
            // interpreted as successful logging.
            return new("UX_AUDIT_INVALID_OUTCOME", false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var level = staged ? LogLevel.Information : LogLevel.Warning;
            if (!_logger.IsEnabled(level))
                return new("UX_AUDIT_SINK_DISABLED", false);

            if (staged)
                Staged(_logger, count, null);
            else
                Denied(_logger, count, null);

            return new(staged ? "UX_AUDIT_STAGED_NOT_CERTIFIED" : "UX_AUDIT_DENIED", true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A misbehaving provider must not expose its exception message.
            return new("UX_AUDIT_SINK_UNAVAILABLE", false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No provider exception, connection string, tenant, actor, SHA,
            // evidence reference or idempotency key reaches the receipt.
            return new("UX_AUDIT_SINK_UNAVAILABLE", false);
        }
    }

    private static bool Classify(V5UxAuditBoundedReadOnlyWorker.BatchOutcome? batch,
        out bool staged, out int count)
    {
        staged = false;
        count = 0;
        if (batch is null || batch.Items is null ||
            batch.ItemsEvaluated < 0 || batch.ItemsEvaluated > 32)
            return false;

        count = batch.ItemsEvaluated;
        if (batch.Code is "STRUCTURAL_REVIEW_ONLY" or "SOME_ITEMS_DENIED")
        {
            if (count == 0 || batch.Items.Count != count)
                return false;

            var hasDenial = false;
            for (var i = 0; i < count; i++)
            {
                var item = batch.Items[i];
                if (item is null || item.Index != i ||
                    string.IsNullOrWhiteSpace(item.Code) ||
                    item.ObservationsReviewed < 0 ||
                    item.ObservationsReviewed > 500)
                    return false;

                hasDenial |= item.Code != "EXTERNAL_EVIDENCE_AND_A5_REQUIRED";
            }

            staged = batch.Code == "STRUCTURAL_REVIEW_ONLY";
            return staged ? !hasDenial : hasDenial;
        }

        if (count != 0 || batch.Items.Count != 0)
            return false;

        return batch.Code is "BATCH_CAPACITY_DENIED"
            or "CANCELLED_NO_WRITE"
            or "INVALID_WORK_SNAPSHOT"
            or "VERIFIED_BATCH_CONTEXT_REQUIRED"
            or "INVALID_OR_DUPLICATE_WORK_KEY"
            or "MIXED_TENANT_OR_SOURCE_BATCH";
    }
}
