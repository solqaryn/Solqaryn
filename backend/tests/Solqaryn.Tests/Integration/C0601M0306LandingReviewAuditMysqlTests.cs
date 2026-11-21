using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>Physical MySQL 8.4 tests in a disposable loopback-only database.</summary>
[Trait("Category", "Integration")]
public sealed class C0601M0306LandingReviewAuditMysqlTests
{
    [Fact]
    public async Task StagingAndDurableAuditCommitExactlyOncePerTenantWithoutCrossTenantLeak()
    {
        var raw = RequiredLocalMysqlConnection();
        var schema = "c0601_m0306_" + Guid.NewGuid().ToString("N");
        await using var admin = new MySqlConnection(raw);
        await admin.OpenAsync();
        await AssertMySql84Async(admin);
        await ExecuteAsync(admin, "CREATE DATABASE " + Quote(schema));
        var isolated = new MySqlConnectionStringBuilder(raw) { Database = schema }.ConnectionString;
        try
        {
            await CreateTablesAsync(isolated, includeAudit: true);
            await SeedAsync(isolated);
            var worker = NewWorker(isolated);

            Assert.True(await worker.StageAuditedAsync(101, "inicio", 3, CancellationToken.None));
            Assert.False(await worker.StageAuditedAsync(101, "inicio", 3, CancellationToken.None));
            Assert.False(await worker.StageAuditedAsync(101, "inicio", 9, CancellationToken.None));
            Assert.False(await worker.StageAuditedAsync(303, "inicio", 3, CancellationToken.None));
            Assert.False(await worker.StageAuditedAsync(101, "privado", 5, CancellationToken.None));
            Assert.False(await worker.StageAuditedAsync(101, "ofertas", 2, CancellationToken.None));
            Assert.False(await worker.StageAuditedAsync(0, "inicio", 3, CancellationToken.None));
            foreach (var invalid in new string?[]
                     { null, "", "Inicio", "-inicio", "inicio-", "inicio' OR 1=1", new string('a', 64) })
                Assert.False(await worker.StageAuditedAsync(101, invalid, 3, CancellationToken.None));

            Assert.True(await worker.StageAuditedAsync(202, "inicio", 9, CancellationToken.None));
            Assert.Equal(2L, await CountAsync(isolated, "V22C0601LandingReviewQueue"));
            Assert.Equal(2L, await CountAsync(isolated, "V22C0601LandingReviewAuditIsolated"));
            Assert.Equal(1L, await CountTenantAsync(isolated, "V22C0601LandingReviewQueue", 101));
            Assert.Equal(1L, await CountTenantAsync(isolated, "V22C0601LandingReviewQueue", 202));
            Assert.Equal(1L, await CountTenantAsync(isolated, "V22C0601LandingReviewAuditIsolated", 101));
            Assert.Equal(1L, await CountTenantAsync(isolated, "V22C0601LandingReviewAuditIsolated", 202));
            Assert.Equal(2L, await ScalarAsync(isolated,
                "SELECT COUNT(*) FROM V22C0601LandingReviewAuditIsolated WHERE EventCode='REVIEW_QUEUED'"));
        }
        finally
        {
            await ExecuteAsync(admin, "DROP DATABASE " + Quote(schema));
        }
    }

    [Fact]
    public async Task AuditSinkFailureRollsBackQueueAndCancellationCannotWriteOrUseBootstrap()
    {
        var raw = RequiredLocalMysqlConnection();
        var schema = "c0601_m0306_" + Guid.NewGuid().ToString("N");
        await using var admin = new MySqlConnection(raw);
        await admin.OpenAsync();
        await AssertMySql84Async(admin);
        await ExecuteAsync(admin, "CREATE DATABASE " + Quote(schema));
        var isolated = new MySqlConnectionStringBuilder(raw) { Database = schema }.ConnectionString;
        try
        {
            await CreateTablesAsync(isolated, includeAudit: false);
            await SeedAsync(isolated);
            var worker = NewWorker(isolated);

            // Queue insert happens, audit INSERT fails, transaction must roll back both.
            Assert.False(await worker.StageAuditedAsync(101, "inicio", 3, CancellationToken.None));
            Assert.Equal(0L, await CountAsync(isolated, "V22C0601LandingReviewQueue"));

            await ExecuteAsync(isolated, """
                CREATE TABLE V22C0601LandingReviewAuditIsolated (
                    AuditId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    TenantId INT NOT NULL, PageSlug VARCHAR(63) NOT NULL,
                    Revision BIGINT NOT NULL, EventCode VARCHAR(32) NOT NULL,
                    RecordedUtc DATETIME(6) NOT NULL)
                """);
            Assert.True(await worker.StageAuditedAsync(101, "inicio", 3, CancellationToken.None));
            Assert.Equal(1L, await CountAsync(isolated, "V22C0601LandingReviewQueue"));
            Assert.Equal(1L, await CountAsync(isolated, "V22C0601LandingReviewAuditIsolated"));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await worker.StageAuditedAsync(101, "inicio", 3, new CancellationToken(true)));
            Assert.Equal(1L, await CountAsync(isolated, "V22C0601LandingReviewAuditIsolated"));

            var bootstrapWorker = NewWorker(raw);
            Assert.False(await bootstrapWorker.StageAuditedAsync(101, "inicio", 3, CancellationToken.None));
            Assert.Equal(1L, await CountAsync(isolated, "V22C0601LandingReviewQueue"));
        }
        finally
        {
            await ExecuteAsync(admin, "DROP DATABASE " + Quote(schema));
        }
    }

    private static C0601IsolatedLandingReviewAuditWorker NewWorker(string connectionString) =>
        new(() => new MySqlConnection(connectionString),
            NullLogger<C0601IsolatedLandingReviewAuditWorker>.Instance);

    private static string RequiredLocalMysqlConnection()
    {
        var raw = Environment.GetEnvironmentVariable("C0601_M0306_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var value = new MySqlConnectionStringBuilder(raw);
        if (value.Server is not ("127.0.0.1" or "localhost") ||
            value.Port != 3306 || value.UserID != "root")
            throw new InvalidOperationException("LOOPBACK_ONLY");
        return raw;
    }

    private static string Quote(string schema) => string.Concat((char)96, schema, (char)96);

    private static async Task AssertMySql84Async(MySqlConnection connection)
    {
        await using var version = connection.CreateCommand();
        version.CommandText = "SELECT VERSION()";
        Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync())!);
    }

    private static async Task CreateTablesAsync(string connectionString, bool includeAudit)
    {
        await ExecuteAsync(connectionString, """
            CREATE TABLE V22C0601LandingDraftIsolated (
                TenantId INT NOT NULL, PageSlug VARCHAR(63) NOT NULL,
                Revision BIGINT NOT NULL, DraftState SMALLINT NOT NULL,
                PRIMARY KEY (TenantId, PageSlug))
            """);
        await ExecuteAsync(connectionString, """
            CREATE TABLE V22C0601LandingReviewQueue (
                TenantId INT NOT NULL, PageSlug VARCHAR(63) NOT NULL,
                Revision BIGINT NOT NULL, QueueState SMALLINT NOT NULL,
                RequestedUtc DATETIME(6) NOT NULL,
                PRIMARY KEY (TenantId, PageSlug, Revision))
            """);
        if (includeAudit)
            await ExecuteAsync(connectionString, """
                CREATE TABLE V22C0601LandingReviewAuditIsolated (
                    AuditId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    TenantId INT NOT NULL, PageSlug VARCHAR(63) NOT NULL,
                    Revision BIGINT NOT NULL, EventCode VARCHAR(32) NOT NULL,
                    RecordedUtc DATETIME(6) NOT NULL)
                """);
    }

    private static Task SeedAsync(string connectionString) => ExecuteAsync(connectionString, """
        INSERT INTO V22C0601LandingDraftIsolated
            (TenantId,PageSlug,Revision,DraftState)
        VALUES (101,'inicio',3,1),(202,'inicio',9,1),
               (101,'privado',5,0),(101,'ofertas',7,1)
        """);

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, sql);
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static Task<long> CountAsync(string connectionString, string table) =>
        ScalarAsync(connectionString, "SELECT COUNT(*) FROM " + table);

    private static async Task<long> CountTenantAsync(string connectionString, string table, int tenant)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE TenantId=@tenant";
        command.Parameters.AddWithValue("@tenant", tenant);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
