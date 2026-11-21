using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0701M0304IsolatedMySqlResourceReaderTests
{
    private const string DbName = "c0701_m0304_bootstrap";
    private static string Connection()
    {
        var raw = Environment.GetEnvironmentVariable("C0701_M0304_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var c = new MySqlConnectionStringBuilder(raw);
        if (c.Server is not ("127.0.0.1" or "localhost") ||
            c.Port != 3306 || c.UserID != "root" || c.Database != DbName)
            throw new InvalidOperationException("ISOLATED_LOOPBACK_ONLY");
        return raw;
    }

    private static async Task ExecuteAsync(MySqlConnection db, string sql,
        int? tenant, int product, int? category = null, int? variant = null,
        long version = 4, int active = 1, int deleted = 0)
    {
        await using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@tenant", (object?)tenant ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@product", product);
        cmd.Parameters.AddWithValue("@category", (object?)category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@variant", (object?)variant ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.Parameters.AddWithValue("@active", active);
        cmd.Parameters.AddWithValue("@deleted", deleted);
        await cmd.ExecuteNonQueryAsync();
    }

    private const string Insert = """
        INSERT INTO V22C0701CatalogSnapshotIsolated
          (TenantId,ProductoId,Version,SucursalId,CategoriaEmpresaId,
           VarianteEmpresaId,Activo,Eliminado)
        VALUES(@tenant,@product,@version,NULL,@category,@variant,@active,@deleted)
        """;

    [Fact]
    public async Task RealMysql84TenantABLegacyNullAmbiguityRevocationAndRollback()
    {
        var raw = Connection();
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
                CREATE TABLE IF NOT EXISTS V22C0701CatalogSnapshotIsolated(
                  TenantId INT NULL, ProductoId INT NOT NULL, Version BIGINT NOT NULL,
                  SucursalId INT NULL, CategoriaEmpresaId INT NULL,
                  VarianteEmpresaId INT NULL, Activo TINYINT(1) NOT NULL,
                  Eliminado TINYINT(1) NOT NULL,
                  INDEX IX_C0701_TenantProduct(TenantId, ProductoId)
                ) ENGINE=InnoDB
                """;
            await create.ExecuteNonQueryAsync();
        }
        var a = RandomNumberGenerator.GetInt32(100000, 150000000);
        var b = a + 1;
        var product = RandomNumberGenerator.GetInt32(160000000, 300000000);
        var reader = new C0701IsolatedMySqlResourceReader(
            () => new MySqlConnection(raw));
        try
        {
            await ExecuteAsync(db, Insert, a, product, a, a);
            await ExecuteAsync(db, Insert, b, product + 1, b, b);
            await ExecuteAsync(db, Insert, null, product + 2);
            var own = await reader.ReadOwnedAsync(a, product, default);
            Assert.NotNull(own);
            Assert.Equal(a, own.Producto.EmpresaId);
            Assert.Equal(4L, own.Producto.Version);
            Assert.True(own.ConsultaFiltradaPorTenant);
            Assert.True(own.PropiedadVerificadaEnServidor);
            Assert.Null(await reader.ReadOwnedAsync(a, product + 1, default));
            Assert.Null(await reader.ReadOwnedAsync(a, product + 2, default));
            Assert.Null(await reader.ReadOwnedAsync(b, product, default));
            Assert.Null(await reader.ReadOwnedAsync(0, product, default));
            Assert.Null(await reader.ReadOwnedAsync(a, 0, default));
            Assert.Equal(b, (await reader.ReadOwnedAsync(
                b, product + 1, default))!.Producto.EmpresaId);

            await ExecuteAsync(db, Insert, a, product, a, a);
            Assert.Null(await reader.ReadOwnedAsync(a, product, default));
            await ExecuteAsync(db,
                "DELETE FROM V22C0701CatalogSnapshotIsolated WHERE TenantId=@tenant AND ProductoId=@product LIMIT 1",
                a, product);
            Assert.NotNull(await reader.ReadOwnedAsync(a, product, default));

            await ExecuteAsync(db,
                "UPDATE V22C0701CatalogSnapshotIsolated SET CategoriaEmpresaId=@category WHERE TenantId=@tenant AND ProductoId=@product",
                a, product, b);
            Assert.Null(await reader.ReadOwnedAsync(a, product, default));
            await ExecuteAsync(db,
                "UPDATE V22C0701CatalogSnapshotIsolated SET CategoriaEmpresaId=@category,VarianteEmpresaId=@variant WHERE TenantId=@tenant AND ProductoId=@product",
                a, product, a, b);
            Assert.Null(await reader.ReadOwnedAsync(a, product, default));
            await ExecuteAsync(db,
                "UPDATE V22C0701CatalogSnapshotIsolated SET CategoriaEmpresaId=@category,VarianteEmpresaId=@variant,Version=0 WHERE TenantId=@tenant AND ProductoId=@product",
                a, product, a, a);
            Assert.Null(await reader.ReadOwnedAsync(a, product, default));
            await ExecuteAsync(db,
                "UPDATE V22C0701CatalogSnapshotIsolated SET Version=4,Activo=0 WHERE TenantId=@tenant AND ProductoId=@product",
                a, product);
            Assert.Null(await reader.ReadOwnedAsync(a, product, default));
            await ExecuteAsync(db,
                "UPDATE V22C0701CatalogSnapshotIsolated SET Activo=1,Eliminado=1 WHERE TenantId=@tenant AND ProductoId=@product",
                a, product);
            Assert.Null(await reader.ReadOwnedAsync(a, product, default));
            await ExecuteAsync(db,
                "UPDATE V22C0701CatalogSnapshotIsolated SET Eliminado=0 WHERE TenantId=@tenant AND ProductoId=@product",
                a, product);
            var fresh = await reader.ReadOwnedAsync(a, product, default);
            Assert.NotNull(fresh);
            var scope = new C0701CatalogDomainRules.VerifiedServerScope(
                a, 10, 20, null, true, true, true, true, false);
            var stale = new C0701CatalogDomainRules.Mutation(
                product, 3, "PRODUCTO", "SKU-TEST", "Catalogo test",
                10m, 4m, 1500, true, true, true, false);
            Assert.Equal("CATALOG_STALE_VERSION",
                C0701CatalogDomainRules.Evaluate(scope, fresh.Producto, stale).Code);
            Assert.False(C0701CatalogDomainRules.Evaluate(
                scope, fresh.Producto, stale).AuthorizesWrite);

            await using (var tx = await db.BeginTransactionAsync())
            {
                await using var cmd = db.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = Insert;
                cmd.Parameters.AddWithValue("@tenant", a);
                cmd.Parameters.AddWithValue("@product", product + 3);
                cmd.Parameters.AddWithValue("@version", 4);
                cmd.Parameters.AddWithValue("@category", a);
                cmd.Parameters.AddWithValue("@variant", a);
                cmd.Parameters.AddWithValue("@active", 1);
                cmd.Parameters.AddWithValue("@deleted", 0);
                await cmd.ExecuteNonQueryAsync();
                await tx.RollbackAsync();
            }
            Assert.Null(await reader.ReadOwnedAsync(a, product + 3, default));
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await reader.ReadOwnedAsync(a, product, cts.Token));
            Assert.Null(await new C0701IsolatedMySqlResourceReader(
                () => throw new InvalidOperationException("offline"))
                .ReadOwnedAsync(a, product, default));
        }
        finally
        {
            await ExecuteAsync(db, """
                DELETE FROM V22C0701CatalogSnapshotIsolated
                 WHERE ProductoId IN (@product,@product + 1,@product + 2,@product + 3)
                """, a, product);
        }
    }
}
