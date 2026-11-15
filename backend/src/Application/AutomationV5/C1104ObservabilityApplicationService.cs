using System.Text.RegularExpressions;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C11-04.M03.02: bounded private observability preflight. A trusted backend
/// evidence reader owns tenant, membership, RBAC, aggregate and audit facts.
/// This class neither queries telemetry nor grants API access or certifies SLOs.
/// </summary>
public interface IC1104ObservabilityEvidenceReader
{
    ValueTask<C1104ObservabilityApplicationService.VerifiedEvidence?> ReadAsync(
        string signalName, string correlationId, CancellationToken cancellationToken);
}

public sealed class C1104ObservabilityApplicationService
{
    private readonly IC1104ObservabilityEvidenceReader _reader;

    public C1104ObservabilityApplicationService(IC1104ObservabilityEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(
        string? SignalName, string? CorrelationId, string? ExpectedSourceSha,
        bool ClientTenantOverride = false, bool RequestsRawPayload = false,
        bool RequestsExporterOrSloCertification = false);

    public sealed record VerifiedEvidence(
        string? ObservedSourceSha, string? TrustedActorTenantId,
        string? PersistedResourceTenantId, bool FromTrustedBackend,
        bool CurrentJwtVerified, bool CurrentMembershipVerified,
        bool CurrentExplicitReadRbacVerified, bool AuditFenceAvailable,
        C1104ObservabilitySignalDomainRules.PrivateAggregate? Aggregate);

    public readonly record struct Result(string Code)
    {
        public bool RequiresAuthenticatedReadAdapter =>
            Code == "TRUSTED_PRIVATE_OBSERVABILITY_ADAPTER_REQUIRED";
        public bool GrantsTenantAccess => false;
        public bool ExportsTelemetry => false;
        public bool CertifiesSloOrCost => false;
        public bool WritesMetricsOrDatabase => false;
    }

    public async ValueTask<Result> PrepareAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The caller can only select a bounded signal/correlation; it cannot
        // nominate its own tenant, raw payload, provider or SLO certification.
        if (request is null || !FullSha(request.ExpectedSourceSha) ||
            string.IsNullOrWhiteSpace(request.SignalName) ||
            request.SignalName.Length > 64 ||
            string.IsNullOrWhiteSpace(request.CorrelationId) ||
            request.CorrelationId.Length > 64)
            return new("INVALID_REQUEST_OR_SOURCE_SHA");

        if (request.ClientTenantOverride || request.RequestsRawPayload ||
            request.RequestsExporterOrSloCertification)
            return new("UNTRUSTED_TENANT_RAW_OR_EXPORT_OVERRIDE");

        if (request.SignalName is not ("http_requests" or "request_latency" or
            "error_rate" or "db_duration"))
            return new("SIGNAL_NOT_ALLOWLISTED");

        if (!Regex.IsMatch(request.CorrelationId,
                @"\A[a-zA-Z0-9_.-]{1,64}\z", RegexOptions.CultureInvariant))
            return new("UNSAFE_CORRELATION");

        var evidence = await _reader.ReadAsync(
            request.SignalName, request.CorrelationId, cancellationToken);

        if (evidence is null ||
            !string.Equals(evidence.ObservedSourceSha, request.ExpectedSourceSha,
                StringComparison.Ordinal) ||
            evidence.Aggregate is null ||
            !string.Equals(evidence.Aggregate.SignalName, request.SignalName,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.Aggregate.CorrelationId, request.CorrelationId,
                StringComparison.Ordinal))
            return new("TRUSTED_AGGREGATE_SOURCE_REQUIRED");

        if (!evidence.FromTrustedBackend || !evidence.CurrentJwtVerified ||
            !evidence.CurrentMembershipVerified ||
            !evidence.CurrentExplicitReadRbacVerified ||
            !evidence.AuditFenceAvailable ||
            string.IsNullOrWhiteSpace(evidence.TrustedActorTenantId) ||
            !string.Equals(evidence.TrustedActorTenantId,
                evidence.PersistedResourceTenantId, StringComparison.Ordinal) ||
            !string.Equals(evidence.Aggregate.IdentityTenant,
                evidence.TrustedActorTenantId, StringComparison.Ordinal) ||
            !string.Equals(evidence.Aggregate.ResourceTenant,
                evidence.PersistedResourceTenantId, StringComparison.Ordinal))
            return new("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED");

        var assessment = C1104ObservabilitySignalDomainRules.Evaluate(evidence.Aggregate);
        if (!assessment.Allowed)
            return new(assessment.Code);

        // Domain validation does not authorize any actual read/export.
        // The future authenticated, audited adapter must enforce policy again.
        return new("TRUSTED_PRIVATE_OBSERVABILITY_ADAPTER_REQUIRED");
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
