using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N31CotizacionPersistenceModelTests
{
    [Fact]
    public void Cotizacion_mapea_tabla_estado_indices_y_fk_cliente_restrictiva()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new CotizacionConfiguration().Configure(modelBuilder.Entity<Cotizacion>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Cotizacion));
        Assert.NotNull(entity);
        Assert.Equal("Cotizaciones", entity!.GetTableName());

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Cotizacion.ClienteId) }) &&
            index.GetDatabaseName() == "IX_Cotizaciones_ClienteId");

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Cotizacion.Estado) }) &&
            index.GetDatabaseName() == "IX_Cotizaciones_Estado");

        var clienteFk = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Cotizacion.ClienteId));
        Assert.Equal(DeleteBehavior.Restrict, clienteFk.DeleteBehavior);

        var detallesFk = modelBuilder.Model.FindEntityType(typeof(CotizacionDetalle))!
            .GetForeignKeys()
            .Single(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(CotizacionDetalle.CotizacionId));
        Assert.Equal(DeleteBehavior.Cascade, detallesFk.DeleteBehavior);
    }

    [Fact]
    public void Detalle_mapea_precision_checks_indices_y_referencias_restrictivas()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new CotizacionDetalleConfiguration().Configure(modelBuilder.Entity<CotizacionDetalle>());

        var entity = modelBuilder.Model.FindEntityType(typeof(CotizacionDetalle));
        Assert.NotNull(entity);
        Assert.Equal("CotizacionDetalles", entity!.GetTableName());

        AssertPrecision(entity, nameof(CotizacionDetalle.Cantidad));
        AssertPrecision(entity, nameof(CotizacionDetalle.PrecioUnitario));

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[]
                {
                    nameof(CotizacionDetalle.CotizacionId),
                    nameof(CotizacionDetalle.ProductoId),
                    nameof(CotizacionDetalle.ProductoVarianteId)
                }) &&
            index.GetDatabaseName() == "IX_CotizacionDetalles_Cotizacion_Producto_Variante");

        AssertRestrict(entity, nameof(CotizacionDetalle.ProductoId));
        AssertRestrict(entity, nameof(CotizacionDetalle.ProductoVarianteId));
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
