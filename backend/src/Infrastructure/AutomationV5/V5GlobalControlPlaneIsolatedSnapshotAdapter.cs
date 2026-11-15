using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5GlobalControlPlaneAuthorizationGate;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-04.M03.04: isolated, read-only, non-authorizing snapshot adapter.
/// It does not query MySQL, authenticate JWT, grant platform roles, implement
/// MFA, persist audit records or register a live dependency in the API.
/// </summary>
public sealed class V5GlobalControlPlaneIsolatedSnapshotAdapter
{
    private readonly string _actorId;
    private readonly string _tenantId;
    private readonly string _sourceSha;
    private readonly Gate.Evidence _captured;

    public V5GlobalControlPlaneIsolatedSnapshotAdapter(
        string actorId, string tenantId, string sourceSha, Gate.Evidence evidence)
    {
        if (!SafeId(actorId)) throw new ArgumentException("Verified actor identifier required.", nameof(actorId));
        if (!SafeId(tenantId)) throw new ArgumentException("Verified tenant identifier required.", nameof(tenantId));
        if (!ValidSha(sourceSha)) throw new ArgumentException("Full lowercase source SHA required.", nameof(sourceSha));
        ArgumentNullException.ThrowIfNull(evidence);
        _actorId = actorId;
        _tenantId = tenantId;
        _sourceSha = sourceSha;
        // All snapshot fields are immutable scalar values or immutable records.
        // Never hold a mutable grant collection or live provider handle.
        _captured = evidence with
        {
            Snapshot = evidence.Snapshot is null ? null : evidence.Snapshot with { }
        };
    }

    public bool AllowsPersistence => false;
    public bool GrantsAccess => false;
    public bool GrantsPlatformDirectory => false;
    public bool GrantsExceptionalTenantLease => false;
    public bool IsLiveProvider => false;

    public async ValueTask<Gate.Result> EvaluateReadOnlyAsync(
        Gate.Request? request, string? observedActorId,
        string? observedTenantId, string? observedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null) return new Gate.Result("ISOLATED_CONTROL_PLANE_REQUEST_REQUIRED");

        // Require caller-provided scope to be identical to the captured server
        // scope. None of these strings is authority derived from a browser.
        if (!SafeId(observedActorId) ||
            !string.Equals(observedActorId, _actorId, StringComparison.Ordinal))
            return new Gate.Result("ISOLATED_CONTROL_PLANE_ACTOR_MISMATCH");
        if (!SafeId(observedTenantId) ||
            !string.Equals(observedTenantId, _tenantId, StringComparison.Ordinal) ||
            !string.Equals(request.RequestedTenantId, _tenantId, StringComparison.Ordinal))
            return new Gate.Result("ISOLATED_CONTROL_PLANE_TENANT_MISMATCH");
        if (!ValidSha(observedSourceSha) ||
            !string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(request.ExpectedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(_captured.ObservedSourceSha, _sourceSha, StringComparison.Ordinal))
            return new Gate.Result("ISOLATED_CONTROL_PLANE_SOURCE_SHA_MISMATCH");

        if (!string.Equals(_captured.VerifiedActorId, _actorId, StringComparison.Ordinal) ||
            !string.Equals(_captured.VerifiedTenantId, _tenantId, StringComparison.Ordinal) ||
            !string.Equals(_captured.Snapshot?.ActorId, _actorId, StringComparison.Ordinal) ||
            !string.Equals(_captured.Snapshot?.VerifiedTargetTenantId, _tenantId, StringComparison.Ordinal))
            return new Gate.Result("ISOLATED_CONTROL_PLANE_CAPTURED_AUTHORITY_MISMATCH");

        var reader = new CapturedReadOnlyReader(_captured);
        // Reuse the actual Application and Domain gates so MFA, grant revocation,
        // independent approval and bounds are tested instead of duplicated.
        return await new Gate(reader).EvaluateAsync(request, cancellationToken);
    }

    private sealed class CapturedReadOnlyReader : IV5GlobalControlPlaneVerifiedEvidenceReader
    {
        private readonly Gate.Evidence _evidence;
        public CapturedReadOnlyReader(Gate.Evidence evidence) => _evidence = evidence;
        public ValueTask<Gate.Evidence?> ReadVerifiedAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Gate.Evidence?>(_evidence);
        }
    }

    private static bool SafeId(string? value) =>
        value is { Length: > 0 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') ||
                       c == '-' || c == '_');

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => (c >= '0' && c <= '9') ||
                       (c >= 'a' && c <= 'f'));
}
