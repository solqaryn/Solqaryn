using Solqaryn.Application.AutomationV5;
using Adapter = Solqaryn.Infrastructure.AutomationV5.C1204IsolatedManifestAdapter;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Trusted in-process port for a synthetic manifest snapshot only.
/// Not a MySQL, restore, migration, cloud backup or external provider.
/// </summary>
public interface IC1204SyntheticManifestReadPort
{
    ValueTask<Adapter.Result> ReadAsync(
        C1204VerifiedActor verifiedActor, string tenant, string key, string sourceSha,
        CancellationToken cancellationToken);
}

/// <summary>Adapter-backed implementation; no write, SQL, HTTP or real restore.</summary>
public sealed class C1204SyntheticManifestReadPort : IC1204SyntheticManifestReadPort
{
    private readonly Adapter _adapter;
    public C1204SyntheticManifestReadPort(Adapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public ValueTask<Adapter.Result> ReadAsync(
        C1204VerifiedActor verifiedActor, string tenant, string key, string sourceSha,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(_adapter.Read(verifiedActor, tenant, key, sourceSha, cancellationToken));
}

/// <summary>
/// C12-04.M03.05: bounded sequential read-only worker for isolated synthetic
/// cutover manifests. No hosted service, IO, SQL, exporter, actual backup,
/// migration or production authority is registered.
/// </summary>
public sealed class C1204SyntheticManifestReviewWorker
{
    public const int MaximumBatch = 8;
    private readonly IC1204SyntheticManifestReadPort _port;
    public C1204SyntheticManifestReviewWorker(IC1204SyntheticManifestReadPort port) =>
        _port = port ?? throw new ArgumentNullException(nameof(port));

    public sealed record Ticket(string? Tenant, string? Key, string? SourceSha, long Revision);
    public readonly record struct Review(string Code, int Examined)
    {
        public bool CertifiesMysqlBackupOrRestore => false;
        public bool AuthorizesSqlOrCutover => false;
        public bool GrantsTenantAccess => false;
        public bool ExecutesHostedWorker => false;
        public bool AuthorizesQaOrProduction => false;
        public bool WritesPersistentData => false;
    }
    private static Review Deny(string code) => new(code, 0);

    public async Task<Review> ReviewAsync(
        C1204VerifiedActor? verifiedActor, IReadOnlyList<Ticket>? requests,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Trusted(verifiedActor)) return Deny("CURRENT_VERIFIED_SERVER_ACTOR_REQUIRED");
        if (requests is null) return Deny("INVALID_SYNTHETIC_MANIFEST_BATCH");

        // Snapshot the bounded request before the first await/source access.
        // Never enumerate a potentially misleading caller-owned collection.
        Ticket[] tickets;
        try
        {
            var n = requests.Count;
            if (n is < 1 or > MaximumBatch) return Deny("INVALID_SYNTHETIC_MANIFEST_BATCH");
            tickets = new Ticket[n];
            for (var i = 0; i < n; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ticket = requests[i];
                if (ticket is null) return Deny("INVALID_SYNTHETIC_MANIFEST_BATCH");
                tickets[i] = ticket;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            return Deny("INVALID_SYNTHETIC_MANIFEST_BATCH");
        }

        var sha = tickets[0].SourceSha;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in tickets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(t.Tenant, verifiedActor!.TenantId, StringComparison.Ordinal) ||
                !SafeKey(t.Key) || !LowercaseSha(t.SourceSha) ||
                !string.Equals(sha, t.SourceSha, StringComparison.Ordinal) ||
                t.Revision is < 1 or > 1000000 ||
                !keys.Add(t.Key!))
                return Deny("INVALID_OR_CROSS_TENANT_MANIFEST");
        }

        foreach (var t in tickets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Adapter.Result observed;
            try
            {
                observed = await _port.ReadAsync(verifiedActor!, t.Tenant!, t.Key!,
                    t.SourceSha!, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return Deny("SYNTHETIC_SOURCE_UNAVAILABLE");
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (observed.Code != "SYNTHETIC_MANIFEST_ONLY_NO_MYSQL_PROOF" ||
                observed.Data is null || observed.Revision != t.Revision ||
                !string.Equals(observed.Data.TenantId, t.Tenant, StringComparison.Ordinal) ||
                !string.Equals(observed.Data.ManifestKey, t.Key, StringComparison.Ordinal) ||
                !string.Equals(observed.Data.SourceSha, t.SourceSha, StringComparison.Ordinal) ||
                observed.Data.Revision != t.Revision ||
                observed.IsAuthoritativeEvidence || observed.CertifiesMySqlRestore ||
                observed.GrantsSqlOrMigration || observed.AuthorizesCutover ||
                observed.AuthorizesQaOrProduction)
                return Deny("SYNTHETIC_MANIFEST_NOT_VERIFIED");
        }
        // This is proof of in-process synthetic matching only, NEVER MySQL restore.
        return new("SYNTHETIC_MANIFEST_REVIEW_ONLY_NO_MYSQL_PROOF", tickets.Length);
    }

    private static bool Trusted(C1204VerifiedActor? actor) =>
        actor is not null && SafeId(actor.ActorId) && SafeId(actor.TenantId) &&
        actor.JwtAuthenticatedByServer && actor.MembershipCurrent &&
        !actor.MembershipRevoked && actor.CanReviewCutover && actor.RecentMfaVerified;
    private static bool SafeId(string? value) =>
        value is { Length: >= 1 and <= 128 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_' or '.');
    private static bool SafeKey(string? value) =>
        value is { Length: >= 3 and <= 64 } && !value.Contains("..", StringComparison.Ordinal) &&
        SafeId(value);
    private static bool LowercaseSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
