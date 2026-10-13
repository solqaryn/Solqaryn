using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public class CommercialTenantRootOwnershipModelTests
{
    [Theory]
    [InlineData(typeof(Producto))]
    [InlineData(typeof(Cliente))]
    [InlineData(typeof(Proveedor))]
    [InlineData(typeof(Compra))]
    [InlineData(typeof(Venta))]
    public void Raiz_comercial_tiene_owner_nullable_fk_restrictiva_y_indice(Type entityType)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("commercial-roots-ownership-model")
            .Options;

        using var db = new AppDbContext(options);
        var entity = db.Model.FindEntityType(entityType);
        Assert.NotNull(entity);

        var owner = entity!.FindProperty("EmpresaId");
        Assert.NotNull(owner);
        Assert.True(owner!.IsNullable);

        var fk = Assert.Single(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Count == 1 && fk.Properties[0].Name == "EmpresaId");
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Count == 1 && index.Properties[0].Name == "EmpresaId");
    }

    [Fact]
    public void Ninguna_raiz_asigna_empresa_legacy_implicitamente()
    {
        Assert.Null(new Producto().EmpresaId);
        Assert.Null(new Cliente().EmpresaId);
        Assert.Null(new Proveedor().EmpresaId);
        Assert.Null(new Compra().EmpresaId);
        Assert.Null(new Venta().EmpresaId);
    }
}
