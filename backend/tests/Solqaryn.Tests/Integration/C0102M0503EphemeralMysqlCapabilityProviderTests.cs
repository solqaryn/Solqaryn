using MySqlConnector;
using Solqaryn.Infrastructure.AutomationV5;
using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C01-02.M05.03: physical ephemeral MySQL provider boundary, not production
/// capability persistence or a runtime authorization grant.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0102M0503EphemeralMysqlCapabilityProviderTests
{
    private static string Bootstrap()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("MYSQL84_EPHEMERAL_REQUIRED");
        var c = new MySqlConnectionStringBuilder(raw);
        if (c.Server != "127.0.0.1" || c.Port != 3306 ||
            c.UserID != "root" || c.Database != "solqaryn_c1502_m0503" ||
            c.SslMode != MySqlSslMode.Disabled)
            throw new InvalidOperationException("MYSQL84_LOOPBACK_ROOT_FIXTURE_REQUIRED");
        return raw;
    }

    private static async Task RunAsync(string connection, string sql)
    {
        await using var db = new MySqlConnection(connection);
        await db.OpenAsync();
        await using var command = db.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class SqlSource(string connection) : IC0102ReadThroughCapabilityEvidenceSource
    {
        private static readonly IReadOnlySet<string> Allowed =
            new HashSet<string>(StringComparer.Ordinal) { "orders.read" };
        private static readonly IReadOnlySet<string> Denied =
            new HashSet<string>(StringComparer.Ordinal) { "security.override" };

        public async ValueTask<Fence.VerifiedResourcePolicy?> ReadUntrustedAsync(
            int tenantId, int resourceId, CancellationToken ct)
        {
            await using var db = new MySqlConnection(connection);
            await db.OpenAsync(ct);
            await using var query = db.CreateCommand();
            query.CommandText = """
                SELECT TenantId,ResourceId,ActorId,OwnerTenantId,IsActive
                FROM C0102CapabilityProviderFixture
                WHERE TenantId=@tenant AND ResourceId=@resource
                LIMIT 2
                """;
            query.Parameters.AddWithValue("@tenant", tenantId);
            query.Parameters.AddWithValue("@resource", resourceId);
            await using var reader = await query.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;
            var tenant = reader.GetInt32(0);
            var resource = reader.GetInt32(1);
            var actor = reader.GetInt32(2);
            var owner = reader.GetInt32(3);
            var active = Convert.ToBoolean(reader.GetValue(4));
            if (await reader.ReadAsync(ct)) return null;

            var policy = new Rules.VerifiedPolicy(
                owner, tenant, actor, 7, 5, 5, "pro", "retail",
                active, active, active, !active, active, active, true,
                Allowed, Allowed, Allowed, Allowed, Denied);
            // Even a physical SQL row is not proof of real JWT membership,
            // grant or production catalog. The provider must downgrade it.
            return new Fence.VerifiedResourcePolicy(
                resource, owner, actor, true, true, policy);
        }
    }

    [Fact]
    public async Task PhysicalMysql84ReReadCrossTenantRevocationAndCleanupNeverGrantsRuntimeAccess()
    {
        var bootstrap = Bootstrap();
        var schema = "c0102_m0503_" + Guid.NewGuid().ToString("N");
        // Schema is generated, ASCII hex only, and lives on the CI loopback instance.
        await RunAsync(bootstrap, $"CREATE DATABASE `{schema}`");
        try
        {
            var c = new MySqlConnectionStringBuilder(bootstrap) { Database = schema };
            var connection = c.ConnectionString;
            await RunAsync(connection, """
                CREATE TABLE C0102CapabilityProviderFixture (
                  TenantId INT NOT NULL, ResourceId INT NOT NULL,
                  ActorId INT NOT NULL, OwnerTenantId INT NOT NULL,
                  IsActive TINYINT(1) NOT NULL,
                  PRIMARY KEY(TenantId,ResourceId)
                ) ENGINE=InnoDB
                """);
            await RunAsync(connection, """
                INSERT INTO C0102CapabilityProviderFixture
                    (TenantId,ResourceId,ActorId,OwnerTenantId,IsActive)
                VALUES (101,501,301,101,1),(202,502,302,202,1)
                """);

            var provider = new C0102ReadThroughCapabilityProvider(new SqlSource(connection));
            var a = await provider.ReadCurrentAsync(101, 501, default);
            var b = await provider.ReadCurrentAsync(202, 502, default);
            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.Equal(101, a.OwnerTenantId);
            Assert.Equal(202, b.OwnerTenantId);
            Assert.False(a.OwnershipReadFromTrustedPersistence);
            Assert.False(a.CurrentCapabilityCatalogReadFromBackend);
            Assert.False(a.Policy!.ExplicitRelationalRbacGrant);
            Assert.False(a.Policy.PlanCurrent);
            Assert.False(a.Policy.RubroCurrent);
            Assert.False(provider.AuthorizesResourceAccess);
            Assert.False(provider.ReadsLiveMysql); // production DI not connected
            Assert.False(provider.CachesAuthorization);
            Assert.Null(await provider.ReadCurrentAsync(101, 502, default));
            Assert.Null(await provider.ReadCurrentAsync(202, 501, default));
            Assert.Null(await provider.ReadCurrentAsync(101, 999999, default));

            // Revocation is read from REAL SQL each call; never reuse old ALLOW.
            await RunAsync(connection, """
                UPDATE C0102CapabilityProviderFixture
                SET IsActive=0 WHERE TenantId=101 AND ResourceId=501
                """);
            Assert.Null(await provider.ReadCurrentAsync(101, 501, default));
            Assert.NotNull(await provider.ReadCurrentAsync(202, 502, default));
            await RunAsync(connection, """
                UPDATE C0102CapabilityProviderFixture
                SET IsActive=1,OwnerTenantId=202 WHERE TenantId=101 AND ResourceId=501
                """);
            Assert.Null(await provider.ReadCurrentAsync(101, 501, default));
            await RunAsync(connection, """
                UPDATE C0102CapabilityProviderFixture
                SET OwnerTenantId=101 WHERE TenantId=101 AND ResourceId=501
                """);
            var restored = await provider.ReadCurrentAsync(101, 501, default);
            Assert.NotNull(restored);
            Assert.False(restored.OwnershipReadFromTrustedPersistence);
            Assert.False(restored.Policy!.ExplicitRelationalRbacGrant);
            await RunAsync(connection, """
                DELETE FROM C0102CapabilityProviderFixture
                WHERE TenantId=101 AND ResourceId=501
                """);
            Assert.Null(await provider.ReadCurrentAsync(101, 501, default));
        }
        finally
        {
            await RunAsync(bootstrap, $"DROP DATABASE IF EXISTS `{schema}`");
        }
    }
}
