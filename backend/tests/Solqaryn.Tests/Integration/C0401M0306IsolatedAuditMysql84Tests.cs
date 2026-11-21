using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0401M0306IsolatedAuditMysql84Tests
{
    private const string DbName = "c0401_m0306_bootstrap";
    private static string Connection()
    {
        var raw = Environment.GetEnvironmentVariable("C0401_M0306_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var cs = new MySqlConnectionStringBuilder(raw);
        if (cs.Server is not ("127.0.0.1" or "localhost") ||
            cs.Port != 3306 || cs.UserID != "root" || cs.Database != DbName)
            throw new InvalidOperationException("ISOLATED_LOOPBACK_MYSQL84_ONLY");
        return raw;
    }

    private static async Task<long> CountAsync(
        MySqlConnection db, int tenant, string eventId)
    {
        await using var cmd = db.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM V22C0401AuditIsolated
             WHERE TenantId=@tenant AND EventId=@id
            """;
        cmd.Parameters.AddWithValue("@tenant", tenant);
        cmd.Parameters.AddWithValue("@id", eventId);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task PhysicalMysql84AuditIsAppendOnlyTenantIsolatedAndSanitized()
    {
        var raw = Connection();
        await using var db = new MySqlConnection(raw);
        await db.OpenAsync();
        await using (var v = db.CreateCommand())
        {
            v.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await v.ExecuteScalarAsync()));
        }
        await using (var create = db.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS V22C0401AuditIsolated(
                  TenantId INT NOT NULL,
                  EventId VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
                  WorkId BIGINT NOT NULL, ActorId BIGINT NOT NULL,
                  EventCode VARCHAR(16) NOT NULL,
                  OccurredUtc DATETIME(6) NOT NULL,
                  PRIMARY KEY(TenantId,EventId),
                  INDEX IX_C0401_AuditWork(TenantId,WorkId,OccurredUtc)
                ) ENGINE=InnoDB
                """;
            await create.ExecuteNonQueryAsync();
        }
        var a = RandomNumberGenerator.GetInt32(100000, 300000000);
        var b = a + 1;
        var id = "C0401_AUDIT_" + Guid.NewGuid().ToString("N");
        var rolledBack = "C0401_ROLLBACK_" + Guid.NewGuid().ToString("N");
        var sink = new C0401IsolatedStructuredAuditSink(
            () => new MySqlConnection(raw));
        try
        {
            var first = await sink.RecordAsync(a, 7777, 55, id, "CLAIM");
            Assert.Equal("AUDIT_RECORDED", first.Code);
            Assert.True(first.Stored);
            Assert.False(first.GrantsAuthorization);
            Assert.False(first.WritesCommercialData);
            var secondTenant = await sink.RecordAsync(b, 7777, 66, id, "ACK");
            Assert.True(secondTenant.Stored);
            Assert.Equal(1L, await CountAsync(db, a, id));
            Assert.Equal(1L, await CountAsync(db, b, id));
            Assert.False((await sink.RecordAsync(a, 7777, 55, id, "CLAIM")).Stored);
            Assert.False((await sink.RecordAsync(a, 7777, 99, id, "DENY")).Stored);
            Assert.Equal(1L, await CountAsync(db, a, id));

            await using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT WorkId,ActorId,EventCode,OccurredUtc
                    FROM V22C0401AuditIsolated
                    WHERE TenantId=@tenant AND EventId=@id
                    """;
                cmd.Parameters.AddWithValue("@tenant", a);
                cmd.Parameters.AddWithValue("@id", id);
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal(7777L, Convert.ToInt64(reader.GetValue(0)));
                Assert.Equal(55L, Convert.ToInt64(reader.GetValue(1)));
                Assert.Equal("CLAIM", reader.GetString(2));
                Assert.True(reader.GetDateTime(3) > DateTime.UtcNow.AddMinutes(-5));
                Assert.False(await reader.ReadAsync());
            }

            Assert.False((await sink.RecordAsync(0, 1, 1, id, "ACK")).Stored);
            Assert.False((await sink.RecordAsync(a, 0, 1, id, "ACK")).Stored);
            Assert.False((await sink.RecordAsync(a, 1, 0, id, "ACK")).Stored);
            Assert.False((await sink.RecordAsync(a, 1, 1, "bad", "ACK")).Stored);
            Assert.False((await sink.RecordAsync(a, 1, 1, id, "DROP")).Stored);
            Assert.False((await sink.RecordAsync(a, 1, 1,
                "eyJhbGciOiJIUzI1NiJ9.jwt.signature", "ACK")).Stored);
            Assert.False((await sink.RecordAsync(a, 1, 1,
                new string('x', 65), "ACK")).Stored);

            // A rollback does not produce an audit artifact that can be
            // misinterpreted as an ACK or a completed operation.
            await using (var tx = await db.BeginTransactionAsync())
            {
                await using var insert = db.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText = """
                    INSERT INTO V22C0401AuditIsolated
                    (TenantId,EventId,WorkId,ActorId,EventCode,OccurredUtc)
                    VALUES (@tenant,@id,1,1,'DENY',UTC_TIMESTAMP(6))
                    """;
                insert.Parameters.AddWithValue("@tenant", a);
                insert.Parameters.AddWithValue("@id", rolledBack);
                await insert.ExecuteNonQueryAsync();
                await tx.RollbackAsync();
            }
            Assert.Equal(0L, await CountAsync(db, a, rolledBack));
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await sink.RecordAsync(a, 1, 1, id, "ACK", cancelled.Token));
            Assert.Equal("AUDIT_REJECTED", (await new C0401IsolatedStructuredAuditSink(
                () => throw new InvalidOperationException("sensitive SQL details"))
                .RecordAsync(a, 1, 1, "C0401_ERROR_EVENT_" + Guid.NewGuid().ToString("N"),
                    "DENY")).Code);
        }
        finally
        {
            await using var clean = db.CreateCommand();
            clean.CommandText = """
                DELETE FROM V22C0401AuditIsolated
                 WHERE TenantId IN (@a,@b) AND EventId IN (@id,@rollback)
                """;
            clean.Parameters.AddWithValue("@a", a);
            clean.Parameters.AddWithValue("@b", b);
            clean.Parameters.AddWithValue("@id", id);
            clean.Parameters.AddWithValue("@rollback", rolledBack);
            await clean.ExecuteNonQueryAsync();
        }
    }
}
