using System.Collections.Frozen;
using Solqaryn.Application.AutomationV5;
using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C01-02.M03.05. This port supplies per-request, non-authoritative capability
/// evidence from an injected source. It is not registered as a live DB/JWT/RBAC
/// reader, worker, queue, endpoint, or permission grant.
/// </summary>
public interface IC0102ReadThroughCapabilityEvidenceSource
{
    ValueTask<Fence.VerifiedResourcePolicy?> ReadUntrustedAsync(
        int tenantId, int resourceId, CancellationToken cancellationToken);
}

/// <summary>
/// Re-reads dynamic provider evidence on every call; never caches an ALLOW.
/// Any returned policy is downgraded to non-authoritative, deeply immutable
/// ordinal collections until a separately certified trusted data binding exists.
/// </summary>
public sealed class C0102ReadThroughCapabilityProvider :
    IC0102VerifiedCapabilityPolicyReader
{
    private readonly IC0102ReadThroughCapabilityEvidenceSource _source;

    public C0102ReadThroughCapabilityProvider(IC0102ReadThroughCapabilityEvidenceSource source)
        => _source = source ?? throw new ArgumentNullException(nameof(source));

    public bool ReadsLiveMysql => false;
    public bool ReadsLiveJwtOrRbac => false;
    public bool WritesOrMigrates => false;
    public bool AuthorizesResourceAccess => false;
    public bool CachesAuthorization => false;

    public async ValueTask<Fence.VerifiedResourcePolicy?> ReadCurrentAsync(
        int authenticatedTenantId, int resourceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (authenticatedTenantId <= 0 || resourceId <= 0)
            return null;

        Fence.VerifiedResourcePolicy? evidence;
        try
        {
            // The provider is a source of facts, never an authenticated grant.
            evidence = await _source.ReadUntrustedAsync(
                authenticatedTenantId, resourceId, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No raw provider errors or secret-bearing messages may escape.
            return null;
        }

        var policy = evidence?.Policy;
        if (evidence is null || policy is null ||
            evidence.ResourceId != resourceId ||
            evidence.OwnerTenantId != authenticatedTenantId ||
            evidence.VerifiedActorId <= 0 ||
            policy.TenantEmpresaId != authenticatedTenantId ||
            policy.ResourceEmpresaId != authenticatedTenantId ||
            policy.VerifiedActorId != evidence.VerifiedActorId ||
            policy.EffectiveRoleId <= 0 ||
            !policy.TenantActive || !policy.MembershipActive ||
            !policy.RoleActive || policy.PermissionRevoked ||
            policy.ResourceBranchId is <= 0 ||
            (policy.ResourceBranchId is int branch &&
                policy.AuthorizedBranchId != branch))
            return null;

        // Deep copies, not caller-owned HashSets masquerading as IReadOnlySet.
        // Provider flags cannot attest their own trusted backend provenance.
        return evidence with
        {
            OwnershipReadFromTrustedPersistence = false,
            CurrentCapabilityCatalogReadFromBackend = false,
            Policy = policy with
            {
                ExplicitRelationalRbacGrant = false,
                PlanCurrent = false,
                RubroCurrent = false,
                PlanCapabilities = Freeze(policy.PlanCapabilities),
                RubroCapabilities = Freeze(policy.RubroCapabilities),
                RoleCapabilities = Freeze(policy.RoleCapabilities),
                TenantCapabilities = Freeze(policy.TenantCapabilities),
                DeniedCapabilities = Freeze(policy.DeniedCapabilities)
            }
        };
    }

    private static IReadOnlySet<string>? Freeze(IReadOnlySet<string>? source)
        => source is null ? null : source.ToFrozenSet(StringComparer.Ordinal);
}
