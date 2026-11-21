using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>Real physical MySQL 8.4, isolated ephemeral schema; no Aiven/production data.</summary>
[Trait("Category", "Integration")]
public sealed class C0601M0304IsolatedLandingMysqlTests
{
    [Fact]
    public async Task IsolatedTenantABDraftMetadataAndCorruptionDenyByDefault()
    {
        var raw = Environment.GetEnvironmentVariable("C0601_M0304_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var root = new MySqlConnectionStringBuilder(raw);
        if (root.Server is not ("127.0.0.1" or "localhost") ||
            root.Port != 3306 || root.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var schema = "c0601_m0304_" + Guid.NewGuid().ToString("N");
        await using var admin = new MySqlConnection(raw);
        await admin.OpenAsync();
        await using (var verify = admin.CreateCommand())
        {
            verify.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await verify.ExecuteScalarAsync())!);
        }
        await using (var create = admin.CreateCommand())
        {
            create.CommandText = "CREATE DATABASE " + (char)96 + schema + (char)96;
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
                INSERT INTO V22C0601LandingDraftIsolated
                    (TenantId, PageSlug, Revision, DraftState, EditedUtc)
                VALUES
                    (101, 'inicio', 3, 0, '2026-11-01 10:00:00'),
                    (202, 'inicio', 9, 1, '2026-11-02 11:00:00')
                """);
            var adapter = new C0601IsolatedLandingMySqlSnapshotAdapter(
                () => new MySqlConnection(isolated));

            var a = await adapter.ReadDraftAsync(101, "inicio", CancellationToken.None);
            Assert.NotNull(a);
            Assert.Equal(101, a.TenantId);
            Assert.Equal("inicio", a.PageSlug);
            Assert.Equal(3L, a.Revision);
            Assert.Equal(0, a.DraftState);
            Assert.Equal(DateTimeKind.Utc, a.EditedUtc.Kind);
            Assert.False(a.AuthorizesPublication);
            Assert.False(a.GrantsPrivateOrCrossTenantRead);
            Assert.False(a.EstablishesLegalOrSeoApproval);
            Assert.False(a.PermitsBusinessMutation);

            var b = await adapter.ReadDraftAsync(202, "inicio", CancellationToken.None);
            Assert.NotNull(b);
            Assert.Equal(202, b.TenantId);
            Assert.Equal(9L, b.Revision);
            Assert.Equal(1, b.DraftState);
            Assert.False(b.AuthorizesPublication);

            // The same slug is not authority to read a different tenant's row.
            Assert.Null(await adapter.ReadDraftAsync(303, "inicio", CancellationToken.None));
            Assert.Null(await adapter.ReadDraftAsync(101, "contacto", CancellationToken.None));
            Assert.Null(await adapter.ReadDraftAsync(0, "inicio", CancellationToken.None));
            Assert.Null(await adapter.ReadDraftAsync(-1, "inicio", CancellationToken.None));
            foreach (var slug in new string?[]
                     { null, "", "-inicio", "inicio-", "Inicio", "inicio/privado",
                       "inicio?tenant=202", "inicio' OR 1=1", new string('a', 64) })
                Assert.Null(await adapter.ReadDraftAsync(101, slug, CancellationToken.None));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await adapter.ReadDraftAsync(101, "inicio", new CancellationToken(true)));

            var before = await CountAsync(isolated);
            Assert.Equal(2L, before);
            Assert.NotNull(await adapter.ReadDraftAsync(101, "inicio", CancellationToken.None));
            Assert.Equal(before, await CountAsync(isolated));

            await ExecuteAsync(isolated, """
                UPDATE V22C0601LandingDraftIsolated SET Revision = 0
                WHERE TenantId = 101 AND PageSlug = 'inicio'
                """);
            Assert.Null(await adapter.ReadDraftAsync(101, "inicio", CancellationToken.None));
            await ExecuteAsync(isolated, """
                UPDATE V22C0601LandingDraftIsolated SET Revision = 3, DraftState = 99
                WHERE TenantId = 101 AND PageSlug = 'inicio'
                """);
            Assert.Null(await adapter.ReadDraftAsync(101, "inicio", CancellationToken.None));
            await ExecuteAsync(isolated, "DROP TABLE V22C0601LandingDraftIsolated");
            Assert.Null(await adapter.ReadDraftAsync(101, "inicio", CancellationToken.None));
        }
        finally
        {
            await using var cleanup = admin.CreateCommand();
            cleanup.CommandText = "DROP DATABASE " + (char)96 + schema + (char)96;
            await cleanup.ExecuteNonQueryAsync();
        }
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
        command.CommandText = "SELECT COUNT(*) FROM V22C0601LandingDraftIsolated";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
