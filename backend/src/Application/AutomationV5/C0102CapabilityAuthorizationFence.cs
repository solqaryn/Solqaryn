using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Server-only port. A production implementation must load the current resource owner,
/// business capability catalog and plan/role/rubro grants from trusted persistence.
/// Never implement this from a request DTO, cookie, user-provided company or cached ALLOW.
/// </summary>
public interface IC0102VerifiedCapabilityPolicyReader
{
    ValueTask<C0102CapabilityAuthorizationFence.VerifiedResourcePolicy?> ReadCurrentAsync(
        int authenticatedTenantId, int resourceId, CancellationToken cancellationToken);
}

/// <summary>
/// C01-02.M03.03: fail-closed capability and tenant authorization boundary.
/// It revalidates current membership and relational RBAC through the existing live
/// identity guard, then intersects resource ownership and server-sourced entitlements.
/// It is not an endpoint, resource reader or permission grant; a separate authenticated
/// resource adapter must still recheck authorization at the point of effect.
/// </summary>
public sealed class C0102CapabilityAuthorizationFence
{
    private readonly IdentidadUniversalAuthorizationGuard _identity;
    private readonly IC0102VerifiedCapabilityPolicyReader _policyReader;

    public C0102CapabilityAuthorizationFence(
        IdentidadUniversalAuthorizationGuard identity,
        IC0102VerifiedCapabilityPolicyReader policyReader)
    {
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _policyReader = policyReader ?? throw new ArgumentNullException(nameof(policyReader));
    }

    public sealed record Intent(
        int ResourceId, int TenantAssertion, int? BranchAssertion,
        string? CapabilityKey, ModuloSistema Module, AccionPermiso Action,
        bool ClientSuppliesGrantsOrPolicy = false,
        bool RequestsWriteExportOrActivation = false);

    public sealed record VerifiedResourcePolicy(
        int ResourceId, int OwnerTenantId, int VerifiedActorId,
        bool OwnershipReadFromTrustedPersistence,
        bool CurrentCapabilityCatalogReadFromBackend,
        Rules.VerifiedPolicy? Policy);

    public readonly record struct Result(string Code)
    {
        public bool NeedsAuthenticatedResourceAdapter =>
            Code == "AUTHENTICATED_CAPABILITY_RESOURCE_ADAPTER_REQUIRED";
        public bool GrantsResourceAccess => false;
        public bool GrantsCapability => false;
        public bool WritesResource => false;
        public bool CanOverrideTenant => false;
        public bool CanExportOrActivate => false;
    }

    public async Task<Result> InspectAsync(
        ContextoTenantActual? authenticated, Intent? intent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (authenticated is null || authenticated.UsuarioId <= 0 ||
            authenticated.EmpresaId <= 0 || authenticated.RolId <= 0 ||
            intent is null || intent.ResourceId <= 0 ||
            intent.TenantAssertion <= 0 ||
            intent.BranchAssertion is <= 0 ||
            !Enum.IsDefined(intent.Module) || !Enum.IsDefined(intent.Action) ||
            !CanonicalCapability(intent.CapabilityKey))
            return new("TRUSTED_CAPABILITY_INTENT_REQUIRED");

        // Client tenant is an equality assertion only, never a way to choose
        // a different company before fetching membership or resource evidence.
        if (intent.TenantAssertion != authenticated.EmpresaId)
            return new("CROSS_TENANT_CAPABILITY_ASSERTION_DENIED");

        if (intent.ClientSuppliesGrantsOrPolicy ||
            intent.RequestsWriteExportOrActivation)
            return new("CLIENT_GRANT_OR_MUTATION_DENIED");

        ContextoTenantActual current;
        try
        {
            current = await _identity.ExigirAccesoActualAsync(
                authenticated, authenticated.EmpresaId,
                intent.Module, intent.Action, intent.BranchAssertion,
                cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CURRENT_MEMBERSHIP_AND_RELATIONAL_RBAC_REQUIRED");
        }

        VerifiedResourcePolicy? evidence;
        try
        {
            evidence = await _policyReader.ReadCurrentAsync(
                current.EmpresaId, intent.ResourceId, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("TRUSTED_CAPABILITY_SOURCE_UNAVAILABLE");
        }

        var policy = evidence?.Policy;
        if (evidence is null || policy is null ||
            !evidence.OwnershipReadFromTrustedPersistence ||
            !evidence.CurrentCapabilityCatalogReadFromBackend ||
            evidence.ResourceId != intent.ResourceId ||
            evidence.OwnerTenantId != current.EmpresaId ||
            evidence.VerifiedActorId != current.UsuarioId ||
            policy.ResourceEmpresaId != evidence.OwnerTenantId ||
            policy.TenantEmpresaId != current.EmpresaId ||
            policy.VerifiedActorId != current.UsuarioId ||
            policy.EffectiveRoleId != current.RolId ||
            policy.ResourceBranchId != intent.BranchAssertion)
            return new("VERIFIED_RESOURCE_CAPABILITY_POLICY_REQUIRED");

        var domain = Rules.Evaluate(current, intent.CapabilityKey, policy);
        if (domain.Code != "VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION")
            return new(domain.Code);

        // Never turn a commercial entitlement or trusted snapshot into a
        // permanent ALLOW, API response, EF filter bypass, job or data effect.
        return new("AUTHENTICATED_CAPABILITY_RESOURCE_ADAPTER_REQUIRED");
    }

    private static bool CanonicalCapability(string? value) =>
        value is { Length: >= 3 and <= 96 } &&
        value.Contains('.') &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= '0' && c <= '9') ||
                       c is '.' or '_' or '-');
}
