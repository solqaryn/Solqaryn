using Solqaryn.Domain.AutomationV5;
namespace Solqaryn.Application.AutomationV5;

/// <summary>Trusted backend evidence; implementations must not consume client identity claims.</summary>
public interface IC0501DesignTokenActorReader
{
    ValueTask<C0501DesignTokenApplicationService.ActorSnapshot?> ReadCurrentAsync(CancellationToken ct);
}
/// <summary>Future adapter: query only using authenticated tenant and exact key.</summary>
public interface IC0501DesignTokenCatalogReader
{
    ValueTask<C0501DesignTokenApplicationService.CatalogSnapshot?> ReadAsync(
        int authenticatedTenantId, string exactKey, CancellationToken ct);
}
/// <summary>
/// C05-01.M03.02: read-only tenant-scoped Application use case. No HTTP, DI, DB,
/// theme mutation, durable idempotency, grant, or provider implementation.
/// </summary>
public sealed class C0501DesignTokenApplicationService
{
    private readonly IC0501DesignTokenActorReader _actors;
    private readonly IC0501DesignTokenCatalogReader _catalog;
    public C0501DesignTokenApplicationService(IC0501DesignTokenActorReader actors,
        IC0501DesignTokenCatalogReader catalog)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public sealed record ClientIntent(int ClaimedTenantId, int SessionEpoch,
        long ExpectedRevision, string? TokenKey, string? TokenType,
        string? TokenValue, string? IdempotencyKey,
        bool PlatformCatalogMutation = false, bool DirectWrite = false);

    public sealed record ActorSnapshot(
        C0501DesignTokenDomainRules.TrustedContext Context,
        bool BackendAuthenticated, bool JwtFresh, bool MembershipFresh, bool RbacFresh);

    public sealed record CatalogSnapshot(int TenantId, string? ExactKey,
        long StoredRevision, IReadOnlySet<string>? ApprovedKeys,
        bool TenantScopedBackendRead, bool OwnerVerified);

    public readonly record struct Result(string Code)
    {
        public bool NeedsAuthenticatedAdapter => Code == "AUTHENTICATED_THEME_ADAPTER_REQUIRED";
        public bool AuthorizesThemeMutation => false;
        public bool AuthorizesTenantAccess => false;
        public bool PersistsAnything => false;
        public bool CertifiesAccessibility => false;
    }

    public async ValueTask<Result> PrepareAsync(ClientIntent? intent,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (intent is null || intent.ClaimedTenantId <= 0 ||
            string.IsNullOrWhiteSpace(intent.TokenKey) || intent.TokenKey.Length > 64 ||
            intent.ExpectedRevision < 0)
            return new("VALID_CLIENT_INTENT_REQUIRED");
        if (intent.DirectWrite || intent.PlatformCatalogMutation)
            return new("UNTRUSTED_MUTATION_DENIED");

        ActorSnapshot? actor;
        try { actor = await _actors.ReadCurrentAsync(ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("BACKEND_AUTHORITY_UNAVAILABLE"); }

        var trusted = actor?.Context;
        if (trusted is null || trusted.ActorId <= 0 || trusted.TenantId <= 0 ||
            !actor!.BackendAuthenticated || !actor.JwtFresh || !actor.MembershipFresh ||
            !actor.RbacFresh || !trusted.MembershipCurrent ||
            !trusted.HasThemeEditPermission || trusted.RoleRevoked ||
            trusted.SessionEpoch <= 0)
            return new("CURRENT_BACKEND_AUTHORITY_REQUIRED");
        if (intent.ClaimedTenantId != trusted.TenantId)
            return new("CLIENT_TENANT_MISMATCH");

        ct.ThrowIfCancellationRequested();
        CatalogSnapshot? catalog;
        try { catalog = await _catalog.ReadAsync(trusted.TenantId, intent.TokenKey!, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("TRUSTED_CATALOG_UNAVAILABLE"); }

        if (catalog is null || catalog.TenantId != trusted.TenantId ||
            !catalog.TenantScopedBackendRead || !catalog.OwnerVerified ||
            !string.Equals(catalog.ExactKey, intent.TokenKey, StringComparison.Ordinal) ||
            catalog.ApprovedKeys is null || catalog.ApprovedKeys.Count == 0)
            return new("TENANT_CATALOG_EVIDENCE_REQUIRED");
        if (catalog.StoredRevision != trusted.ActiveRevision)
            return new("STALE_CATALOG_SNAPSHOT_RETRY_REQUIRED");

        var proposal = new C0501DesignTokenDomainRules.ProposedToken(
            intent.ClaimedTenantId, intent.SessionEpoch, intent.ExpectedRevision,
            intent.TokenKey!, intent.TokenType!, intent.TokenValue!,
            intent.IdempotencyKey!, intent.PlatformCatalogMutation);
        var verdict = C0501DesignTokenDomainRules.Evaluate(
            trusted, proposal, catalog.ApprovedKeys);
        return verdict.ReadyForTrustedApplicationReview
            ? new("AUTHENTICATED_THEME_ADAPTER_REQUIRED") : new(verdict.Code);
    }
}
