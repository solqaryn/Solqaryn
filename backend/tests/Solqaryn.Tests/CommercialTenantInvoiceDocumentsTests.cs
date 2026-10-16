using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantInvoiceDocumentsTests
{
    private static AppDbContext NewDb(HttpContext? http = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tenant-invoice-" + Guid.NewGuid().ToString("N"))
            .Options, tenantHttpContextAccessor: http is null ? null :
                new HttpContextAccessor { HttpContext = http });

    [Theory]
    [InlineData(typeof(FacturaDetalle), "FacturaId")]
    [InlineData(typeof(FacturaPago), "FacturaId")]
    [InlineData(typeof(CompraDocumento), "CompraId")]
    public void Descendientes_tienen_owner_indice_y_fk(Type type, string parentKey)
    {
        using var db = NewDb();
        var entity = db.Model.FindEntityType(type)!;
        Assert.True(entity.FindProperty("EmpresaId")!.IsNullable);
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Select(p => p.Name)
            .SequenceEqual(new[] { "EmpresaId", parentKey }));
        Assert.Contains(entity.GetForeignKeys(), x =>
            x.PrincipalEntityType.ClrType == typeof(Empresa) &&
            x.Properties.Count == 1 && x.Properties[0].Name == "EmpresaId" &&
            x.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public async Task Dependientes_nuevos_heredan_owner_y_no_aceptan_cruce_AB()
    {
        var http = new DefaultHttpContext();
        await using var db = NewDb(http);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var productoA = new Producto { EmpresaId = a.Id, Nombre = "Producto A" };
        var productoB = new Producto { EmpresaId = b.Id, Nombre = "Producto B" };
        var ventaA = new Venta { EmpresaId = a.Id, NumeroVenta = "VA-1" };
        var compraA = new Compra { EmpresaId = a.Id, NumeroCompra = "CA-1" };
        db.Productos.AddRange(productoA,productoB);
        db.Ventas.Add(ventaA);
        db.Compras.Add(compraA);
        await db.SaveChangesAsync();
        var facturaA = new Factura { EmpresaId = a.Id, VentaId = ventaA.Id, NumeroFactura = "FA-1" };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var line = new FacturaDetalle { FacturaId = facturaA.Id, ProductoId = productoA.Id };
        var payment = new FacturaPago { FacturaId = facturaA.Id, Monto = 1m };
        var attachment = new CompraDocumento { CompraId = compraA.Id, NombreOriginal = "compra.pdf" };
        db.FacturaDetalles.Add(line);
        db.FacturaPagos.Add(payment);
        db.CompraDocumentos.Add(attachment);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, line.EmpresaId);
        Assert.Equal(a.Id, payment.EmpresaId);
        Assert.Equal(a.Id, attachment.EmpresaId);

        db.FacturaDetalles.Add(new FacturaDetalle { FacturaId = facturaA.Id, ProductoId = productoB.Id });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Tenant_A_rechaza_pago_sobre_factura_B()
    {
        var http = new DefaultHttpContext();
        await using var db = NewDb(http);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var saleB = new Venta { EmpresaId=b.Id, NumeroVenta="VB-1" };
        db.Ventas.Add(saleB);
        await db.SaveChangesAsync();
        var invB = new Factura { EmpresaId=b.Id, VentaId=saleB.Id, NumeroFactura="FB-1" };
        db.Facturas.Add(invB);
        await db.SaveChangesAsync();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey]=a.Id;
        db.FacturaPagos.Add(new FacturaPago { FacturaId=invB.Id, Monto=1m });
        await Assert.ThrowsAsync<ForbiddenAccessException>(()=>db.SaveChangesAsync());
    }
}
