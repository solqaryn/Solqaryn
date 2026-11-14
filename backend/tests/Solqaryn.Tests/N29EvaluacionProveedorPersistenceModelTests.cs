using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N29EvaluacionProveedorPersistenceModelTests
{
    [Fact]
    public void Configuracion_mapea_tabla_precision_indices_y_FKs_restrictivas()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new EvaluacionProveedorConfiguration().Configure(modelBuilder.Entity<EvaluacionProveedor>());

        var entity = modelBuilder.Model.FindEntityType(typeof(EvaluacionProveedor));
        Assert.NotNull(entity);
        Assert.Equal("EvaluacionesProveedor", entity!.GetTableName());

        AssertPrecision(entity, nameof(EvaluacionProveedor.CantidadOrdenada));
        AssertPrecision(entity, nameof(EvaluacionProveedor.CantidadAceptada));
        AssertPrecision(entity, nameof(EvaluacionProveedor.CantidadDanada));
        AssertPrecision(entity, nameof(EvaluacionProveedor.CantidadSobrante));

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(EvaluacionProveedor.RecepcionCompraId) }) &&
            index.GetDatabaseName() == "IX_EvaluacionesProveedor_RecepcionCompra");

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(EvaluacionProveedor.OrdenCompraId) }) &&
            index.GetDatabaseName() == "IX_EvaluacionesProveedor_OrdenCompra");

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[]
                {
                    nameof(EvaluacionProveedor.ProveedorId),
                    nameof(EvaluacionProveedor.FechaRecepcionUtc)
                }) &&
            index.GetDatabaseName() == "IX_EvaluacionesProveedor_Proveedor_FechaRecepcion");

        AssertRestrict(entity, nameof(EvaluacionProveedor.ProveedorId));
        AssertRestrict(entity, nameof(EvaluacionProveedor.OrdenCompraId));
        AssertRestrict(entity, nameof(EvaluacionProveedor.RecepcionCompraId));
    }

    [Fact]
    public void Configuracion_no_inventa_unicidad_que_impida_multiples_evaluaciones_historicas()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new EvaluacionProveedorConfiguration().Configure(modelBuilder.Entity<EvaluacionProveedor>());

        var entity = modelBuilder.Model.FindEntityType(typeof(EvaluacionProveedor));
        Assert.NotNull(entity);

        Assert.DoesNotContain(entity!.GetIndexes(), index => index.IsUnique);
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
