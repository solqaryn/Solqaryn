using System.Collections.Frozen;
using Gate = Solqaryn.Application.AutomationV5.C0103DynamicConfigurationAuthorizationGate;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C01-03.M03.04: bounded, read-only synthetic snapshot adapter for the
/// C01-03.M03.03 authorization preflight. NOT an EF/MySQL reader, live identity
/// authority, durable compare-and-swap implementation or write permission.
/// </summary>
public sealed class C0103IsolatedCatalogSnapshotAdapter : IC0103TenantConfigurationCatalogSource
{
    private readonly int _tenantId;
    private readonly string _canonicalKey;
    private readonly long _revision;
    private readonly string _sourceSha;
    private readonly Gate.CatalogSnapshot _captured;

    public C0103IsolatedCatalogSnapshotAdapter(
        int authenticatedTenantId,
        string canonicalKey,
        long capturedRevision,
        string sourceSha,
        Gate.CatalogSnapshot captured)
    {
        if (authenticatedTenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(authenticatedTenantId));
        if (string.IsNullOrWhiteSpace(canonicalKey) || canonicalKey.Length > 64)
            throw new ArgumentException("A bounded canonical key is required.", nameof(canonicalKey));
        if (capturedRevision <= 0)
            throw new ArgumentOutOfRangeException(nameof(capturedRevision));
        if (!FullSha(sourceSha))
            throw new ArgumentException("A complete source SHA is required.", nameof(sourceSha));
        ArgumentNullException.ThrowIfNull(captured);

        _tenantId = authenticatedTenantId;
        _canonicalKey = canonicalKey;
        _revision = capturedRevision;
        _sourceSha = sourceSha;

        // An external set may use OrdinalIgnoreCase or mutate after capture.
        // Make a defensive, strictly ordinal copy before any evaluation.
        _captured = captured with
        {
            ApprovedKeys = captured.ApprovedKeys is null
                ? null
                : captured.ApprovedKeys.ToFrozenSet(StringComparer.Ordinal)
        };
    }

    public bool ReadsOrWritesMysql => false;
    public bool ReadsLiveJwtOrRbac => false;
    public bool PerformsDurableCas => false;
    public bool GrantsTenantOrPlatformWrite => false;
    public bool PerformsMigrationOrCutover => false;

    public ValueTask<Gate.CatalogSnapshot?> ReadAsync(
        int authenticatedTenantId,
        string requestedKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (authenticatedTenantId != _tenantId ||
            !string.Equals(requestedKey, _canonicalKey, StringComparison.Ordinal))
            return Denied();

        var s = _captured;
        if (s.OwnerTenantId != _tenantId ||
            !s.BackendReadVerified || !s.PersistedOwnerVerified ||
            s.Version != _revision ||
            !string.Equals(s.SourceSha, _sourceSha, StringComparison.Ordinal) ||
            s.Definition is null ||
            !string.Equals(s.Definition.CanonicalKey, _canonicalKey, StringComparison.Ordinal) ||
            s.ApprovedKeys is null ||
            !s.ApprovedKeys.Contains(_canonicalKey))
            return Denied();

        // These are captured synthetic assertions only. A real trusted
        // persistence reader must be supplied separately and revalidate
        // ownership/version/authorization before any live operation.
        return ValueTask.FromResult<Gate.CatalogSnapshot?>(s);
    }

    private static ValueTask<Gate.CatalogSnapshot?> Denied() =>
        ValueTask.FromResult<Gate.CatalogSnapshot?>(null);

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
