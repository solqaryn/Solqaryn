using Microsoft.Extensions.Logging;
using Worker = Solqaryn.Application.AutomationV5.C0304IsolatedIncidentReadOnlyDispatchWorker;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C03-04.M03.06: bounded incident/continuity preflight audit. ILogger is
/// neither a durable audit store nor a verified incident authority.
/// No hosted worker, backup runner, provider, HTTP endpoint or DI registration.
/// </summary>
public sealed class C0304IncidentContinuityStructuredAuditSink
{
    private static readonly EventId AuditEvent = new(30406, "V5IncidentContinuityAudit");
    private readonly ILogger<C0304IncidentContinuityStructuredAuditSink> _logger;

    /// <summary>
    /// Inputs must eventually originate from independent authenticated backend
    /// sources; this record and a synthetic flag are NOT proof of real JWT,
    /// persisted ownership, RBAC, backup health, or evidence availability.
    /// </summary>
    public sealed record ObservedContext(
        int VerifiedTenantId,
        int ObservedTenantId,
        int BackendActorId,
        string? ExpectedCommitSha,
        string? ObservedCommitSha,
        Guid CorrelationId,
        bool CurrentBackendEvidenceConfirmed,
        bool PermissionRevoked,
        bool ClientClaimsAuthorityOrRestore);

    public readonly record struct AuditReceipt(string Code)
    {
        public bool LoggerAcceptedEvent => Code is
            "SAFE_INCIDENT_PREVIEW_AUDIT_RECORDED" or "SAFE_INCIDENT_DENIAL_AUDIT_RECORDED";
        public bool GrantsIncidentEvidence => false;
        public bool GrantsMutation => false;
        public bool GrantsBackupRestore => false;
        public bool StartsJobOrWorker => false;
        public bool CertifiesJwtOrPersistence => false;
        public bool CertifiesDurableAudit => false;
        public bool AllowsReleaseOrDeployment => false;
    }

    public C0304IncidentContinuityStructuredAuditSink(
        ILogger<C0304IncidentContinuityStructuredAuditSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Accepts ONLY isolated preview receipts, never raw incident, actor,
    /// tenant, idempotency, backup, JWT, error, exception or secret payloads.
    /// Always non-authorizing. Even a nominal preview needs a future
    /// authenticated server adapter for ANY incident effect or disclosure.
    /// </summary>
    public AuditReceipt RecordBatch(
        IReadOnlyList<Worker.PreviewReceipt>? previews,
        ObservedContext? context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var trustedScope = context is not null &&
            context.VerifiedTenantId > 0 &&
            context.ObservedTenantId == context.VerifiedTenantId &&
            context.BackendActorId > 0 &&
            context.CurrentBackendEvidenceConfirmed &&
            !context.PermissionRevoked &&
            !context.ClientClaimsAuthorityOrRestore &&
            context.CorrelationId != Guid.Empty &&
            CanonicalSha(context.ExpectedCommitSha) &&
            string.Equals(context.ExpectedCommitSha, context.ObservedCommitSha,
                StringComparison.Ordinal);

        var knownBatch = previews is { Count: > 0 } &&
            previews.Count <= Worker.MaximumBatchSize &&
            previews.All(r => KnownPreviewCode(r.Code));

        var previewOnly = knownBatch && previews!.All(r =>
            string.Equals(r.Code, "AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED",
                StringComparison.Ordinal));

        var outcome = !trustedScope || !knownBatch
            ? "UNVERIFIED_INCIDENT_AUDIT_DENIED"
            : previewOnly
                ? "INCIDENT_PREFLIGHT_ONLY_NO_PERMISSION"
                : "INCIDENT_PREVIEW_DENIED_NO_PERMISSION";

        var safeCorrelation = trustedScope && knownBatch
            ? context!.CorrelationId.ToString("N")
            : "REDACTED";
        var safeCount = trustedScope && knownBatch ? previews!.Count : 0;

        try
        {
            if (!_logger.IsEnabled(LogLevel.Information))
                return new AuditReceipt("INCIDENT_AUDIT_LOGGER_UNAVAILABLE");

            // Fixed schema only. No client-supplied code/key, actor, tenant,
            // incident, SHA, reason, token, exception or backup details.
            _logger.LogInformation(AuditEvent,
                "V5 incident continuity {AuditOutcome} {CorrelationId} {PreviewCount}",
                outcome, safeCorrelation, safeCount);

            return new AuditReceipt(trustedScope && knownBatch && previewOnly
                ? "SAFE_INCIDENT_PREVIEW_AUDIT_RECORDED"
                : "SAFE_INCIDENT_DENIAL_AUDIT_RECORDED");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never put the thrown exception, message or stack trace in a receipt.
            return new AuditReceipt("INCIDENT_AUDIT_LOGGER_UNAVAILABLE");
        }
    }

    private static bool CanonicalSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool KnownPreviewCode(string? code) => code is
        "AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED" or
        "INCIDENT_BATCH_REQUIRED" or
        "INCIDENT_BATCH_LIMIT_DENIED" or
        "VERIFIED_INCIDENT_WORKER_SCOPE_REQUIRED" or
        "INCIDENT_TICKET_INVALID" or
        "INCIDENT_TICKET_DUPLICATE" or
        "INCIDENT_UNVERIFIED_WORK_SOURCE" or
        "INCIDENT_MUTATION_OR_RESTORE_NOT_DISPATCHED" or
        "INCIDENT_CROSS_TENANT_QUEUE_DENIED" or
        "INCIDENT_CROSS_BRANCH_QUEUE_DENIED" or
        "INCIDENT_STALE_QUEUE_SHA_DENIED";
}
