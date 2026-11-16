using Rules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// A future trusted, read-only backend adapter may report sector module provider health.
/// Implementations must not enqueue work, grant capabilities or mutate module lifecycle.
/// </summary>
public interface IC0804SectorModuleReadinessSource
{
    ValueTask<C0804SectorModuleReadOnlyIntegrationBoundary.ProviderSnapshot?> ReadAsync(
        string verifiedTenantId, string exactModuleKey, CancellationToken cancellationToken);
}

/// <summary>
/// C08-04.M03.05: inert integration boundary for a sector module provider.
/// The existing M03.03 authorization fence must be satisfied before any read-only probe.
/// No DI registration, worker, queue, external provider, SQL, JWT middleware or activation.
/// </summary>
public sealed class C0804SectorModuleReadOnlyIntegrationBoundary
{
    private readonly C0804SectorModuleAuthorizationFence _fence;
    private readonly IC0804SectorModuleReadinessSource _source;

    public C0804SectorModuleReadOnlyIntegrationBoundary(
        C0804SectorModuleAuthorizationFence fence,
        IC0804SectorModuleReadinessSource source)
    {
        _fence = fence ?? throw new ArgumentNullException(nameof(fence));
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public sealed record ProviderSnapshot(
        string? TenantId, string? ModuleKey, string? SourceSha, long ModuleRevision,
        bool UsesReadOnlyTransport, bool ProviderReportsReady);

    public readonly record struct Result(string Code)
    {
        public bool GrantsTenantAccess => false;
        public bool EnablesModule => false;
        public bool EnqueuesWorker => false;
        public bool CallsMutatingProvider => false;
        public bool WritesDatabase => false;
        public bool CertifiesRuntimeProvider => false;
        public bool AuthorizesRelease => false;
    }

    public async ValueTask<Result> ProbeAsync(
        C0804SectorModuleAuthorizationFence.Intent? intent,
        bool clientRequestsDispatch = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || intent.Operation != Rules.Operation.Inspect)
            return new("READ_ONLY_INSPECTION_REQUIRED");
        if (clientRequestsDispatch || intent.ClientClaimsPermissions ||
            intent.ClientRequestsGlobalMutation)
            return new("UNTRUSTED_DISPATCH_OR_PRIVILEGE_DENIED");

        // The fence checks trusted actor, relational RBAC, tenant ownership, audit
        // availability, source SHA, module revision and capability allowlist.
        var preflight = await _fence.InspectAsync(intent, cancellationToken);
        if (!preflight.NeedsFutureAuthenticatedCasAdapter)
            return new(preflight.Code);

        // Only an already-authorized backend actor can cause a read-only probe.
        // This is not a durable CAS or proof that an external provider is connected.
        ProviderSnapshot? response;
        try
        {
            response = await _source.ReadAsync(
                intent.TenantAssertion!, intent.ModuleKey!, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("READ_ONLY_PROVIDER_UNAVAILABLE");
        }

        if (response is null || !response.UsesReadOnlyTransport ||
            !string.Equals(response.TenantId, intent.TenantAssertion, StringComparison.Ordinal) ||
            !string.Equals(response.ModuleKey, intent.ModuleKey, StringComparison.Ordinal) ||
            !string.Equals(response.SourceSha, intent.ExpectedSourceSha, StringComparison.Ordinal) ||
            response.ModuleRevision != intent.ExpectedRevision)
            return new("UNTRUSTED_PROVIDER_SNAPSHOT_DENIED");

        return new(response.ProviderReportsReady
            ? "READ_ONLY_PROVIDER_READY_NON_AUTHORIZING"
            : "READ_ONLY_PROVIDER_NOT_READY");
    }
}
