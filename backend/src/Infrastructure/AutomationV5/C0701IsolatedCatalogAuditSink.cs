using System.Data;
using System.Data.Common;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C07-01.M03.06, isolated catalog structured audit. No DI registration, no product tables or providers.
/// The caller MUST validate signed identity, tenant membership and RBAC before supplying IDs.
/// Only numeric IDs and bounded event codes are written; never log raw exceptions, JWT, PII or payloads.
/// </summary>
public sealed class C0701IsolatedCatalogAuditSink(Func<DbConnection> connectionFactory)
{
    private readonly Func<DbConnection> _factory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public readonly record struct Receipt(string Code)
    {
        public bool Stored => Code == "AUDIT_RECORDED";
        public bool GrantsAuthorization => false;
        public bool SendsProviderEvents => false;
    }

    public sealed record AuditEntry(long ProductId, long CatalogVersion, string EventId, string EventCode);

    private static bool SafeEventId(string? id) =>
        id is { Length: >= 16 and <= 64 } &&
        id.All(static c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_');

    private static bool SafeCode(string? code) =>
        code is "ENQUEUE" or "LEASE" or "ACK" or "DENY" or "FAIL";

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public async ValueTask<Receipt> RecordAsync(
        int verifiedTenantId, long productId, long catalogVersion, long verifiedActorId,
        string? eventId, string? eventCode, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (verifiedTenantId <= 0 || productId <= 0 || catalogVersion <= 0 ||
            verifiedActorId <= 0 || !SafeEventId(eventId) || !SafeCode(eventCode))
            return new("AUDIT_REJECTED");

        try
        {
            await using var db = _factory();
            if (db is null) return new("AUDIT_REJECTED");
            await db.OpenAsync(token);
            await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
            await using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO V22C0701CatalogAuditIsolated
                    (TenantId, ProductId, CatalogVersion, ActorId, EventId, EventCode, OccurredUtc)
                VALUES (@tenant, @product, @version, @actor, @event, @code, @occurred)
                """;
            Add(cmd, "@tenant", verifiedTenantId);
            Add(cmd, "@product", productId);
            Add(cmd, "@version", catalogVersion);
            Add(cmd, "@actor", verifiedActorId);
            Add(cmd, "@event", eventId!);
            Add(cmd, "@code", eventCode!);
            Add(cmd, "@occurred", DateTime.UtcNow);
            if (await cmd.ExecuteNonQueryAsync(token) != 1)
                return new("AUDIT_REJECTED");
            await tx.CommitAsync(token);
            return new("AUDIT_RECORDED");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Duplicate event, storage failure, or SQL error: fail closed, never echo exception.
            return new("AUDIT_REJECTED");
        }
    }

    /// <summary>Never expose as a public endpoint without authenticated tenant/RBAC checks.</summary>
    public async ValueTask<IReadOnlyList<AuditEntry>> ReadTenantAsync(
        int verifiedTenantId, long productId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (verifiedTenantId <= 0 || productId <= 0) return [];
        try
        {
            await using var db = _factory();
            if (db is null) return [];
            await db.OpenAsync(token);
            await using var cmd = db.CreateCommand();
            cmd.CommandText = """
                SELECT ProductId, CatalogVersion, EventId, EventCode
                  FROM V22C0701CatalogAuditIsolated
                 WHERE TenantId=@tenant AND ProductId=@product
                 ORDER BY OccurredUtc, EventId LIMIT 100
                """;
            Add(cmd, "@tenant", verifiedTenantId);
            Add(cmd, "@product", productId);
            var rows = new List<AuditEntry>();
            await using var reader = await cmd.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                rows.Add(new(reader.GetInt64(0), reader.GetInt64(1),
                    reader.GetString(2), reader.GetString(3)));
            return rows;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return []; }
    }
}
