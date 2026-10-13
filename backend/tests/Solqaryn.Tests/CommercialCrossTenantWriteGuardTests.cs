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

    [Fact]
    public async Task Empresa_propietaria_asignada_no_puede_trasladarse_a_otra()
    {
        await using var db = NewContext();
        var empresaA = new Empresa("Empresa A");
        var empresaB = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var producto = new Producto { Nombre = "Producto A", EmpresaId = empresaA.Id };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        producto.EmpresaId = empresaB.Id;
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Dato_legacy_no_puede_recibir_owner_por_escritura_ordinaria()
    {
        await using var db = NewContext();
        var empresa = new Empresa("Empresa A");
        db.Set<Empresa>().Add(empresa);
        await db.SaveChangesAsync();
        var proveedor = new Proveedor { Nombre = "Legacy", EmpresaId = null };
        db.Proveedores.Add(proveedor);
        await db.SaveChangesAsync();

        proveedor.EmpresaId = empresa.Id;
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }
}
