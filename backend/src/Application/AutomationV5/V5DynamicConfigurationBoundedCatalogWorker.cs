using DomainRules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C01-03.M03.05: bounded tenant-scoped, read-only in-process catalog worker.
/// Sources must be authoritative. This does not authenticate JWT, query MySQL,
/// execute durable CAS, authorize an HTTP endpoint or expose configuration values.
/// </summary>
public sealed class V5DynamicConfigurationBoundedCatalogWorker
{
    public const int MaximumKeys = 16;
    private readonly IV5DynamicConfigurationCurrentActorReader _actors;
    private readonly IV5DynamicConfigurationServerCatalogReader _catalog;

    public V5DynamicConfigurationBoundedCatalogWorker(
        IV5DynamicConfigurationCurrentActorReader actors,
        IV5DynamicConfigurationServerCatalogReader catalog)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public sealed record Request(int TenantAssertion, string? ExpectedSourceSha,
        IReadOnlyCollection<string>? ExactKeys);
    public sealed record Descriptor(string Key, DomainRules.ValueKind Kind, long StoredVersion);
    public sealed record Review(string Code, IReadOnlyList<Descriptor> Descriptors)
    {
        public bool GrantsRead => false;
        public bool GrantsWrite => false;
        public bool GrantsPlatformScope => false;
        public bool CertifiesDurableCas => false;
        public bool CertifiesJwtOrPersistence => false;
    }

    private static readonly string[] ReservedPrefixes =
    {
        "disable_", "bypass_", "secret", "secrets.", "database.", "security.",
        "jwt.", "worker.", "automation.", "auth."
    };

    public async ValueTask<Review> ProjectAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.TenantAssertion <= 0 ||
            !FullSha(request.ExpectedSourceSha) || request.ExactKeys is null ||
            request.ExactKeys.Count is < 1 or > MaximumKeys)
            return Deny("CONFIG_BATCH_REQUEST_INVALID");

        // Snapshot input before awaiting external code.
        var keys = request.ExactKeys.ToArray();
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (!SafeKey(key) || !unique.Add(key))
                return Deny("CONFIG_BATCH_KEY_INVALID_OR_DUPLICATE");
        }

        V5DynamicConfigurationApplicationPreflight.ActorEvidence? actor;
        try { actor = await _actors.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return Deny("CONFIG_BATCH_ACTOR_SOURCE_UNAVAILABLE"); }

        if (actor is null || actor.ActorId <= 0 ||
            actor.AuthenticatedTenantId != request.TenantAssertion ||
            !actor.CurrentBackendIdentityVerified || !actor.MembershipAndGrantsFresh ||
            !string.Equals(actor.SourceSha, request.ExpectedSourceSha, StringComparison.Ordinal) ||
            actor.Authority is not { } authority ||
            authority.AuthenticatedTenantId != actor.AuthenticatedTenantId ||
            !authority.ServerAuthenticated || !authority.MembershipCurrent ||
            !authority.RbacConfigWritePermission || !authority.EntitlementVerified ||
            authority.PermissionRevoked)
            return Deny("CONFIG_BATCH_CURRENT_TENANT_AUTHORITY_REQUIRED");

        Array.Sort(keys, StringComparer.Ordinal);
        var result = new List<Descriptor>(keys.Length);
        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            V5DynamicConfigurationApplicationPreflight.CatalogEvidence? entry;
            try { entry = await _catalog.ReadAsync(actor.AuthenticatedTenantId, key, cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            { return Deny("CONFIG_BATCH_CATALOG_SOURCE_UNAVAILABLE"); }

            if (entry is null || entry.OwnerTenantId != actor.AuthenticatedTenantId ||
                !entry.ReadFromTrustedBackend || !entry.OwnershipVerified ||
                entry.StoredVersion <= 0 ||
                !string.Equals(entry.SourceSha, request.ExpectedSourceSha, StringComparison.Ordinal) ||
                entry.Definition is not { } definition ||
                !string.Equals(definition.CanonicalKey, key, StringComparison.Ordinal) ||
                definition.AllowedScope != DomainRules.ConfigScope.Tenant ||
                definition.ContainsSecret || !Enum.IsDefined(definition.Kind) ||
                entry.ApprovedKeys is null)
                return Deny("CONFIG_BATCH_TENANT_CATALOG_REJECTED");

            // The trusted provider can supply an IReadOnlySet with an enumerator
            // that fails after ReadAsync returns. Deny without leaking the provider
            // exception or returning a partial cross-tenant projection.
            bool approved;
            try
            {
                approved = entry.ApprovedKeys.Any(
                    approvedKey => string.Equals(approvedKey, key, StringComparison.Ordinal));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return Deny("CONFIG_BATCH_CATALOG_SOURCE_UNAVAILABLE");
            }
            if (!approved)
                return Deny("CONFIG_BATCH_TENANT_CATALOG_REJECTED");

            // Metadata only; never expose values, grants or secrets.
            result.Add(new Descriptor(key, definition.Kind, entry.StoredVersion));
        }

        return new Review("CONFIG_BATCH_PROJECTION_ONLY", Array.AsReadOnly(result.ToArray()));
    }

    private static Review Deny(string code) => new(code, Array.Empty<Descriptor>());
    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
    private static bool SafeKey(string? key) =>
        key is { Length: > 0 and <= 64 } &&
        !key.Contains("..", StringComparison.Ordinal) &&
        key.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') || c is '.' or '_') &&
        !ReservedPrefixes.Any(p => key.StartsWith(p, StringComparison.OrdinalIgnoreCase));
}
