using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantTaxDiscountTests
{
    private static AppDbContext Context(HttpContext? http = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tax-tenant-" + Guid.NewGuid().ToString("N")).Options,
            tenantHttpContextAccessor: http is null ? null :
                new HttpContextAccessor { HttpContext = http });

    [Theory]
    [InlineData(typeof(VentaImpuesto), "VentaId")]
    [InlineData(typeof(CompraImpuesto), "CompraId")]
    [InlineData(typeof(VentaDescuento), "VentaId")]
    [InlineData(typeof(HistorialUsoDescuento), "VentaId")]
    public void Registros_fiscales_y_descuentos_tienen_fk_empresa_e_indice(Type type, string key)
    {
        using var db = Context();
        var model = db.Model.FindEntityType(type)!;
        Assert.True(model.FindProperty("EmpresaId")!.IsNullable);
        Assert.Contains(model.GetIndexes(), x =>
            x.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmpresaId", key }));
        Assert.Contains(model.GetForeignKeys(), x =>
            x.PrincipalEntityType.ClrType == typeof(Empresa) &&
            x.Properties.SingleOrDefault()?.Name == "EmpresaId" &&
            x.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public async Task Ajustes_de_venta_y_compra_heredan_propietario_del_documento()
    {
        await using var db = Context();
        var a = new Empresa("Empresa A");
        db.Set<Empresa>().Add(a);
        await db.SaveChangesAsync();
        var v = new Venta { EmpresaId = a.Id, NumeroVenta = "VENTA-A" };
        var c = new Compra { EmpresaId = a.Id, NumeroCompra = "COMPRA-A" };
        db.Ventas.Add(v);
        db.Compras.Add(c);
        await db.SaveChangesAsync();

        var impuestoVenta = new VentaImpuesto { VentaId = v.Id, ImpuestoId = 1 };
        var impuestoCompra = new CompraImpuesto { CompraId = c.Id, ImpuestoId = 1 };
        var descuento = new VentaDescuento { VentaId = v.Id, DescuentoId = 1 };
        var historial = new HistorialUsoDescuento { VentaId = v.Id, DescuentoId = 1 };
        db.VentaImpuestos.Add(impuestoVenta);
        db.CompraImpuestos.Add(impuestoCompra);
        db.VentaDescuentos.Add(descuento);
        db.HistorialUsoDescuentos.Add(historial);
        await db.SaveChangesAsync();

        Assert.Equal(a.Id, impuestoVenta.EmpresaId);
        Assert.Equal(a.Id, impuestoCompra.EmpresaId);
        Assert.Equal(a.Id, descuento.EmpresaId);
        Assert.Equal(a.Id, historial.EmpresaId);
    }

    [Fact]
    public async Task Compra_A_rechaza_impuesto_propiedad_B()
    {
        await using var db = Context();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var compraA = new Compra { EmpresaId = a.Id, NumeroCompra = "COM-A" };
        db.Compras.Add(compraA);
        await db.SaveChangesAsync();

        db.CompraImpuestos.Add(new CompraImpuesto
        {
            CompraId = compraA.Id, ImpuestoId = 1, EmpresaId = b.Id
        });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Historial_venta_A_no_acepta_cliente_B()
    {
        await using var db = Context();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var v = new Venta { EmpresaId = a.Id, NumeroVenta = "VEN-A" };
        var clienteB = new Cliente { EmpresaId = b.Id, Nombre = "Cliente B" };
        db.Ventas.Add(v);
        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();

        db.HistorialUsoDescuentos.Add(new HistorialUsoDescuento
        {
            VentaId = v.Id, ClienteId = clienteB.Id, DescuentoId = 1
        });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task La_empresa_A_no_puede_escribir_descuentos_de_venta_B()
    {
        var http = new DefaultHttpContext();
        await using var db = Context(http);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var vB = new Venta { EmpresaId = b.Id, NumeroVenta = "VEN-B" };
        db.Ventas.Add(vB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        db.VentaDescuentos.Add(new VentaDescuento { VentaId = vB.Id, DescuentoId = 1 });
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => db.SaveChangesAsync());
    }
}
