using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N33ReservaAutomaticaPersistenceTests
{
    [Fact]
    public void ReservaInventario_PedidoVenta_TieneRelacionUnoAUnoOpcionalRestrict()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ReservaInventario));
        Assert.NotNull(entity);

        var property = entity!.FindProperty(nameof(ReservaInventario.PedidoVentaId));
        Assert.NotNull(property);
        Assert.True(property!.IsNullable);

        var index = entity.GetIndexes().Single(x => x.Properties.Count == 1 && x.Properties[0].Name == nameof(ReservaInventario.PedidoVentaId));
        Assert.True(index.IsUnique);
        Assert.Equal("UX_ReservasInventario_PedidoVentaId", index.GetDatabaseName());

        var foreignKey = entity.GetForeignKeys().Single(x => x.Properties.Count == 1 && x.Properties[0].Name == nameof(ReservaInventario.PedidoVentaId));
        Assert.Equal(typeof(PedidoVenta), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal("FK_ReservasInventario_PedidosVenta_PedidoVentaId", foreignKey.GetConstraintName());
    }

    [Fact]
    public void ReservaInventario_ConservaRelacionVentaExistente()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ReservaInventario));
        Assert.NotNull(entity);

        Assert.NotNull(entity!.FindProperty(nameof(ReservaInventario.VentaId)));
        Assert.Contains(entity.GetForeignKeys(), x => x.Properties.Any(p => p.Name == nameof(ReservaInventario.VentaId)) && x.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void MigracionN33C_MaterializaRelacionYRollbackFailClosed()
    {
        var migration = new InspectableN33Migration();
        var up = migration.BuildUp();

        var addColumn = up.OfType<AddColumnOperation>()
            .Single(x => x.Table == "ReservasInventario" && x.Name == "PedidoVentaId");
        Assert.True(addColumn.IsNullable);

        var createIndex = up.OfType<CreateIndexOperation>()
            .Single(x => x.Table == "ReservasInventario" && x.Name == "UX_ReservasInventario_PedidoVentaId");
        Assert.True(createIndex.IsUnique);
        Assert.Equal(new[] { "PedidoVentaId" }, createIndex.Columns);

        var addForeignKey = up.OfType<AddForeignKeyOperation>()
            .Single(x => x.Table == "ReservasInventario" && x.Name == "FK_ReservasInventario_PedidosVenta_PedidoVentaId");
        Assert.Equal("PedidosVenta", addForeignKey.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, addForeignKey.OnDelete);

        var postGuard = Assert.Single(up.OfType<SqlOperation>());
        Assert.Contains("__N33CPostGuard", postGuard.Sql, StringComparison.Ordinal);
        Assert.Contains("UX_ReservasInventario_PedidoVentaId", postGuard.Sql, StringComparison.Ordinal);
        Assert.Contains("FK_ReservasInventario_PedidosVenta_PedidoVentaId", postGuard.Sql, StringComparison.Ordinal);

        var down = migration.BuildDown();
        var downGuardIndex = down.FindIndex(x => x is SqlOperation sql && sql.Sql.Contains("__N33CDownGuard", StringComparison.Ordinal));
        var dropColumnIndex = down.FindIndex(x => x is DropColumnOperation drop && drop.Table == "ReservasInventario" && drop.Name == "PedidoVentaId");

        Assert.True(downGuardIndex >= 0);
        Assert.True(dropColumnIndex > downGuardIndex, "El DownGuard debe ejecutarse antes de eliminar PedidoVentaId.");
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=solqaryn_n33_model;User=root;Password=test;", ServerVersion.Parse("8.0.36-mysql"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class InspectableN33Migration : N3_3_C_PedidoVentaReservaInventario
    {
        public List<MigrationOperation> BuildUp()
        {
            var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
            Up(builder);
            return builder.Operations.ToList();
        }

        public List<MigrationOperation> BuildDown()
        {
            var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
            Down(builder);
            return builder.Operations.ToList();
        }
    }
}
