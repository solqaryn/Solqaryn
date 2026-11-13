using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Authorized read-only projection of V5 requirement coverage. Tenant, scope and
/// work-item bindings must come from a trusted server snapshot, never user input.
/// This service cannot grant JWT/RBAC rights, write PLAN/COLA or certify A5.
/// </summary>
public sealed class V5CoverageTenantProjectionService
{
    private readonly V5RequirementCoverageProjectionService _projection = new();

    public V5RequirementCoverageProjectionService.ProjectionResult Project(
        string actorTaskId,
        string scopeKey,
        string tenantKey,
        IReadOnlyCollection<V5RequirementCoverageRules.Entry> mappings,
        IReadOnlyDictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel> observedPlan,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingCatalogIds,
        string exactCommitSha,
        IReadOnlySet<string> authorizedTaskIds,
        IReadOnlyDictionary<string, IReadOnlySet<string>> trustedTaskScopes,
        IReadOnlyDictionary<string, string> trustedScopeTenants,
        IReadOnlyDictionary<string, string> trustedWorkItemScopes,
        IReadOnlyDictionary<string, string> trustedRequirementScopes)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(observedPlan);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingCatalogIds);
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        ArgumentNullException.ThrowIfNull(trustedTaskScopes);
        ArgumentNullException.ThrowIfNull(trustedScopeTenants);
        ArgumentNullException.ThrowIfNull(trustedWorkItemScopes);
        ArgumentNullException.ThrowIfNull(trustedRequirementScopes);

        if (string.IsNullOrWhiteSpace(actorTaskId) ||
            !authorizedTaskIds.Contains(actorTaskId))
            return Reject("UNAUTHORIZED_ACTOR");

        if (string.IsNullOrWhiteSpace(scopeKey) ||
            !trustedTaskScopes.TryGetValue(actorTaskId, out var allowedScopes) ||
            allowedScopes is null || !allowedScopes.Contains(scopeKey))
            return Reject("SCOPE_DENIED");

        if (!trustedScopeTenants.TryGetValue(scopeKey, out var expectedTenant) ||
            string.IsNullOrWhiteSpace(expectedTenant) ||
            string.IsNullOrWhiteSpace(tenantKey) ||
            !string.Equals(expectedTenant, tenantKey, StringComparison.Ordinal))
            return Reject("TENANT_MISMATCH");

        // Materialize once so authorization checks and projection consume precisely
        // the same immutable collection snapshot.
        var snapshot = mappings.ToArray();
        foreach (var entry in snapshot)
        {
            if (entry is null ||
                !trustedWorkItemScopes.TryGetValue(entry.WorkItemId, out var workScope) ||
                !string.Equals(workScope, scopeKey, StringComparison.Ordinal))
                return Reject("WORK_ITEM_SCOPE_DENIED");

            if (!trustedRequirementScopes.TryGetValue(entry.RequirementKey, out var requirementScope) ||
                !string.Equals(requirementScope, scopeKey, StringComparison.Ordinal))
                return Reject("REQUIREMENT_SCOPE_DENIED");
        }

        // The projection still rejects unknown catalog/requirement keys, duplicates,
        // missing snapshots, moved SHAs and insufficient evidence.
        return _projection.Project(
            snapshot, observedPlan, authorizedRequirementKeys, existingCatalogIds,
            exactCommitSha);
    }

    private static V5RequirementCoverageProjectionService.ProjectionResult Reject(string code) =>
        new(V5RequirementCoverageRules.Verdict.Reject(code),
            Array.Empty<V5RequirementCoverageProjectionService.CoverageRow>());
}
