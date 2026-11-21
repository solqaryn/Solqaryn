using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0701M0306CatalogAuditTests
{
    [Fact]
    public async Task MySql84IsolatedAuditTenantBoundaryReplaySanitizationAndCancellation()
    {
        var raw = Environment.GetEnvironmentVariable("C0701_M0306_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var info = new MySqlConnectionStringBuilder(raw);
        if (info.Server is not ("localhost" or "127.0.0.1") ||
            info.Port != 3306 || info.Database != "solqaryn_c0701_audit" ||
            info.UserID != "root")
            throw new InvalidOperationException("LOOPBACK_MYSQL84_ONLY");
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
                CREATE TABLE IF NOT EXISTS V22C0701CatalogAuditIsolated(
                    TenantId INT NOT NULL, ProductId BIGINT NOT NULL,
                    CatalogVersion BIGINT NOT NULL, ActorId BIGINT NOT NULL,
                    EventId VARCHAR(64) NOT NULL, EventCode VARCHAR(16) NOT NULL,
                    OccurredUtc DATETIME(6) NOT NULL,
                    PRIMARY KEY (TenantId,ProductId,EventId)
                ) ENGINE=InnoDB
                """;
            await create.ExecuteNonQueryAsync();
        }
        var tenantA = RandomNumberGenerator.GetInt32(100000, 300000000);
        var tenantB = tenantA + 1;
        var productId = (long)RandomNumberGenerator.GetInt32(10000, 500000000);
        var sink = new C0701IsolatedCatalogAuditSink(() => new MySqlConnection(raw));
        const string eventA = "catalog_a_20261121_01";
        const string eventB = "catalog_b_20261121_01";
        try
        {
            Assert.Equal("AUDIT_REJECTED", (await sink.RecordAsync(0, productId, 7, 1, eventA, "LEASE")).Code);
            Assert.Equal("AUDIT_REJECTED", (await sink.RecordAsync(tenantA, productId, 0, 1, eventA, "LEASE")).Code);
            Assert.Equal("AUDIT_REJECTED", (await sink.RecordAsync(tenantA, productId, 7, 0, eventA, "LEASE")).Code);
            Assert.Equal("AUDIT_REJECTED", (await sink.RecordAsync(tenantA, productId, 7, 1,
                "Bearer secret@example.com", "LEASE")).Code);
            Assert.Equal("AUDIT_REJECTED", (await sink.RecordAsync(tenantA, productId, 7, 1,
                eventA, "EXCEPTION:secret")).Code);

            var a = await sink.RecordAsync(tenantA, productId, 7, 101, eventA, "LEASE");
            Assert.True(a.Stored);
            Assert.False(a.GrantsAuthorization);
            Assert.False(a.SendsProviderEvents);
            Assert.Equal("AUDIT_REJECTED", (await sink.RecordAsync(tenantA, productId, 7,
                101, eventA, "ACK")).Code); // Never silently overwrite historical record.
            Assert.True((await sink.RecordAsync(tenantB, productId, 8, 202, eventB, "DENY")).Stored);
            var rowsA = await sink.ReadTenantAsync(tenantA, productId);
            var rowsB = await sink.ReadTenantAsync(tenantB, productId);
            Assert.Single(rowsA);
            Assert.Single(rowsB);
            Assert.Equal("LEASE", rowsA[0].EventCode);
            Assert.Equal(7L, rowsA[0].CatalogVersion);
            Assert.Equal(eventA, rowsA[0].EventId);
            Assert.Equal("DENY", rowsB[0].EventCode);
            Assert.Equal(eventB, rowsB[0].EventId);
            Assert.Empty(await sink.ReadTenantAsync(tenantA, productId + 1));
            Assert.Empty(await sink.ReadTenantAsync(0, productId));
            Assert.Empty(await sink.ReadTenantAsync(tenantB + 1, productId));

            var unavailable = new C0701IsolatedCatalogAuditSink(
                () => throw new InvalidOperationException("TOP_SECRET_DATABASE_PASSWORD"));
            Assert.Equal("AUDIT_REJECTED", (await unavailable.RecordAsync(tenantA, productId,
                7, 101, "catalog_error_20261121_01", "FAIL")).Code);
            Assert.Empty(await unavailable.ReadTenantAsync(tenantA, productId));

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await sink.RecordAsync(tenantA, productId, 7, 101,
                    "catalog_cancel_20261121_01", "FAIL", cancelled.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await sink.ReadTenantAsync(tenantA, productId, cancelled.Token));
            Assert.Single(await sink.ReadTenantAsync(tenantA, productId));
        }
        finally
        {
            foreach (var tenant in new[] { tenantA, tenantB })
            {
                await using var delete = db.CreateCommand();
                delete.CommandText = """
                    DELETE FROM V22C0701CatalogAuditIsolated
                     WHERE TenantId=@tenant AND ProductId=@product
                    """;
                delete.Parameters.AddWithValue("@tenant", tenant);
                delete.Parameters.AddWithValue("@product", productId);
                await delete.ExecuteNonQueryAsync();
            }
        }
    }
}
