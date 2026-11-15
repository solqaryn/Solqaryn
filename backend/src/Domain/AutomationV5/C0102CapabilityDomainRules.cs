using Solqaryn.Domain.Security;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C01-02.M03.01: reglas puras de interseccion de capabilities por empresa,
/// rol empresarial efectivo, plan, rubro, sucursal y recurso OWNED.
/// Una capability o entitlement comercial nunca sustituye un permiso RBAC.
/// No autentica JWT ni concede acceso a API, datos, workers o proveedores.
/// </summary>
public static class C0102CapabilityDomainRules
{
    public sealed record VerifiedPolicy(
        int TenantEmpresaId,
        int ResourceEmpresaId,
        int VerifiedActorId,
        int EffectiveRoleId,
        int? AuthorizedBranchId,
        int? ResourceBranchId,
        string? PlanCode,
        string? RubroCode,
        bool TenantActive,
        bool MembershipActive,
        bool RoleActive,
        bool PermissionRevoked,
        bool ExplicitRelationalRbacGrant,
        bool PlanCurrent,
        bool RubroCurrent,
        IReadOnlySet<string>? PlanCapabilities,
        IReadOnlySet<string>? RubroCapabilities,
        IReadOnlySet<string>? RoleCapabilities,
        IReadOnlySet<string>? TenantCapabilities,
        IReadOnlySet<string>? DeniedCapabilities);

    public readonly record struct Decision(string Code)
    {
        // El permiso de API y la autenticacion se deben revalidar en Application/API.
        public bool CanGrantAccess => false;
        public bool CanWriteOrReadResource => false;
        public bool CanOverrideTenant => false;
        public bool CanCreateRuntimeGrant => false;
    }

    public static Decision Evaluate(
        ContextoTenantActual? context, string? capability, VerifiedPolicy? policy)
    {
        if (context is null || policy is null ||
            !SafeCapability(capability) || !SafeCode(policy.PlanCode) ||
            !SafeCode(policy.RubroCode))
            return Deny("CAPABILITY_EVIDENCE_REQUIRED");

        if (context.UsuarioId <= 0 || context.EmpresaId <= 0 || context.RolId <= 0 ||
            policy.VerifiedActorId != context.UsuarioId ||
            policy.TenantEmpresaId != context.EmpresaId ||
            policy.ResourceEmpresaId != context.EmpresaId ||
            policy.EffectiveRoleId != context.RolId)
            return Deny("ACTOR_ROLE_OR_TENANT_MISMATCH");

        if (!policy.TenantActive || !policy.MembershipActive ||
            !policy.RoleActive || policy.PermissionRevoked)
            return Deny("MEMBERSHIP_ROLE_OR_TENANT_REVOKED");

        // Una sucursal de recurso siempre requiere alcance de sucursal coincidente,
        // nunca se infiere del frontend ni de un id de empresa suministrado por cliente.
        if (policy.ResourceBranchId is int resourceBranch &&
            (resourceBranch <= 0 || policy.AuthorizedBranchId != resourceBranch))
            return Deny("RESOURCE_BRANCH_NOT_AUTHORIZED");

        if (!policy.ExplicitRelationalRbacGrant)
            return Deny("EXPLICIT_RBAC_GRANT_REQUIRED");

        if (!policy.PlanCurrent || !policy.RubroCurrent ||
            policy.PlanCapabilities is null || policy.RubroCapabilities is null ||
            policy.RoleCapabilities is null || policy.TenantCapabilities is null ||
            policy.DeniedCapabilities is null)
            return Deny("SERVER_CAPABILITY_POLICY_INCOMPLETE");

        // Comparacion ordinal: una lista con comparer insensible a mayusculas
        // nunca eleva una capability de nombre/caso diferente.
        if (!HasExact(policy.PlanCapabilities, capability!) ||
            !HasExact(policy.RubroCapabilities, capability!) ||
            !HasExact(policy.RoleCapabilities, capability!) ||
            !HasExact(policy.TenantCapabilities, capability!) ||
            HasExact(policy.DeniedCapabilities, capability!))
            return Deny("CAPABILITY_NOT_ENTITLED_OR_DISABLED");

        // Comprobacion de Domain, no permiso de runtime ni cached ALLOW.
        return new Decision("VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION");
    }

    private static Decision Deny(string reason) => new(reason);
    private static bool HasExact(IReadOnlySet<string> source, string candidate) =>
        source.Any(x => string.Equals(x, candidate, StringComparison.Ordinal));

    private static bool SafeCapability(string? value) =>
        value is { Length: >= 3 and <= 96 } &&
        value.Contains('.') &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= '0' && c <= '9') || c is '.' or '_' or '-');

    private static bool SafeCode(string? value) =>
        value is { Length: >= 2 and <= 48 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}
