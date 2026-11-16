using App = Solqaryn.Application.AutomationV5.C0501DesignTokenApplicationService;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C05-01.M03.04: isolated, immutable synthetic theme-policy snapshot.
/// This is NOT an authenticated persistence/JWT/HTTP provider and cannot apply CSS.
/// A real effect requires a separate backend implementation with fresh authority.
/// </summary>
public sealed class C0501DesignTokenIsolatedSnapshotAdapter
{
    private readonly App.ActorSnapshot _actor;
    private readonly App.CatalogSnapshot _catalog;
    private readonly string _sourceSha;

    public C0501DesignTokenIsolatedSnapshotAdapter(
        App.ActorSnapshot actor, App.CatalogSnapshot catalog, string sourceSha)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(catalog);
        if (actor.Context is null || actor.Context.ActorId <= 0 ||
            actor.Context.TenantId <= 0)
            throw new ArgumentException("Captured trusted identity is required.", nameof(actor));
        if (!CanonicalSha(sourceSha))
            throw new ArgumentException("Canonical 40-character SHA required.", nameof(sourceSha));

        _actor = actor;
        // Never trust a caller-owned or case-insensitive mutable set after capture.
        _catalog = catalog with
        {
            ApprovedKeys = catalog.ApprovedKeys is null
                ? null
                : new HashSet<string>(catalog.ApprovedKeys, StringComparer.Ordinal)
        };
        _sourceSha = sourceSha;
    }

    public bool IsLivePersistenceProvider => false;
    public bool ReadsOrWritesMysql => false;
    public bool AuthenticatesJwt => false;
    public bool GrantsTenantAccess => false;
    public bool GrantsRbac => false;
    public bool WritesTheme => false;
    public bool InstallsTenantContext => false;
    public bool CertifiesAccessibility => false;

    public async ValueTask<App.Result> InspectReadOnlyAsync(
        App.ClientIntent? intent,
        int observedAuthenticatedActorId,
        int observedAuthenticatedTenantId,
        long observedRevision,
        int observedServerSessionEpoch,
        string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null)
            return new("ISOLATED_THEME_INTENT_REQUIRED");
        if (observedAuthenticatedActorId != _actor.Context.ActorId ||
            observedAuthenticatedTenantId != _actor.Context.TenantId)
            return new("ISOLATED_THEME_ACTOR_OR_TENANT_MISMATCH");
        if (intent.ClaimedTenantId != _actor.Context.TenantId)
            return new("ISOLATED_THEME_CLIENT_TENANT_DENIED");
        if (observedRevision != _actor.Context.ActiveRevision)
            return new("ISOLATED_THEME_REVISION_MISMATCH");
        if (observedServerSessionEpoch != _actor.Context.SessionEpoch)
            return new("ISOLATED_THEME_SESSION_EPOCH_MISMATCH");
        if (!string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal))
            return new("ISOLATED_THEME_SOURCE_SHA_MISMATCH");
        if (_catalog.TenantId != _actor.Context.TenantId)
            return new("ISOLATED_THEME_CATALOG_TENANT_MISMATCH");

        // Application validates the same current-looking *synthetic* evidence
        // for membership, RBAC, revocation, typed values, revision and session.
        // It returns a preflight requirement, never a mutation authorization.
        var application = new App(new CapturedActorReader(_actor),
            new CapturedCatalogReader(_catalog));
        return await application.PrepareAsync(intent, cancellationToken);
    }

    private sealed class CapturedActorReader(App.ActorSnapshot captured)
        : Solqaryn.Application.AutomationV5.IC0501DesignTokenActorReader
    {
        public ValueTask<App.ActorSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<App.ActorSnapshot?>(captured);
        }
    }

    private sealed class CapturedCatalogReader(App.CatalogSnapshot captured)
        : Solqaryn.Application.AutomationV5.IC0501DesignTokenCatalogReader
    {
        public ValueTask<App.CatalogSnapshot?> ReadAsync(
            int authenticatedTenantId, string exactKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<App.CatalogSnapshot?>(
                captured.TenantId == authenticatedTenantId &&
                string.Equals(captured.ExactKey, exactKey, StringComparison.Ordinal)
                    ? captured : null);
        }
    }

    private static bool CanonicalSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
