using Solqaryn.Application.Security;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Backend-only policy source. Implementations must load current resource owner,
/// token catalog and server session from trusted persistence, never from a DTO.
/// No implementation or DI registration is supplied by this microtask.
/// </summary>
public interface IC0501VerifiedThemeStateReader
{
    ValueTask<C0501DesignTokenCurrentAuthorizationFence.VerifiedThemeState?> ReadCurrentAsync(
        int verifiedTenantId, string exactTokenKey, CancellationToken cancellationToken);
}

/// <summary>
/// C05-01.M03.03: combines current relational configuration/Edit permission with
/// a backend-sourced theme policy and existing Domain/Application design token rules.
/// This is strictly a non-authorizing preflight, never a runtime theme mutation.
/// </summary>
public sealed class C0501DesignTokenCurrentAuthorizationFence
{
    private readonly IdentidadUniversalAuthorizationGuard _identity;
    private readonly IC0501VerifiedThemeStateReader _state;

    public C0501DesignTokenCurrentAuthorizationFence(
        IdentidadUniversalAuthorizationGuard identity, IC0501VerifiedThemeStateReader state)
    {
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public sealed record VerifiedThemeState(
        int TenantId,
        string? ExactTokenKey,
        long CurrentRevision,
        int CurrentServerSessionEpoch,
        IReadOnlySet<string>? ApprovedTokenKeys,
        bool OwnerVerifiedFromPersistence,
        bool CatalogReadFromTrustedBackend,
        bool JwtValidatedByBackendAndNotRevoked);

    public readonly record struct Result(string Code)
    {
        public bool RequiresSeparatelyAuthenticatedThemeAdapter =>
            Code == "AUTHENTICATED_THEME_ADAPTER_REQUIRED";
        public bool GrantsAccess => false;
        public bool WritesTheme => false;
        public bool CanOverrideTenant => false;
        public bool ChangesProviderState => false;
        public bool CertifiesHttpJwtOrMySql => false;
    }

    public async Task<Result> InspectAsync(
        ContextoTenantActual? authenticated,
        C0501DesignTokenApplicationService.ClientIntent? intent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (authenticated is null || authenticated.EmpresaId <= 0 ||
            authenticated.UsuarioId <= 0 || authenticated.RolId <= 0 ||
            intent is null || intent.ClaimedTenantId <= 0 ||
            string.IsNullOrWhiteSpace(intent.TokenKey) ||
            intent.TokenKey.Length > 64)
            return new("TRUSTED_THEME_INTENT_REQUIRED");

        if (intent.ClaimedTenantId != authenticated.EmpresaId)
            return new("CLIENT_TENANT_ASSERTION_DENIED");
        if (intent.PlatformCatalogMutation || intent.DirectWrite)
            return new("CLIENT_MUTATION_OR_GLOBAL_GRANT_DENIED");

        ContextoTenantActual current;
        try
        {
            current = await _identity.ExigirAccesoActualAsync(
                authenticated, authenticated.EmpresaId,
                ModuloSistema.Configuracion, AccionPermiso.Editar,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("CURRENT_RELATIONAL_CONFIGURATION_RBAC_REQUIRED"); }

        VerifiedThemeState? state;
        try { state = await _state.ReadCurrentAsync(
            current.EmpresaId, intent.TokenKey!, cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("TRUSTED_THEME_STATE_UNAVAILABLE"); }

        if (state is null || state.TenantId != current.EmpresaId ||
            !string.Equals(state.ExactTokenKey, intent.TokenKey, StringComparison.Ordinal) ||
            state.CurrentRevision < 0 || state.CurrentServerSessionEpoch <= 0 ||
            state.ApprovedTokenKeys is null || state.ApprovedTokenKeys.Count == 0 ||
            !state.OwnerVerifiedFromPersistence || !state.CatalogReadFromTrustedBackend ||
            !state.JwtValidatedByBackendAndNotRevoked)
            return new("CURRENT_VERIFIED_THEME_POLICY_REQUIRED");

        // The existing Application layer is reused with snapshots constructed
        // solely AFTER verifying current identity, membership, relational RBAC,
        // authenticated tenant and server-owned token policy.
        var trusted = new C0501DesignTokenDomainRules.TrustedContext(
            current.UsuarioId, current.EmpresaId, true, true, true,
            false, state.CurrentRevision, state.CurrentServerSessionEpoch);
        var actor = new C0501DesignTokenApplicationService.ActorSnapshot(
            trusted, true, true, true, true);
        var catalog = new C0501DesignTokenApplicationService.CatalogSnapshot(
            state.TenantId, state.ExactTokenKey, state.CurrentRevision,
            new HashSet<string>(state.ApprovedTokenKeys, StringComparer.Ordinal),
            true, true);
        var application = new C0501DesignTokenApplicationService(
            new ActorReader(actor), new CatalogReader(catalog));
        var result = await application.PrepareAsync(intent, cancellationToken);
        return new(result.Code);
    }

    private sealed class ActorReader(C0501DesignTokenApplicationService.ActorSnapshot actor)
        : IC0501DesignTokenActorReader
    {
        public ValueTask<C0501DesignTokenApplicationService.ActorSnapshot?> ReadCurrentAsync(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<C0501DesignTokenApplicationService.ActorSnapshot?>(actor);
        }
    }

    private sealed class CatalogReader(C0501DesignTokenApplicationService.CatalogSnapshot catalog)
        : IC0501DesignTokenCatalogReader
    {
        public ValueTask<C0501DesignTokenApplicationService.CatalogSnapshot?> ReadAsync(
            int authenticatedTenantId, string exactKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<C0501DesignTokenApplicationService.CatalogSnapshot?>(
                catalog.TenantId == authenticatedTenantId &&
                string.Equals(catalog.ExactKey, exactKey, StringComparison.Ordinal)
                    ? catalog : null);
        }
    }
}
