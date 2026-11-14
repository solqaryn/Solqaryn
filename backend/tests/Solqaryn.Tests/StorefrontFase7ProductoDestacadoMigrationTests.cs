using Solqaryn.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests;

public sealed class StorefrontFase7ProductoDestacadoMigrationTests
{
    [Fact]
    public void Up_EsIdempotenteAnteDdlParcial()
    {
        var migration = new StorefrontFase7ProductoDestacado();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");

        InvokeProtected(migration, "Up", builder);

        Assert.Empty(builder.Operations.OfType<AddColumnOperation>());
        Assert.Empty(builder.Operations.OfType<CreateIndexOperation>());

        var sql = builder.Operations.OfType<SqlOperation>().Select(x => x.Sql).ToArray();
        Assert.Equal(2, sql.Length);
        Assert.Contains("INFORMATION_SCHEMA.COLUMNS", sql[0], StringComparison.Ordinal);
        Assert.Contains("COLUMN_NAME = 'EsDestacado'", sql[0], StringComparison.Ordinal);
        Assert.Contains("INFORMATION_SCHEMA.STATISTICS", sql[1], StringComparison.Ordinal);
        Assert.Contains("IX_Productos_EsDestacado_Activo", sql[1], StringComparison.Ordinal);
    }

    private static void InvokeProtected(Migration migration, string method, MigrationBuilder builder)
    {
        typeof(StorefrontFase7ProductoDestacado)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, new object[] { builder });
    }
}
