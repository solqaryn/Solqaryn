using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public class CommercialTenantScopedRootIndexesTests
{
    private static AppDbContext Context() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("commercial-tenant-index-model-" + Guid.NewGuid())
            .Options);

    [Theory]
    [InlineData(typeof(Producto))]
    [InlineData(typeof(Cliente))]
    [InlineData(typeof(Proveedor))]
    [InlineData(typeof(Compra))]
    [InlineData(typeof(Venta))]
    public void Las_raices_tienen_indice_de_identidad_compuesta_con_empresa(Type tipo)
    {
        using var db = Context();
        var entity = db.Model.FindEntityType(tipo);
        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), idx =>
            idx.IsUnique && idx.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", "Id" }));
    }

    [Theory]
    [InlineData(typeof(Cliente), "IdentidadORTNNormalizada")]
    [InlineData(typeof(Proveedor), "DocumentoNormalizado")]
    [InlineData(typeof(Compra), "NumeroCompra")]
    [InlineData(typeof(Venta), "NumeroVenta")]
    public void Los_identificadores_comerciales_no_son_unicos_globales(Type tipo, string campo)
    {
        using var db = Context();
        var entity = db.Model.FindEntityType(tipo);
        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), idx =>
            idx.IsUnique && idx.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", campo }));
        Assert.DoesNotContain(entity.GetIndexes(), idx =>
            idx.IsUnique && idx.Properties.Count == 1 && idx.Properties[0].Name == campo);
    }
}
