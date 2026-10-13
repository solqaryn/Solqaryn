using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialCrossTenantWriteGuardTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("commercial-cross-owner-" + Guid.NewGuid().ToString("N"))
            .Options);

    [Fact]
    public async Task Venta_rechaza_cliente_explicito_de_otra_empresa()
    {
        await using var db = NewContext();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        var tipo = new TipoCliente { Codigo = "CONSUMIDOR", Nombre = "Consumidor", NombreNormalizado = "consumidor" };
        db.Set<Empresa>().AddRange(a, b);
        db.TipoClientes.Add(tipo);
        await db.SaveChangesAsync();

        var clienteB = new Cliente { Nombre = "Cliente B", EmpresaId = b.Id, TipoClienteId = tipo.Id };
        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();

        db.Ventas.Add(new Venta { NumeroVenta = "TEST-VTA-0001", EmpresaId = a.Id, ClienteId = clienteB.Id });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Compra_rechaza_proveedor_explicito_de_otra_empresa()
    {
        await using var db = NewContext();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var proveedorB = new Proveedor { Nombre = "Proveedor B", EmpresaId = b.Id };
        db.Proveedores.Add(proveedorB);
        await db.SaveChangesAsync();

        db.Compras.Add(new Compra { NumeroCompra = "TEST-COM-0001", EmpresaId = a.Id, ProveedorId = proveedorB.Id });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task VentaDetalle_rechaza_producto_explicito_de_otra_empresa()
    {
        await using var db = NewContext();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var productoB = new Producto { Nombre = "Producto B", EmpresaId = b.Id };
        var ventaA = new Venta { NumeroVenta = "TEST-VTA-0002", EmpresaId = a.Id };
        db.Productos.Add(productoB);
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();

        db.VentaDetalles.Add(new VentaDetalle { VentaId = ventaA.Id, ProductoId = productoB.Id, Cantidad = 1 });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CompraDetalle_rechaza_producto_explicito_de_otra_empresa()
    {
        await using var db = NewContext();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var productoB = new Producto { Nombre = "Producto B", EmpresaId = b.Id };
        var compraA = new Compra { NumeroCompra = "TEST-COM-0002", EmpresaId = a.Id };
        db.Productos.Add(productoB);
        db.Compras.Add(compraA);
        await db.SaveChangesAsync();

        db.CompraDetalles.Add(new CompraDetalle { CompraId = compraA.Id, ProductoId = productoB.Id, Cantidad = 1 });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task No_atribuye_ni_rechaza_legacy_sin_propietario_comprobado()
    {
        await using var db = NewContext();
        db.Proveedores.Add(new Proveedor { Nombre = "Dato historico" });
        await db.SaveChangesAsync();
        Assert.Null((await db.Proveedores.SingleAsync()).EmpresaId);
    }
}
