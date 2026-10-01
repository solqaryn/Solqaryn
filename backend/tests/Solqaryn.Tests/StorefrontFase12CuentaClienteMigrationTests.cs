using Solqaryn.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests;

public sealed class StorefrontFase12CuentaClienteMigrationTests
{
    [Fact]
    public void Up_EsIdempotenteYValidaEsquemaAdoptado()
    {
        var migration = new StorefrontFase12CuentaCliente();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");

        typeof(StorefrontFase12CuentaCliente)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, new object[] { builder });

        Assert.Empty(builder.Operations.OfType<CreateTableOperation>());
        Assert.Empty(builder.Operations.OfType<CreateIndexOperation>());

        var sql = builder.Operations.OfType<SqlOperation>().Select(x => x.Sql).ToArray();
        Assert.Contains(sql, x => x.Contains("CREATE TABLE IF NOT EXISTS TiendaCuentasCliente", StringComparison.Ordinal));
        Assert.Contains(sql, x => x.Contains("CREATE TABLE IF NOT EXISTS TiendaDireccionesCliente", StringComparison.Ordinal));
        Assert.Contains(sql, x => x.Contains("CREATE TABLE IF NOT EXISTS TiendaFavoritosCliente", StringComparison.Ordinal));
        Assert.Contains(sql, x => x.Contains("CREATE TABLE IF NOT EXISTS TiendaSesionesCliente", StringComparison.Ordinal));
        Assert.Contains(sql, x => x.Contains("INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS", StringComparison.Ordinal));
        Assert.Contains(sql, x => x.Contains("__SOLQARYN_SCHEMA_MISMATCH_StorefrontFase12CuentaCliente__", StringComparison.Ordinal));
    }
}
