using MySql.Data.MySqlClient;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C07-01.M05.03: real MySQL 8.4 ephemeral integration of isolated catalog reader.
/// Never connects to Aiven/PROD or authorizes signed JWT, API or writes.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0701M0503CatalogPersistenceAcceptanceTests
{
    private const string BootstrapDb = "c0701_m0503_bootstrap";

    private static string LocalOnly()
    {
        var raw = Environment.GetEnvironmentVariable("C0701_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("C0701_M0503_EPHEMERAL_MYSQL84_REQUIRED");
        var value = new MySqlConnectionStringBuilder(raw);
        if (value.Server is not ("127.0.0.1" or "localhost") ||
            value.Port != 3306 || value.UserID != "root" || value.Database != BootstrapDb)
            throw new InvalidOperationException("C0701_M0503_LOCAL_MYSQL84_ONLY");
        return raw;
    }

    private static async Task ExecuteAsync(MySqlConnection db, string sql)
    {
        await using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private const string Create = """
        CREATE TABLE V22C0701CatalogSnapshotIsolated(
            TenantId INT NULL, ProductoId INT NOT NULL, Version BIGINT NOT NULL,
            SucursalId INT NULL, CategoriaEmpresaId INT NULL,
            VarianteEmpresaId INT NULL, Activo TINYINT(1) NOT NULL,
            Eliminado TINYINT(1) NOT NULL,
            INDEX IX_C0701_TenantProduct(TenantId,ProductoId)
        ) ENGINE=InnoDB
        """;

    [Fact]
    public async Task RealMysql84TenantABVersionRollbackAndAmbiguityFailClosed()
    {
        var cs = LocalOnly();
        await using var db = new MySqlConnection(cs);
        await db.OpenAsync();
        await using (var version = db.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
        }
        await ExecuteAsync(db, Create);
        var reader = new C0701IsolatedMySqlResourceReader(() => new MySqlConnection(cs));

        try
        {
            await ExecuteAsync(db, """
                INSERT INTO V22C0701CatalogSnapshotIsolated
                  (TenantId,ProductoId,Version,SucursalId,CategoriaEmpresaId,VarianteEmpresaId,Activo,Eliminado)
                VALUES
                  (71,9101,3,7,71,71,1,0),
                  (72,9102,8,8,72,72,1,0),
                  (NULL,9103,9,7,NULL,NULL,1,0)
                """);
            var ownA = await reader.ReadOwnedAsync(71, 9101, default);
            Assert.NotNull(ownA);
            Assert.True(ownA.ConsultaFiltradaPorTenant);
            Assert.True(ownA.PropiedadVerificadaEnServidor);
            Assert.Equal(71, ownA.Producto.EmpresaId);
            Assert.Equal(3L, ownA.Producto.Version);
            Assert.Equal(72, (await reader.ReadOwnedAsync(72, 9102, default))!.Producto.EmpresaId);
            Assert.Null(await reader.ReadOwnedAsync(71, 9102, default));
            Assert.Null(await reader.ReadOwnedAsync(72, 9101, default));
            Assert.Null(await reader.ReadOwnedAsync(71, 9103, default));
            Assert.Null(await reader.ReadOwnedAsync(71, 0, default));
            Assert.Null(await reader.ReadOwnedAsync(0, 9101, default));

            // Uncommitted change is not visible to a separate tenant-filtered reader.
            await using (var tx = await db.BeginTransactionAsync())
            {
                await using var cmd = db.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    UPDATE V22C0701CatalogSnapshotIsolated SET Version=99
                    WHERE TenantId=71 AND ProductoId=9101
                    """;
                Assert.Equal(1, await cmd.ExecuteNonQueryAsync());
                Assert.Equal(3L, (await reader.ReadOwnedAsync(71, 9101, default))!.Producto.Version);
                await tx.RollbackAsync();
            }
            Assert.Equal(3L, (await reader.ReadOwnedAsync(71, 9101, default))!.Producto.Version);

            var scope = new Rules.VerifiedServerScope(71, 9, 3, 7, true, true, true, true, false);
            var mutation = new Rules.Mutation(9101, 2, "PRODUCTO", "SKU-91",
                "Taladro A", 20m, 10m, 1500, true, true, true, false);
            var stale = Rules.Evaluate(scope, ownA.Producto, mutation);
            Assert.Equal("CATALOG_STALE_VERSION", stale.Code);
            Assert.False(stale.AuthorizesWrite);
            Assert.False(stale.CertifiesJwtOrDatabase);

            var revoked = Rules.Evaluate(scope with { PermissionRevoked = true },
                ownA.Producto, mutation with { ExpectedVersion = 3 });
            Assert.Equal("TENANT_MEMBERSHIP_OR_RBAC_DENIED", revoked.Code);
            Assert.False(revoked.AuthorizesApiAccess);

            // A duplicate tenant/product snapshot must be rejected as ambiguous.
            await ExecuteAsync(db, """
                INSERT INTO V22C0701CatalogSnapshotIsolated
                  (TenantId,ProductoId,Version,SucursalId,CategoriaEmpresaId,VarianteEmpresaId,Activo,Eliminado)
                VALUES (71,9101,4,7,71,71,1,0)
                """);
            Assert.Null(await reader.ReadOwnedAsync(71, 9101, default));
            await ExecuteAsync(db, """
                DELETE FROM V22C0701CatalogSnapshotIsolated
                WHERE TenantId=71 AND ProductoId=9101 AND Version=4
                """);
            Assert.NotNull(await reader.ReadOwnedAsync(71, 9101, default));

            await ExecuteAsync(db, """
                UPDATE V22C0701CatalogSnapshotIsolated SET CategoriaEmpresaId=72
                WHERE TenantId=71 AND ProductoId=9101
                """);
            Assert.Null(await reader.ReadOwnedAsync(71, 9101, default));
            await ExecuteAsync(db, """
                UPDATE V22C0701CatalogSnapshotIsolated SET CategoriaEmpresaId=71,Eliminado=1
                WHERE TenantId=71 AND ProductoId=9101
                """);
            Assert.Null(await reader.ReadOwnedAsync(71, 9101, default));

            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await reader.ReadOwnedAsync(71, 9101, canceled.Token));
        }
        finally
        {
            await ExecuteAsync(db, "DROP TABLE IF EXISTS V22C0701CatalogSnapshotIsolated");
        }
    }
}
