using System.Collections.Generic;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C14-01.M03.04: the source must be a server-trusted, read-only snapshot
/// provider. This interface does NOT implement authentication, EF, or SQL.
/// No default DI registration exists; a future live binding needs its own gate.
/// </summary>
public interface IC1401TrustedAcceptanceSnapshotSource
{
    ValueTask<C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext?> ReadVerifiedAsync(
        string resourceKey, CancellationToken cancellationToken);
}

/// <summary>
/// Isolated fail-closed bridge to the existing Application authorization preflight.
/// Never grants access, never writes, and does not authenticate a real HTTP request.
/// </summary>
public sealed class C1401IsolatedFunctionalAcceptanceAuthorityAdapter
    : IC1401FunctionalAcceptanceAuthorityReader
{
    private readonly IC1401TrustedAcceptanceSnapshotSource _source;

    public C1401IsolatedFunctionalAcceptanceAuthorityAdapter(
        IC1401TrustedAcceptanceSnapshotSource source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public async ValueTask<C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext?> ReadAsync(
        string resourceKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!SafeResourceKey(resourceKey))
            return null;

        C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext? snapshot;
        try
        {
            snapshot = await _source.ReadVerifiedAsync(resourceKey, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }

        if (snapshot is null ||
            !string.Equals(snapshot.PersistedResourceKey, resourceKey, StringComparison.Ordinal) ||
            !IsFullSha(snapshot.SourceCommitSha) ||
            snapshot.ActorId <= 0 || snapshot.TenantId <= 0 ||
            !snapshot.CurrentBackendJwtVerified ||
            !snapshot.CurrentMembershipVerified ||
            !snapshot.CurrentRelationalRbacVerified ||
            !snapshot.PersistedOwnershipVerified ||
            snapshot.SessionOrRoleRevoked ||
            snapshot.ApprovedCaseKeys is not { Count: > 0 } ||
            snapshot.ApprovedActionKeys is not { Count: > 0 })
            return null;

        // Copy mutable provider-owned collections. Source-chosen case-insensitive
        // comparers must never widen an approved case/action catalog.
        return snapshot with
        {
            ApprovedCaseKeys = new HashSet<string>(
                snapshot.ApprovedCaseKeys, StringComparer.Ordinal),
            ApprovedActionKeys = new HashSet<string>(
                snapshot.ApprovedActionKeys, StringComparer.Ordinal)
        };
    }

    private static bool IsFullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeResourceKey(string? key) =>
        key is { Length: >= 1 and <= 96 } &&
        key.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_');
}
