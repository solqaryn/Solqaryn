using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class UbicacionAlmacenPersistenceModelTests
{
    [Fact]
    public void Modelo_Persistente_Blinda_Jerarquia_Codigo_Activo_Y_SoftDelete()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n13c-model-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(UbicacionAlmacen));

        Assert.NotNull(entity);
        Assert.Equal("UbicacionesAlmacen", entity!.GetTableName());
        Assert.NotNull(entity.GetQueryFilter());

        var almacenFk = Assert.Single(entity.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(UbicacionAlmacen.AlmacenId) })));
        Assert.Equal(DeleteBehavior.Restrict, almacenFk.DeleteBehavior);
        Assert.Equal(typeof(Almacen), almacenFk.PrincipalEntityType.ClrType);

        var jerarquiaFk = Assert.Single(entity.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(UbicacionAlmacen.AlmacenId),
                nameof(UbicacionAlmacen.UbicacionPadreId)
            })));
        Assert.Equal(DeleteBehavior.Restrict, jerarquiaFk.DeleteBehavior);
        Assert.Equal(typeof(UbicacionAlmacen), jerarquiaFk.PrincipalEntityType.ClrType);
        Assert.Equal(
            new[] { nameof(UbicacionAlmacen.AlmacenId), nameof(UbicacionAlmacen.Id) },
            jerarquiaFk.PrincipalKey.Properties.Select(p => p.Name));

        var alternateKey = Assert.Single(entity.GetKeys().Where(k => !k.IsPrimaryKey()));
        Assert.Equal(
            new[] { nameof(UbicacionAlmacen.AlmacenId), nameof(UbicacionAlmacen.Id) },
            alternateKey.Properties.Select(p => p.Name));

        var codigoActivo = entity.FindProperty("CodigoActivoUnico");
        Assert.NotNull(codigoActivo);
        Assert.Equal("IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)", codigoActivo!.GetComputedColumnSql());

        var indiceCodigo = Assert.Single(entity.GetIndexes().Where(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(UbicacionAlmacen.AlmacenId),
                "CodigoActivoUnico"
            })));
        Assert.True(indiceCodigo.IsUnique);
        Assert.Equal("UX_UbicacionesAlmacen_Almacen_Codigo_Activo", indiceCodigo.GetDatabaseName());
    }
}
