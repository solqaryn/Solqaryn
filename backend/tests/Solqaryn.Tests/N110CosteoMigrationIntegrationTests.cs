using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class N110CosteoMigrationIntegrationTests
{
    [Fact]
    public async Task Historia_N110_ConservaGuardsSeedCosteo_Y_ModeloOracleActual()
    {
        var migration = new N1_10_CosteoPersistencia();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        typeof(N1_10_CosteoPersistencia)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, new object[] { builder });

        var sql = string.Join(
            "\n",
            builder.Operations.OfType<SqlOperation>().Select(x => x.Sql));

        Assert.Contains("CK_N110C_Guard_Cero", sql, StringComparison.Ordinal);
        Assert.Contains("'Storefront'", sql, StringComparison.Ordinal);
        Assert.Contains("Promedio Ponderado compatible", sql, StringComparison.Ordinal);
        Assert.Contains("CK_N110C_PostGuard_Cero", sql, StringComparison.Ordinal);

        var tables = builder.Operations
            .OfType<CreateTableOperation>()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("PoliticasCosteoInventario", tables);
        Assert.Contains("CostosEstandarInventario", tables);
        Assert.Contains("CapasCostoInventario", tables);
        Assert.Contains("AsignacionesCostoMovimientoInventario", tables);
        Assert.Contains("VariacionesCostoEstandarInventario", tables);

        var dbName = $"test_n110_phase7_model_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(
                $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        await using var context = new AppDbContext(options);
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(context);

            Assert.NotNull(context.Model.FindEntityType(typeof(PoliticaCosteoInventario)));
            Assert.NotNull(context.Model.FindEntityType(typeof(CostoEstandarInventario)));
            Assert.NotNull(context.Model.FindEntityType(typeof(CapaCostoInventario)));
            Assert.NotNull(context.Model.FindEntityType(typeof(AsignacionCostoMovimientoInventario)));
            Assert.NotNull(context.Model.FindEntityType(typeof(VariacionCostoEstandarInventario)));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
