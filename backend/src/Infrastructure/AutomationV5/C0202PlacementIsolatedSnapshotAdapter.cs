using Gate = Solqaryn.Application.AutomationV5.C0202PlacementApplicationGate;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-02.M03.03: in-memory, non-authorizing tenant placement snapshot
/// adapter. It never selects a connection, changes DB routing or cuts over
/// any tenant. Evidence is synthetic and not an authenticated backend.
/// </summary>
public sealed class C0202PlacementIsolatedSnapshotAdapter
{
    private readonly string _tenantId;
    private readonly string _actorId;
    private readonly string _sourceSha;
    private readonly long _revision;
    private readonly Gate.Evidence _captured;

    public C0202PlacementIsolatedSnapshotAdapter(
        string tenantId, string actorId, string sourceSha,
        long revision, Gate.Evidence evidence)
    {
        if (!SafeId(tenantId)) throw new ArgumentException("Safe tenant ID required.", nameof(tenantId));
        if (!SafeId(actorId)) throw new ArgumentException("Safe actor ID required.", nameof(actorId));
        if (!FullSha(sourceSha)) throw new ArgumentException("Full commit SHA required.", nameof(sourceSha));
        if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
        ArgumentNullException.ThrowIfNull(evidence);

        _tenantId = tenantId;
        _actorId = actorId;
        _sourceSha = sourceSha;
        _revision = revision;
        // Records contain only immutable scalar members. Capture at
        // construction, never use a mutable client-supplied storage source.
        _captured = evidence with
        {
            Snapshot = evidence.Snapshot is null ? null : evidence.Snapshot with { }
        };
    }

    public bool IsLiveStorageProvider => false;
    public bool AuthorizesConnection => false;
    public bool AuthorizesMigration => false;
    public bool AuthorizesCutover => false;
    public bool AuthorizesDualWrite => false;
    public bool AuthorizesCrossTenantFallback => false;
    public bool CanReadOrWriteTenantDatabase => false;
    public bool CertifiesJwtMySqlRestoreOrProduction => false;

    public ValueTask<Gate.Result> EvaluateReadOnlyAsync(
        Gate.Request? request, string? observedTenantId,
        string? observedActorId, long observedRevision,
        string? observedSourceSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null)
            return Denied("ISOLATED_PLACEMENT_REQUEST_REQUIRED");
        if (!string.Equals(observedTenantId, _tenantId, StringComparison.Ordinal) ||
            !string.Equals(request.TenantId, _tenantId, StringComparison.Ordinal))
            return Denied("ISOLATED_PLACEMENT_TENANT_MISMATCH");
        if (!string.Equals(observedActorId, _actorId, StringComparison.Ordinal))
            return Denied("ISOLATED_PLACEMENT_ACTOR_MISMATCH");
        if (observedRevision != _revision || request.ExpectedRevision != _revision)
            return Denied("ISOLATED_PLACEMENT_REVISION_MISMATCH");
        if (!string.Equals(observedSourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(request.SourceSha, _sourceSha, StringComparison.Ordinal))
            return Denied("ISOLATED_PLACEMENT_SHA_MISMATCH");

        // Independent verification of the captured source prevents client
        // identifiers and a forged reader from laundering tenant ownership.
        var snapshot = _captured.Snapshot;
        if (snapshot is null ||
            !string.Equals(_captured.SourceSha, _sourceSha, StringComparison.Ordinal) ||
            !string.Equals(_captured.VerifiedTenantId, _tenantId, StringComparison.Ordinal) ||
            !string.Equals(_captured.PersistedOwnerTenantId, _tenantId, StringComparison.Ordinal) ||
            !string.Equals(snapshot.VerifiedTenantId, _tenantId, StringComparison.Ordinal) ||
            !string.Equals(snapshot.AssignmentOwnerTenantId, _tenantId, StringComparison.Ordinal) ||
            !string.Equals(snapshot.AuthenticatedActorId, _actorId, StringComparison.Ordinal) ||
            snapshot.CurrentAssignmentRevision != _revision)
            return Denied("ISOLATED_PLACEMENT_CAPTURED_EVIDENCE_MISMATCH");

        // Domain and Application still check role, JWT/membership, binding,
        // CAS revision, rollback proof and idempotency. A nominal preflight
        // always requires a separate authenticated storage adapter.
        return new Gate(new CapturedReadOnlyReader(_tenantId, _captured))
            .EvaluateAsync(request, cancellationToken);
    }

    private sealed class CapturedReadOnlyReader : IC0202PlacementEvidenceReader
    {
        private readonly string _tenantId;
        private readonly Gate.Evidence _evidence;
        public CapturedReadOnlyReader(string tenantId, Gate.Evidence evidence)
        {
            _tenantId = tenantId;
            _evidence = evidence;
        }
        public ValueTask<Gate.Evidence?> ReadAsync(
            string tenantId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Gate.Evidence?>(
                string.Equals(_tenantId, tenantId, StringComparison.Ordinal)
                    ? _evidence : null);
        }
    }

    private static ValueTask<Gate.Result> Denied(string code) =>
        ValueTask.FromResult(new Gate.Result(code));

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    private static bool SafeId(string? value) =>
        value is { Length: >= 2 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}
