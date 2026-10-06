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
public sealed class N08PersistenciaLimpiezaIntegrationTests
{
    [Fact]
    public async Task Historia_N08C_ConservaBackfill_Y_ModeloOracleConservaOrigenesTipados()
    {
        var migration = new N0_8_PersistenciaLimpiezaTransicional();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        typeof(N0_8_PersistenciaLimpiezaTransicional)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, new object[] { builder });

        var sql = string.Join(
            "\n",
            builder.Operations.OfType<SqlOperation>().Select(x => x.Sql));

        Assert.Contains("CK_N08C_Guard_Cero", sql, StringComparison.Ordinal);
        Assert.Contains("UPDATE Compras c", sql, StringComparison.Ordinal);
        Assert.Contains("MetodoPagoId", sql, StringComparison.Ordinal);
        Assert.Contains("CK_N08C_PostGuard_Cero", sql, StringComparison.Ordinal);

        var addColumn = Assert.Single(
            builder.Operations.OfType<AddColumnOperation>(),
            x => x.Table == "Compras" && x.Name == "MetodoPagoId");
        Assert.True(addColumn.IsNullable);

        Assert.Contains(
            builder.Operations.OfType<CreateIndexOperation>(),
            x => x.Table == "Compras" && x.Name == "IX_Compras_MetodoPagoId");
        Assert.Contains(
            builder.Operations.OfType<AddForeignKeyOperation>(),
            x => x.Table == "Compras" &&
                 x.Name == "FK_Compras_MetodosPago_MetodoPagoId" &&
                 x.PrincipalTable == "MetodosPago");

        var dbName = $"test_n08_phase7_model_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(
                $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        await using var context = new AppDbContext(options);
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(context);

            var compraType = context.Model.FindEntityType(typeof(Compra));
            Assert.NotNull(compraType?.FindProperty(nameof(Compra.MetodoPagoId)));

            var movimientoType = context.Model.FindEntityType(typeof(MovimientoInventario));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.CompraId)));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.VentaId)));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.ConsumoInsumoId)));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.AjusteInventarioId)));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
