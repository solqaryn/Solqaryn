using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C01-04.M03.06: isolated InnoDB sink for redacted case-file QA observations.
/// Neither the supplied numeric scope nor a source's boolean flags prove current
/// JWT, RBAC, membership or case ownership. Deliberately NOT registered in DI
/// and NOT exposed by HTTP; a future server adapter must enforce those gates.
/// </summary>
public sealed class V5UniversalCaseFileQaDurableMySqlAuditSink
    : IV5UniversalCaseFileQaScopeBoundSink
{
    private const string Table = "V23C0104CaseFileQaAuditIsolated";
    private readonly Func<DbConnection> _connectionFactory;
    private readonly int _scopeTenantId;
    private readonly int _scopeCaseFileId;
    private readonly TimeProvider _clock;

    private static readonly Regex Microtask = new(
        @"\AC[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex Sha = new(
        @"\A[0-9a-f]{40}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> Routes = new(StringComparer.Ordinal)
    {
        "casefile.timeline", "casefile.authorization",
        "casefile.attachment", "casefile.qa-evidence"
    };
    private static readonly HashSet<string> Statuses = new(StringComparer.Ordinal)
    {
        "queued", "in_progress", "completed"
    };
    private static readonly HashSet<string> Conclusions = new(StringComparer.Ordinal)
    {
        "success", "failure", "cancelled", "skipped", "pending"
    };

    public V5UniversalCaseFileQaDurableMySqlAuditSink(
        Func<DbConnection> connectionFactory, int scopeTenantId, int scopeCaseFileId,
        TimeProvider? clock = null)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
        _scopeTenantId = scopeTenantId;
        _scopeCaseFileId = scopeCaseFileId;
        _clock = clock ?? TimeProvider.System;
    }

    public bool MatchesCanonicalScope(string tenantScope, string caseFileScope) =>
        _scopeTenantId > 0 && _scopeCaseFileId > 0 &&
        string.Equals(tenantScope,
            _scopeTenantId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) &&
        string.Equals(caseFileScope,
            _scopeCaseFileId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    public async ValueTask WriteAsync(
        V5UniversalCaseFileQaEvidenceAudit.SafeAuditEntry entry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Defend this separately callable sink against stale/future replay before SQL I/O.
        if (_scopeTenantId <= 0 || _scopeCaseFileId <= 0 || !Valid(entry) ||
            !FreshUtc(entry.ObservedAtUtc, _clock.GetUtcNow()))
            throw new InvalidOperationException("CASEFILE_QA_DURABLE_SCOPE_INVALID");

        try
        {
            await using var db = _connectionFactory()
                ?? throw new InvalidOperationException("CASEFILE_QA_DURABLE_DB_MISSING");
            await db.OpenAsync(cancellationToken);
            await using var tx = await db.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            await using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"""
                INSERT INTO {Table}
                    (TenantId, CaseFileId, MicrotaskId, RouteKey, SourceSha,
                     CheckName, CheckStatus, CheckConclusion, ObservedUtc,
                     VerificationClass, CorrelationId, EvidenceKey, ProviderWorkflowId, ProviderRunId,
                     ProviderAttempt, ProviderJobId, ProviderCheckRunId, ProviderCheckSuiteId)
                VALUES (@tenant, @casefile, @task, @route, @sha,
                        @name, @status, @conclusion, @observed, @class, @correlation, @key,
                        @workflow, @run, @attempt, @job, @check, @suite)
                """;
            Add(cmd, "@tenant", _scopeTenantId);
            Add(cmd, "@casefile", _scopeCaseFileId);
            Add(cmd, "@task", entry.MicrotaskId);
            Add(cmd, "@route", entry.RouteKey);
            Add(cmd, "@sha", entry.SourceSha);
            Add(cmd, "@name", entry.CheckName);
            Add(cmd, "@status", entry.CheckStatus);
            Add(cmd, "@conclusion", entry.CheckConclusion);
            // DATETIME(6) and the legacy evidence key share microsecond precision.
            var observed = entry.ObservedAtUtc.UtcDateTime;
            Add(cmd, "@observed", observed.AddTicks(-(observed.Ticks % 10)));
            Add(cmd, "@class", entry.VerificationClass);
            // Correlation is separate from the immutable idempotency key.
            Add(cmd, "@correlation", entry.CorrelationId.ToString("N"));
            Add(cmd, "@key", EvidenceKey(entry));
            Add(cmd, "@workflow", entry.ProviderJob?.WorkflowId);
            Add(cmd, "@run", entry.ProviderJob?.RunId);
            Add(cmd, "@attempt", entry.ProviderJob?.Attempt);
            Add(cmd, "@job", entry.ProviderJob?.JobId);
            Add(cmd, "@check", entry.ProviderJob?.CheckRunId);
            Add(cmd, "@suite", entry.ProviderJob?.CheckSuiteId);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("CASEFILE_QA_DURABLE_WRITE_REJECTED");
            cancellationToken.ThrowIfCancellationRequested();
            // Affected-row count alone cannot establish that the stored envelope
            // matches the receipt (for example, a BEFORE INSERT trigger may alter it).
            // Reuse the typed parameters and check within the same transaction.
            cmd.CommandText = $"""
                SELECT COUNT(*) FROM {Table}
                WHERE TenantId=@tenant AND CaseFileId=@casefile
                  AND CAST(EvidenceKey AS BINARY)=CAST(@key AS BINARY)
                  AND CAST(MicrotaskId AS BINARY)=CAST(@task AS BINARY)
                  AND CAST(RouteKey AS BINARY)=CAST(@route AS BINARY)
                  AND CAST(SourceSha AS BINARY)=CAST(@sha AS BINARY)
                  AND CAST(CheckName AS BINARY)=CAST(@name AS BINARY)
                  AND CAST(CheckStatus AS BINARY)=CAST(@status AS BINARY)
                  AND CAST(CheckConclusion AS BINARY)=CAST(@conclusion AS BINARY)
                  AND ObservedUtc=@observed
                  AND CAST(VerificationClass AS BINARY)=CAST(@class AS BINARY)
                  AND CAST(CorrelationId AS BINARY)=CAST(@correlation AS BINARY)
                  AND ProviderWorkflowId <=> @workflow AND ProviderRunId <=> @run
                  AND ProviderAttempt <=> @attempt AND ProviderJobId <=> @job
                  AND ProviderCheckRunId <=> @check AND ProviderCheckSuiteId <=> @suite
                """;
            if (Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken),
                    CultureInfo.InvariantCulture) != 1)
                throw new InvalidOperationException("CASEFILE_QA_DURABLE_WRITE_REJECTED");
            cancellationToken.ThrowIfCancellationRequested();
            await tx.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never disclose SQL, DSN, tenant, case, token or provider details.
            throw new InvalidOperationException("CASEFILE_QA_DURABLE_WRITE_REJECTED");
        }
    }

    private static bool Valid(V5UniversalCaseFileQaEvidenceAudit.SafeAuditEntry? e) =>
        e is not null && e.CorrelationId != Guid.Empty && e.MicrotaskId is not null && Microtask.IsMatch(e.MicrotaskId) &&
        e.SourceSha is not null && Sha.IsMatch(e.SourceSha) &&
        Routes.Contains(e.RouteKey) && V5UniversalCaseFileQaEvidenceAudit.IsAllowedCheck(e.MicrotaskId, e.CheckName) &&
        Statuses.Contains(e.CheckStatus) && Conclusions.Contains(e.CheckConclusion) &&
        ((e.CheckStatus == "completed" && e.CheckConclusion != "pending") ||
         (e.CheckStatus != "completed" && e.CheckConclusion == "pending")) &&
        e.ObservedAtUtc != default && e.ObservedAtUtc.Offset == TimeSpan.Zero &&
        e.VerificationClass == "UNVERIFIED_EXTERNAL_REVIEW_REQUIRED" &&
        V5UniversalCaseFileQaEvidenceAudit.ProviderJobMatches(e.ProviderJob,
            e.MicrotaskId, e.SourceSha, e.CheckName, e.CheckStatus, e.CheckConclusion);

    private static bool FreshUtc(DateTimeOffset observed, DateTimeOffset nowUtc) =>
        observed >= nowUtc.AddMinutes(-10) && observed <= nowUtc.AddSeconds(5);

    // Tenant and case are part of the SQL primary key. A provider build identity
    // is immutable across observation timestamps and cannot overwrite prior receipts.
    // Historical observations without provider identity retain timestamp granularity
    // (MySQL DATETIME(6)); they never masquerade as provider-bound receipts.
    private static string EvidenceKey(V5UniversalCaseFileQaEvidenceAudit.SafeAuditEntry e)
    {
        var build = e.ProviderJob is { } job
            ? FormattableString.Invariant($"github|{job.RunId}|{job.Attempt}|{job.JobId}")
            : FormattableString.Invariant($"legacy|{e.ObservedAtUtc.UtcDateTime.Ticks / 10}");
        var canonical = $"{e.MicrotaskId}|{e.RouteKey}|{e.SourceSha}|{e.CheckName}|{build}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
