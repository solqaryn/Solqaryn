using System.Reflection;
using Solqaryn.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public class N42CuentaBancariaMigrationTests
{
    [Fact]
    public void Up_CreatesCuentaBancariaWithExpectedConstraintsAndIndexes()
    {
        var builder = ExecuteMigrationMethod("Up");

        var table = Assert.Single(builder.Operations.OfType<CreateTableOperation>(),
            operation => operation.Name == "CuentasBancarias");

        Assert.Contains(table.Columns, column => column.Name == "BancoId" && !column.IsNullable);
        Assert.Contains(table.Columns, column => column.Name == "NumeroCuenta" && !column.IsNullable && column.MaxLength == 50);
        Assert.Contains(table.Columns, column => column.Name == "Moneda" && !column.IsNullable && column.MaxLength == 3);
        Assert.Contains(table.Columns, column => column.Name == "SaldoInicial" && !column.IsNullable && column.ColumnType == "decimal(18,2)");
        Assert.Contains(table.Columns, column => column.Name == "Estado" && !column.IsNullable);

        var foreignKey = Assert.Single(table.ForeignKeys,
            fk => fk.Name == "FK_CuentasBancarias_Bancos_BancoId");
        Assert.Equal("Bancos", foreignKey.PrincipalTable);
        Assert.NotNull(foreignKey.PrincipalColumns);
        Assert.Equal("Id", Assert.Single(foreignKey.PrincipalColumns!));
        Assert.Equal(ReferentialAction.Restrict, foreignKey.OnDelete);

        Assert.Contains(table.CheckConstraints,
            check => check.Name == "CK_CuentasBancarias_BancoId" && check.Sql.Contains("BancoId"));
        Assert.Contains(table.CheckConstraints,
            check => check.Name == "CK_CuentasBancarias_Estado" && check.Sql.Contains("IN (1, 2)"));
        Assert.Contains(table.CheckConstraints,
            check => check.Name == "CK_CuentasBancarias_SaldoInicial" && check.Sql.Contains(">= 0"));

        var indexes = builder.Operations.OfType<CreateIndexOperation>()
            .Where(index => index.Table == "CuentasBancarias")
            .ToList();

        Assert.Contains(indexes, index => index.Name == "IX_CuentasBancarias_BancoId" && index.Columns.SequenceEqual(new[] { "BancoId" }));
        Assert.Contains(indexes, index => index.Name == "IX_CuentasBancarias_Estado" && index.Columns.SequenceEqual(new[] { "Estado" }));
        Assert.Contains(indexes, index => index.Name == "UX_CuentasBancarias_BancoId_NumeroCuenta"
            && index.IsUnique
            && index.Columns.SequenceEqual(new[] { "BancoId", "NumeroCuenta" }));
    }

    [Fact]
    public void Down_DropsOnlyCuentaBancariaTable()
    {
        var builder = ExecuteMigrationMethod("Down");
        var drop = Assert.Single(builder.Operations.OfType<DropTableOperation>());
        Assert.Equal("CuentasBancarias", drop.Name);
    }

    private static MigrationBuilder ExecuteMigrationMethod(string methodName)
    {
        var migration = new N4_2_C_CuentaBancaria();
        var method = typeof(N4_2_C_CuentaBancaria).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        method!.Invoke(migration, new object[] { builder });
        return builder;
    }
}
