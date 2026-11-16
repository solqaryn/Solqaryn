using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

public sealed record V5ArchitectureMigrationAuditRecord(
    string Code, int ReviewedCount);

public interface IV5ArchitectureMigrationAuditSink
{
    ValueTask<bool> WriteAsync(
        V5ArchitectureMigrationAuditRecord record, CancellationToken ct);
}

public sealed class V5ArchitectureMigrationLoggerAuditSink
    : IV5ArchitectureMigrationAuditSink
{
    private readonly ILogger<V5ArchitectureMigrationLoggerAuditSink> _logger;
    public V5ArchitectureMigrationLoggerAuditSink(
        ILogger<V5ArchitectureMigrationLoggerAuditSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public ValueTask<bool> WriteAsync(
        V5ArchitectureMigrationAuditRecord record, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Never include tenant name, host, DSN, user, request keys, or errors.
        _logger.LogInformation(
            "Migration readiness preview audit code={EventCode} count={ReviewedCount}",
            record.Code, record.ReviewedCount);
        return ValueTask.FromResult(true);
    }
}

public interface IV5ArchitectureMigrationAuditReader
{
    ValueTask<V5ArchitectureMigrationBoundedReadWorker.Review> ReadAsync(
        IReadOnlyList<V5ArchitectureMigrationBoundedReadWorker.Probe?>? probes,
        CancellationToken ct);
}

public sealed class V5ArchitectureMigrationBoundedAuditReader
    : IV5ArchitectureMigrationAuditReader
{
    private readonly V5ArchitectureMigrationBoundedReadWorker _worker;
    public V5ArchitectureMigrationBoundedAuditReader(
        V5ArchitectureMigrationBoundedReadWorker worker) =>
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));

    public ValueTask<V5ArchitectureMigrationBoundedReadWorker.Review> ReadAsync(
        IReadOnlyList<V5ArchitectureMigrationBoundedReadWorker.Probe?>? probes,
        CancellationToken ct) => _worker.PreviewAsync(probes, ct);
}

/// <summary>
/// C15-02.M03.06: per-session, one-shot, in-process R2 audit facade.
/// All decisions remain synthetic. No actual backup, restore, SQL, DNS,
/// secrets, cloud APIs, JWT, hosted worker, or environment promotion.
/// </summary>
public sealed class V5ArchitectureMigrationSafeAuditService
{
    public const int MaximumOperations = 128;
    private readonly IV5ArchitectureMigrationAuditReader _reader;
    private readonly IV5ArchitectureMigrationAuditSink _sink;
    private readonly SemaphoreSlim _singleFlight = new(1, 1);
    private readonly HashSet<string> _attempts = new(StringComparer.Ordinal);

    public V5ArchitectureMigrationSafeAuditService(
        IV5ArchitectureMigrationAuditReader reader,
        IV5ArchitectureMigrationAuditSink sink)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public sealed record Result(string Code, int ReviewedCount)
    {
        public bool AuthorizesDeployment => false;
        public bool AuthorizesDataMigration => false;
        public bool ReadsOrWritesMySql => false;
        public bool AuthorizesBackupRestore => false;
        public bool MutatesDnsOrSecrets => false;
        public bool AuthorizesTenantOrRbac => false;
        public bool StartsHostedWorker => false;
        public bool PersistsAudit => false;
        public bool CertifiesProduction => false;
    }

    public async ValueTask<Result> ReviewAsync(
        string? operationKey, string? observedSha, string? observedTenant,
        IReadOnlyList<V5ArchitectureMigrationBoundedReadWorker.Probe?>? probes,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!SafeKey(operationKey) || !FullSha(observedSha) ||
            !SafeTenant(observedTenant) || probes is null ||
            probes.Count is < 1 or > V5ArchitectureMigrationBoundedReadWorker.MaximumBatch)
            return await DeniedAsync("MIGRATION_AUDIT_CONTEXT_REQUIRED", ct);

        // Reject mixed/faked scopes before any delegated read or output.
        if (probes.Any(p => p is null || p.Context is null ||
            p.RequestsExternalMutation ||
            !string.Equals(p.ObservedSha, observedSha, StringComparison.Ordinal) ||
            !string.Equals(p.ObservedTenant, observedTenant, StringComparison.Ordinal)))
            return await DeniedAsync("MIGRATION_AUDIT_MIXED_SCOPE_DENIED", ct);

        if (!await _singleFlight.WaitAsync(0, ct))
            return await DeniedAsync("MIGRATION_AUDIT_CONCURRENT_DENIED", ct);

        try
        {
            if (_attempts.Count >= MaximumOperations)
                return await DeniedAsync("MIGRATION_AUDIT_CAPACITY_REAUTH_REQUIRED", ct);
            if (!_attempts.Add(operationKey!))
                return await DeniedAsync("MIGRATION_AUDIT_DUPLICATE_DENIED", ct);

            V5ArchitectureMigrationBoundedReadWorker.Review preview;
            try { preview = await _reader.ReadAsync(probes, ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return await DeniedAsync("MIGRATION_AUDIT_READER_UNAVAILABLE", ct);
            }

            if (preview is null ||
                preview.Code != "MIGRATION_PREVIEW_EXTERNAL_EVIDENCE_REQUIRED" ||
                preview.Items is null || preview.Items.Count != probes.Count ||
                preview.Items.Any(item => item is null ||
                    item.ItemsReviewed is < 1 or > 64) ||
                preview.AuthorizesDeployment || preview.AuthorizesMySqlMigration ||
                preview.AuthorizesDnsOrSecretMutation || preview.GrantsTenantAccess ||
                preview.GrantsA5 || preview.StartsHostedWorker ||
                preview.WritesAuditOrInstitutionalData ||
                preview.CertifiesRealBackupRestore)
                return await DeniedAsync("MIGRATION_AUDIT_AUTHORITY_DENIED", ct);

            const string code = "MIGRATION_AUDIT_EXTERNAL_EVIDENCE_REQUIRED";
            if (!await WriteAsync(code, preview.Items.Count, ct))
                return new Result("MIGRATION_AUDIT_SINK_UNAVAILABLE", 0);
            return new Result(code, preview.Items.Count);
        }
        finally { _singleFlight.Release(); }
    }

    private async ValueTask<Result> DeniedAsync(string code, CancellationToken ct)
    {
        if (!await WriteAsync(code, 0, ct))
            return new Result("MIGRATION_AUDIT_SINK_UNAVAILABLE", 0);
        return new Result(code, 0);
    }

    private async ValueTask<bool> WriteAsync(string code, int count,
        CancellationToken ct)
    {
        try
        {
            return await _sink.WriteAsync(
                new V5ArchitectureMigrationAuditRecord(code, count), ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Sensitive exception message is never logged or returned.
            return false;
        }
    }

    private static bool FullSha(string? v) =>
        v is { Length: 40 } &&
        v.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool SafeTenant(string? v) =>
        v is { Length: >= 2 and <= 128 } &&
        v.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_');

    private static bool SafeKey(string? v) =>
        v is { Length: >= 3 and <= 96 } &&
        v.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_');
}
