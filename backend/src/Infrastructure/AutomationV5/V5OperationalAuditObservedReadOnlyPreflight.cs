using Microsoft.Extensions.Logging;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-05.M03.06: bounded structured logging for the existing read-only audit
/// gateway. No raw tenant, subject, provider response, SHA or exception is logged.
/// </summary>
public sealed class V5OperationalAuditObservedReadOnlyPreflight
{
    private static readonly EventId Evaluated = new(150506, "V5AuditPreflightEvaluated");
    private static readonly EventId ProviderUnavailable = new(150507, "V5AuditProviderUnavailable");
    private static readonly EventId Cancelled = new(150508, "V5AuditPreflightCancelled");
    private static readonly EventId UnexpectedFailure = new(150509, "V5AuditObservationFailed");

    private readonly V5OperationalAuditReadOnlyProviderGateway _gateway;
    private readonly ILogger<V5OperationalAuditObservedReadOnlyPreflight> _logger;

    public V5OperationalAuditObservedReadOnlyPreflight(
        IV5OperationalAuditReadOnlyEvidenceProvider provider,
        ILogger<V5OperationalAuditObservedReadOnlyPreflight> logger)
    {
        _gateway = new V5OperationalAuditReadOnlyProviderGateway(
            provider ?? throw new ArgumentNullException(nameof(provider)));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<V5OperationalAuditReadOnlyProviderGateway.Outcome> PrepareAsync(
        V5OperationalAuditSecurityGate.Context? security,
        string? expectedHeadSha,
        string? expectedSourceRevision,
        IReadOnlySet<string>? expectedAuthorizedTaskIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gateway.PrepareReadOnlyAsync(
                security, expectedHeadSha, expectedSourceRevision,
                expectedAuthorizedTaskIds, cancellationToken);

            // The structured argument is from a finite internal vocabulary.
            var kind = Classify(result.Code);
            if (kind == "PROVIDER_UNAVAILABLE")
                _logger.LogWarning(ProviderUnavailable,
                    "V5 operational audit preflight {OutcomeClass}", kind);
            else
                _logger.LogInformation(Evaluated,
                    "V5 operational audit preflight {OutcomeClass}", kind);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(Cancelled,
                "V5 operational audit preflight {OutcomeClass}", "CANCELLED");
            throw;
        }
        catch (Exception)
        {
            // No exception details or credentials enter telemetry or return values.
            _logger.LogError(UnexpectedFailure,
                "V5 operational audit preflight {OutcomeClass}", "FAILED_SAFE");
            return new V5OperationalAuditReadOnlyProviderGateway.Outcome(
                "AUDIT_OBSERVATION_UNAVAILABLE", 0);
        }
    }

    private static string Classify(string? code) => code switch
    {
        "AUTHENTICATED_EVIDENCE_REQUIRED" => "NEEDS_EXTERNAL_VERIFICATION",
        "EVIDENCE_PROVIDER_UNAVAILABLE" => "PROVIDER_UNAVAILABLE",
        "VERIFIED_SECURITY_CONTEXT_REQUIRED"
            or "TENANT_MEMBERSHIP_MISMATCH"
            or "REVOKED_OR_INACTIVE_MEMBERSHIP"
            or "QUERY_FILTER_BYPASS_DENIED"
            or "INSTITUTIONAL_WRITE_OUT_OF_SCOPE"
            or "EVIDENCE_PROVIDER_TENANT_MISMATCH"
            or "JAVIER_SIX_TASK_CATALOG_REQUIRED"
            or "TASK_CATALOG_MISMATCH"
            or "CHECK_SHA_MISMATCH"
            or "SOURCE_REVISION_MISMATCH" => "DENIED",
        _ => "REVIEW_REQUIRED"
    };
}