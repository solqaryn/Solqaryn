using MySql.Data.MySqlClient;
using Xunit;
using Adapter = Solqaryn.Application.AutomationV5.C0304IsolatedIncidentPersistenceProjectionAdapter;

namespace Solqaryn.Tests;

/// <summary>C03-04.M05.03: isolated real MySQL 8.4 fixture, never a production incident repository.</summary>
[Trait("Category", "Integration")]
public sealed class C0304M0503IncidentMySqlPersistenceIntegrationTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static MySqlConnectionStringBuilder LocalOnly(string raw)
    {
        var c = new MySqlConnectionStringBuilder(raw);
        if (c.Server is not ("127.0.0.1" or "localhost") || c.Port != 3306)
            throw new InvalidOperationException("Only ephemeral localhost MySQL:3306 is permitted.");
        return c;
    }

    [Theory]
    [InlineData("Server=prod.example;Port=3306;User=root;Password=fixture;")]
    [InlineData("Server=127.0.0.1;Port=3307;User=root;Password=fixture;")]
    public void BlocksRemoteAndNonstandardPort(string raw) =>
        Assert.Throws<InvalidOperationException>(() => LocalOnly(raw));

    private static async Task<Adapter.StoredMetadata?> ReadAsync(string cs, int tenant)
    {
        using var db = new MySqlConnection(cs);
        await db.OpenAsync();
        using var cmd = new MySqlCommand("""
            SELECT TenantId, BranchId, Version, State, Revoked, Sanitized, CommitSha
            FROM IncidentSnapshotFixture WHERE TenantId=@tenant AND IncidentId=7
            """, db);
        cmd.Parameters.AddWithValue("@tenant", tenant);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new Adapter.StoredMetadata(reader.GetInt32(0), reader.GetInt32(1),
            reader.GetInt64(2), reader.GetString(3), true,
            Convert.ToInt32(reader.GetValue(4)) != 0,
            Convert.ToInt32(reader.GetValue(5)) != 0, reader.GetString(6));
    }

    [Fact]
    public async Task MySql84_TenantABRevocationAndRollbackKeepIncidentGateClosed()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL service is required.");
        var local = LocalOnly(raw);
        var database = "v20_c0304_m0503_" + Guid.NewGuid().ToString("N");
        var adminCs = new MySqlConnectionStringBuilder(local.ConnectionString) { Database = "mysql" };
        var isolatedCs = new MySqlConnectionStringBuilder(local.ConnectionString) { Database = database };
        using var admin = new MySqlConnection(adminCs.ConnectionString);
        await admin.OpenAsync();
        using (var check = new MySqlCommand("SELECT VERSION()", admin))
            Assert.StartsWith("8.4.", Convert.ToString(await check.ExecuteScalarAsync()));
        try
        {
            using (var create = new MySqlCommand($"CREATE DATABASE {database}", admin))
                await create.ExecuteNonQueryAsync();
            using (var db = new MySqlConnection(isolatedCs.ConnectionString))
            {
                await db.OpenAsync();
                using var ddl = new MySqlCommand("""
                    CREATE TABLE IncidentSnapshotFixture(
                        IncidentId BIGINT NOT NULL, TenantId INT NOT NULL, BranchId INT NOT NULL,
                        Version BIGINT NOT NULL, State VARCHAR(30) NOT NULL,
                        Revoked TINYINT NOT NULL, Sanitized TINYINT NOT NULL, CommitSha CHAR(40) NOT NULL,
                        PRIMARY KEY(TenantId, IncidentId)) ENGINE=InnoDB
                    """, db);
                await ddl.ExecuteNonQueryAsync();
                using var seed = new MySqlCommand("""
                    INSERT INTO IncidentSnapshotFixture VALUES
                    (7,100,7,4,'Open',0,1,@sha),(7,200,8,5,'Contained',0,1,@sha)
                    """, db);
                seed.Parameters.AddWithValue("@sha", Sha);
                Assert.Equal(2, await seed.ExecuteNonQueryAsync());
            }
            var a = await ReadAsync(isolatedCs.ConnectionString, 100);
            var b = await ReadAsync(isolatedCs.ConnectionString, 200);
            Assert.NotNull(a);
            Assert.NotNull(b);
            var allow = Adapter.Project(a, 100, 7, Sha);
            Assert.Equal("AUTHENTICATED_INCIDENT_GATE_REQUIRED", allow.Code);
            Assert.False(allow.AuthorizesBackupRestore);
            Assert.False(allow.AuthorizesIncidentMutation);
            Assert.False(allow.CertifiesPersistedOwnership);
            Assert.Equal("AUTHENTICATED_INCIDENT_GATE_REQUIRED",
                Adapter.Project(b, 200, 8, Sha).Code);
            Assert.Equal("INCIDENT_CROSS_TENANT_DENIED", Adapter.Project(a, 200, 7, Sha).Code);
            Assert.Equal("INCIDENT_BRANCH_SCOPE_DENIED", Adapter.Project(a, 100, 8, Sha).Code);
            Assert.Equal("EXACT_SOURCE_SHA_REQUIRED",
                Adapter.Project(a, 100, 7, new string('f', 40)).Code);
            Assert.Equal("VERIFIED_INCIDENT_SOURCE_REQUIRED",
                Adapter.Project(await ReadAsync(isolatedCs.ConnectionString, 300), 300, 7, Sha).Code);

            using (var db = new MySqlConnection(isolatedCs.ConnectionString))
            {
                await db.OpenAsync();
                using var revoke = new MySqlCommand(
                    "UPDATE IncidentSnapshotFixture SET Revoked=1 WHERE TenantId=100 AND IncidentId=7", db);
                Assert.Equal(1, await revoke.ExecuteNonQueryAsync());
            }
            Assert.Equal("INCIDENT_ACCESS_REVOKED",
                Adapter.Project(await ReadAsync(isolatedCs.ConnectionString, 100), 100, 7, Sha).Code);
            Assert.Equal("AUTHENTICATED_INCIDENT_GATE_REQUIRED",
                Adapter.Project(await ReadAsync(isolatedCs.ConnectionString, 200), 200, 8, Sha).Code);

            using (var db = new MySqlConnection(isolatedCs.ConnectionString))
            {
                await db.OpenAsync();
                using var transaction = db.BeginTransaction();
                using var update = new MySqlCommand(
                    "UPDATE IncidentSnapshotFixture SET State='Resolved',Version=9 WHERE TenantId=200", db, transaction);
                Assert.Equal(1, await update.ExecuteNonQueryAsync());
                transaction.Rollback();
            }
            var unchanged = await ReadAsync(isolatedCs.ConnectionString, 200);
            Assert.NotNull(unchanged);
            Assert.Equal("Contained", unchanged.State);
            Assert.Equal(5, unchanged.Version);
        }
        finally
        {
            using var drop = new MySqlCommand($"DROP DATABASE IF EXISTS {database}", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
