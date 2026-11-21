using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>Physical, disposable MySQL 8.4 review queue. Never contacts Aiven.</summary>
[Trait("Category", "Integration")]
public sealed class C0601M0305LandingReviewQueueMysqlTests
{
    [Fact]
    public async Task ReadyTenantDraftStagesExactlyOnceWithRealMysqlAndNoForeignOrStaleRows()
    {
        var raw = Environment.GetEnvironmentVariable("C0601_M0305_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var root = new MySqlConnectionStringBuilder(raw);
        if (root.Server is not ("127.0.0.1" or "localhost") ||
            root.Port != 3306 || root.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var schema = "c0601_m0305_" + Guid.NewGuid().ToString("N");
        await using var admin = new MySqlConnection(raw);
        await admin.OpenAsync();
        await using (var version = admin.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync())!);
        }
        await using (var create = admin.CreateCommand())
        {
            create.CommandText = "CREATE DATABASE `" + schema + "`";
            await create.ExecuteNonQueryAsync();
        }
        var isolated = new MySqlConnectionStringBuilder(raw) { Database = schema }.ConnectionString;
        try
        {
            await ExecuteAsync(isolated, """
                CREATE TABLE V22C0601LandingDraftIsolated (
                    TenantId INT NOT NULL, PageSlug VARCHAR(63) NOT NULL,
                    Revision BIGINT NOT NULL, DraftState SMALLINT NOT NULL,
                    EditedUtc DATETIME(6) NOT NULL,
                    PRIMARY KEY (TenantId, PageSlug))
                """);
            await ExecuteAsync(isolated, """
                CREATE TABLE V22C0601LandingReviewQueue (
                    TenantId INT NOT NULL, PageSlug VARCHAR(63) NOT NULL,
                    Revision BIGINT NOT NULL, QueueState SMALLINT NOT NULL,
                    RequestedUtc DATETIME(6) NOT NULL,
                    PRIMARY KEY (TenantId, PageSlug, Revision))
                """);
            await ExecuteAsync(isolated, """
                INSERT INTO V22C0601LandingDraftIsolated
                    (TenantId, PageSlug, Revision, DraftState, EditedUtc)
                VALUES
                    (101, 'inicio', 3, 1, '2026-11-01 10:00:00'),
                    (202, 'inicio', 9, 1, '2026-11-02 10:00:00'),
                    (101, 'privado', 5, 0, '2026-11-03 10:00:00'),
                    (101, 'ofertas', 4, 1, '2026-11-04 10:00:00')
                """);
            var worker = new C0601IsolatedLandingReviewQueueWorker(
                () => new MySqlConnection(isolated));
            Assert.True(await worker.StageReviewAsync(101, "inicio", 3, CancellationToken.None));
            Assert.False(await worker.StageReviewAsync(101, "inicio", 3, CancellationToken.None));
            Assert.False(await worker.StageReviewAsync(101, "inicio", 9, CancellationToken.None));
            Assert.False(await worker.StageReviewAsync(303, "inicio", 3, CancellationToken.None));
            Assert.False(await worker.StageReviewAsync(101, "privado", 5, CancellationToken.None));
            Assert.False(await worker.StageReviewAsync(0, "inicio", 3, CancellationToken.None));
            Assert.False(await worker.StageReviewAsync(101, "inicio", 0, CancellationToken.None));
            foreach (var bad in new string?[]
                     { null, "", "-inicio", "inicio-", "Inicio",
                       "inicio?tenant=202", "inicio' OR 1=1", new string('a', 64) })
                Assert.False(await worker.StageReviewAsync(101, bad, 3, CancellationToken.None));
            Assert.Equal(1L, await CountAsync(isolated));
            await ExecuteAsync(isolated, """
                UPDATE V22C0601LandingDraftIsolated SET Revision = 6
                 WHERE TenantId = 101 AND PageSlug = 'ofertas'
                """);
            Assert.False(await worker.StageReviewAsync(101, "ofertas", 4, CancellationToken.None));
            Assert.True(await worker.StageReviewAsync(101, "ofertas", 6, CancellationToken.None));
            Assert.True(await worker.StageReviewAsync(202, "inicio", 9, CancellationToken.None));
            Assert.Equal(3L, await CountAsync(isolated));
            Assert.Equal(2L, await CountTenantAsync(isolated, 101));
            Assert.Equal(1L, await CountTenantAsync(isolated, 202));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await worker.StageReviewAsync(101, "inicio", 3, new CancellationToken(true)));
            Assert.Equal(3L, await CountAsync(isolated));
            await ExecuteAsync(isolated, "DROP TABLE V22C0601LandingReviewQueue");
            Assert.False(await worker.StageReviewAsync(101, "inicio", 3, CancellationToken.None));
        }
        finally
        {
            await using var cleanup = admin.CreateCommand();
            cleanup.CommandText = "DROP DATABASE `" + schema + "`";
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task RefusesNonIsolatedBootstrapDatabaseBeforeAnyDatabaseMutation()
    {
        var raw = Environment.GetEnvironmentVariable("C0601_M0305_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var worker = new C0601IsolatedLandingReviewQueueWorker(
            () => new MySqlConnection(raw));
        Assert.False(await worker.StageReviewAsync(101, "inicio", 3, CancellationToken.None));
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM V22C0601LandingReviewQueue";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<long> CountTenantAsync(string connectionString, int tenant)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM V22C0601LandingReviewQueue WHERE TenantId=@tenant";
        command.Parameters.AddWithValue("@tenant", tenant);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
