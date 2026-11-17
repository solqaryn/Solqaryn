using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C12-04.M03.06: fail-closed in-process synthetic manifest audit.
/// No MySQL, HTTP, real backup/restore, production or durable storage.
/// The caller must supply a Review created in a trusted server context.
/// </summary>
public sealed class C1204SyntheticManifestAuditSink
{
    public const int AuditEventId = 12040306;
    private static readonly EventId AuditEvent = new(AuditEventId, "C1204SyntheticManifestReview");

    // Exactly one IsEnabled precheck. Delegates must not check a second time.
    private static readonly Action<ILogger, int, Exception?> Staged =
        LoggerMessage.Define<int>(LogLevel.Information, AuditEvent,
            "C1204 synthetic manifest review matched in process. Examined={Examined}",
            new LogDefineOptions { SkipEnabledCheck = true });
    private static readonly Action<ILogger, string, Exception?> Denied =
        LoggerMessage.Define<string>(LogLevel.Warning, AuditEvent,
            "C1204 synthetic manifest review denied. Reason={Reason}",
            new LogDefineOptions { SkipEnabledCheck = true });

    private readonly ILogger<C1204SyntheticManifestAuditSink> _logger;

    public C1204SyntheticManifestAuditSink(ILogger<C1204SyntheticManifestAuditSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public readonly record struct Receipt(string Code, bool LogInvoked)
    {
        public bool PersistsDurably => false;
        public bool CertifiesMysqlBackupOrRestore => false;
        public bool AuthorizesSqlOrCutover => false;
        public bool GrantsTenantAccess => false;
        public bool CertifiesJwtOrHttp => false;
        public bool AuthorizesQaOrProduction => false;
    }

    public Receipt Record(C1204SyntheticManifestReviewWorker.Review review,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var matched = review.Code == "SYNTHETIC_MANIFEST_REVIEW_ONLY_NO_MYSQL_PROOF";
        var denied = review.Code is "CURRENT_VERIFIED_SERVER_ACTOR_REQUIRED"
            or "INVALID_SYNTHETIC_MANIFEST_BATCH"
            or "INVALID_OR_CROSS_TENANT_MANIFEST"
            or "SYNTHETIC_SOURCE_UNAVAILABLE"
            or "SYNTHETIC_MANIFEST_NOT_VERIFIED";

        if ((!matched && !denied) ||
            (matched && (review.Examined < 1 ||
                         review.Examined > C1204SyntheticManifestReviewWorker.MaximumBatch)) ||
            (denied && review.Examined != 0))
            return new("SYNTHETIC_AUDIT_INVALID_OUTCOME", false);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var level = matched ? LogLevel.Information : LogLevel.Warning;
            if (!_logger.IsEnabled(level))
                return new("SYNTHETIC_AUDIT_SINK_DISABLED", false);

            // No actor, tenant, SHA, manifest key, exception or free-form input.
            if (matched)
                Staged(_logger, review.Examined, null);
            else
                Denied(_logger, review.Code, null);

            return new(matched ? "SYNTHETIC_AUDIT_ATTEMPTED_NO_MYSQL_PROOF"
                : "SYNTHETIC_AUDIT_DENIAL_ATTEMPTED", true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new("SYNTHETIC_AUDIT_SINK_UNAVAILABLE", false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("SYNTHETIC_AUDIT_SINK_UNAVAILABLE", false);
        }
    }
}
