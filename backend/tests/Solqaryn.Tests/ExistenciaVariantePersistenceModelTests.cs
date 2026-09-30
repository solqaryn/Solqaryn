using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class ExistenciaVariantePersistenceModelTests
{
    [Fact]
    public void Modelo_Persistente_Blinda_Unicidad_StockDisponible_Y_Ubicacion_Del_Mismo_Almacen()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n14c-model-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(ExistenciaVariante));

        Assert.NotNull(entity);
        Assert.Equal("ExistenciasVariante", entity!.GetTableName());
        Assert.Same(context.Set<ExistenciaVariante>(), context.ExistenciasVariante);

        var stockDisponible = entity.FindProperty(nameof(ExistenciaVariante.StockDisponible));
        Assert.NotNull(stockDisponible);
        Assert.Equal("StockFisico - StockReservado", stockDisponible!.GetComputedColumnSql());

        var ubicacionNormalizada = entity.FindProperty("UbicacionAlmacenIdUnica");
        Assert.NotNull(ubicacionNormalizada);
        Assert.Equal("IFNULL(UbicacionAlmacenId, 0)", ubicacionNormalizada!.GetComputedColumnSql());

        var unique = Assert.Single(entity.GetIndexes().Where(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(ExistenciaVariante.ProductoVarianteId),
                nameof(ExistenciaVariante.AlmacenId),
                "UbicacionAlmacenIdUnica"
            })));
        Assert.True(unique.IsUnique);
        Assert.Equal("UX_ExistenciasVariante_Variante_Almacen_Ubicacion", unique.GetDatabaseName());

        var ubicacionFk = Assert.Single(entity.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(ExistenciaVariante.AlmacenId),
                nameof(ExistenciaVariante.UbicacionAlmacenId)
            })));
        Assert.Equal(DeleteBehavior.Restrict, ubicacionFk.DeleteBehavior);
        Assert.Equal(typeof(UbicacionAlmacen), ubicacionFk.PrincipalEntityType.ClrType);
        Assert.Equal(
            new[] { nameof(UbicacionAlmacen.AlmacenId), nameof(UbicacionAlmacen.Id) },
            ubicacionFk.PrincipalKey.Properties.Select(p => p.Name));

        var varianteFk = Assert.Single(entity.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(ExistenciaVariante.ProductoVarianteId) })));
        Assert.Equal(DeleteBehavior.Restrict, varianteFk.DeleteBehavior);

        var almacenFk = Assert.Single(entity.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(ExistenciaVariante.AlmacenId) })));
        Assert.Equal(DeleteBehavior.Restrict, almacenFk.DeleteBehavior);
    }
}
