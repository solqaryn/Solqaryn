using System.Data.Common;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C06-04.M03.05 isolated InnoDB queue; deliberately not registered in DI.
/// This class does not authenticate a JWT or authorize a tenant, and has no provider.
/// A future server-side adapter must verify its caller before invoking this store.
/// </summary>
public sealed class C0604IsolatedMySqlDurableQueue
{
    private const string Table = "v22_c0604_portal_work";
    private readonly Func<DbConnection> _factory;

    public C0604IsolatedMySqlDurableQueue(Func<DbConnection> factory)
        => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public enum Reservation { Rejected, Stored, Duplicate, Conflict }

    public sealed record LeasedWork(
        long Id, string ResourceId, string Operation, string IdempotencyKey,
        long ResourceVersion, int Attempt);

    public async Task<Reservation> ReserveAsync(
        int trustedTenantId, string? resourceId, string? operation,
        string? idempotencyKey, long resourceVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (trustedTenantId <= 0 || !SafeId(resourceId, 128) ||
            operation is not ("customer-preference-sync" or "customer-reservation-confirmation") ||
            !SafeId(idempotencyKey, 64) || idempotencyKey!.Length < 8 ||
            resourceVersion <= 0)
            return Reservation.Rejected;

        await using var connection = _factory();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var insert = Command(connection, transaction,
            $"INSERT IGNORE INTO {Table} (tenant_id, resource_id, operation, idempotency_key, resource_version, state, attempts) VALUES (@tenant,@resource,@operation,@key,@version,'PENDING',0)",
            ("@tenant", trustedTenantId), ("@resource", resourceId!),
            ("@operation", operation!), ("@key", idempotencyKey!),
            ("@version", resourceVersion));
        var inserted = await insert.ExecuteNonQueryAsync(cancellationToken);
        await using var read = Command(connection, transaction,
            $"SELECT resource_id, operation, resource_version FROM {Table} WHERE tenant_id=@tenant AND idempotency_key=@key FOR UPDATE",
            ("@tenant", trustedTenantId), ("@key", idempotencyKey!));

        Reservation result;
        await using (var reader = await read.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("Durable queue readback unavailable.");
            var identical = string.Equals(reader.GetString(0), resourceId, StringComparison.Ordinal) &&
                            string.Equals(reader.GetString(1), operation, StringComparison.Ordinal) &&
                            reader.GetInt64(2) == resourceVersion;
            result = !identical ? Reservation.Conflict :
                inserted == 1 ? Reservation.Stored : Reservation.Duplicate;
        }
        cancellationToken.ThrowIfCancellationRequested();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LeasedWork?> TryLeaseAsync(
        int trustedTenantId, string? workerId, DateTime nowUtc,
        int leaseMinutes, int maxAttempts, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (trustedTenantId <= 0 || !SafeId(workerId, 64) ||
            nowUtc.Kind != DateTimeKind.Utc || leaseMinutes is < 1 or > 30 ||
            maxAttempts is < 1 or > 10)
            return null;

        await using var connection = _factory();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        // An expired last attempt with no ACK must not remain LEASED forever.
        // This tenant-scoped transition is durable and never retries provider delivery.
        await using (var exhausted = Command(connection, transaction,
            $"UPDATE {Table} SET state='DEAD', leased_by=NULL, lease_until_utc=NULL WHERE tenant_id=@tenant AND state='LEASED' AND attempts>=@max AND lease_until_utc<=@now",
            ("@tenant", trustedTenantId), ("@max", maxAttempts), ("@now", nowUtc)))
        {
            await exhausted.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var select = Command(connection, transaction,
            $"SELECT id, resource_id, operation, idempotency_key, resource_version, attempts FROM {Table} WHERE tenant_id=@tenant AND attempts<@max AND (state='PENDING' OR (state='LEASED' AND lease_until_utc<=@now)) ORDER BY id LIMIT 1 FOR UPDATE SKIP LOCKED",
            ("@tenant", trustedTenantId), ("@now", nowUtc), ("@max", maxAttempts));

        LeasedWork? job = null;
        await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
                job = new LeasedWork(reader.GetInt64(0), reader.GetString(1),
                    reader.GetString(2), reader.GetString(3), reader.GetInt64(4),
                    checked(reader.GetInt32(5) + 1));
        }
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        await using var update = Command(connection, transaction,
            $"UPDATE {Table} SET state='LEASED', attempts=attempts+1, leased_by=@worker, lease_until_utc=@until WHERE tenant_id=@tenant AND id=@id",
            ("@worker", workerId!), ("@until", nowUtc.AddMinutes(leaseMinutes)),
            ("@tenant", trustedTenantId), ("@id", job.Id));
        if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException("Lease CAS failed.");
        cancellationToken.ThrowIfCancellationRequested();
        await transaction.CommitAsync(cancellationToken);
        return job;
    }

    public async Task<bool> SettleAsync(
        int trustedTenantId, long workId, string? workerId, int expectedAttempt, DateTime nowUtc,
        bool providerAcknowledged, int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (trustedTenantId <= 0 || workId <= 0 || !SafeId(workerId, 64) ||
            nowUtc.Kind != DateTimeKind.Utc || maxAttempts is < 1 or > 10 ||
            expectedAttempt is < 1 or > 10 || expectedAttempt > maxAttempts)
            return false;

        await using var connection = _factory();
        await connection.OpenAsync(cancellationToken);
        await using var update = connection.CreateCommand();
        update.CommandText = $"UPDATE {Table} SET state=CASE WHEN @ack=1 THEN 'DONE' WHEN attempts>=@max THEN 'DEAD' ELSE 'PENDING' END, leased_by=NULL, lease_until_utc=NULL WHERE tenant_id=@tenant AND id=@id AND state='LEASED' AND leased_by=@worker AND attempts=@attempt AND lease_until_utc>@now";
        Add(update, "@ack", providerAcknowledged ? 1 : 0);
        Add(update, "@max", maxAttempts);
        Add(update, "@tenant", trustedTenantId);
        Add(update, "@id", workId);
        Add(update, "@worker", workerId!);
        Add(update, "@attempt", expectedAttempt);
        Add(update, "@now", nowUtc);
        return await update.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static DbCommand Command(DbConnection connection, DbTransaction transaction,
        string sql, params (string Name, object Value)[] values)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in values) Add(command, name, value);
        return command;
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static bool SafeId(string? value, int limit) =>
        value is { Length: > 0 } && value.Length <= limit &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_' or '.');
}
