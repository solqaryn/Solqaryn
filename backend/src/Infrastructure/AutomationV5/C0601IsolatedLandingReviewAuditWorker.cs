using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C06-01.M03.06: isolated transactional review queue and durable audit.
/// Not registered in live DI. Caller must enforce backend tenant authorization.
/// </summary>
public sealed class C0601IsolatedLandingReviewAuditWorker(
    Func<DbConnection> connectionFactory,
    ILogger<C0601IsolatedLandingReviewAuditWorker> logger)
{
    private readonly Func<DbConnection> _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly ILogger<C0601IsolatedLandingReviewAuditWorker> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private const string IsolatedPrefix = "c0601_m0306_";
    private static readonly EventId AuditEvent = new(601306, "C0601IsolatedReviewAudit");

    private const string StageSql = """
        INSERT IGNORE INTO V22C0601LandingReviewQueue
            (TenantId, PageSlug, Revision, QueueState, RequestedUtc)
        SELECT TenantId, PageSlug, Revision, 0, UTC_TIMESTAMP(6)
          FROM V22C0601LandingDraftIsolated
         WHERE TenantId = @tenant AND PageSlug = @slug
           AND Revision = @revision AND DraftState = 1
         LIMIT 1
        """;

    private const string AuditSql = """
        INSERT INTO V22C0601LandingReviewAuditIsolated
            (TenantId, PageSlug, Revision, EventCode, RecordedUtc)
        VALUES (@tenant, @slug, @revision, 'REVIEW_QUEUED', UTC_TIMESTAMP(6))
        """;

    /// <summary>True means exactly one new queue row and audit row committed.</summary>
    public async ValueTask<bool> StageAuditedAsync(
        int serverVerifiedTenantId, string? pageSlug, long expectedRevision,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (serverVerifiedTenantId <= 0 || expectedRevision <= 0 ||
            !IsCanonicalSlug(pageSlug))
        {
            SafeLog("DENIED");
            return false;
        }

        try
        {
            await using var connection = _connectionFactory();
            if (connection is null)
            {
                SafeLog("DENIED");
                return false;
            }

            await connection.OpenAsync(cancellationToken);
            if (!IsIsolatedSchema(connection.Database))
            {
                SafeLog("DENIED");
                return false;
            }

            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            await using var stage = connection.CreateCommand();
            stage.Transaction = transaction;
            stage.CommandText = StageSql;
            AddParameters(stage, serverVerifiedTenantId, pageSlug!, expectedRevision);

            if (await stage.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                SafeLog("DENIED");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await using var audit = connection.CreateCommand();
            audit.Transaction = transaction;
            audit.CommandText = AuditSql;
            AddParameters(audit, serverVerifiedTenantId, pageSlug!, expectedRevision);
            if (await audit.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                SafeLog("DENIED");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(cancellationToken);
            SafeLog("STAGED");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SafeLog("CANCELLED");
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A failed audit insert rolls back on transaction disposal.
            // Do not emit exception details, SQL, tenant or draft metadata.
            SafeLog("DENIED");
            return false;
        }
    }

    private void SafeLog(string code)
    {
        try
        {
            _logger.LogInformation(AuditEvent,
                "Isolated landing review audit result {AuditCode}", code);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Logging failures cannot change the committed database result.
        }
    }

    private static void AddParameters(DbCommand command, int tenantId, string slug, long revision)
    {
        AddParameter(command, "@tenant", tenantId);
        AddParameter(command, "@slug", slug);
        AddParameter(command, "@revision", revision);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static bool IsIsolatedSchema(string? database)
    {
        if (database is null ||
            !database.StartsWith(IsolatedPrefix, StringComparison.Ordinal) ||
            database.Length != IsolatedPrefix.Length + 32)
            return false;
        foreach (var ch in database.AsSpan(IsolatedPrefix.Length))
            if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f')))
                return false;
        return true;
    }

    private static bool IsCanonicalSlug(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 63 ||
            value[0] == '-' || value[^1] == '-')
            return false;
        foreach (var ch in value)
            if (!((ch >= 'a' && ch <= 'z') ||
                  (ch >= '0' && ch <= '9') || ch == '-'))
                return false;
        return true;
    }
}
