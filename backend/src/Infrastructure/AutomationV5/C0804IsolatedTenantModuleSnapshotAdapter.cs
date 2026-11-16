using Fence = Solqaryn.Application.AutomationV5.C0804SectorModuleAuthorizationFence;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>C08-04.M03.04: synthetic scoped snapshot, never a live authority or durable CAS.</summary>
public sealed class C0804IsolatedTenantModuleSnapshotAdapter : IC0804TenantModuleSource
{
    private readonly string _tenant;
    private readonly string _key;
    private readonly long _revision;
    private readonly string _sha;
    private readonly Fence.ModuleEvidence _snapshot;

    public C0804IsolatedTenantModuleSnapshotAdapter(
        string tenant, string key, long revision, string sha, Fence.ModuleEvidence snapshot)
    {
        if (!SafeToken(tenant) || !SafeToken(key))
            throw new ArgumentException("Bounded tenant and module required.");
        if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
        if (!FullSha(sha)) throw new ArgumentException("Full SHA required.", nameof(sha));
        ArgumentNullException.ThrowIfNull(snapshot);
        _tenant = tenant;
        _key = key;
        _revision = revision;
        _sha = sha;
        // A client-captured snapshot cannot claim persisted authority or an audit/CAS fence.
        _snapshot = snapshot with { PersistedTenantReadVerified = false, AuditFenceAvailable = false };
    }

    public bool ReadsLiveMysql => false;
    public bool ReadsJwtOrRbac => false;
    public bool ActivatesModule => false;
    public bool PerformsDurableCas => false;
    public bool WritesOrMigrates => false;

    public ValueTask<Fence.ModuleEvidence?> ReadAsync(
        string authenticatedTenantId, string exactModuleKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!string.Equals(authenticatedTenantId, _tenant, StringComparison.Ordinal) ||
            !string.Equals(exactModuleKey, _key, StringComparison.Ordinal) ||
            !string.Equals(_snapshot.TenantOwner, _tenant, StringComparison.Ordinal) ||
            !string.Equals(_snapshot.ModuleKey, _key, StringComparison.Ordinal) ||
            !string.Equals(_snapshot.SourceSha, _sha, StringComparison.Ordinal) ||
            _snapshot.Revision != _revision)
            return ValueTask.FromResult<Fence.ModuleEvidence?>(null);
        return ValueTask.FromResult<Fence.ModuleEvidence?>(_snapshot);
    }

    private static bool SafeToken(string? v) =>
        v is { Length: > 0 and <= 96 } &&
        v.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                   c is '.' or '_' or '-');

    private static bool FullSha(string? v) =>
        v is { Length: 40 } && v.All(Uri.IsHexDigit);
}
