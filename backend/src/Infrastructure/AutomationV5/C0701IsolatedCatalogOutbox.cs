using System.Data.Common;
using System.Globalization;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Isolated catalog outbox for C07-01.M03.05; tenant/product/version CAS and idempotency. NEVER registered in
/// production DI. Tenant identity MUST come from server-authenticated RBAC.
/// No live provider, plan quota, permission, payment or customer writes.
/// </summary>
public sealed class C0701IsolatedCatalogOutbox(Func<DbConnection> connectionFactory)
{
    public sealed record Lease(int TenantId, long ProductId, long CatalogVersion, string WorkerId, long Revision);

    private readonly Func<DbConnection> _factory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private static bool WorkerOk(string? worker) =>
        worker is { Length: >= 8 and <= 64 } &&
        worker.All(static c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z'
            or >= '0' and <= '9' or '-' or '_');

    private static void Parameter(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }

    public async ValueTask<Lease?> TryClaimAsync(
        int tenantId, long productId, long catalogVersion, string workerId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (tenantId <= 0 || productId <= 0 || catalogVersion <= 0 || !WorkerOk(workerId)) return null;
        try
        {
            await using var db = _factory();
            if (db is null) return null;
            await db.OpenAsync(token);
            var now = DateTime.UtcNow;
            await using (var update = db.CreateCommand())
            {
                update.CommandText = """
                    UPDATE V22C0701CatalogOutboxIsolated
                       SET LeaseOwner=@producter, LeaseUntilUtc=@until,
                           Revision=Revision+1
                     WHERE TenantId=@tenant AND ProductId=@product AND CatalogVersion=@version AND State='PENDING'
                       AND (LeaseOwner IS NULL OR LeaseUntilUtc < @now)
                    """;
                Parameter(update, "@producter", workerId);
                Parameter(update, "@until", now.AddMinutes(5));
                Parameter(update, "@tenant", tenantId);
                Parameter(update, "@product", productId);
                Parameter(update, "@version", catalogVersion);
                Parameter(update, "@now", now);
                if (await update.ExecuteNonQueryAsync(token) != 1) return null;
            }
            await using var read = db.CreateCommand();
            read.CommandText = """
                SELECT Revision,LeaseOwner,LeaseUntilUtc FROM V22C0701CatalogOutboxIsolated
                 WHERE TenantId=@tenant AND ProductId=@product AND CatalogVersion=@version AND State='PENDING'
                """;
            Parameter(read, "@tenant", tenantId);
            Parameter(read, "@product", productId);
            Parameter(read, "@version", catalogVersion);
            await using var reader = await read.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token) ||
                !string.Equals(reader.GetString(1), workerId, StringComparison.Ordinal) ||
                reader.GetDateTime(2) <= now) return null;
            var revision = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture);
            if (revision <= 0 || await reader.ReadAsync(token)) return null;
            return new Lease(tenantId, productId, catalogVersion, workerId, revision);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return null; }
    }

    public async ValueTask<bool> TryAckAsync(Lease? lease, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (lease is null || lease.TenantId <= 0 || lease.ProductId <= 0 ||
            lease.CatalogVersion <= 0 || lease.Revision <= 0 || !WorkerOk(lease.WorkerId)) return false;
        try
        {
            await using var db = _factory();
            if (db is null) return false;
            await db.OpenAsync(token);
            await using var command = db.CreateCommand();
            command.CommandText = """
                UPDATE V22C0701CatalogOutboxIsolated
                   SET State='ACK', LeaseOwner=NULL, LeaseUntilUtc=NULL,
                       Revision=Revision+1
                 WHERE TenantId=@tenant AND ProductId=@product AND CatalogVersion=@version AND State='PENDING'
                   AND LeaseOwner=@producter AND Revision=@revision
                   AND LeaseUntilUtc>@now
                """;
            Parameter(command, "@tenant", lease.TenantId);
            Parameter(command, "@product", lease.ProductId);
            Parameter(command, "@version", lease.CatalogVersion);
            Parameter(command, "@producter", lease.WorkerId);
            Parameter(command, "@revision", lease.Revision);
            Parameter(command, "@now", DateTime.UtcNow);
            return await command.ExecuteNonQueryAsync(token) == 1;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return false; }
    }
}
