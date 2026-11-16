using Microsoft.Extensions.Logging;
using Queue = Solqaryn.Infrastructure.AutomationV5.C0202PlacementIsolatedDispatchQueue;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-02.M03.06: redact-only audit of synthetic placement rehearsal results.
/// No durable log, real authorization, DB router, hosted worker or cutover.
/// </summary>
public sealed class C0202PlacementStructuredAuditSink
{
    private static readonly EventId AuditEvent = new(202206, "V5PlacementSyntheticAudit");
    private readonly ILogger<C0202PlacementStructuredAuditSink> _logger;

    /// <summary>
    /// Caller is responsible for independent authenticated backend provenance.
    /// Even true synthetic proof flags are NOT a substitute for live JWT/RBAC,
    /// DB ownership, durable receipts or migration authority.
    /// </summary>
    public sealed record ObservedContext(
        string? VerifiedSyntheticTenant,
        string? ObservedSyntheticTenant,
        string? VerifiedSyntheticActor,
        string? ObservedSyntheticActor,
        string? ExpectedSourceSha,
        string? ObservedSourceSha,
        Guid CorrelationId,
        bool IsolatedBackendEvidenceConfirmed,
        bool CurrentMembershipVerified,
        bool PermissionRevoked,
        bool ClientRequestsDsnOrCutover);

    public readonly record struct Receipt(string Code)
    {
        public bool LoggerAcceptedEvent =>
            Code is "SAFE_PLACEMENT_PREVIEW_AUDIT_RECORDED" or
                "SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED";
        public bool GrantsTenantAccess => false;
        public bool GrantsJwtOrRbac => false;
        public bool StartsWorkerOrQueue => false;
        public bool SelectsDatabase => false;
        public bool AllowsMigrationOrCutover => false;
        public bool CertifiesMySql => false;
        public bool CertifiesDurableAudit => false;
        public bool AllowsReleaseOrDeployment => false;
    }

    public C0202PlacementStructuredAuditSink(
        ILogger<C0202PlacementStructuredAuditSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Receipt Record(
        Queue.Outcome outcome,
        ObservedContext? context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scopeValid = context is not null &&
            SafeId(context.VerifiedSyntheticTenant) &&
            SafeId(context.VerifiedSyntheticActor) &&
            string.Equals(context.VerifiedSyntheticTenant,
                context.ObservedSyntheticTenant, StringComparison.Ordinal) &&
            string.Equals(context.VerifiedSyntheticActor,
                context.ObservedSyntheticActor, StringComparison.Ordinal) &&
            FullSha(context.ExpectedSourceSha) &&
            string.Equals(context.ExpectedSourceSha,
                context.ObservedSourceSha, StringComparison.Ordinal) &&
            context.CorrelationId != Guid.Empty &&
            context.IsolatedBackendEvidenceConfirmed &&
            context.CurrentMembershipVerified &&
            !context.PermissionRevoked &&
            !context.ClientRequestsDsnOrCutover;

        var allowedCode = KnownResult(outcome.Code);
        var consistentCount = outcome.SyntheticQueuedCount is >= 0 and <= 1 &&
            (!string.Equals(outcome.Code, "QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER",
                StringComparison.Ordinal) || outcome.SyntheticQueuedCount == 1);

        var trusted = scopeValid && allowedCode && consistentCount;
        var preview = trusted &&
            string.Equals(outcome.Code, "QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER",
                StringComparison.Ordinal);
        var outcomeCode = !trusted
            ? "UNTRUSTED_PLACEMENT_AUDIT_DENIED"
            : preview
                ? "SYNTHETIC_PREFLIGHT_ONLY_NO_AUTHORITY"
                : "PLACEMENT_PREVIEW_DENIED_NO_AUTHORITY";
        var correlation = trusted ? context!.CorrelationId.ToString("N") : "REDACTED";
        var count = trusted ? outcome.SyntheticQueuedCount : 0;

        try
        {
            if (!_logger.IsEnabled(LogLevel.Information))
                return new Receipt("PLACEMENT_AUDIT_LOGGER_UNAVAILABLE");

            // Fixed, minimal log schema. No raw payload, tenant, actor, SHA,
            // idempotency key, DSN, token, exception, error string or PII.
            _logger.LogInformation(AuditEvent,
                "V5 placement audit {Outcome} {CorrelationId} {SyntheticCount}",
                outcomeCode, correlation, count);
            return new Receipt(preview
                ? "SAFE_PLACEMENT_PREVIEW_AUDIT_RECORDED"
                : "SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new Receipt("PLACEMENT_AUDIT_LOGGER_UNAVAILABLE");
        }
    }

    private static bool SafeId(string? value) =>
        value is { Length: >= 2 and <= 128 } && value.All(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool KnownResult(string? code) => code is
        "QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER" or
        "QUEUE_UNTRUSTED_OR_LIVE_COMMAND_DENIED" or
        "QUEUE_INVALID_BINDING_DENIED" or
        "QUEUE_IDEMPOTENT_REPLAY_NO_EFFECT" or
        "QUEUE_REPLAY_CONFLICT" or
        "QUEUE_SINGLE_ATTEMPT_CAPACITY" or
        "QUEUE_PREFLIGHT_DENIED" or
        "QUEUE_INSPECTION_DENIED" or
        "QUEUE_SNAPSHOT_ONLY";
}
