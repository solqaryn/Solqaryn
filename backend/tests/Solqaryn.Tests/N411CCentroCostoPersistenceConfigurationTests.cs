using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class N411CCentroCostoPersistenceConfigurationTests
{
    private static IModel BuildModel()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.ApplyConfiguration(new CentroCostoConfiguration());
        return modelBuilder.FinalizeModel();
    }

    [Fact]
    public void CentroCosto_UsaTablaYFiltroDeSoftDelete()
    {
        var entity = BuildModel().FindEntityType(typeof(CentroCosto));

        Assert.NotNull(entity);
        Assert.Equal("CentrosCosto", entity!.GetTableName());
        Assert.NotNull(entity.GetQueryFilter());
    }

    [Fact]
    public void CodigoActivoUnico_EsIndiceUnicoPersistente()
    {
        var entity = BuildModel().FindEntityType(typeof(CentroCosto));
        Assert.NotNull(entity);

        var property = entity!.FindProperty("CodigoActivoUnico");
        Assert.NotNull(property);
        Assert.Equal(40, property!.GetMaxLength());
        Assert.Contains("Eliminado = 0", property.GetComputedColumnSql() ?? string.Empty);

        var index = entity.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == "CodigoActivoUnico");

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
        Assert.Equal("UX_CentrosCosto_Codigo_Activo", index.GetDatabaseName());
    }

    [Fact]
    public void Sucursal_EsLaUnicaRelacionEstructuralConfigurada()
    {
        var entity = BuildModel().FindEntityType(typeof(CentroCosto));
        Assert.NotNull(entity);

        var foreignKeys = entity!.GetForeignKeys().ToList();
        var foreignKey = Assert.Single(foreignKeys);

        Assert.Equal(nameof(CentroCosto.SucursalId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Persistencia_ExponeChecksDeTipoYAsociacion()
    {
        var entity = BuildModel().FindEntityType(typeof(CentroCosto));
        Assert.NotNull(entity);

        var checks = entity!.GetCheckConstraints().ToDictionary(x => x.Name!, x => x.Sql);

        Assert.Contains("CK_CentrosCosto_Tipo", checks.Keys);
        Assert.Contains("CK_CentrosCosto_Asociacion", checks.Keys);
        Assert.Contains("Tipo` IN (1, 2, 3, 4)", checks["CK_CentrosCosto_Tipo"]);
        Assert.Contains("SucursalId` IS NOT NULL", checks["CK_CentrosCosto_Asociacion"]);
        Assert.Contains("SucursalId` IS NULL", checks["CK_CentrosCosto_Asociacion"]);
    }
}
