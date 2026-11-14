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
    [InlineData(typeof(ProductoVariante))]
    [InlineData(typeof(ProductoImagen))]
    [InlineData(typeof(VentaDetalle))]
    [InlineData(typeof(CompraDetalle))]
    [InlineData(typeof(Factura))]
    public void Los_descendientes_tienen_empresa_persistida_con_fk_e_indice(Type tipo)
    {
        using var db = Context();
        var entity = db.Model.FindEntityType(tipo);
        Assert.NotNull(entity);
        var owner = entity!.FindProperty("EmpresaId");
        Assert.NotNull(owner);
        Assert.True(owner!.IsNullable);
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Count == 1 && fk.Properties[0].Name == "EmpresaId" &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(entity.GetIndexes(), idx =>
            idx.Properties.Count >= 2 && idx.Properties[0].Name == "EmpresaId");
    }

    [Theory]
    [InlineData(typeof(ProductoVariante), "Sku")]
    [InlineData(typeof(ProductoVariante), "CodigoBarras")]
    [InlineData(typeof(Factura), "NumeroFactura")]
    public void Las_unicidades_de_derivados_son_por_empresa(Type tipo, string campo)
    {
        using var db = Context();
        var entity = db.Model.FindEntityType(tipo)!;
        Assert.Contains(entity.GetIndexes(), idx =>
            idx.IsUnique && idx.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", campo }));
        Assert.DoesNotContain(entity.GetIndexes(), idx =>
            idx.IsUnique && idx.Properties.Count == 1 && idx.Properties[0].Name == campo);
    }

    [Fact]
    public async Task Registros_nuevos_heredan_empresa_del_documento_o_producto()
    {
        await using var db = Context();
        var empresa = new Empresa("Empresa A");
        db.Set<Empresa>().Add(empresa);
        await db.SaveChangesAsync();
        var producto = new Producto { EmpresaId = empresa.Id, Nombre = "Producto A" };
        var venta = new Venta { EmpresaId = empresa.Id, NumeroVenta = "VTA-A-1" };
        var compra = new Compra { EmpresaId = empresa.Id, NumeroCompra = "COM-A-1" };
        db.Productos.Add(producto);
        db.Ventas.Add(venta);
        db.Compras.Add(compra);
        await db.SaveChangesAsync();
        var variante = new ProductoVariante { ProductoId = producto.Id };
        var imagen = new ProductoImagen { ProductoId = producto.Id };
        var detalleVenta = new VentaDetalle { VentaId = venta.Id, ProductoId = producto.Id };
        var detalleCompra = new CompraDetalle { CompraId = compra.Id, ProductoId = producto.Id };
        var factura = new Factura { VentaId = venta.Id, NumeroFactura = "FACT-A-1" };
        db.ProductoVariantes.Add(variante);
        db.ProductoImagenes.Add(imagen);
        db.VentaDetalles.Add(detalleVenta);
        db.CompraDetalles.Add(detalleCompra);
        db.Facturas.Add(factura);
        await db.SaveChangesAsync();
        Assert.Equal(empresa.Id, variante.EmpresaId);
        Assert.Equal(empresa.Id, imagen.EmpresaId);
        Assert.Equal(empresa.Id, detalleVenta.EmpresaId);
        Assert.Equal(empresa.Id, detalleCompra.EmpresaId);
        Assert.Equal(empresa.Id, factura.EmpresaId);
    }

    [Fact]
    public async Task Variante_no_puede_reasignarse_a_otra_empresa()
    {
        await using var db = Context();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var pA = new Producto { Nombre = "Producto A", EmpresaId = a.Id };
        var pB = new Producto { Nombre = "Producto B", EmpresaId = b.Id };
        db.Productos.AddRange(pA, pB);
        await db.SaveChangesAsync();
        var vA = new ProductoVariante { ProductoId = pA.Id, Sku = "SKU-A-1" };
        db.ProductoVariantes.Add(vA);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, vA.EmpresaId);
        vA.ProductoId = pB.Id;
        await Assert.ThrowsAsync<Solqaryn.Application.Exceptions.BusinessRuleException>(
            () => db.SaveChangesAsync());
    }

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
