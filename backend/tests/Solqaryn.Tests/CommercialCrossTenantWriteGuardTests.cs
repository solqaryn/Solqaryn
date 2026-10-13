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
    public async Task Venta_nueva_acepta_producto_del_mismo_tenant_en_un_solo_save()
    {
        await using var db = NewContext();
        var empresaA = new Empresa("Empresa A");
        db.Set<Empresa>().Add(empresaA);
        await db.SaveChangesAsync();
        var productoA = new Producto { Nombre = "Producto A", EmpresaId = empresaA.Id };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();

        var ventaA = new Venta { NumeroVenta = "NUEVA-A-01", EmpresaId = empresaA.Id };
        ventaA.Detalles.Add(new VentaDetalle { ProductoId = productoA.Id, Cantidad = 1 });
        db.Ventas.Add(ventaA);

        await db.SaveChangesAsync();
        Assert.Equal(empresaA.Id, (await db.Ventas.SingleAsync()).EmpresaId);
        Assert.Equal(productoA.Id, (await db.VentaDetalles.SingleAsync()).ProductoId);
    }

    [Fact]
    public async Task Compra_nueva_acepta_producto_del_mismo_tenant_en_un_solo_save()
    {
        await using var db = NewContext();
        var empresaA = new Empresa("Empresa A");
        db.Set<Empresa>().Add(empresaA);
        await db.SaveChangesAsync();
        var productoA = new Producto { Nombre = "Producto A", EmpresaId = empresaA.Id };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();

        var compraA = new Compra { NumeroCompra = "NUEVA-A-01", EmpresaId = empresaA.Id };
        compraA.Detalles.Add(new CompraDetalle { ProductoId = productoA.Id, Cantidad = 1 });
        db.Compras.Add(compraA);

        await db.SaveChangesAsync();
        Assert.Equal(empresaA.Id, (await db.Compras.SingleAsync()).EmpresaId);
        Assert.Equal(productoA.Id, (await db.CompraDetalles.SingleAsync()).ProductoId);
    }

    [Fact]
    public async Task Venta_nueva_rechaza_producto_ajeno_en_el_mismo_save()
    {
        await using var db = NewContext();
        var empresaA = new Empresa("Empresa A");
        var empresaB = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();
        var productoB = new Producto { Nombre = "Producto B", EmpresaId = empresaB.Id };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();

        var ventaA = new Venta { NumeroVenta = "NUEVA-A-B", EmpresaId = empresaA.Id };
        ventaA.Detalles.Add(new VentaDetalle { ProductoId = productoB.Id, Cantidad = 1 });
        db.Ventas.Add(ventaA);

        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
        Assert.Empty(await db.Ventas.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Cliente_nuevo_y_venta_nueva_del_mismo_tenant_son_validos()
    {
        await using var db = NewContext();
        var empresaA = new Empresa("Empresa A");
        db.Set<Empresa>().Add(empresaA);
        await db.SaveChangesAsync();

        var clienteA = new Cliente { Nombre = "Cliente A", EmpresaId = empresaA.Id };
        var ventaA = new Venta
        {
            NumeroVenta = "CLIENTE-A-NUEVO",
            EmpresaId = empresaA.Id,
            Cliente = clienteA
        };
        db.Ventas.Add(ventaA);

        await db.SaveChangesAsync();
        Assert.Equal(empresaA.Id, (await db.Clientes.SingleAsync()).EmpresaId);
        Assert.Equal(clienteA.Id, (await db.Ventas.SingleAsync()).ClienteId);
    }

    [Fact]
    public async Task Cliente_nuevo_y_venta_ajena_en_el_mismo_save_son_rechazados()
    {
        await using var db = NewContext();
        var empresaA = new Empresa("Empresa A");
        var empresaB = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var ventaA = new Venta
        {
            NumeroVenta = "CLIENTE-B-RECHAZADO",
            EmpresaId = empresaA.Id,
            Cliente = new Cliente { Nombre = "Cliente B", EmpresaId = empresaB.Id }
        };
        db.Ventas.Add(ventaA);

        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
        Assert.Empty(await db.Ventas.AsNoTracking().ToListAsync());
    }

    [Fact]
    public void Guardar_sincrono_bloquea_entidad_comercial()
    {
        using var db = NewContext();
        db.Productos.Add(new Producto { Nombre = "Prohibido" });
        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
        Assert.Throws<NotSupportedException>(() => db.SaveChanges(acceptAllChangesOnSuccess: false));
    }

    [Fact]
    public async Task Overload_async_bool_no_elude_la_validacion_empresarial()
    {
        await using var db = NewContext();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var cliente = new Cliente { Nombre = "Cliente B", EmpresaId = b.Id };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        db.Ventas.Add(new Venta { NumeroVenta = "ASYNC-BOOL-1", EmpresaId = a.Id, ClienteId = cliente.Id });
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => db.SaveChangesAsync(acceptAllChangesOnSuccess: false, CancellationToken.None));
    }

    [Fact]
    public async Task Update_desconectado_rechaza_reescritura_de_empresa()
    {
        await using var db = NewContext();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var producto = new Producto { Nombre = "Producto original", EmpresaId = a.Id };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        db.Entry(producto).State = EntityState.Detached;

        // OriginalValue y CurrentValue se inicializan ambos con el owner falso.
        db.Productos.Update(new Producto
        {
            Id = producto.Id, Nombre = "Producto desconectado", EmpresaId = b.Id
        });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
        var real = await db.Productos.AsNoTracking().SingleAsync(p => p.Id == producto.Id);
        Assert.Equal(a.Id, real.EmpresaId);
    }

    [Fact]
    public async Task Venta_de_duenio_conocido_no_acepta_cliente_legacy_ambiguo()
    {
        await using var db = NewContext();
        var a = new Empresa("Empresa A");
        db.Set<Empresa>().Add(a);
        await db.SaveChangesAsync();
        var cliente = new Cliente { Nombre = "Sin attribution", EmpresaId = null };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        db.Ventas.Add(new Venta { NumeroVenta = "LEGACY-001", EmpresaId = a.Id, ClienteId = cliente.Id });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
        Assert.Null(cliente.EmpresaId);
    }

    [Fact]
    public async Task VentaDetalle_rechaza_variante_de_otro_producto_incluso_en_misma_empresa()
    {
        await using var db = NewContext();
        var empresa = new Empresa("Empresa A");
        db.Set<Empresa>().Add(empresa);
        await db.SaveChangesAsync();
        var productoA = new Producto { Nombre = "Producto A", EmpresaId = empresa.Id };
        var productoB = new Producto { Nombre = "Producto B", EmpresaId = empresa.Id };
        var venta = new Venta { NumeroVenta = "VAR-001", EmpresaId = empresa.Id };
        db.Productos.AddRange(productoA, productoB);
        db.Ventas.Add(venta);
        await db.SaveChangesAsync();

        var varianteB = new ProductoVariante { ProductoId = productoB.Id };
        db.ProductoVariantes.Add(varianteB);
        await db.SaveChangesAsync();

        db.VentaDetalles.Add(new VentaDetalle
        {
            VentaId = venta.Id, ProductoId = productoA.Id,
            ProductoVarianteId = varianteB.Id, Cantidad = 1
        });
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

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
