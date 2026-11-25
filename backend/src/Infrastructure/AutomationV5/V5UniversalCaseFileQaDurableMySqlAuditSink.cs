using System.Data;
using System.Data.Common;
using System.Globalization;
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
                     VerificationClass)
                VALUES (@tenant, @casefile, @task, @route, @sha,
                        @name, @status, @conclusion, @observed, @class)
                """;
            Add(cmd, "@tenant", _scopeTenantId);
            Add(cmd, "@casefile", _scopeCaseFileId);
            Add(cmd, "@task", entry.MicrotaskId);
            Add(cmd, "@route", entry.RouteKey);
            Add(cmd, "@sha", entry.SourceSha);
            Add(cmd, "@name", entry.CheckName);
            Add(cmd, "@status", entry.CheckStatus);
            Add(cmd, "@conclusion", entry.CheckConclusion);
            Add(cmd, "@observed", entry.ObservedAtUtc.UtcDateTime);
            Add(cmd, "@class", entry.VerificationClass);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken) != 1)
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
        e is not null && e.MicrotaskId is not null && Microtask.IsMatch(e.MicrotaskId) &&
        e.SourceSha is not null && Sha.IsMatch(e.SourceSha) &&
        Routes.Contains(e.RouteKey) && V5UniversalCaseFileQaEvidenceAudit.IsAllowedCheck(e.MicrotaskId, e.CheckName) &&
        Statuses.Contains(e.CheckStatus) && Conclusions.Contains(e.CheckConclusion) &&
        ((e.CheckStatus == "completed" && e.CheckConclusion != "pending") ||
         (e.CheckStatus != "completed" && e.CheckConclusion == "pending")) &&
        e.ObservedAtUtc != default && e.ObservedAtUtc.Offset == TimeSpan.Zero &&
        e.VerificationClass == "UNVERIFIED_EXTERNAL_REVIEW_REQUIRED";

    private static bool FreshUtc(DateTimeOffset observed, DateTimeOffset nowUtc) =>
        observed >= nowUtc.AddMinutes(-10) && observed <= nowUtc.AddSeconds(5);

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
