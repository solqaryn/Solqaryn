using Rules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>Fresh backend identity, current relational membership and RBAC; never client claims.</summary>
public interface IC0103TrustedConfigurationActorSource
{
    ValueTask<C0103DynamicConfigurationAuthorizationGate.ActorSnapshot?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>Reads must filter tenant + canonical key at the authoritative source.</summary>
public interface IC0103TenantConfigurationCatalogSource
{
    ValueTask<C0103DynamicConfigurationAuthorizationGate.CatalogSnapshot?> ReadAsync(
        int authenticatedTenantId, string requestedKey, CancellationToken cancellationToken);
}

/// <summary>
/// C01-03.M03.03: a fail-closed authorization fence for tenant config preflight.
/// This is not a JWT handler, database transaction, a platform admin grant or DI binding.
/// </summary>
public sealed class C0103DynamicConfigurationAuthorizationGate
{
    private readonly IC0103TrustedConfigurationActorSource _actors;
    private readonly IC0103TenantConfigurationCatalogSource _catalog;

    public C0103DynamicConfigurationAuthorizationGate(
        IC0103TrustedConfigurationActorSource actors,
        IC0103TenantConfigurationCatalogSource catalog)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public sealed record ActorSnapshot(
        int ActorId, int TenantId, string? SourceSha,
        bool CurrentBackendJwtVerified, bool MembershipFresh,
        bool RelationalRbacGrantFresh, bool EntitlementFresh,
        bool Revoked, Rules.TrustedAuthority? Authority);

    public sealed record CatalogSnapshot(
        int OwnerTenantId, string? SourceSha, long Version,
        bool BackendReadVerified, bool PersistedOwnerVerified,
        Rules.CatalogKey? Definition, IReadOnlySet<string>? ApprovedKeys);

    public readonly record struct Decision(string Code)
    {
        public bool NeedsAuthenticatedDurableCasAdapter =>
            Code == "AUTHENTICATED_CONFIGURATION_TRANSACTION_AND_A5_REQUIRED";
        public bool GrantsConfigurationWrite => false;
        public bool AllowsPlatformOverride => false;
        public bool AllowsCrossTenantRead => false;
        public bool CertifiesDurableCas => false;
        public bool AuthorizesQaOrProduction => false;
    }

    public async ValueTask<Decision> InspectAsync(
        V5DynamicConfigurationApplicationPreflight.ClientIntent? intent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || string.IsNullOrWhiteSpace(intent.Key) ||
            intent.Key.Length > 64 || !FullSha(intent.ExpectedSourceSha) ||
            intent.ExpectedVersion <= 0)
            return new("VALID_CONFIG_KEY_VERSION_SHA_REQUIRED");

        if (intent.RequestedScope != Rules.ConfigScope.Tenant)
            return new("INDEPENDENT_PLATFORM_AUTHORIZATION_REQUIRED");

        if (intent.ClientSuppliesAuthority || intent.RequestsSecurityBypass)
            return new("CLIENT_AUTHORITY_OR_SECURITY_BYPASS_DENIED");

        ActorSnapshot? actor;
        try { actor = await _actors.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("CURRENT_ACTOR_BACKEND_SOURCE_UNAVAILABLE"); }

        var auth = actor?.Authority;
        if (actor is null || auth is null || actor.ActorId <= 0 || actor.TenantId <= 0 ||
            !actor.CurrentBackendJwtVerified || !actor.MembershipFresh ||
            !actor.RelationalRbacGrantFresh || !actor.EntitlementFresh ||
            actor.Revoked || !auth.ServerAuthenticated || !auth.MembershipCurrent ||
            !auth.RbacConfigWritePermission || !auth.EntitlementVerified ||
            auth.PermissionRevoked || auth.AuthenticatedTenantId != actor.TenantId ||
            !string.Equals(actor.SourceSha, intent.ExpectedSourceSha,
                StringComparison.Ordinal))
            return new("CURRENT_TRUSTED_JWT_MEMBERSHIP_RBAC_AND_TENANT_REQUIRED");

        if (intent.TenantAssertion != actor.TenantId)
            return new("CROSS_TENANT_CLIENT_ASSERTION_DENIED");

        cancellationToken.ThrowIfCancellationRequested();
        CatalogSnapshot? catalog;
        try { catalog = await _catalog.ReadAsync(actor.TenantId, intent.Key!, cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("CURRENT_TENANT_CATALOG_SOURCE_UNAVAILABLE"); }

        if (catalog is null || catalog.OwnerTenantId != actor.TenantId ||
            catalog.Version <= 0 || !catalog.BackendReadVerified ||
            !catalog.PersistedOwnerVerified || catalog.Definition is null ||
            catalog.ApprovedKeys is null ||
            !string.Equals(catalog.SourceSha, intent.ExpectedSourceSha, StringComparison.Ordinal) ||
            !string.Equals(catalog.Definition.CanonicalKey, intent.Key,
                StringComparison.Ordinal))
            return new("PERSISTED_TENANT_CATALOG_AND_CANONICAL_KEY_REQUIRED");

        var actorProof = new V5DynamicConfigurationApplicationPreflight.ActorEvidence(
            actor.ActorId, actor.TenantId, actor.SourceSha, true, true, auth);
        var catalogProof = new V5DynamicConfigurationApplicationPreflight.CatalogEvidence(
            catalog.OwnerTenantId, catalog.SourceSha, true, true,
            catalog.Version, catalog.Definition, catalog.ApprovedKeys);

        var app = new V5DynamicConfigurationApplicationPreflight(
            new CapturedActor(actorProof), new CapturedCatalog(catalogProof));
        var result = await app.ReviewAsync(intent, cancellationToken);

        // Even a valid Domain and Application preflight cannot grant any
        // configuration mutation. Real authenticated CAS and independent A5
        // must be implemented and revalidated in a separate authorized scope.
        return result.RequiresAuthenticatedCasAdapter
            ? new("AUTHENTICATED_CONFIGURATION_TRANSACTION_AND_A5_REQUIRED")
            : new(result.Code);
    }

    private sealed class CapturedActor(
        V5DynamicConfigurationApplicationPreflight.ActorEvidence captured)
        : IV5DynamicConfigurationCurrentActorReader
    {
        public ValueTask<V5DynamicConfigurationApplicationPreflight.ActorEvidence?> ReadCurrentAsync(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<V5DynamicConfigurationApplicationPreflight.ActorEvidence?>(captured);
        }
    }

    private sealed class CapturedCatalog(
        V5DynamicConfigurationApplicationPreflight.CatalogEvidence captured)
        : IV5DynamicConfigurationServerCatalogReader
    {
        public ValueTask<V5DynamicConfigurationApplicationPreflight.CatalogEvidence?> ReadAsync(
            int tenantId, string key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (tenantId != captured.OwnerTenantId ||
                !string.Equals(key, captured.Definition?.CanonicalKey, StringComparison.Ordinal))
                return ValueTask.FromResult<V5DynamicConfigurationApplicationPreflight.CatalogEvidence?>(null);
            return ValueTask.FromResult<V5DynamicConfigurationApplicationPreflight.CatalogEvidence?>(captured);
        }
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
