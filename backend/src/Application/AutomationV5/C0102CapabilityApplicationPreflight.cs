using Solqaryn.Domain.Security;
using Guard = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C01-02.M03.02: a non-authorizing Application preflight for capability intents.
/// The tenant context and policy must be constructed by verified backend code;
/// client-supplied tenant, branch or capability never grant RBAC or resource access.
/// No API route, persistence adapter, plan mutation, feature activation or runtime
/// grant is registered by this class.
/// </summary>
public sealed class C0102CapabilityApplicationPreflight
{
    public sealed record ClientIntent(
        int RequestedTenantEmpresaId,
        int? RequestedBranchId,
        string? CapabilityKey,
        bool IncludesClientGrantOrPolicy,
        bool RequestsWriteExportOrActivation);

    public sealed record ReviewResult(string Code, bool RequiresLiveBackendAuthorization)
    {
        public bool GrantsAccess => false;
        public bool ReadsOrWritesResource => false;
        public bool CreatesCapabilityGrant => false;
        public bool MayOverrideTenant => false;
    }

    public ReviewResult ReviewOnly(
        ContextoTenantActual? verifiedContext,
        Guard.VerifiedPolicy? trustedBackendPolicy,
        ClientIntent? intent)
    {
        if (intent is null)
            return new("CAPABILITY_INTENT_REQUIRED", false);

        if (verifiedContext is null || trustedBackendPolicy is null)
            return new("TRUSTED_BACKEND_CONTEXT_AND_POLICY_REQUIRED", false);

        // The client company identifier is only an equality assertion.
        // It must never select, reconstruct or override the trusted tenant.
        if (intent.RequestedTenantEmpresaId <= 0 ||
            intent.RequestedTenantEmpresaId != verifiedContext.EmpresaId)
            return new("CLIENT_TENANT_CONSISTENCY_MISMATCH", false);

        // Branch selection is equally untrusted; a scoped resource requires
        // exact server-verified scope and a tenant-wide resource has no branch.
        if (trustedBackendPolicy.ResourceBranchId != intent.RequestedBranchId ||
            intent.RequestedBranchId is <= 0)
            return new("CLIENT_BRANCH_CONSISTENCY_MISMATCH", false);

        if (intent.IncludesClientGrantOrPolicy)
            return new("CLIENT_POLICY_OR_GRANT_OVERRIDE_DENIED", false);

        if (intent.RequestsWriteExportOrActivation)
            return new("NON_AUTHORIZING_PREFLIGHT_CANNOT_MUTATE_OR_EXPORT", false);

        Guard.Decision verified = Guard.Evaluate(
            verifiedContext, intent.CapabilityKey, trustedBackendPolicy);

        // Domain is deliberately non-authorizing even on valid evidence.
        // Application/API must independently verify live membership, JWT,
        // effective relational RBAC, plan, rubro, branch, resource ownership,
        // revocation and audit before any real read or write.
        return new(
            verified.Code,
            verified.Code == "VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION");
    }
}
