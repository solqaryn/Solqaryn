using Gate = Solqaryn.Application.AutomationV5.V17IntelligentTenantLoginAuthorization;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-01.M03.04: isolated, read-only snapshot of an authenticated user's
/// tenant preference. Synthetic evidence only: not an EF/MySQL repository,
/// live JWT reader, session store, tenant router or production authority.
/// </summary>
public sealed class C0201TenantPreferenceIsolatedSnapshotAdapter
{
    private readonly int _actorId;
    private readonly long _revision;
    private readonly string _sourceSha;
    private readonly Gate.TrustedSnapshot _captured;

    public C0201TenantPreferenceIsolatedSnapshotAdapter(
        int authenticatedActorId, long capturedRevision, string sourceSha,
        Gate.TrustedSnapshot captured)
    {
        if (authenticatedActorId <= 0)
            throw new ArgumentOutOfRangeException(nameof(authenticatedActorId));
        if (capturedRevision <= 0)
            throw new ArgumentOutOfRangeException(nameof(capturedRevision));
        if (!FullSha(sourceSha))
            throw new ArgumentException("Full source SHA is required.", nameof(sourceSha));
        ArgumentNullException.ThrowIfNull(captured);
        if (captured.Memberships?.Any(m => m is null) == true)
            throw new ArgumentException("Null membership is not valid.", nameof(captured));

        _actorId = authenticatedActorId;
        _revision = capturedRevision;
        _sourceSha = sourceSha;
        // Freeze the collection. The adapter must not observe later mutations
        // of a caller-owned List while resolving a request.
        _captured = captured with
        {
            Memberships = captured.Memberships?.Select(m => m with { }).ToArray()
        };
    }

    public bool IsLivePersistenceProvider => false;
    public bool IssuesJwt => false;
    public bool AuthorizesResourceAccess => false;
    public bool InstallsTenantContext => false;
    public bool WritesPreference => false;
    public bool PerformsTenantSwitch => false;
    public bool ReadsOrWritesMysql => false;

    public ValueTask<Gate.Decision> EvaluateReadOnlyAsync(
        Gate.Request? request, int observedAuthenticatedActorId,
        long observedRevision, string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null)
            return Denied("ISOLATED_PREFERENCE_REQUEST_REQUIRED");
        if (observedAuthenticatedActorId != _actorId || _captured.ActorId != _actorId)
            return Denied("ISOLATED_PREFERENCE_ACTOR_MISMATCH");
        if (observedRevision != _revision)
            return Denied("ISOLATED_PREFERENCE_REVISION_MISMATCH");
        if (!string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal))
            return Denied("ISOLATED_PREFERENCE_SOURCE_SHA_MISMATCH");

        // The existing Application gate performs current actor/membership/
        // preference/session/role checks on the captured synthetic evidence.
        // Its Decision never issues a JWT or grants access. A separate trusted
        // backend adapter must re-read live persistence before any action.
        return new Gate(new CapturedReader(_captured))
            .ResolveAsync(request, cancellationToken);
    }

    private sealed class CapturedReader : ITrustedTenantLoginAuthorityReader
    {
        private readonly Gate.TrustedSnapshot _snapshot;

        public CapturedReader(Gate.TrustedSnapshot snapshot) => _snapshot = snapshot;

        public ValueTask<Gate.TrustedSnapshot?> ReadCurrentAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Gate.TrustedSnapshot?>(_snapshot);
        }
    }

    private static ValueTask<Gate.Decision> Denied(string code) =>
        ValueTask.FromResult(new Gate.Decision(
            code, null, Array.Empty<Gate.Choice>()));

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
