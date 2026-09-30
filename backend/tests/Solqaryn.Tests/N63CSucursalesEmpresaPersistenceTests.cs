using System.Reflection;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N63CSucursalesEmpresaPersistenceTests
{
    [Fact]
    public void Modelo_MantieneLegacyNullable_Y_AplicaCodigoUnicoPorEmpresa()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n63c-{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);
        var sucursal = db.Model.FindEntityType(typeof(Sucursal));
        Assert.NotNull(sucursal);

        var empresaId = sucursal!.FindProperty(nameof(Sucursal.EmpresaId));
        Assert.NotNull(empresaId);
        Assert.True(empresaId!.IsNullable);

        var fk = Assert.Single(sucursal.GetForeignKeys(), candidate =>
            candidate.PrincipalEntityType.ClrType == typeof(Empresa) &&
            candidate.Properties.Count == 1 &&
            candidate.Properties[0].Name == nameof(Sucursal.EmpresaId));
        Assert.False(fk.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);

        var codigoActivo = sucursal.FindProperty("CodigoActivoUnico");
        Assert.NotNull(codigoActivo);
        Assert.Equal(64, codigoActivo!.GetMaxLength());

        var computedSql = codigoActivo.GetComputedColumnSql();
        Assert.NotNull(computedSql);
        Assert.Contains("EmpresaId", computedSql!, StringComparison.Ordinal);
        Assert.Contains("'LEGACY'", computedSql, StringComparison.Ordinal);
        Assert.Contains("UPPER(TRIM(Codigo))", computedSql, StringComparison.Ordinal);

        var uniqueIndex = Assert.Single(sucursal.GetIndexes(), index =>
            index.GetDatabaseName() == "UX_Sucursales_Codigo_Activo");
        Assert.True(uniqueIndex.IsUnique);
        Assert.Single(uniqueIndex.Properties);
        Assert.Equal("CodigoActivoUnico", uniqueIndex.Properties[0].Name);
    }

    [Fact]
    public void Migracion_ValidaClaveObjetivo_AntesDeRetirarIndiceGlobal_SinBackfillArbitrario()
    {
        var migration = new N63CSucursalesEmpresaPersistence();
        var up = typeof(N63CSucursalesEmpresaPersistence)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);

        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        up!.Invoke(migration, new object[] { builder });

        var operations = builder.Operations.OfType<SqlOperation>().ToArray();
        Assert.Equal(2, operations.Length);

        var precheckSql = operations[0].Sql;
        Assert.Contains("ADD COLUMN `CodigoActivoEmpresaN63C`", precheckSql, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX `UX_Sucursales_Empresa_Codigo_Activo_N63C`", precheckSql, StringComparison.Ordinal);
        Assert.Contains("EmpresaId IS NULL", precheckSql, StringComparison.Ordinal);
        Assert.Contains("'LEGACY'", precheckSql, StringComparison.Ordinal);

        var swapSql = operations[1].Sql;
        Assert.Contains("DROP INDEX `UX_Sucursales_Codigo_Activo`", swapSql, StringComparison.Ordinal);
        Assert.Contains("RENAME INDEX `UX_Sucursales_Empresa_Codigo_Activo_N63C`", swapSql, StringComparison.Ordinal);
        Assert.Contains("TO `UX_Sucursales_Codigo_Activo`", swapSql, StringComparison.Ordinal);

        var allSql = string.Join("\n", operations.Select(operation => operation.Sql));
        Assert.DoesNotContain("UPDATE `Sucursales`", allSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MODIFY COLUMN `EmpresaId`", allSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CHANGE COLUMN `EmpresaId`", allSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rollback_FallaCerrado_SiLosDatosYaNoCumplenUnicidadGlobal()
    {
        var migration = new N63CSucursalesEmpresaPersistence();
        var down = typeof(N63CSucursalesEmpresaPersistence)
            .GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(down);

        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        down!.Invoke(migration, new object[] { builder });

        var operations = builder.Operations.OfType<SqlOperation>().ToArray();
        Assert.Equal(2, operations.Length);
        Assert.Contains("ADD COLUMN `CodigoActivoGlobalRollback`", operations[0].Sql, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX `UX_Sucursales_Codigo_Global_Rollback`", operations[0].Sql, StringComparison.Ordinal);
        Assert.Contains("IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)", operations[0].Sql, StringComparison.Ordinal);
        Assert.Contains("RENAME INDEX `UX_Sucursales_Codigo_Global_Rollback`", operations[1].Sql, StringComparison.Ordinal);
    }
}
