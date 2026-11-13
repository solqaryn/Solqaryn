using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Fail-closed, side-effect-free tenant and actor authorization for V5 work-item
/// operations. All authorization snapshots MUST originate from a trusted server
/// source; request headers or client-provided dictionaries are not trusted.
/// This guard does not grant JWT/RBAC rights or replace the authoritative CAS.
/// </summary>
public sealed class V5WorkItemTenantAuthorizationGuard
{
    public enum Operation { Read, Claim, Validate, CloseDev, ReviewA5 }

    public sealed record AccessRequest(
        string ActorTaskId,
        string ScopeKey,
        string TenantKey,
        V5WorkItemRules.Snapshot Item,
        Operation RequestedOperation);

    public V5WorkItemRules.Decision Authorize(
        AccessRequest request,
        IReadOnlySet<string> authorizedTaskIds,
        IReadOnlyDictionary<string, IReadOnlySet<string>> trustedTaskScopes,
        IReadOnlyDictionary<string, string> trustedScopeTenants,
        IReadOnlySet<string> existingCatalogIds)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Item);
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        ArgumentNullException.ThrowIfNull(trustedTaskScopes);
        ArgumentNullException.ThrowIfNull(trustedScopeTenants);
        ArgumentNullException.ThrowIfNull(existingCatalogIds);

        if (string.IsNullOrWhiteSpace(request.ActorTaskId) ||
            !authorizedTaskIds.Contains(request.ActorTaskId))
            return V5WorkItemRules.Decision.Deny("UNAUTHORIZED_ACTOR");

        if (string.IsNullOrWhiteSpace(request.ScopeKey) ||
            !trustedTaskScopes.TryGetValue(request.ActorTaskId, out var allowedScopes) ||
            allowedScopes is null || !allowedScopes.Contains(request.ScopeKey))
            return V5WorkItemRules.Decision.Deny("SCOPE_DENIED");

        if (!trustedScopeTenants.TryGetValue(request.ScopeKey, out var trustedTenant) ||
            string.IsNullOrWhiteSpace(trustedTenant) ||
            string.IsNullOrWhiteSpace(request.TenantKey) ||
            !string.Equals(trustedTenant, request.TenantKey, StringComparison.Ordinal))
            return V5WorkItemRules.Decision.Deny("TENANT_MISMATCH");

        if (string.IsNullOrWhiteSpace(request.Item.Id) ||
            !existingCatalogIds.Contains(request.Item.Id))
            return V5WorkItemRules.Decision.Deny("INVALID_ID");

        var item = request.Item;
        return request.RequestedOperation switch
        {
            Operation.Read => V5WorkItemRules.Decision.Permit(),
            Operation.Claim => item.State == "PENDIENTE" &&
                string.IsNullOrWhiteSpace(item.OwnerTaskId)
                ? V5WorkItemRules.Decision.Permit()
                : V5WorkItemRules.Decision.Deny("CLAIM_CONFLICT"),
            Operation.Validate => IsCurrentOwner(item, request.ActorTaskId, "EN_PROGRESO")
                ? V5WorkItemRules.Decision.Permit()
                : V5WorkItemRules.Decision.Deny("OWNER_OR_STATE_MISMATCH"),
            Operation.CloseDev => IsCurrentOwner(item, request.ActorTaskId, "VALIDANDO")
                ? V5WorkItemRules.Decision.Permit()
                : V5WorkItemRules.Decision.Deny("OWNER_OR_STATE_MISMATCH"),
            Operation.ReviewA5 => item.State == "LISTO" &&
                !string.IsNullOrWhiteSpace(item.OwnerTaskId) &&
                authorizedTaskIds.Contains(item.OwnerTaskId) &&
                !string.Equals(item.OwnerTaskId, request.ActorTaskId, StringComparison.Ordinal)
                ? V5WorkItemRules.Decision.Permit()
                : V5WorkItemRules.Decision.Deny("REVIEW_NOT_INDEPENDENT"),
            _ => V5WorkItemRules.Decision.Deny("OPERATION_DENIED")
        };
    }

    private static bool IsCurrentOwner(
        V5WorkItemRules.Snapshot item, string actorTaskId, string expectedState) =>
        string.Equals(item.State, expectedState, StringComparison.Ordinal) &&
        !string.IsNullOrWhiteSpace(item.OwnerTaskId) &&
        string.Equals(item.OwnerTaskId, actorTaskId, StringComparison.Ordinal);
}
