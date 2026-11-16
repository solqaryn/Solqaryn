using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Read-only port for synthetic migration checks. It does not represent
/// a live provider, grant, database connection, JWT, backup or deployer.
/// </summary>
public interface IV5ArchitectureMigrationReadReviewPort
{
    ValueTask<V5ArchitectureMigrationAuthorizedPreflight.Result> InspectAsync(
        V5ArchitectureMigrationAuthorizedPreflight.Context? context,
        string? observedSha, string? observedTenant,
        CancellationToken cancellationToken);
}

public sealed class V5ArchitectureMigrationIsolatedReadPort
    : IV5ArchitectureMigrationReadReviewPort
{
    private readonly V5ArchitectureMigrationIsolatedSnapshotAdapter _adapter;
    public V5ArchitectureMigrationIsolatedReadPort(
        V5ArchitectureMigrationIsolatedSnapshotAdapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public ValueTask<V5ArchitectureMigrationAuthorizedPreflight.Result> InspectAsync(
        V5ArchitectureMigrationAuthorizedPreflight.Context? context,
        string? observedSha, string? observedTenant,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            _adapter.PrepareReadOnly(context, observedSha, observedTenant));
    }
}

/// <summary>
/// C15-02.M03.05: bounded in-process sequential migration-readiness preview.
/// Never runs a hosted worker, SQL, backup/restore, DNS, secret or deployment.
/// All records are discarded on failure; no partial readiness certification.
/// </summary>
public sealed class V5ArchitectureMigrationBoundedReadWorker
{
    public const int MaximumBatch = 12;
    private readonly IV5ArchitectureMigrationReadReviewPort _port;

    public V5ArchitectureMigrationBoundedReadWorker(
        IV5ArchitectureMigrationReadReviewPort port) =>
        _port = port ?? throw new ArgumentNullException(nameof(port));

    public sealed record Probe(
        string? AttemptKey,
        string? ObservedSha,
        string? ObservedTenant,
        V5ArchitectureMigrationAuthorizedPreflight.Context? Context,
        bool RequestsExternalMutation = false);

    public sealed record Item(string AttemptKey, int ItemsReviewed);

    public sealed record Review(string Code, IReadOnlyList<Item> Items)
    {
        public bool AuthorizesDeployment => false;
        public bool AuthorizesMySqlMigration => false;
        public bool AuthorizesDnsOrSecretMutation => false;
        public bool StartsHostedWorker => false;
        public bool WritesAuditOrInstitutionalData => false;
        public bool GrantsTenantAccess => false;
        public bool GrantsA5 => false;
        public bool CertifiesRealBackupRestore => false;
    }

    public async ValueTask<Review> PreviewAsync(
        IReadOnlyList<Probe?>? probes,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (probes is null || probes.Count is < 1 or > MaximumBatch)
            return Denied("MIGRATION_PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE");

        string? expectedSha = null;
        string? expectedTenant = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Entire batch must satisfy static security boundaries before ANY
        // delegated read; caller-controlled context is not authentication.
        foreach (var probe in probes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (probe is null || !SafeKey(probe.AttemptKey) ||
                !FullSha(probe.ObservedSha) ||
                !SafeTenant(probe.ObservedTenant) ||
                probe.Context is null ||
                probe.RequestsExternalMutation)
                return Denied("MIGRATION_PREVIEW_UNTRUSTED_REQUEST");

            if (!seen.Add(probe.AttemptKey!))
                return Denied("MIGRATION_PREVIEW_DUPLICATE_KEY");

            if (expectedSha is null)
            {
                expectedSha = probe.ObservedSha;
                expectedTenant = probe.ObservedTenant;
            }
            else if (!string.Equals(expectedSha, probe.ObservedSha,
                         StringComparison.Ordinal) ||
                     !string.Equals(expectedTenant, probe.ObservedTenant,
                         StringComparison.Ordinal))
                return Denied("MIGRATION_PREVIEW_MIXED_SCOPE");
        }

        var records = new List<Item>(probes.Count);
        foreach (var probe in probes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            V5ArchitectureMigrationAuthorizedPreflight.Result result;
            try
            {
                result = await _port.InspectAsync(
                    probe!.Context, probe.ObservedSha, probe.ObservedTenant,
                    cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return Denied("MIGRATION_PREVIEW_SOURCE_UNAVAILABLE");
            }

            if (result.Code != "AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED" ||
                result.ItemsReviewed is < 1 or > 64 ||
                result.AuthorizesDeployment || result.AuthorizesDataMigration ||
                result.AuthorizesSecretsOrDnsMutation ||
                result.GrantsA5OrEnvironmentPromotion)
                return Denied("MIGRATION_PREVIEW_AUTHORITY_REJECTED");

            records.Add(new Item(probe!.AttemptKey!, result.ItemsReviewed));
        }

        return new Review("MIGRATION_PREVIEW_EXTERNAL_EVIDENCE_REQUIRED",
            records.AsReadOnly());
    }

    private static Review Denied(string code) =>
        new(code, Array.Empty<Item>());

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => (c >= '0' && c <= '9') ||
                     (c >= 'a' && c <= 'f'));

    private static bool SafeTenant(string? tenant) =>
        tenant is { Length: >= 2 and <= 128 } &&
        tenant.All(c => (c >= 'a' && c <= 'z') ||
                        (c >= 'A' && c <= 'Z') ||
                        (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool SafeKey(string? key) =>
        key is { Length: >= 3 and <= 96 } &&
        key.All(c => (c >= 'a' && c <= 'z') ||
                     (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') || c is '-' or '_');
}
