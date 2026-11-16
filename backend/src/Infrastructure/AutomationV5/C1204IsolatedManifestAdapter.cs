using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C12-04.M03.04 process-local synthetic manifest; not a trusted MySQL proof.
/// No SQL, EF, HTTP, JWT authentication, cutover or production authority.
/// </summary>
public sealed class C1204IsolatedManifestAdapter
{
    public enum Checkpoint
    {
        SchemaInventoried = 1, TenantPartitionsReviewed = 2,
        SyntheticCountsCompared = 3, RollbackOutlined = 4
    }

    public sealed record Draft(string? TenantId, string? ManifestKey,
        string? ExpectedSourceSha, long ExpectedRevision,
        IReadOnlyList<Checkpoint>? Checkpoints);

    public sealed record Manifest(string TenantId, string ManifestKey, string SourceSha,
        long Revision, IReadOnlyList<Checkpoint> Checkpoints);

    public readonly record struct Result(string Code, long Revision, Manifest? Data)
    {
        public bool IsAuthoritativeEvidence => false;
        public bool CertifiesMySqlRestore => false;
        public bool GrantsSqlOrMigration => false;
        public bool AuthorizesCutover => false;
        public bool AuthorizesQaOrProduction => false;
    }

    private sealed record Stored(string ActorId, string Sha, long Revision, Checkpoint[] Steps);
    private readonly object _sync = new();
    private readonly Dictionary<(string Tenant, string Key), Stored> _items = new();

    private static Result Deny(string code) => new(code, 0, null);
    private static bool SafeId(string? text) =>
        text is { Length: >= 1 and <= 128 } &&
        text.All(ch => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') ||
                       (ch >= '0' && ch <= '9') || ch is '-' or '_' or '.');
    private static bool SafeKey(string? text) =>
        text is { Length: >= 3 and <= 64 } &&
        !text.Contains("..", StringComparison.Ordinal) && SafeId(text);
    private static bool FullSha(string? text) =>
        text is { Length: 40 } && text.All(Uri.IsHexDigit);
    private static bool ActorMatches(C1204VerifiedActor? actor, string? tenant) =>
        actor is not null && SafeId(actor.ActorId) && SafeId(actor.TenantId) &&
        SafeId(tenant) && string.Equals(actor.TenantId, tenant, StringComparison.Ordinal) &&
        actor.JwtAuthenticatedByServer && actor.MembershipCurrent &&
        !actor.MembershipRevoked && actor.CanReviewCutover && actor.RecentMfaVerified;
    private static Manifest Copy(string tenant, string key, Stored stored) =>
        new(tenant, key, stored.Sha, stored.Revision,
            Array.AsReadOnly((Checkpoint[])stored.Steps.Clone()));

    public Result Stage(C1204VerifiedActor? trustedActor, Draft? request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || !ActorMatches(trustedActor, request.TenantId))
            return Deny("CURRENT_SERVER_ACTOR_REQUIRED");
        if (!SafeKey(request.ManifestKey) || !FullSha(request.ExpectedSourceSha) ||
            request.ExpectedRevision < 0 || request.Checkpoints is null)
            return Deny("INVALID_ISOLATED_MANIFEST");

        // Capture mutable caller input exactly once before validation and storage.
        Checkpoint[] steps;
        try
        {
            var count = request.Checkpoints.Count;
            if (count is < 1 or > 4) return Deny("INVALID_ISOLATED_MANIFEST");
            var captured = new List<Checkpoint>(count);
            var seen = new HashSet<Checkpoint>();
            foreach (var step in request.Checkpoints)
            {
                if (captured.Count >= count || !Enum.IsDefined(step) || !seen.Add(step))
                    return Deny("INVALID_ISOLATED_MANIFEST");
                captured.Add(step);
            }
            if (captured.Count != count) return Deny("INVALID_ISOLATED_MANIFEST");
            steps = captured.ToArray();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            return Deny("INVALID_ISOLATED_MANIFEST");
        }

        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = (request.TenantId!, request.ManifestKey!);
            _items.TryGetValue(id, out var previous);
            if ((previous is null && request.ExpectedRevision != 0) ||
                (previous is not null &&
                 (previous.Revision != request.ExpectedRevision ||
                  !string.Equals(previous.Sha, request.ExpectedSourceSha, StringComparison.Ordinal) ||
                  !string.Equals(previous.ActorId, trustedActor!.ActorId, StringComparison.Ordinal))) ||
                request.ExpectedRevision == long.MaxValue)
                return Deny("ISOLATED_REVISION_CONFLICT");

            if (previous is null && _items.Keys.Count(k => k.Tenant == request.TenantId) >= 32)
                return Deny("ISOLATED_TENANT_CAPACITY_REACHED");

            var stored = new Stored(trustedActor!.ActorId!, request.ExpectedSourceSha!,
                request.ExpectedRevision + 1, steps);
            _items[id] = stored;
            return new("SYNTHETIC_MANIFEST_ONLY_NO_MYSQL_PROOF", stored.Revision,
                Copy(request.TenantId!, request.ManifestKey!, stored));
        }
    }

    public Result Read(C1204VerifiedActor? trustedActor, string? tenantId,
        string? manifestKey, string? expectedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ActorMatches(trustedActor, tenantId))
            return Deny("CURRENT_SERVER_ACTOR_REQUIRED");
        if (!SafeKey(manifestKey) || !FullSha(expectedSourceSha))
            return Deny("INVALID_ISOLATED_MANIFEST");
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_items.TryGetValue((tenantId!, manifestKey!), out var stored) ||
                !string.Equals(stored.ActorId, trustedActor!.ActorId, StringComparison.Ordinal) ||
                !string.Equals(stored.Sha, expectedSourceSha, StringComparison.Ordinal))
                return Deny("ISOLATED_MANIFEST_NOT_FOUND");
            return new("SYNTHETIC_MANIFEST_ONLY_NO_MYSQL_PROOF", stored.Revision,
                Copy(tenantId!, manifestKey!, stored));
        }
    }
}
