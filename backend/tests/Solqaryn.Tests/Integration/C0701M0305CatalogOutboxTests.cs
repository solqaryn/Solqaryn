using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0701M0305CatalogOutboxTests
{
    [Fact]
    public async Task Mysql84CatalogVersionTenantCASStaleReplayAndCancellation()
    {
        var raw = Environment.GetEnvironmentVariable("C0701_M0305_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var info = new MySqlConnectionStringBuilder(raw);
        if (info.Server is not ("localhost" or "127.0.0.1") ||
            info.Port != 3306 || info.Database != "solqaryn_c0701_catalog" ||
            info.UserID != "root")
            throw new InvalidOperationException("LOOPBACK_MYSQL_ONLY");
        await using var db = new MySqlConnection(raw);
        await db.OpenAsync();
        await using (var version = db.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
        }
        await using (var create = db.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS V22C0701CatalogOutboxIsolated(
                    TenantId INT NOT NULL, ProductId BIGINT NOT NULL,
                    CatalogVersion BIGINT NOT NULL,
                    State VARCHAR(12) NOT NULL DEFAULT 'PENDING',
                    LeaseOwner VARCHAR(64) NULL, LeaseUntilUtc DATETIME(6) NULL,
                    Revision BIGINT NOT NULL DEFAULT 1,
                    PRIMARY KEY(TenantId,ProductId)
                ) ENGINE=InnoDB
                """;
            await create.ExecuteNonQueryAsync();
        }
        var a = RandomNumberGenerator.GetInt32(100000, 300000000);
        var b = a + 1;
        long productId = RandomNumberGenerator.GetInt32(10000, 500000000);
        var store = new C0701IsolatedCatalogOutbox(() => new MySqlConnection(raw));

        async Task Execute(string sql, int tenant)
        {
            await using var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@tenant", tenant);
            cmd.Parameters.AddWithValue("@product", productId);
            await cmd.ExecuteNonQueryAsync();
        }

        try
        {
            const string insert = """
                INSERT INTO V22C0701CatalogOutboxIsolated(TenantId,ProductId,CatalogVersion,State,Revision)
                VALUES(@tenant,@product,7,'PENDING',1)
                """;
            await Execute(insert, a);
            await Execute(insert, b);
            await Assert.ThrowsAnyAsync<MySqlException>(() => Execute(insert, a));
            Assert.Null(await store.TryClaimAsync(a, productId, 7, "bad id!"));
            Assert.Null(await store.TryClaimAsync(0, productId, 7, "worker_A1"));
            Assert.Null(await store.TryClaimAsync(a, productId, 6, "worker_A1"));
            var first = await store.TryClaimAsync(a, productId, 7, "worker_A1");
            Assert.NotNull(first);
            Assert.Equal(2L, first.Revision);
            Assert.Null(await store.TryClaimAsync(a, productId, 7, "worker_A2"));
            var other = await store.TryClaimAsync(b, productId, 7, "worker_B1");
            Assert.NotNull(other);
            Assert.False(await store.TryAckAsync(first with { TenantId = b }));
            Assert.False(await store.TryAckAsync(first with { WorkerId = "worker_A2" }));
            Assert.False(await store.TryAckAsync(first with { Revision = 1 }));
            Assert.False(await store.TryAckAsync(first with { CatalogVersion = 6 }));
            Assert.True(await store.TryAckAsync(first));
            Assert.False(await store.TryAckAsync(first));
            Assert.Null(await store.TryClaimAsync(a, productId, 7, "worker_A3"));

            await Execute("""
                UPDATE V22C0701CatalogOutboxIsolated
                   SET LeaseUntilUtc='2000-01-01 00:00:00', CatalogVersion=8
                 WHERE TenantId=@tenant AND ProductId=@product
                """, b);
            Assert.Null(await store.TryClaimAsync(b, productId, 7, "worker_B2"));
            var reclaimed = await store.TryClaimAsync(b, productId, 8, "worker_B2");
            Assert.NotNull(reclaimed);
            Assert.True(reclaimed.Revision > other.Revision);
            Assert.False(await store.TryAckAsync(other));
            Assert.True(await store.TryAckAsync(reclaimed));

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await store.TryClaimAsync(a, productId, 7, "worker_A1", cancelled.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await store.TryAckAsync(first, cancelled.Token));
        }
        finally
        {
            const string cleanup = """
                DELETE FROM V22C0701CatalogOutboxIsolated
                 WHERE TenantId=@tenant AND ProductId=@product
                """;
            await Execute(cleanup, a);
            await Execute(cleanup, b);
        }
    }
}
