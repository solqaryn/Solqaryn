using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N32PedidoVentaPersistenceModelTests
{
    [Fact]
    public void Pedido_mapea_cardinalidad_idempotencia_y_fks_restrictivas()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new PedidoVentaConfiguration().Configure(modelBuilder.Entity<PedidoVenta>());

        var entity = modelBuilder.Model.FindEntityType(typeof(PedidoVenta));
        Assert.NotNull(entity);
        Assert.Equal("PedidosVenta", entity!.GetTableName());

        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(PedidoVenta.CotizacionId) }) &&
            index.GetDatabaseName() == "UX_PedidosVenta_CotizacionId");

        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(PedidoVenta.IdempotencyKey) }) &&
            index.GetDatabaseName() == "UX_PedidosVenta_IdempotencyKey");

        Assert.Equal(128, entity.FindProperty(nameof(PedidoVenta.IdempotencyKey))!.GetMaxLength());
        Assert.Equal(64, entity.FindProperty(nameof(PedidoVenta.IdempotencyFingerprint))!.GetMaxLength());
        Assert.Contains(entity.GetCheckConstraints(), check => check.Name == "CK_PedidosVenta_Idempotencia_Atomica");
        Assert.Contains(entity.GetCheckConstraints(), check => check.Name == "CK_PedidosVenta_IdempotencyFingerprint_Sha256");

        AssertRestrict(entity, nameof(PedidoVenta.CotizacionId));
        AssertRestrict(entity, nameof(PedidoVenta.ClienteId));

        var detallesFk = modelBuilder.Model.FindEntityType(typeof(PedidoVentaDetalle))!
            .GetForeignKeys()
            .Single(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(PedidoVentaDetalle.PedidoVentaId));
        Assert.Equal(DeleteBehavior.Cascade, detallesFk.DeleteBehavior);
    }

    [Fact]
    public void Detalle_mapea_precision_snapshots_y_referencias_restrictivas()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new PedidoVentaDetalleConfiguration().Configure(modelBuilder.Entity<PedidoVentaDetalle>());

        var entity = modelBuilder.Model.FindEntityType(typeof(PedidoVentaDetalle));
        Assert.NotNull(entity);
        Assert.Equal("PedidoVentaDetalles", entity!.GetTableName());

        AssertPrecision(entity, nameof(PedidoVentaDetalle.Cantidad));
        AssertPrecision(entity, nameof(PedidoVentaDetalle.PrecioUnitario));
        Assert.Equal(80, entity.FindProperty(nameof(PedidoVentaDetalle.ProductoSkuSnapshot))!.GetMaxLength());
        Assert.Equal(150, entity.FindProperty(nameof(PedidoVentaDetalle.ProductoNombreSnapshot))!.GetMaxLength());

        AssertRestrict(entity, nameof(PedidoVentaDetalle.ProductoId));
        AssertRestrict(entity, nameof(PedidoVentaDetalle.ProductoVarianteId));
    }

    private static void AssertPrecision(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entity, string propertyName)
    {
        var property = entity.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(18, property!.GetPrecision());
        Assert.Equal(4, property.GetScale());
    }

    private static void AssertRestrict(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entity, string foreignKeyProperty)
    {
        var foreignKey = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Count == 1 && fk.Properties[0].Name == foreignKeyProperty);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}
