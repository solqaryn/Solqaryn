using System.Data;
using System.Data.Common;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-01.M03.06: isolated, bounded, structured audit of plan/queue events.
/// The caller MUST independently authenticate tenant/actor/RBAC before calling.
/// Never registered in production DI; no JWT, payload, PII, secrets, billing
/// actions or Aiven schema are accepted or written by this component.
/// </summary>
public sealed class C0401IsolatedStructuredAuditSink(Func<DbConnection> connectionFactory)
{
    private readonly Func<DbConnection> _connections =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public readonly record struct WriteResult(string Code)
    {
        public bool Stored => Code == "AUDIT_RECORDED";
        public bool GrantsAuthorization => false;
        public bool WritesCommercialData => false;
    }

    private static bool SafeEventId(string? value) =>
        value is { Length: >= 16 and <= 64 } &&
        value.All(static c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_');

    private static bool SafeEventCode(string? code) =>
        code is "CLAIM" or "ACK" or "DENY" or "EXPIRE";

    private const string InsertSql = """
        INSERT INTO V22C0401AuditIsolated
          (TenantId, EventId, WorkId, ActorId, EventCode, OccurredUtc)
        VALUES (@tenant, @event, @work, @actor, @code, @occurred)
        """;

    public async ValueTask<WriteResult> RecordAsync(
        int verifiedTenantId, long workId, long verifiedActorId,
        string? eventId, string? eventCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (verifiedTenantId <= 0 || workId <= 0 || verifiedActorId <= 0 ||
            !SafeEventId(eventId) || !SafeEventCode(eventCode))
            return new("AUDIT_REJECTED");
        try
        {
            await using var db = _connections();
            if (db is null) return new("AUDIT_REJECTED");
            await db.OpenAsync(cancellationToken);
            await using var tx = await db.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            await using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = InsertSql;
            static void Param(DbCommand command, string name, object value)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            Param(cmd, "@tenant", verifiedTenantId);
            Param(cmd, "@event", eventId!);
            Param(cmd, "@work", workId);
            Param(cmd, "@actor", verifiedActorId);
            Param(cmd, "@code", eventCode!);
            Param(cmd, "@occurred", DateTime.UtcNow);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken) != 1)
                return new("AUDIT_REJECTED");

            // A duplicate EventId+TenantId fails with the DB unique key:
            // never silently overwrite an existing audit or select a
            // preferred replay with conflicting fields.
            await tx.CommitAsync(cancellationToken);
            return new("AUDIT_RECORDED");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No SQL details, JWT, headers, actor attributes or raw payload
            // escape through a public error response.
            return new("AUDIT_REJECTED");
        }
    }
}
