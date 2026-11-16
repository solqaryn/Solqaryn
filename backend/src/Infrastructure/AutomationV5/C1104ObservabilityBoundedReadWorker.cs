using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Future authenticated source may implement this port. No real telemetry
/// provider, tenant read, exporter, database or hosted worker is registered.
/// </summary>
public interface IC1104BoundedObservabilityPreviewPort
{
    Task<C1104ObservabilityIsolatedSnapshotAdapter.Outcome> InspectAsync(
        C1104ObservabilityAuthorizationGate.Request request,
        string syntheticTenant, string observedSourceSha,
        CancellationToken cancellationToken);
}

/// <summary>Compose the existing isolated snapshot adapter, never a live source.</summary>
public sealed class C1104ObservabilitySnapshotPreviewPort(
    C1104ObservabilityIsolatedSnapshotAdapter adapter) : IC1104BoundedObservabilityPreviewPort
{
    private readonly C1104ObservabilityIsolatedSnapshotAdapter _adapter =
        adapter ?? throw new ArgumentNullException(nameof(adapter));

    public Task<C1104ObservabilityIsolatedSnapshotAdapter.Outcome> InspectAsync(
        C1104ObservabilityAuthorizationGate.Request request,
        string syntheticTenant, string observedSourceSha, CancellationToken cancellationToken) =>
        _adapter.InspectAsync(request, syntheticTenant, observedSourceSha, cancellationToken);
}

/// <summary>
/// C11-04.M03.05: sequential bounded in-process preview for isolated synthetic
/// observability, never SLO certification, a telemetry exporter or a hosted worker.
/// </summary>
public sealed class C1104ObservabilityBoundedReadWorker
{
    public const int MaximumBatch = 16;
    private readonly IC1104BoundedObservabilityPreviewPort _port;

    public C1104ObservabilityBoundedReadWorker(IC1104BoundedObservabilityPreviewPort port) =>
        _port = port ?? throw new ArgumentNullException(nameof(port));

    public sealed record Ticket(string? SignalName, string? CorrelationId,
        string? ExpectedSourceSha, string? VerifiedSyntheticTenant,
        string? ObservedSyntheticTenant, bool RequestsExportOrRelease = false);

    public readonly record struct Review(string Code, int Examined)
    {
        public bool GrantsTenantRead => false;
        public bool ExposesTelemetry => false;
        public bool WritesData => false;
        public bool StartsHostedWorker => false;
        public bool ExportsMetrics => false;
        public bool CertifiesSloOrCost => false;
        public bool GrantsJwtOrRbac => false;
        public bool AuthorizesQaOrProduction => false;
    }

    public async Task<Review> InspectBatchAsync(IReadOnlyList<Ticket>? batch,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batch is null || batch.Count is < 1 or > MaximumBatch)
            return Deny("OBSERVABILITY_BATCH_REQUIRED_OR_TOO_LARGE");

        string? sourceSha = null;
        string? tenant = null;
        var distinct = new HashSet<string>(StringComparer.Ordinal);
        // Prevalidate every entry BEFORE touching any source.
        foreach (var item in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item is null || item.SignalName is not
                ("http_requests" or "request_latency" or "error_rate" or "db_duration") ||
                !SafeKey(item.CorrelationId) || !FullSha(item.ExpectedSourceSha) ||
                !SafeTenant(item.VerifiedSyntheticTenant) ||
                !string.Equals(item.VerifiedSyntheticTenant, item.ObservedSyntheticTenant,
                    StringComparison.Ordinal) || item.RequestsExportOrRelease)
                return Deny("OBSERVABILITY_UNTRUSTED_OR_INVALID_TICKET");

            if (sourceSha is null) sourceSha = item.ExpectedSourceSha;
            if (tenant is null) tenant = item.VerifiedSyntheticTenant;
            if (!string.Equals(sourceSha, item.ExpectedSourceSha, StringComparison.Ordinal) ||
                !string.Equals(tenant, item.VerifiedSyntheticTenant, StringComparison.Ordinal))
                return Deny("OBSERVABILITY_MIXED_TENANT_OR_SOURCE_SHA");

            if (!distinct.Add(item.SignalName + "|" + item.CorrelationId))
                return Deny("OBSERVABILITY_DUPLICATE_TICKET");
        }

        foreach (var item in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C1104ObservabilityIsolatedSnapshotAdapter.Outcome result;
            try
            {
                result = await _port.InspectAsync(
                    new C1104ObservabilityAuthorizationGate.Request(
                        item.SignalName, item.CorrelationId, item.ExpectedSourceSha),
                    tenant!, sourceSha!, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return Deny("OBSERVABILITY_SOURCE_UNAVAILABLE");
            }
            if (!result.RequiresSeparateAuthenticatedApi)
                return Deny("OBSERVABILITY_SOURCE_REJECTED");
        }

        // No data are returned, no result certifies an SLO, costs or authorization.
        return new("OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API", batch.Count);
    }

    private static Review Deny(string code) => new(code, 0);
    private static bool FullSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static bool SafeTenant(string? value) =>
        value is { Length: >= 3 and <= 128 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_');
    private static bool SafeKey(string? value) =>
        value is { Length: >= 1 and <= 64 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_' or '.');
}
