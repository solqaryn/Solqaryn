using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Security;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C05-04.M03.04. Isolated, read-only snapshot adapter for UI-lightness evidence.
/// Deliberately not registered in API DI and not a live identity, cache, EF, HTTP,
/// Core Web Vitals or browser provider. It cannot authorize access or persistence.
/// </summary>
public sealed class V5UiLightnessIsolatedSnapshotAdapter
{
    private readonly int _tenantId;
    private readonly string _resourceId;
    private readonly string _sourceSha;
    private readonly V5UiLightnessTrustedSnapshot _captured;

    public V5UiLightnessIsolatedSnapshotAdapter(
        int tenantId, string resourceId, string sourceSha,
        V5UiLightnessTrustedSnapshot captured)
    {
        if (tenantId <= 0) throw new ArgumentOutOfRangeException(nameof(tenantId));
        if (!SafeResourceId(resourceId)) throw new ArgumentException("Resource must be opaque and bounded.", nameof(resourceId));
        if (!ValidSha(sourceSha)) throw new ArgumentException("A lowercase full source SHA is required.", nameof(sourceSha));
        ArgumentNullException.ThrowIfNull(captured);
        ArgumentNullException.ThrowIfNull(captured.Request);
        ArgumentNullException.ThrowIfNull(captured.Build);

        _tenantId = tenantId;
        _resourceId = resourceId;
        _sourceSha = sourceSha;
        // Both records contain only immutable scalar values. No caller-owned
        // collections or live authority handles are retained by this adapter.
        _captured = captured with
        {
            Request = captured.Request with { },
            Build = captured.Build with { }
        };
    }

    public bool GrantsAccess => false;
    public bool AllowsPersistence => false;
    public bool CertifiesBrowserVitals => false;
    public bool IsLiveProvider => false;

    public async Task<V5UiLightnessDomainRules.Verdict> EvaluateReadOnlyAsync(
        ContextoTenantActual authenticated, string? requestedResource,
        string? observedSourceSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(authenticated);

        if (!SafeResourceId(requestedResource) ||
            !string.Equals(requestedResource, _resourceId, StringComparison.Ordinal))
            return new("ISOLATED_RESOURCE_SCOPE_MISMATCH");

        if (!ValidSha(observedSourceSha) ||
            !string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(_captured.Build.ExpectedCommitSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(_captured.Build.MeasuredCommitSha, _sourceSha, StringComparison.Ordinal))
            return new("ISOLATED_SOURCE_SHA_MISSING_OR_STALE");

        // A verified backend actor must already own this tenant. Never take
        // tenant identity from a browser-selected ID or cache key.
        authenticated.ExigirEmpresa(_tenantId);

        if (_captured.Request.AuthenticatedTenantId != _tenantId ||
            _captured.Request.PersistedResourceTenantId != _tenantId)
            return new("TENANT_CACHE_ISOLATION_REQUIRED");

        var source = new CapturedReadOnlySource(
            authenticated.UsuarioId, _tenantId, _resourceId, _captured);
        // Exercise the real Application + Domain preflight without exporting
        // this fixture as a DI-registered trusted backend provider.
        return await new V5UiLightnessApplicationService(source).EvaluateCurrentAsync(
            authenticated, _tenantId, _resourceId, cancellationToken);
    }

    private sealed class CapturedReadOnlySource : IV5UiLightnessTrustedSnapshotSource
    {
        private readonly int _actorId;
        private readonly int _tenantId;
        private readonly string _resourceId;
        private readonly V5UiLightnessTrustedSnapshot _evidence;

        public CapturedReadOnlySource(int actorId, int tenantId,
            string resourceId, V5UiLightnessTrustedSnapshot evidence)
        {
            _actorId = actorId;
            _tenantId = tenantId;
            _resourceId = resourceId;
            _evidence = evidence;
        }

        public Task<V5UiLightnessTrustedSnapshot?> LoadAsync(
            int usuarioId, int empresaId, string resourceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            V5UiLightnessTrustedSnapshot? result =
                usuarioId == _actorId && empresaId == _tenantId &&
                string.Equals(resourceId, _resourceId, StringComparison.Ordinal)
                    ? _evidence : null;
            return Task.FromResult(result);
        }
    }

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } && value.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool SafeResourceId(string? value) =>
        value is { Length: > 0 and <= 128 } &&
        value.All(c => (c >= '0' && c <= '9') ||
                       (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       c == '_' || c == '-');
}
