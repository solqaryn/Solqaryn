using System.Collections.Generic;
using System.Linq;
using Solqaryn.Infrastructure.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C13-04.M03.04. Bounded, isolated in-memory visual-review snapshot adapter.
/// This is not EF/MySQL persistence, a live authority provider, or a release gate.
/// Every read/write revalidates the current trusted authority source.
/// </summary>
public sealed class C1304VisualReviewSnapshotAdapter
{
    private readonly IC1304VisualReviewAuthoritySource _source;
    private readonly TimeProvider _clock;
    private readonly object _sync = new();
    private readonly Dictionary<(int TenantId, string Key), Stored> _items = new();

    private sealed record Stored(string SourceSha, long Revision,
        DateTimeOffset RecordedUtc, string[] Surfaces);

    public sealed record Snapshot(int TenantId, string Key, string SourceSha,
        long Revision, DateTimeOffset RecordedUtc, IReadOnlyList<string> Surfaces);

    public readonly record struct Outcome(string Code, long Revision, Snapshot? Data)
    {
        public bool GrantsJwtOrRbac => false;
        public bool GrantsTenantAccess => false;
        public bool CertifiesBrowserReview => false;
        public bool PersistsDurably => false;
        public bool AuthorizesQaOrProduction => false;
    }

    public C1304VisualReviewSnapshotAdapter(
        IC1304VisualReviewAuthoritySource source, TimeProvider clock)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    private static Outcome Deny(string code) => new(code, 0, null);

    private static bool SafeKey(string? value) =>
        value is { Length: >= 3 and <= 64 } &&
        value.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    private static Snapshot Copy(int tenantId, string key, Stored entry) =>
        new(tenantId, key, entry.SourceSha, entry.Revision, entry.RecordedUtc,
            Array.AsReadOnly((string[])entry.Surfaces.Clone()));

    private async Task<(string Code, HashSet<string>? Allowed)> CheckAsync(
        int tenantId, string sha, CancellationToken cancellationToken)
    {
        C1304VisualReviewAuthorityAdapter.Authority? current;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            current = await _source.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return ("CURRENT_AUTHORITY_UNAVAILABLE", null);
        }

        var now = _clock.GetUtcNow();
        if (current is null || current.ActorId <= 0 ||
            current.TenantId != tenantId || !current.IdentityVerified ||
            !current.MembershipCurrent || !current.ExplicitReviewPermission ||
            current.RoleRevoked || !current.SourceAuthenticated ||
            !FullSha(current.SourceSha) ||
            !string.Equals(current.SourceSha, sha, StringComparison.Ordinal) ||
            current.ValidUntilUtc <= now || current.ValidUntilUtc > now.AddMinutes(15) ||
            current.ServerApprovedSurfaces is null)
            return ("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED", null);

        // Defensive copy: never retain or return the authority source's mutable set.
        var allowed = new HashSet<string>(current.ServerApprovedSurfaces, StringComparer.Ordinal);
        if (allowed.Count is < 1 or > 64 || allowed.Any(x => !SafeKey(x)))
            return ("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED", null);

        return ("OK", allowed);
    }

    public async Task<Outcome> WriteAsync(
        int tenantId, string? key, string? expectedSha, long expectedRevision,
        IReadOnlyList<string>? surfaces, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tenantId <= 0 || !SafeKey(key) || !FullSha(expectedSha) ||
            expectedRevision < 0 || surfaces is null ||
            surfaces.Count is < 1 or > 64 || surfaces.Any(x => !SafeKey(x)) ||
            surfaces.Distinct(StringComparer.Ordinal).Count() != surfaces.Count)
            return Deny("INVALID_SNAPSHOT_SCOPE");

        var auth = await CheckAsync(tenantId, expectedSha!, cancellationToken);
        if (auth.Allowed is null) return Deny(auth.Code);
        if (surfaces.Any(x => !auth.Allowed.Contains(x)))
            return Deny("SURFACE_NOT_APPROVED_BY_CURRENT_AUTHORITY");

        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = (tenantId, key!);
            _items.TryGetValue(id, out var previous);
            if ((previous is null && expectedRevision != 0) ||
                (previous is not null &&
                 (previous.Revision != expectedRevision ||
                  !string.Equals(previous.SourceSha, expectedSha, StringComparison.Ordinal))) ||
                expectedRevision == long.MaxValue)
                return Deny("SNAPSHOT_REVISION_CONFLICT");

            if (previous is null && _items.Keys.Count(x => x.TenantId == tenantId) >= 64)
                return Deny("TENANT_SNAPSHOT_CAPACITY_REACHED");

            var stored = new Stored(expectedSha!, expectedRevision + 1,
                _clock.GetUtcNow(), surfaces.ToArray());
            _items[id] = stored;
            return new("SNAPSHOT_STAGED_BROWSER_A5_REQUIRED", stored.Revision,
                Copy(tenantId, key!, stored));
        }
    }

    public async Task<Outcome> ReadAsync(
        int tenantId, string? key, string? expectedSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tenantId <= 0 || !SafeKey(key) || !FullSha(expectedSha))
            return Deny("INVALID_SNAPSHOT_SCOPE");

        var auth = await CheckAsync(tenantId, expectedSha!, cancellationToken);
        if (auth.Allowed is null) return Deny(auth.Code);

        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_items.TryGetValue((tenantId, key!), out var stored) ||
                !string.Equals(stored.SourceSha, expectedSha, StringComparison.Ordinal))
                return Deny("SNAPSHOT_NOT_FOUND");
            if (stored.Surfaces.Any(x => !auth.Allowed.Contains(x)))
                return Deny("SURFACE_NOT_APPROVED_BY_CURRENT_AUTHORITY");
            return new("SNAPSHOT_STAGED_BROWSER_A5_REQUIRED", stored.Revision,
                Copy(tenantId, key!, stored));
        }
    }
}
