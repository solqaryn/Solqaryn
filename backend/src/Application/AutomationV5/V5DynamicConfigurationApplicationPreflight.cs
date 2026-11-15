using DomainRules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>Current server identity and grants; never constructed from a browser DTO.</summary>
public interface IV5DynamicConfigurationCurrentActorReader
{
    ValueTask<V5DynamicConfigurationApplicationPreflight.ActorEvidence?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// A future trusted Infrastructure implementation must scope reads by server-verified tenant.
/// No implementation, database access or DI registration is introduced by this change.
/// </summary>
public interface IV5DynamicConfigurationServerCatalogReader
{
    ValueTask<V5DynamicConfigurationApplicationPreflight.CatalogEvidence?> ReadAsync(
        int authenticatedTenantId, string exactKey, CancellationToken cancellationToken);
}

/// <summary>
/// C01-03.M03.02: tenant-scoped, read-only Application preflight. It never mutates
/// configuration and never authorizes a durable CAS, grant or platform override.
/// </summary>
public sealed class V5DynamicConfigurationApplicationPreflight
{
    private readonly IV5DynamicConfigurationCurrentActorReader _actors;
    private readonly IV5DynamicConfigurationServerCatalogReader _catalog;

    public V5DynamicConfigurationApplicationPreflight(
        IV5DynamicConfigurationCurrentActorReader actors,
        IV5DynamicConfigurationServerCatalogReader catalog)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public sealed record ClientIntent(
        string? Key, DomainRules.ValueKind DeclaredKind,
        DomainRules.ConfigScope RequestedScope, int? TenantAssertion,
        string? Value, long ExpectedVersion, string? ExpectedSourceSha,
        bool ClientSuppliesAuthority = false, bool RequestsSecurityBypass = false);

    public sealed record ActorEvidence(
        int ActorId, int AuthenticatedTenantId, string? SourceSha,
        bool CurrentBackendIdentityVerified, bool MembershipAndGrantsFresh,
        DomainRules.TrustedAuthority? Authority);

    public sealed record CatalogEvidence(
        int OwnerTenantId, string? SourceSha,
        bool ReadFromTrustedBackend, bool OwnershipVerified,
        long StoredVersion, DomainRules.CatalogKey? Definition,
        IReadOnlySet<string>? ApprovedKeys);

    public readonly record struct ReviewResult(string Code)
    {
        public bool RequiresAuthenticatedCasAdapter =>
            Code == "AUTHENTICATED_DURABLE_CONFIG_ADAPTER_REQUIRED";
        public bool GrantsWrite => false;
        public bool GrantsPlatformOverride => false;
        public bool GrantsCrossTenantRead => false;
        public bool CertifiesDurableCas => false;
    }

    public async ValueTask<ReviewResult> ReviewAsync(
        ClientIntent? intent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || string.IsNullOrWhiteSpace(intent.Key) ||
            intent.Key.Length > 64 || !FullSha(intent.ExpectedSourceSha) ||
            intent.ExpectedVersion <= 0)
            return new("VALID_CONFIG_INTENT_AND_SOURCE_REQUIRED");

        // Platform configuration requires a separately approved backend authority.
        // This tenant-only use case may not infer it from an intent.
        if (intent.RequestedScope != DomainRules.ConfigScope.Tenant)
            return new("SEPARATE_PLATFORM_AUTHORITY_REQUIRED");
        if (intent.ClientSuppliesAuthority || intent.RequestsSecurityBypass)
            return new("CLIENT_AUTHORITY_AND_BYPASS_DENIED");

        ActorEvidence? actor;
        try
        {
            actor = await _actors.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CURRENT_BACKEND_AUTHORITY_UNAVAILABLE");
        }

        if (actor is null || actor.ActorId <= 0 ||
            actor.AuthenticatedTenantId <= 0 ||
            !actor.CurrentBackendIdentityVerified ||
            !actor.MembershipAndGrantsFresh || actor.Authority is null ||
            actor.Authority.AuthenticatedTenantId != actor.AuthenticatedTenantId ||
            !string.Equals(actor.SourceSha, intent.ExpectedSourceSha,
                StringComparison.Ordinal))
            return new("CURRENT_BACKEND_IDENTITY_REQUIRED");

        // The untrusted tenant is only an equality assertion and never selects a DB.
        if (intent.TenantAssertion != actor.AuthenticatedTenantId)
            return new("CLIENT_TENANT_MISMATCH");

        cancellationToken.ThrowIfCancellationRequested();
        CatalogEvidence? evidence;
        try
        {
            evidence = await _catalog.ReadAsync(actor.AuthenticatedTenantId,
                intent.Key!, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("TRUSTED_CONFIG_CATALOG_UNAVAILABLE");
        }

        if (evidence is null || evidence.OwnerTenantId != actor.AuthenticatedTenantId ||
            !evidence.ReadFromTrustedBackend || !evidence.OwnershipVerified ||
            evidence.StoredVersion <= 0 || evidence.Definition is null ||
            evidence.ApprovedKeys is null ||
            !string.Equals(evidence.SourceSha, intent.ExpectedSourceSha,
                StringComparison.Ordinal))
            return new("PERSISTED_TENANT_CATALOG_EVIDENCE_REQUIRED");

        var change = new DomainRules.ProposedChange(
            intent.Key, intent.DeclaredKind, intent.RequestedScope,
            intent.TenantAssertion, intent.Value, intent.ExpectedVersion,
            evidence.StoredVersion, false, false);

        var verdict = DomainRules.Evaluate(actor.Authority, evidence.Definition,
            change, evidence.ApprovedKeys);
        return verdict.Code == "TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED"
            ? new("AUTHENTICATED_DURABLE_CONFIG_ADAPTER_REQUIRED")
            : new(verdict.Code);
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
