using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N37NotaCreditoClientePersistenceTests
{
    [Fact]
    public void NotaCreditoCliente_MapeaFacturaVentaMontoEIndicesSinInventarCardinalidad()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(NotaCreditoCliente));

        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), x => !x.IsUnique && x.GetDatabaseName() == "IX_NotasCreditoCliente_FacturaId");
        Assert.Contains(entity.GetIndexes(), x => !x.IsUnique && x.GetDatabaseName() == "IX_NotasCreditoCliente_VentaId");
        Assert.Contains(entity.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Factura) && x.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.DoesNotContain(entity.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Venta));
        Assert.Equal("decimal(18,4)", entity.FindProperty(nameof(NotaCreditoCliente.MontoCredito))!.GetColumnType());
        Assert.Equal(3, entity.FindProperty(nameof(NotaCreditoCliente.Moneda))!.GetMaxLength());
    }

    [Fact]
    public void MigracionN37C_CreaTablaIndicesYRollbackFailClosed()
    {
        var migration = new InspectableN37Migration();
        var up = migration.BuildUp();

        Assert.Contains(up.OfType<CreateTableOperation>(), x => x.Name == "NotasCreditoCliente");
        Assert.Contains(up.OfType<CreateIndexOperation>(), x => x.Name == "IX_NotasCreditoCliente_FacturaId" && !x.IsUnique);
        Assert.Contains(up.OfType<CreateIndexOperation>(), x => x.Name == "IX_NotasCreditoCliente_VentaId" && !x.IsUnique);
        Assert.DoesNotContain(up.OfType<CreateIndexOperation>(), x => x.IsUnique);
        Assert.Contains(up.OfType<SqlOperation>(), x => x.Sql.Contains("__N37CPostGuard", StringComparison.Ordinal));

        var down = migration.BuildDown();
        var guard = down.FindIndex(x => x is SqlOperation sql && sql.Sql.Contains("__N37CDownGuard", StringComparison.Ordinal));
        var firstDrop = down.FindIndex(x => x is DropTableOperation);
        Assert.True(guard >= 0 && firstDrop > guard);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=solqaryn_n37_model;User=root;Password=test;", ServerVersion.Parse("8.0.36-mysql"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class InspectableN37Migration : N3_7_C_NotaCreditoCliente
    {
        public List<MigrationOperation> BuildUp() { var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql"); Up(builder); return builder.Operations.ToList(); }
        public List<MigrationOperation> BuildDown() { var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql"); Down(builder); return builder.Operations.ToList(); }
    }
}
