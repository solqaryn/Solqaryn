using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Evidence is loaded server-side, never from a browser DTO or an arbitrary header.
/// The resulting verdict is a preflight classification, not an access grant.
/// </summary>
public sealed record V5UiLightnessTrustedSnapshot(
    V5UiLightnessDomainRules.TrustedRequestContext Request,
    V5UiLightnessDomainRules.TrustedBuildEvidence Build);

public interface IV5UiLightnessTrustedSnapshotSource
{
    Task<V5UiLightnessTrustedSnapshot?> LoadAsync(
        int usuarioId, int empresaId, string resourceId,
        CancellationToken cancellationToken);
}

/// <summary>
/// C05-04.M03.02: Application boundary for tenant-scoped performance preflight.
/// No endpoint, DI registration, HTTP request, provider or persistent store.
/// </summary>
public sealed class V5UiLightnessApplicationService
{
    private readonly IV5UiLightnessTrustedSnapshotSource _source;

    public V5UiLightnessApplicationService(IV5UiLightnessTrustedSnapshotSource source)
        => _source = source ?? throw new ArgumentNullException(nameof(source));

    public async Task<V5UiLightnessDomainRules.Verdict> EvaluateCurrentAsync(
        ContextoTenantActual authenticated,
        int empresaId,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authenticated);

        // Fail before any I/O: tenant A can never ask the trusted source about B.
        authenticated.ExigirEmpresa(empresaId);
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("Un recurso confiable es obligatorio.", nameof(resourceId));
        cancellationToken.ThrowIfCancellationRequested();

        var trusted = await _source.LoadAsync(
            authenticated.UsuarioId, empresaId, resourceId, cancellationToken);
        if (trusted?.Request is null || trusted.Build is null)
            return new V5UiLightnessDomainRules.Verdict("TRUSTED_EVIDENCE_REQUIRED");

        // The source is trusted by contract, but its payload cannot silently
        // change the actor/tenant, even if it is stale or misconfigured.
        if (trusted.Request.AuthenticatedTenantId != empresaId ||
            trusted.Request.PersistedResourceTenantId != empresaId)
            return new V5UiLightnessDomainRules.Verdict("TENANT_CACHE_ISOLATION_REQUIRED");

        return V5UiLightnessDomainRules.Evaluate(trusted.Request, trusted.Build);
    }
}
