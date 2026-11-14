using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexIndexContractTests
{
    [Fact]
    public void ModeloEf_ConservaIndicesOperativosParaFiltrosYOrdenDelKardex()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(MovimientoInventario));
        Assert.NotNull(entity);

        var indices = entity!.GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(p => p.Name)))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ProductoId,ProductoVarianteId,Fecha", indices);
        Assert.Contains("AlmacenId,UbicacionAlmacenId,Fecha", indices);
        Assert.Contains("CompraId,Fecha", indices);
        Assert.Contains("VentaId,Fecha", indices);
        Assert.Contains("ConsumoInsumoId,Fecha", indices);
        Assert.Contains("AjusteInventarioId,Fecha", indices);
        Assert.Contains("CorrelationId,Fecha", indices);
    }

    [Fact]
    public void CorrelationId_EsRequeridoYLimitadoA64EnModeloEf()
    {
        using var context = CrearContexto();
        var property = context.Model.FindEntityType(typeof(MovimientoInventario))?
            .FindProperty(nameof(MovimientoInventario.CorrelationId));

        Assert.NotNull(property);
        Assert.False(property!.IsNullable);
        Assert.Equal(64, property.GetMaxLength());
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n15-kardex-index-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
