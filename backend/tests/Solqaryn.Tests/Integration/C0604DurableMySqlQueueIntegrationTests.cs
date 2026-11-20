using MySqlConnector;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Queue = Solqaryn.Infrastructure.AutomationV5.C0604IsolatedMySqlDurableQueue;

namespace Solqaryn.Tests.Integration;

public sealed class C0604DurableMySqlQueueIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task InnoDbTenantIsolationAtomicIdempotencyLeasesAndDeadLetter()
    {
        // Only the dedicated isolated GitHub 8.4 lane sets this connection.
        // A generic lane without it cannot claim a real DB integration pass.
        var connectionString = Environment.GetEnvironmentVariable("C0604_MYSQL84_TEST");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        const string table = "v22_c0604_portal_work";
        await using var admin = new MySqlConnection(connectionString);
        await admin.OpenAsync();
        await using (var create = admin.CreateCommand())
        {
            create.CommandText = $"""
                CREATE TABLE IF NOT EXISTS {table} (
                  id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                  tenant_id INT NOT NULL,
                  resource_id VARCHAR(128) NOT NULL,
                  operation VARCHAR(64) NOT NULL,
                  idempotency_key VARCHAR(64) NOT NULL,
                  resource_version BIGINT NOT NULL,
                  state VARCHAR(8) NOT NULL,
                  attempts INT NOT NULL DEFAULT 0,
                  leased_by VARCHAR(64) NULL,
                  lease_until_utc DATETIME(6) NULL,
                  UNIQUE KEY uq_tenant_idempotency (tenant_id,idempotency_key)
                ) ENGINE=InnoDB;
                """;
            await create.ExecuteNonQueryAsync();
        }
        try
        {
            await using (var clear = admin.CreateCommand())
            {
                clear.CommandText = $"DELETE FROM {table}";
                await clear.ExecuteNonQueryAsync();
            }
            var queue = new Queue(() => new MySqlConnection(connectionString));
            var now = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(Queue.Reservation.Rejected,
                await queue.ReserveAsync(0, "customer_A", "customer-preference-sync", "request_0001", 1));
            Assert.Equal(Queue.Reservation.Rejected,
                await queue.ReserveAsync(11, "customer_A", "transfer-funds", "request_0001", 1));
            Assert.Equal(Queue.Reservation.Stored,
                await queue.ReserveAsync(11, "customer_A", "customer-preference-sync", "request_0001", 3));
            Assert.Equal(Queue.Reservation.Duplicate,
                await queue.ReserveAsync(11, "customer_A", "customer-preference-sync", "request_0001", 3));
            Assert.Equal(Queue.Reservation.Conflict,
                await queue.ReserveAsync(11, "customer_B", "customer-preference-sync", "request_0001", 3));
            Assert.Equal(Queue.Reservation.Stored,
                await queue.ReserveAsync(12, "customer_B", "customer-preference-sync", "request_0001", 3));

            Assert.Null(await queue.TryLeaseAsync(13, "worker_13", now, 1, 2));
            var first = await queue.TryLeaseAsync(11, "worker_A", now, 1, 2);
            Assert.NotNull(first);
            Assert.Equal("customer_A", first!.ResourceId);
            Assert.Equal(1, first.Attempt);
            Assert.Null(await queue.TryLeaseAsync(11, "worker_B", now, 1, 2));
            Assert.False(await queue.SettleAsync(12, first.Id, "worker_A", now, true, 2));
            Assert.False(await queue.SettleAsync(11, first.Id, "worker_B", now, true, 2));

            Assert.True(await queue.SettleAsync(11, first.Id, "worker_A", now, false, 2));
            var retry = await queue.TryLeaseAsync(11, "worker_B", now.AddSeconds(1), 1, 2);
            Assert.NotNull(retry);
            Assert.Equal(first.Id, retry!.Id);
            Assert.Equal(2, retry.Attempt);
            Assert.True(await queue.SettleAsync(11, retry.Id, "worker_B", now.AddSeconds(2), false, 2));
            Assert.Null(await queue.TryLeaseAsync(11, "worker_C", now.AddMinutes(2), 1, 2));

            var otherTenant = await queue.TryLeaseAsync(12, "worker_C", now, 1, 2);
            Assert.NotNull(otherTenant);
            Assert.True(await queue.SettleAsync(12, otherTenant!.Id, "worker_C", now, true, 2));
            Assert.Null(await queue.TryLeaseAsync(12, "worker_D", now.AddMinutes(5), 1, 2));

            Assert.Equal(Queue.Reservation.Stored,
                await queue.ReserveAsync(14, "customer_C", "customer-reservation-confirmation", "request_0002", 4));
            var leased = await queue.TryLeaseAsync(14, "worker_D", now, 1, 2);
            Assert.NotNull(leased);
            var reclaimed = await queue.TryLeaseAsync(14, "worker_E", now.AddMinutes(2), 1, 2);
            Assert.NotNull(reclaimed);
            Assert.Equal(leased!.Id, reclaimed!.Id);
            Assert.False(await queue.SettleAsync(14, leased.Id, "worker_D", now.AddMinutes(2), true, 2));
            Assert.True(await queue.SettleAsync(14, reclaimed.Id, "worker_E", now.AddMinutes(2), true, 2));

            var snapshot = new List<(int Tenant, string State, int Attempts)>();
            await using (var inspect = admin.CreateCommand())
            {
                inspect.CommandText = $"SELECT tenant_id,state,attempts FROM {table} ORDER BY tenant_id";
                await using var reader = await inspect.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    snapshot.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
            }
            Assert.Contains(snapshot, x => x == (11, "DEAD", 2));
            Assert.Contains(snapshot, x => x == (12, "DONE", 1));
            Assert.Contains(snapshot, x => x == (14, "DONE", 2));
            Assert.Equal(3, snapshot.Count);
        }
        finally
        {
            await using var drop = admin.CreateCommand();
            drop.CommandText = $"DROP TABLE IF EXISTS {table}";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
