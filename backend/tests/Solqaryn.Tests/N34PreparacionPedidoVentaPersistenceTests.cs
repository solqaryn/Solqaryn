using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N34PreparacionPedidoVentaPersistenceTests
{
    [Fact]
    public void Preparacion_MapeaUnoAUnoYClaveFisicaUnica()
    {
        using var context = CreateContext();
        var cabecera = context.Model.FindEntityType(typeof(PreparacionPedidoVenta));
        var detalle = context.Model.FindEntityType(typeof(PreparacionPedidoVentaDetalle));
        Assert.NotNull(cabecera);
        Assert.NotNull(detalle);
        Assert.Contains(cabecera!.GetIndexes(), x => x.IsUnique && x.GetDatabaseName() == "UX_PreparacionesPedidoVenta_PedidoVentaId");
        Assert.Contains(cabecera.GetIndexes(), x => x.IsUnique && x.GetDatabaseName() == "UX_PreparacionesPedidoVenta_ReservaInventarioId");
        Assert.Contains(cabecera.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(PedidoVenta) && x.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(cabecera.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(ReservaInventario) && x.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(detalle!.GetIndexes(), x => x.IsUnique && x.GetDatabaseName() == "UX_PreparacionPedidoVentaDetalles_ClaveFisica");
        Assert.Contains(detalle.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(PreparacionPedidoVenta) && x.DeleteBehavior == DeleteBehavior.Cascade);
    }

    [Fact]
    public void MigracionN34C_CreaTablasYRollbackFailClosed()
    {
        var migration = new InspectableN34Migration();
        var up = migration.BuildUp();
        Assert.Contains(up.OfType<CreateTableOperation>(), x => x.Name == "PreparacionesPedidoVenta");
        Assert.Contains(up.OfType<CreateTableOperation>(), x => x.Name == "PreparacionPedidoVentaDetalles");
        Assert.Contains(up.OfType<CreateIndexOperation>(), x => x.Name == "UX_PreparacionesPedidoVenta_PedidoVentaId" && x.IsUnique);
        Assert.Contains(up.OfType<CreateIndexOperation>(), x => x.Name == "UX_PreparacionPedidoVentaDetalles_ClaveFisica" && x.IsUnique);
        Assert.Contains(up.OfType<SqlOperation>(), x => x.Sql.Contains("__N34CPostGuard", StringComparison.Ordinal));

        var down = migration.BuildDown();
        var guard = down.FindIndex(x => x is SqlOperation sql && sql.Sql.Contains("__N34CDownGuard", StringComparison.Ordinal));
        var firstDrop = down.FindIndex(x => x is DropTableOperation);
        Assert.True(guard >= 0 && firstDrop > guard);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=solqaryn_n34_model;User=root;Password=test;")
            .Options;
        return new AppDbContext(options);
    }

    private sealed class InspectableN34Migration : N3_4_C_PreparacionPedidoVenta
    {
        public List<MigrationOperation> BuildUp() { var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql"); Up(builder); return builder.Operations.ToList(); }
        public List<MigrationOperation> BuildDown() { var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql"); Down(builder); return builder.Operations.ToList(); }
    }
}
