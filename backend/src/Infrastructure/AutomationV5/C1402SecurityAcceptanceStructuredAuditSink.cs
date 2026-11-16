using Microsoft.Extensions.Logging;
using Gate = Solqaryn.Application.AutomationV5.C1402SecurityAcceptanceAuthorizationGate;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C14-02.M03.06: bounded structured security acceptance audit, no live provider.
/// A client cannot establish evidence by setting these fields; actual trusted
/// source verification and persistence belong to a separate authorized scope.
/// </summary>
public sealed class C1402SecurityAcceptanceStructuredAuditSink
{
    private static readonly EventId AuditEvent = new(140206, "V5SecurityAcceptanceAudit");
    private readonly ILogger<C1402SecurityAcceptanceStructuredAuditSink> _logger;

    public sealed record ObservedContext(
        string? VerifiedResourceKey,
        string? SourceSha,
        int BackendActorId,
        int VerifiedTenantId,
        int ObservedTenantId,
        Guid CorrelationId,
        bool CurrentBackendEvidenceVerified);

    public readonly record struct Receipt(string Code)
    {
        public bool LoggerAcceptedEvent =>
            Code is "SAFE_AUDIT_PREVIEW_RECORDED" or "SAFE_AUDIT_DENIAL_RECORDED";
        public bool GrantsAccess => false;
        public bool GrantsFileOrJob => false;
        public bool GrantsInstitutionalWrite => false;
        public bool AllowsRestorationOrDeployment => false;
        public bool CertifiesSecurityAcceptance => false;
        public bool CertifiesDurableAudit => false;
    }

    public C1402SecurityAcceptanceStructuredAuditSink(
        ILogger<C1402SecurityAcceptanceStructuredAuditSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Never serialize raw request/evidence, user-provided reason, token, URL,
    /// sensitive identifiers or exception message. ILogger is not durable audit.
    /// A successful preflight never authorizes any security-sensitive action.
    /// </summary>
    public Receipt Record(
        Gate.Request? request,
        Gate.Decision decision,
        ObservedContext? observed,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var verified = request is not null &&
            observed is not null &&
            observed.CurrentBackendEvidenceVerified &&
            SafeResourceKey(request.ResourceKey) &&
            string.Equals(observed.VerifiedResourceKey, request.ResourceKey,
                StringComparison.Ordinal) &&
            FullSha(request.ExpectedCommitSha) &&
            string.Equals(request.ExpectedCommitSha, observed.SourceSha,
                StringComparison.Ordinal) &&
            observed.BackendActorId > 0 &&
            observed.VerifiedTenantId > 0 &&
            observed.ObservedTenantId == observed.VerifiedTenantId &&
            observed.CorrelationId != Guid.Empty &&
            Enum.IsDefined(request.Surface) &&
            !request.ClientControlsTenantOrRole &&
            !request.RequestsBypassOrRestore;

        var outcome = !verified
            ? "REJECTED_UNTRUSTED_AUDIT"
            : decision.RequiresSeparateAuthenticatedAdapter
                ? "PREFLIGHT_ONLY_NO_PERMISSION"
                : "DENIED_NO_PERMISSION";

        var correlation = verified ? observed!.CorrelationId.ToString("N") : "REDACTED";
        try
        {
            if (!_logger.IsEnabled(LogLevel.Information))
                return new("AUDIT_LOGGER_UNAVAILABLE");

            // Fixed schema: no request key, actor ID, tenant ID, raw decision
            // code, original exceptions, idempotency keys, claims or PII.
            _logger.LogInformation(
                AuditEvent,
                "V5 security acceptance {AuditOutcome} {CorrelationId} {ContextVerified}",
                outcome, correlation, verified);

            return new(verified ? "SAFE_AUDIT_PREVIEW_RECORDED" : "SAFE_AUDIT_DENIAL_RECORDED");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_LOGGER_UNAVAILABLE");
        }
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool SafeResourceKey(string? value) =>
        value is { Length: >= 1 and <= 96 } && value.All(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c is '-' or '_');
}
