using System.Collections.Frozen;
using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;
using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C01-02.M03.04: synthetic, tenant-scoped, read-only evidence only.
/// Not a live persistence source, RBAC reader, resource access grant or
/// transaction. Deliberately returns non-authoritative flags to the gate.
/// </summary>
public sealed class C0102IsolatedCapabilityPolicySnapshotAdapter :
    IC0102VerifiedCapabilityPolicyReader
{
    private readonly int _tenantId;
    private readonly int _resourceId;
    private readonly long _revision;
    private readonly string _sourceSha;
    private readonly Fence.VerifiedResourcePolicy _captured;

    public C0102IsolatedCapabilityPolicySnapshotAdapter(
        int authenticatedTenantId, int resourceId, long revision,
        string sourceSha, Fence.VerifiedResourcePolicy captured)
    {
        if (authenticatedTenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(authenticatedTenantId));
        if (resourceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(resourceId));
        if (revision <= 0)
            throw new ArgumentOutOfRangeException(nameof(revision));
        if (!FullSha(sourceSha))
            throw new ArgumentException("Full source SHA required.", nameof(sourceSha));
        ArgumentNullException.ThrowIfNull(captured);
        _tenantId = authenticatedTenantId;
        _resourceId = resourceId;
        _revision = revision;
        _sourceSha = sourceSha;

        // Capture immutable copies of caller-owned mutable collections.
        // A permissive comparer or later mutations cannot broaden a capability.
        var policy = captured.Policy;
        _captured = captured with
        {
            OwnershipReadFromTrustedPersistence = false,
            CurrentCapabilityCatalogReadFromBackend = false,
            Policy = policy is null ? null : policy with
            {
                ExplicitRelationalRbacGrant = false,
                PlanCurrent = false,
                PlanCapabilities = Freeze(policy.PlanCapabilities),
                RubroCapabilities = Freeze(policy.RubroCapabilities),
                RoleCapabilities = Freeze(policy.RoleCapabilities),
                TenantCapabilities = Freeze(policy.TenantCapabilities),
                DeniedCapabilities = Freeze(policy.DeniedCapabilities)
            }
        };
    }

    public bool ReadsLiveMysql => false;
    public bool ReadsLiveJwtOrRbac => false;
    public bool AuthorizesResourceAccess => false;
    public bool PerformsDurableCas => false;
    public bool WritesOrMigrates => false;

    public ValueTask<Fence.VerifiedResourcePolicy?> ReadCurrentAsync(
        int authenticatedTenantId, int resourceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (authenticatedTenantId != _tenantId || resourceId != _resourceId ||
            _captured.ResourceId != _resourceId ||
            _captured.OwnerTenantId != _tenantId ||
            _captured.VerifiedActorId <= 0 ||
            _captured.Policy is null ||
            _captured.Policy.TenantEmpresaId != _tenantId ||
            _captured.Policy.ResourceEmpresaId != _tenantId)
            return Denied();

        // Revision/SHA are capture provenance labels, not an authenticated
        // persistence revision or a current authority lookup.
        if (_revision <= 0 || !FullSha(_sourceSha))
            return Denied();
        return ValueTask.FromResult<Fence.VerifiedResourcePolicy?>(_captured);
    }

    // Never expose a HashSet through IReadOnlySet: callers can downcast it and
    // mutate the captured policy after the original source was copied.
    // FrozenSet owns an ordinal, genuinely immutable copy for every reader.
    private static IReadOnlySet<string>? Freeze(IReadOnlySet<string>? values) =>
        values is null ? null : values.ToFrozenSet(StringComparer.Ordinal);

    private static ValueTask<Fence.VerifiedResourcePolicy?> Denied() =>
        ValueTask.FromResult<Fence.VerifiedResourcePolicy?>(null);

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
