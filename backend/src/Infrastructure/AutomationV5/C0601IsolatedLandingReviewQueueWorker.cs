using System.Data;
using System.Data.Common;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C06-01.M03.05: bounded, idempotent queue staging for isolated landing drafts.
/// No production DI registration, external delivery, publication or payment flow.
/// The tenant ID MUST have been verified by the caller on the server. This type
/// cannot establish membership or authorize any user itself.
/// </summary>
public sealed class C0601IsolatedLandingReviewQueueWorker(Func<DbConnection> connectionFactory)
{
    private readonly Func<DbConnection> _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private const string IsolatedSchemaPrefix = "c0601_m0305_";
    private const string StageSql = """
        INSERT IGNORE INTO V22C0601LandingReviewQueue
            (TenantId, PageSlug, Revision, QueueState, RequestedUtc)
        SELECT TenantId, PageSlug, Revision, 0, UTC_TIMESTAMP(6)
          FROM V22C0601LandingDraftIsolated
         WHERE TenantId = @tenant
           AND PageSlug = @slug
           AND Revision = @revision
           AND DraftState = 1
         LIMIT 1
        """;

    /// <summary>
    /// A true result means ONE new row was staged, not that a message was sent or
    /// a landing page published. Duplicate, stale, foreign or corrupt rows deny.
    /// </summary>
    public async ValueTask<bool> StageReviewAsync(
        int serverVerifiedTenantId, string? pageSlug,
        long expectedRevision, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (serverVerifiedTenantId <= 0 || expectedRevision <= 0 ||
            !IsCanonicalSlug(pageSlug))
            return false;

        try
        {
            await using var connection = _connectionFactory();
            if (connection is null)
                return false;

            await connection.OpenAsync(cancellationToken);
            if (!IsEphemeralSchema(connection.Database))
                return false;

            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = StageSql;

            AddParameter(command, "@tenant", serverVerifiedTenantId);
            AddParameter(command, "@slug", pageSlug!);
            AddParameter(command, "@revision", expectedRevision);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Do not propagate SQL, tenant or draft metadata into caller output.
            // A retry is safe: the composite queue primary key is idempotent.
            return false;
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static bool IsEphemeralSchema(string? database)
    {
        if (database is null ||
            !database.StartsWith(IsolatedSchemaPrefix, StringComparison.Ordinal) ||
            database.Length != IsolatedSchemaPrefix.Length + 32)
            return false;
        foreach (var ch in database.AsSpan(IsolatedSchemaPrefix.Length))
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
