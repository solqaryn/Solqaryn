using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-04.M03.04: isolated, read-only UX observation snapshot adapter.
/// Input snapshots never authenticate a tenant or certify a real browser result.
/// </summary>
public sealed class V5UxAuditIsolatedSnapshotAdapter
{
    private readonly V5UxAuditApplicationService.Request _snapshot;
    private readonly V5UxAuditAuthorizedApplicationService _preflight = new();

    public V5UxAuditIsolatedSnapshotAdapter(V5UxAuditApplicationService.Request source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.AuthorizedSurfaces);
        ArgumentNullException.ThrowIfNull(source.AuthorizedCriteria);
        ArgumentNullException.ThrowIfNull(source.Observations);

        // Copy all mutable collections so a caller cannot expand approved
        // surfaces, criteria or observations after the snapshot is captured.
        _snapshot = source with
        {
            AuthorizedSurfaces = new HashSet<string>(
                source.AuthorizedSurfaces, StringComparer.Ordinal),
            AuthorizedCriteria = new HashSet<string>(
                source.AuthorizedCriteria, StringComparer.Ordinal),
            Observations = source.Observations.ToArray()
        };
    }

    public V5UxAuditAuthorizedApplicationService.Result PrepareReadOnly(
        V5UxAuditAuthorizedApplicationService.TrustedAuthority? authoritySnapshot,
        string? observedSourceSha, string? observedTenantScope)
    {
        if (string.IsNullOrWhiteSpace(observedSourceSha) ||
            !string.Equals(observedSourceSha, _snapshot.ExpectedSourceSha,
                StringComparison.Ordinal))
            return new("SOURCE_SHA_MISMATCH", 0);

        if (string.IsNullOrWhiteSpace(observedTenantScope) ||
            !string.Equals(observedTenantScope, _snapshot.ServerResolvedTenantScope,
                StringComparison.Ordinal))
            return new("TENANT_SNAPSHOT_MISMATCH", 0);

        // The supplied authority is only a snapshot, not proof of a signed
        // JWT, actual RBAC membership, Playwright measurements or A5.
        return _preflight.Prepare(authoritySnapshot, _snapshot);
    }

    public bool AllowsPersistence => false;
    public bool AllowsInstitutionalWrite => false;
    public bool CertifiesExternalMeasurement => false;
    public bool AuthorizesProduction => false;
    public bool GrantsAccess => false;
}
