using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-02.M03.06: isolated durable preflight audit only. It is not registered
/// in production DI, cannot reach Aiven, and never authorizes contract writes.
/// </summary>
public sealed class C0402IsolatedMySqlContractAuditSink(Func<DbConnection> factory)
    : IC0402ContractAuditSink
{
    private readonly Func<DbConnection> _factory = factory ??
        throw new ArgumentNullException(nameof(factory));

    public async ValueTask<bool> AppendAsync(
        C0402ContractAuditEntry entry, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (entry is null ||
            entry.EventCode != "C0402_CONTRACT_PREFLIGHT" ||
            entry.OutcomeCode is not { Length: >= 3 and <= 96 } ||
            !entry.OutcomeCode.All(static c =>
                c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_'))
            return false;
        try
        {
            await using var db = _factory();
            if (db is not MySqlConnection mysql ||
                mysql.State != ConnectionState.Closed ||
                !IsIsolated(mysql.ConnectionString))
                return false;
            await db.OpenAsync(token);
            await using var tx = await db.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, token);
            await using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO V23C0402ContractAuditIsolated
                (EventId, EventCode, OutcomeCode, IsDomainCandidateOnly, OccurredUtc)
                VALUES (@id, @event, @outcome, @candidate, @utc)
                """;
            void Add(string name, object value)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value;
                cmd.Parameters.Add(p);
            }
            Add("@id", Guid.NewGuid().ToString("N"));
            Add("@event", entry.EventCode);
            Add("@outcome", entry.OutcomeCode);
            Add("@candidate", entry.IsDomainCandidateOnly);
            Add("@utc", DateTime.UtcNow);
            if (await cmd.ExecuteNonQueryAsync(token) != 1)
                return false;
            await tx.CommitAsync(token);
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never reveal SQL, connection strings, tenant, actor or raw payload.
            return false;
        }
    }

    private static bool IsIsolated(string connectionString)
    {
        try
        {
            var info = new MySqlConnectionStringBuilder(connectionString);
            const string prefix = "c0402_m0306_";
            var schema = info.Database;
            return (info.Server is "127.0.0.1" or "localhost") &&
                info.Port == 3306 && info.UserID == "root" &&
                schema is not null && schema.Length == prefix.Length + 32 &&
                schema.StartsWith(prefix, StringComparison.Ordinal) &&
                schema[prefix.Length..].All(Uri.IsHexDigit);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }
}
