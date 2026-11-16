using Microsoft.Extensions.Logging;
using Gate = Solqaryn.Application.AutomationV5.V5TenancySecurityAuthorizationGate;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-03.M03.06: a bounded, non-authorizing structured audit boundary.
/// A caller must obtain context from a trusted backend source; this component
/// does not establish identity, persist an event, grant access, or register DI.
/// </summary>
public sealed class V5TenancySecurityStructuredAuditSink
{
    public sealed record TrustedAuditContext(
        int VerifiedTenantId,
        int ObservedTenantId,
        int VerifiedActorId,
        string? SourceSha,
        Guid CorrelationId,
        bool BackendEvidenceVerified);

    public readonly record struct Receipt(string Code)
    {
        public bool WasRecorded =>
            Code is "AUDIT_RECORDED_NON_AUTHORIZING" or "UNVERIFIED_CONTEXT_DENIED_AND_RECORDED";
        public bool GrantsResourceRead => false;
        public bool GrantsFileDownload => false;
        public bool GrantsBackgroundJob => false;
        public bool GrantsControlPlane => false;
        public bool GrantsInstitutionalWrite => false;
        public bool AllowsTenantFilterBypass => false;
        public bool CertifiesDurableAudit => false;
        public bool AuthorizesQaOrProduction => false;
    }

    private static readonly EventId SecurityAuditEvent = new(150306, "V5SecurityAudit");
    private readonly ILogger<V5TenancySecurityStructuredAuditSink> _logger;

    public V5TenancySecurityStructuredAuditSink(
        ILogger<V5TenancySecurityStructuredAuditSink> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records ONLY fixed, allowlisted fields. Never serialize Request,
    /// Result.Code, exception text, idempotency keys, JWT, PII or secrets.
    /// A log accepted by ILogger is not a durable delivery guarantee.
    /// </summary>
    public Receipt Record(
        Gate.Request? request,
        Gate.Result result,
        TrustedAuditContext? context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Both the caller-supplied request and its *separate* trusted observation
        // must match. Even success is just preflight, never an authorization.
        var verified = request is not null &&
            request.ResourceId > 0 &&
            Enum.IsDefined(request.Surface) &&
            context is not null &&
            context.BackendEvidenceVerified &&
            context.VerifiedTenantId > 0 &&
            context.VerifiedActorId > 0 &&
            context.ObservedTenantId == context.VerifiedTenantId &&
            context.CorrelationId != Guid.Empty &&
            FullSha(context.SourceSha) &&
            string.Equals(request.ExpectedSourceSha, context.SourceSha, StringComparison.Ordinal) &&
            !request.RequestsTenantFilterBypass &&
            !request.AttemptsCrossTenantEnumeration &&
            !request.RequestsInstitutionalWrite;

        var outcome = !verified
            ? "INVALID_TRUSTED_CONTEXT"
            : result.NeedsAuthenticatedExecutionAdapter
                ? "PREFLIGHT_NON_AUTHORIZING"
                : "DENIED_NON_AUTHORIZING";

        // No IDs, serialized claims, client-supplied free text, or raw errors.
        var correlation = verified ? context!.CorrelationId.ToString("N") : "REDACTED";
        var sourceSha = verified ? context!.SourceSha! : "REDACTED";

        try
        {
            if (!_logger.IsEnabled(LogLevel.Information))
                return new("AUDIT_SINK_UNAVAILABLE");

            _logger.LogInformation(
                SecurityAuditEvent,
                "V5 security audit {AuditOutcome} {CorrelationId} {SourceSha} {ScopeVerified}",
                outcome, correlation, sourceSha, verified);

            return verified
                ? new("AUDIT_RECORDED_NON_AUTHORIZING")
                : new("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Never expose sink failures or exception data to a user or caller.
            return new("AUDIT_SINK_UNAVAILABLE");
        }
    }

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}
