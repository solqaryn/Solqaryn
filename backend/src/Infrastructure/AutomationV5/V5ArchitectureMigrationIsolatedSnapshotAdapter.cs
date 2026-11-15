using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-02.M03.04: isolated read-only migration snapshot adapter.
/// Supplied snapshots do not authenticate JWTs or authorize external actions.
/// </summary>
public sealed class V5ArchitectureMigrationIsolatedSnapshotAdapter
{
    private readonly V5ArchitectureMigrationAuthorizedPreflight _preflight = new();
    private readonly V5ArchitectureMigrationDomainRules.TrustedBoundary _boundary;
    private readonly V5ArchitectureMigrationDomainRules.Candidate?[] _items;

    public V5ArchitectureMigrationIsolatedSnapshotAdapter(
        V5ArchitectureMigrationDomainRules.TrustedBoundary boundary,
        IEnumerable<V5ArchitectureMigrationDomainRules.Candidate?> items)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(items);
        _boundary = boundary with
        {
            ApprovedProviderCatalog = new HashSet<string>(
                boundary.ApprovedProviderCatalog ?? new HashSet<string>(StringComparer.Ordinal),
                StringComparer.Ordinal)
        };
        _items = items.ToArray();
        if (_items.Length is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(items),
                "An isolated batch must contain 1 to 64 candidates.");
    }

    public V5ArchitectureMigrationAuthorizedPreflight.Result PrepareReadOnly(
        V5ArchitectureMigrationAuthorizedPreflight.Context? contextSnapshot,
        string? observedHeadSha, string? observedTenantId)
    {
        if (string.IsNullOrWhiteSpace(observedHeadSha) ||
            !string.Equals(observedHeadSha, _boundary.ExpectedCommitSha,
                StringComparison.Ordinal))
            return new("SOURCE_HEAD_SHA_MISMATCH", 0);
        if (string.IsNullOrWhiteSpace(observedTenantId) ||
            !string.Equals(observedTenantId, _boundary.AuthenticatedTenantId,
                StringComparison.Ordinal))
            return new("SOURCE_TENANT_MISMATCH", 0);

        // Context data remain untrusted until authenticated by an external
        // backend adapter; a structural success cannot perform any mutation.
        return _preflight.Prepare(contextSnapshot,
            new V5ArchitectureMigrationApplicationService.Request(_boundary, _items));
    }

    public bool AllowsPersistence => false;
    public bool AllowsDatabaseMigration => false;
    public bool AllowsExternalProviderWrite => false;
    public bool AllowsEnvironmentPromotion => false;
    public bool CanIssueIndependentA5 => false;
}
