using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110CosteoPersistenciaModelTests
{
    [Fact]
    public void Modelo_EF_contiene_entidades_claves_y_relaciones_de_costeo()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n110-model-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var model = db.Model;

        var empresaConfiguracion = model.FindEntityType(typeof(EmpresaConfiguracion));
        Assert.NotNull(empresaConfiguracion);
        Assert.Equal("EmpresaConfiguraciones", empresaConfiguracion!.GetTableName());

        var politica = model.FindEntityType(typeof(PoliticaCosteoInventario));
        Assert.NotNull(politica);
        Assert.Equal("PoliticasCosteoInventario", politica!.GetTableName());
        Assert.Contains(politica.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(EmpresaConfiguracion) &&
            fk.PrincipalEntityType.GetTableName() == "EmpresaConfiguraciones");
        Assert.Contains(politica.GetIndexes(), i =>
            i.IsUnique && i.Properties.Any(p => p.Name == "EmpresaConfiguracionVigenteId"));

        var estandar = model.FindEntityType(typeof(CostoEstandarInventario));
        Assert.NotNull(estandar);
        var pkEstandar = estandar!.FindPrimaryKey();
        Assert.Contains(estandar.GetKeys(), k =>
            k != pkEstandar && k.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "Id" }));
        Assert.Contains(estandar.GetIndexes(), i =>
            i.IsUnique && i.Properties.Any(p => p.Name == "ProductoVarianteVigenteId"));

        var capa = model.FindEntityType(typeof(CapaCostoInventario));
        Assert.NotNull(capa);
        Assert.Equal("CapasCostoInventario", capa!.GetTableName());
        Assert.Contains(capa.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(UbicacionAlmacen) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "AlmacenId", "UbicacionAlmacenId" }));
        Assert.Contains(capa.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CapaCostoInventario) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "CapaCostoOrigenId" }));

        var asignacion = model.FindEntityType(typeof(AsignacionCostoMovimientoInventario));
        Assert.NotNull(asignacion);
        Assert.Contains(asignacion!.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CapaCostoInventario) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "CapaCostoInventarioId" }));

        var variacion = model.FindEntityType(typeof(VariacionCostoEstandarInventario));
        Assert.NotNull(variacion);
        Assert.Contains(variacion!.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CostoEstandarInventario) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "CostoEstandarInventarioId" }));
    }
}
