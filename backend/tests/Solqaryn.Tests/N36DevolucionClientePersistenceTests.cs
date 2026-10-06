using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N36DevolucionClientePersistenceTests
{
    [Fact]
    public void Devolucion_MapeaIdempotenciaRelacionVentaFacturaYLineaUnica()
    {
        using var context = CreateContext();
        var cabecera = context.Model.FindEntityType(typeof(DevolucionCliente));
        var detalle = context.Model.FindEntityType(typeof(DevolucionClienteDetalle));

        Assert.NotNull(cabecera);
        Assert.NotNull(detalle);
        Assert.Contains(cabecera!.GetIndexes(), x => x.IsUnique && x.GetDatabaseName() == "UX_DevolucionesCliente_IdempotencyKey");
        Assert.Contains(cabecera.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Venta) && x.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(cabecera.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Factura) && x.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(detalle!.GetIndexes(), x => x.IsUnique && x.GetDatabaseName() == "UX_DevolucionClienteDetalles_LineaVenta");
        Assert.Contains(detalle.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(DevolucionCliente) && x.DeleteBehavior == DeleteBehavior.Cascade);
        Assert.Contains(detalle.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(VentaDetalle) && x.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal("decimal(18,2)", detalle.FindProperty(nameof(DevolucionClienteDetalle.PrecioUnitarioSnapshot))!.GetColumnType());
    }

    [Fact]
    public void MigracionN36C_CreaTablasIdempotenciaYRollbackFailClosed()
    {
        var migration = new InspectableN36Migration();
        var up = migration.BuildUp();
        Assert.Contains(up.OfType<CreateTableOperation>(), x => x.Name == "DevolucionesCliente");
        Assert.Contains(up.OfType<CreateTableOperation>(), x => x.Name == "DevolucionClienteDetalles");
        Assert.Contains(up.OfType<CreateIndexOperation>(), x => x.Name == "UX_DevolucionesCliente_IdempotencyKey" && x.IsUnique);
        Assert.Contains(up.OfType<CreateIndexOperation>(), x => x.Name == "UX_DevolucionClienteDetalles_LineaVenta" && x.IsUnique);
        Assert.Contains(up.OfType<SqlOperation>(), x => x.Sql.Contains("__N36CPostGuard", StringComparison.Ordinal));

        var down = migration.BuildDown();
        var guard = down.FindIndex(x => x is SqlOperation sql && sql.Sql.Contains("__N36CDownGuard", StringComparison.Ordinal));
        var firstDrop = down.FindIndex(x => x is DropTableOperation);
        Assert.True(guard >= 0 && firstDrop > guard);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=solqaryn_n36_model;User=root;Password=test;", mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class InspectableN36Migration : N3_6_C_DevolucionCliente
    {
        public List<MigrationOperation> BuildUp() { var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql"); Up(builder); return builder.Operations.ToList(); }
        public List<MigrationOperation> BuildDown() { var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql"); Down(builder); return builder.Operations.ToList(); }
    }
}
