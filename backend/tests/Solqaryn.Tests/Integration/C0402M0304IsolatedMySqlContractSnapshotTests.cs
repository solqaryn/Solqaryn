using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0402M0304IsolatedMySqlContractSnapshotTests
{
    [Fact]
    public async Task RealMysql84TenantVersionStateAndRollbackFailClosed()
    {
        var raw = Environment.GetEnvironmentVariable("C0402_M0304_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var root = new MySqlConnectionStringBuilder(raw);
        if (root.Server is not ("127.0.0.1" or "localhost") ||
            root.Port != 3306 || root.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var schema = "c0402_m0304_" + Guid.NewGuid().ToString("N");
        await using var admin = new MySqlConnection(raw);
        await admin.OpenAsync();
        await using (var version = admin.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync())!);
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
                CREATE TABLE V22C0402ContractSnapshotIsolated (
                    TenantId INT NOT NULL, ContractId BIGINT NOT NULL,
                    StateId INT NOT NULL, Version BIGINT NOT NULL,
                    StartUtc DATETIME(6) NOT NULL, EndUtc DATETIME(6) NULL,
                    PRIMARY KEY (TenantId, ContractId))
                """);
            await ExecuteAsync(isolated, """
                INSERT INTO V22C0402ContractSnapshotIsolated
                    (TenantId, ContractId, StateId, Version, StartUtc, EndUtc)
                VALUES
                    (101, 7001, 3, 2, '2026-01-01 00:00:00', NULL),
                    (202, 7001, 4, 9, '2026-01-01 00:00:00', NULL)
                """);

            var source = new C0402IsolatedMySqlContractSnapshotAdapter(
                () => new MySqlConnection(isolated));
            var tenantA = await source.ReadOwnedAsync(101, 7001, CancellationToken.None);
            Assert.NotNull(tenantA);
            Assert.Equal(101, tenantA.Contract.EmpresaId);
            Assert.Equal(7001, tenantA.Contract.ContractId);
            Assert.Equal(2, tenantA.Contract.Version);
            Assert.Equal(Rules.ContractState.Active, tenantA.Contract.State);
            Assert.Equal(DateTimeKind.Utc, tenantA.Contract.StartUtc.Kind);
            Assert.True(tenantA.ServerTenantFiltered);
            Assert.True(tenantA.ResourceOwnedByActorTenant);
            Assert.False(tenantA.PolicyProvenanceVerified);
            Assert.False(tenantA.CommercialPolicy.D01RepresentationApproved);
            Assert.False(tenantA.CommercialPolicy.D02TermsApproved);
            Assert.False(tenantA.CommercialPolicy.D08RetentionPolicyApproved);

            var tenantB = await source.ReadOwnedAsync(202, 7001, CancellationToken.None);
            Assert.NotNull(tenantB);
            Assert.Equal(202, tenantB.Contract.EmpresaId);
            Assert.Equal(9, tenantB.Contract.Version);
            Assert.Equal(Rules.ContractState.Suspended, tenantB.Contract.State);
            Assert.Null(await source.ReadOwnedAsync(303, 7001, CancellationToken.None));
            Assert.Null(await source.ReadOwnedAsync(101, 7002, CancellationToken.None));
            Assert.Null(await source.ReadOwnedAsync(0, 7001, CancellationToken.None));
            Assert.Null(await source.ReadOwnedAsync(101, 0, CancellationToken.None));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await source.ReadOwnedAsync(101, 7001, new CancellationToken(true)));

            await ExecuteAsync(isolated, """
                UPDATE V22C0402ContractSnapshotIsolated SET StateId = 99
                WHERE TenantId = 101 AND ContractId = 7001
                """);
            Assert.Null(await source.ReadOwnedAsync(101, 7001, CancellationToken.None));
            await ExecuteAsync(isolated, """
                UPDATE V22C0402ContractSnapshotIsolated SET StateId = 3, Version = 0
                WHERE TenantId = 101 AND ContractId = 7001
                """);
            Assert.Null(await source.ReadOwnedAsync(101, 7001, CancellationToken.None));
            await ExecuteAsync(isolated, "DROP TABLE V22C0402ContractSnapshotIsolated");
            Assert.Null(await source.ReadOwnedAsync(101, 7001, CancellationToken.None));
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
}
