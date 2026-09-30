using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class N19TrazabilidadPersistenceModelTests
{
    [Fact]
    public void ProductoVariante_Debe_Persistir_Politica_OptIn_De_Trazabilidad()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n19-variante-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var variante = db.Model.FindEntityType(typeof(ProductoVariante));

        Assert.NotNull(variante);
        Assert.NotNull(variante!.FindProperty(nameof(ProductoVariante.ControlaLote)));
        Assert.NotNull(variante.FindProperty(nameof(ProductoVariante.ControlaNumeroSerie)));
        Assert.NotNull(variante.FindProperty(nameof(ProductoVariante.ControlaFechaVencimiento)));
        Assert.NotNull(variante.FindProperty(nameof(ProductoVariante.DiasAlertaVencimiento)));
        Assert.Null(variante.FindProperty(nameof(ProductoVariante.RequiereTrazabilidad)));
    }

    [Fact]
    public void Modelo_EF_Debe_Mapear_Lotes_Y_Series_Con_Identidades_Unicas_Y_Fks_Restrictivas()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n19-trazabilidad-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var lote = db.Model.FindEntityType(typeof(LoteInventario));
        var serie = db.Model.FindEntityType(typeof(SerieInventario));

        Assert.NotNull(lote);
        Assert.NotNull(serie);
        Assert.Equal("LotesInventario", lote!.GetTableName());
        Assert.Equal("SeriesInventario", serie!.GetTableName());

        var loteUnico = lote.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_LotesInventario_Variante_Codigo");
        Assert.True(loteUnico.IsUnique);
        Assert.Equal(new[] { "ProductoVarianteId", "Codigo" }, loteUnico.Properties.Select(p => p.Name));

        Assert.Contains(lote.GetKeys(), key =>
            key.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "Id" }));

        var serieUnica = serie.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_SeriesInventario_NumeroSerie");
        Assert.True(serieUnica.IsUnique);
        Assert.Equal(new[] { "NumeroSerie" }, serieUnica.Properties.Select(p => p.Name));

        var indiceSerieLote = serie.GetIndexes().Single(i =>
            i.GetDatabaseName() == "IX_SeriesInventario_Variante_LoteInventarioId");
        Assert.Equal(new[] { "ProductoVarianteId", "LoteInventarioId" }, indiceSerieLote.Properties.Select(p => p.Name));

        var fkSerieLote = serie.GetForeignKeys().Single(fk =>
            fk.PrincipalEntityType.ClrType == typeof(LoteInventario));
        Assert.Equal(new[] { "ProductoVarianteId", "LoteInventarioId" }, fkSerieLote.Properties.Select(p => p.Name));
        Assert.Equal(new[] { "ProductoVarianteId", "Id" }, fkSerieLote.PrincipalKey.Properties.Select(p => p.Name));
        Assert.Equal(DeleteBehavior.Restrict, fkSerieLote.DeleteBehavior);

        Assert.All(lote.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
        Assert.All(serie.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [Fact]
    public void Modelo_EF_Debe_Conservar_Longitudes_E_Indices_Operativos_De_Trazabilidad()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n19-metadata-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var lote = db.Model.FindEntityType(typeof(LoteInventario))!;
        var serie = db.Model.FindEntityType(typeof(SerieInventario))!;

        Assert.Equal(100, lote.FindProperty(nameof(LoteInventario.Codigo))!.GetMaxLength());
        Assert.False(lote.FindProperty(nameof(LoteInventario.Codigo))!.IsNullable);
        Assert.Contains(lote.GetIndexes(), i =>
            i.GetDatabaseName() == "IX_LotesInventario_FechaVencimiento" &&
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { "FechaVencimiento" }));

        Assert.Equal(160, serie.FindProperty(nameof(SerieInventario.NumeroSerie))!.GetMaxLength());
        Assert.False(serie.FindProperty(nameof(SerieInventario.NumeroSerie))!.IsNullable);
        Assert.Contains(serie.GetIndexes(), i =>
            i.GetDatabaseName() == "IX_SeriesInventario_Variante_Estado" &&
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "Estado" }));
    }

    [Fact]
    public void DbContext_Debe_Exponer_Lotes_Y_Series()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n19-dbsets-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);

        Assert.NotNull(db.LotesInventario);
        Assert.NotNull(db.SeriesInventario);
    }
}
