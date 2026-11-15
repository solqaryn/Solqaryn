using Solqaryn.Application.AutomationV5;
using Domain = Solqaryn.Domain.AutomationV5.C1104ObservabilitySignalDomainRules;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C11-04.M03.04: isolated, immutable snapshot adapter for testing Application
/// authorization and Domain safe aggregate preflight. No registered DI, SQL,
/// telemetry exporter, SLO certification, HTTP endpoint or external provider.
/// </summary>
public sealed class C1104ObservabilityIsolatedSnapshotAdapter
{
    private readonly string _tenant;
    private readonly string _sourceSha;
    private readonly C1104VerifiedPrincipal? _principal;
    private readonly C1104ObservabilityApplicationService.VerifiedEvidence? _evidence;
    public C1104ObservabilityIsolatedSnapshotAdapter(
        string trustedSyntheticTenant, string sourceSha,
        C1104VerifiedPrincipal? principal,
        C1104ObservabilityApplicationService.VerifiedEvidence? evidence)
    {
        if (!SafeTenant(trustedSyntheticTenant) || !FullSha(sourceSha))
            throw new ArgumentException("Trusted synthetic tenant and SHA required.");
        _tenant = trustedSyntheticTenant;
        _sourceSha = sourceSha;
        _principal = principal is null ? null : principal with { };
        var aggregate = evidence?.Aggregate;
        var labels = aggregate?.Labels is null ? null :
            new Dictionary<string, string>(aggregate.Labels, StringComparer.Ordinal);
        _evidence = evidence is null ? null : evidence with
        {
            Aggregate = aggregate is null ? null : aggregate with { Labels = labels }
        };
    }

    public readonly record struct Outcome(string Code)
    {
        public bool RequiresSeparateAuthenticatedApi =>
            Code == "AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED";
        public bool GrantsTenantRead => false;
        public bool ExposesTelemetry => false;
        public bool WritesDatabase => false;
        public bool ExportsMetrics => false;
        public bool CertifiesSloOrCost => false;
        public bool HasRealJwtAuthentication => false;
    }

    public async Task<Outcome> InspectAsync(
        C1104ObservabilityAuthorizationGate.Request? request,
        string? observedSyntheticTenant, string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null ||
            !string.Equals(observedSyntheticTenant, _tenant, StringComparison.Ordinal) ||
            !string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(request.ExpectedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(_principal?.TenantScope, _tenant, StringComparison.Ordinal))
            return new("ISOLATED_OBSERVABILITY_TENANT_OR_SHA_MISMATCH");

        var gate = new C1104ObservabilityAuthorizationGate(
            new PrincipalPort(_principal), new EvidencePort(_tenant, _evidence));
        var result = await gate.EvaluateAsync(request, cancellationToken);
        return new(result.Code);
    }

    private sealed class PrincipalPort(C1104VerifiedPrincipal? principal)
        : IC1104VerifiedPrincipalSource
    {
        public ValueTask<C1104VerifiedPrincipal?> CurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(principal);
        }
    }

    private sealed class EvidencePort(
        string tenant,
        C1104ObservabilityApplicationService.VerifiedEvidence? evidence)
        : IC1104TenantPartitionedEvidenceSource
    {
        public ValueTask<C1104ObservabilityApplicationService.VerifiedEvidence?> ReadAsync(
            string verifiedTenantScope, string signalName, string correlationId,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                string.Equals(verifiedTenantScope, tenant, StringComparison.Ordinal) &&
                string.Equals(evidence?.Aggregate?.SignalName, signalName, StringComparison.Ordinal) &&
                string.Equals(evidence?.Aggregate?.CorrelationId, correlationId, StringComparison.Ordinal)
                    ? evidence : null);
        }
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool SafeTenant(string? value) =>
        value is { Length: >= 3 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}