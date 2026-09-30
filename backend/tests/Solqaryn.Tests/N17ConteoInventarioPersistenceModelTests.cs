using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class N17ConteoInventarioPersistenceModelTests
{
    [Fact]
    public void Modelo_Persistente_Blinda_Scope_Y_Clave_Fisica_Del_Conteo()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n17c-model-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var conteo = context.Model.FindEntityType(typeof(ConteoInventario));
        var detalle = context.Model.FindEntityType(typeof(ConteoInventarioDetalle));

        Assert.NotNull(conteo);
        Assert.NotNull(detalle);
        Assert.Equal("ConteosInventario", conteo!.GetTableName());
        Assert.Equal("ConteoInventarioDetalles", detalle!.GetTableName());

        var numero = Assert.Single(conteo.GetIndexes().Where(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(ConteoInventario.Numero) })));
        Assert.True(numero.IsUnique);

        var scopeConteo = Assert.Single(conteo.GetKeys().Where(k =>
            k.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(ConteoInventario.Id), nameof(ConteoInventario.AlmacenId)
            })));
        Assert.Equal("AK_ConteosInventario_Id_AlmacenId", scopeConteo.GetName());

        var scopeUbicacionFk = Assert.Single(conteo.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(ConteoInventario.AlmacenId),
                nameof(ConteoInventario.UbicacionAlmacenId)
            })));
        Assert.Equal(DeleteBehavior.Restrict, scopeUbicacionFk.DeleteBehavior);
        Assert.Equal(typeof(UbicacionAlmacen), scopeUbicacionFk.PrincipalEntityType.ClrType);
        Assert.Equal(
            new[] { nameof(UbicacionAlmacen.AlmacenId), nameof(UbicacionAlmacen.Id) },
            scopeUbicacionFk.PrincipalKey.Properties.Select(p => p.Name));

        var conteoMismoAlmacenFk = Assert.Single(detalle.GetForeignKeys().Where(fk =>
            fk.PrincipalEntityType.ClrType == typeof(ConteoInventario) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(ConteoInventarioDetalle.ConteoInventarioId),
                nameof(ConteoInventarioDetalle.AlmacenId)
            })));
        Assert.Equal(DeleteBehavior.Cascade, conteoMismoAlmacenFk.DeleteBehavior);
        Assert.Equal(
            new[] { nameof(ConteoInventario.Id), nameof(ConteoInventario.AlmacenId) },
            conteoMismoAlmacenFk.PrincipalKey.Properties.Select(p => p.Name));

        var ubicacionNormalizada = detalle.FindProperty("UbicacionNormalizada");
        Assert.NotNull(ubicacionNormalizada);
        Assert.Equal("COALESCE(`UbicacionAlmacenId`, 0)", ubicacionNormalizada!.GetComputedColumnSql());

        var claveFisica = Assert.Single(detalle.GetIndexes().Where(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(ConteoInventarioDetalle.ConteoInventarioId),
                nameof(ConteoInventarioDetalle.ProductoVarianteId),
                nameof(ConteoInventarioDetalle.AlmacenId),
                "UbicacionNormalizada"
            })));
        Assert.True(claveFisica.IsUnique);
        Assert.Equal("UX_ConteoDetalles_ClaveFisica", claveFisica.GetDatabaseName());

        var detalleUbicacionFk = Assert.Single(detalle.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(ConteoInventarioDetalle.AlmacenId),
                nameof(ConteoInventarioDetalle.UbicacionAlmacenId)
            })));
        Assert.Equal(DeleteBehavior.Restrict, detalleUbicacionFk.DeleteBehavior);
        Assert.Equal(typeof(UbicacionAlmacen), detalleUbicacionFk.PrincipalEntityType.ClrType);

        var ajusteFk = Assert.Single(detalle.GetForeignKeys().Where(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(ConteoInventarioDetalle.AjusteInventarioId) })));
        Assert.Equal(DeleteBehavior.Restrict, ajusteFk.DeleteBehavior);
        Assert.Equal(typeof(AjusteInventario), ajusteFk.PrincipalEntityType.ClrType);
    }
}
