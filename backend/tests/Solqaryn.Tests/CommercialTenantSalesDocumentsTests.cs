using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantSalesDocumentsTests
{
    private static AppDbContext NewDb(HttpContext? http = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("tenant-sales-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: http is null ? null :
                new HttpContextAccessor { HttpContext = http });

    [Theory]
    [InlineData(typeof(Cotizacion))]
    [InlineData(typeof(CotizacionDetalle))]
    [InlineData(typeof(PedidoVenta))]
    [InlineData(typeof(PedidoVentaDetalle))]
    public void Cotizaciones_y_pedidos_tienen_empresa_y_fk(Type entityType)
    {
        using var db = NewDb();
        var entity = db.Model.FindEntityType(entityType)!;
        Assert.True(entity.FindProperty("EmpresaId")!.IsNullable);
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.SingleOrDefault()?.Name == "EmpresaId" &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void Pedidos_no_comparten_idempotencia_entre_empresas()
    {
        using var db = NewDb();
        var model = db.Model.FindEntityType(typeof(PedidoVenta))!;
        Assert.Contains(model.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", "IdempotencyKey" }));
        Assert.DoesNotContain(model.GetIndexes(), x =>
            x.IsUnique && x.Properties.Count == 1 &&
            x.Properties[0].Name == "IdempotencyKey");
    }

    [Fact]
    public async Task Cotizacion_y_pedido_heredan_empresa_A_del_cliente()
    {
        await using var db = NewDb();
        var a = new Empresa("Empresa A");
        db.Set<Empresa>().Add(a);
        await db.SaveChangesAsync();
        var client = new Cliente { Nombre = "Cliente A", EmpresaId = a.Id };
        var product = new Producto { Nombre = "Producto A", EmpresaId = a.Id };
        db.Clientes.Add(client);
        db.Productos.Add(product);
        await db.SaveChangesAsync();

        var quote = new Cotizacion {
            ClienteId=client.Id, Cliente=client, ClienteNombreSnapshot="Cliente A"
        };
        var line = new CotizacionDetalle { ProductoId=product.Id, Producto=product };
        line.EstablecerValores(1m,10m);
        quote.Detalles.Add(line);
        db.Cotizaciones.Add(quote);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, quote.EmpresaId);
        Assert.Equal(a.Id, line.EmpresaId);

        quote.Enviar(1, DateTime.UtcNow);
        quote.Aceptar(1, DateTime.UtcNow);
        await db.SaveChangesAsync();
        var order = PedidoVenta.CrearDesdeCotizacion(quote);
        db.PedidosVenta.Add(order);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, order.EmpresaId);
        Assert.All(await db.PedidoVentaDetalles.ToListAsync(), d =>
            Assert.Equal(a.Id, d.EmpresaId));
    }

    [Fact]
    public async Task Cotizacion_A_rechaza_producto_B()
    {
        await using var db = NewDb();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var clientA = new Cliente { Nombre = "Cliente A", EmpresaId=a.Id };
        var productB = new Producto { Nombre = "Producto B", EmpresaId=b.Id };
        db.Clientes.Add(clientA);
        db.Productos.Add(productB);
        await db.SaveChangesAsync();
        var quote = new Cotizacion {
            ClienteId=clientA.Id, Cliente=clientA, ClienteNombreSnapshot="Cliente A"
        };
        var detail = new CotizacionDetalle { ProductoId=productB.Id, Producto=productB };
        detail.EstablecerValores(1m,1m);
        quote.Detalles.Add(detail);
        db.Cotizaciones.Add(quote);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>db.SaveChangesAsync());
        Assert.Empty(await db.Cotizaciones.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Marca_RBAC_A_rechaza_cotizacion_heredada_de_B()
    {
        var http = new DefaultHttpContext();
        await using var db=NewDb(http);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var clientB = new Cliente { Nombre="Cliente B", EmpresaId=b.Id };
        db.Clientes.Add(clientB);
        await db.SaveChangesAsync();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey]=a.Id;
        db.Cotizaciones.Add(new Cotizacion {
            ClienteId=clientB.Id, Cliente=clientB, ClienteNombreSnapshot="Cliente B"
        });
        await Assert.ThrowsAsync<ForbiddenAccessException>(()=>db.SaveChangesAsync());
    }
}
