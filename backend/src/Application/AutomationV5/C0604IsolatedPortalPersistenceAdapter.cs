using Gate = Solqaryn.Application.AutomationV5.C0604PortalClienteAuthorizationGate;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C06-04.M03.04: optional isolated read-only persistence boundary.
/// A real backend implementation must independently authenticate source provenance,
/// persisted resource ownership and current JWT/session/permission evidence.
/// No provider, endpoint or DI binding is installed by this adapter.
/// </summary>
public interface IC0604TrustedPortalPersistenceReader
{
    ValueTask<C0604IsolatedPortalPersistenceAdapter.PersistedRead?> ReadAsync(
        string resourceId, CancellationToken cancellationToken);
}

/// <summary>
/// Translates independently verified, request-scoped stored evidence into the
/// existing Application authorization preflight. This bridge neither queries
/// MySQL itself nor authorizes access to private resources.
/// </summary>
public sealed class C0604IsolatedPortalPersistenceAdapter
    : IC0604PortalAuthorizationEvidenceReader
{
    private readonly IC0604TrustedPortalPersistenceReader _source;

    public C0604IsolatedPortalPersistenceAdapter(IC0604TrustedPortalPersistenceReader source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public sealed record PersistedRead(
        string? ResourceId, string? SourceSha, long PersistedVersion,
        bool TrustedBackendProvenance, bool PersistedOwnershipChecked,
        bool PermissionRevoked, DateTime CapturedAtUtc, Gate.Evidence? Evidence);

    public async ValueTask<Gate.Evidence?> ReadCurrentAsync(
        string resourceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!SafeResourceKey(resourceId))
            return null;

        PersistedRead? record;
        try
        {
            record = await _source.ReadAsync(resourceId, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Backend/source errors never become accepted authorization evidence.
            return null;
        }

        if (record is null ||
            !string.Equals(record.ResourceId, resourceId, StringComparison.Ordinal) ||
            !CanonicalSha(record.SourceSha) ||
            record.PersistedVersion <= 0 ||
            !record.TrustedBackendProvenance ||
            !record.PersistedOwnershipChecked ||
            record.PermissionRevoked ||
            record.CapturedAtUtc.Kind != DateTimeKind.Utc ||
            record.Evidence?.Snapshot is not { } snapshot ||
            !string.Equals(record.Evidence.ObservedSourceSha,
                record.SourceSha, StringComparison.Ordinal) ||
            snapshot.PersistedVersion != record.PersistedVersion ||
            snapshot.ServerVerifiedEmpresaId != record.Evidence.VerifiedEmpresaId ||
            snapshot.ServerVerifiedCustomerId != record.Evidence.VerifiedCustomerId ||
            snapshot.ServerVerifiedAccountId != record.Evidence.VerifiedAccountId)
            return null;

        // The downstream gate still rejects missing JWT/RBAC, revoked sessions,
        // cross-tenant resource ownership and stale snapshots. This bridge only
        // hands off evidence; its flags cannot prove live DB or HTTP authentication.
        return record.Evidence;
    }

    private static bool SafeResourceKey(string? key) =>
        key is { Length: > 0 and <= 128 } &&
        key.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool CanonicalSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}
