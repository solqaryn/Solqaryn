using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class InsumosAislamientoVentasTests
{
    [Fact]
    public async Task SaveChangesAsync_Rechaza_VentaDetalle_De_Insumo_Administrativo()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);

        var insumo = new Producto
        {
            Nombre = "Bolsa de empaque",
            Marca = "Interno",
            Modelo = "BOLSA-01",
            TipoInventario = TipoInventario.InsumoAdministrativo,
            Cantidad = 20,
            Costo = 2m,
            Precio = 2m,
            Activo = true
        };
        context.Productos.Add(insumo);
        await context.SaveChangesAsync();

        var venta = new Venta
        {
            NumeroVenta = "V-TEST-INSUMO",
            ClienteNombre = "Cliente prueba",
            Estado = EstadoDocumento.Borrador
        };
        venta.Detalles.Add(new VentaDetalle
        {
            ProductoId = insumo.Id,
            Cantidad = 1,
            PrecioUnitario = 2m,
            CostoUnitarioSnapshot = 2m,
            Subtotal = 2m,
            UtilidadBruta = 0m,
            ProductoNombreSnapshot = insumo.Nombre,
            ProductoMarcaSnapshot = insumo.Marca,
            ProductoModeloSnapshot = insumo.Modelo
        });
        context.Ventas.Add(venta);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => context.SaveChangesAsync());
        Assert.Contains("insumo administrativo", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    // R2: el enum puede evolucionar (Servicio=3), y una fila historica o
    // corrupta nunca debe quedar autorizada para ventas solo por no ser Insumo=2.
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(99)]
    public async Task SaveChangesAsync_FailClosed_Para_Tipo_No_Vendible_Rastreado(int tipoPersistido)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);
        var producto = new Producto
        {
            Nombre = "Tipo no comercial R2",
            Marca = "Interna",
            Modelo = "Tipo-no-admitido",
            TipoInventario = (TipoInventario)tipoPersistido,
            Cantidad = 1,
            Costo = 1m,
            Precio = 1m,
            Activo = true
        };
        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        var venta = new Venta
        {
            NumeroVenta = $"R2-NON-SELLABLE-{tipoPersistido}",
            ClienteNombre = "Prueba R2",
            Estado = EstadoDocumento.Borrador
        };
        venta.Detalles.Add(new VentaDetalle
        {
            ProductoId = producto.Id,
            Cantidad = 1,
            PrecioUnitario = 1m,
            CostoUnitarioSnapshot = 1m,
            Subtotal = 1m,
            UtilidadBruta = 0m,
            ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = producto.Marca,
            ProductoModeloSnapshot = producto.Modelo
        });
        context.Ventas.Add(venta);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => context.SaveChangesAsync());
        Assert.Contains("no puede venderse ni facturarse", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.VentaDetalles.AsNoTracking().ToListAsync());
    }

    // R2: el rechazo tambien debe cubrir el producto guardado y no rastreado
    // por el DbContext que intenta incorporar el detalle de venta.
    [Theory]
    [InlineData(3)]
    [InlineData(99)]
    public async Task SaveChangesAsync_FailClosed_Para_Tipo_No_Vendible_En_Base(int tipoPersistido)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        int productoId;
        await using (var seed = new AppDbContext(options))
        {
            var producto = new Producto
            {
                Nombre = "Tipo persistido no vendible",
                Marca = "Interna",
                Modelo = "Persistido",
                TipoInventario = (TipoInventario)tipoPersistido,
                Cantidad = 1,
                Costo = 1m,
                Precio = 1m,
                Activo = true
            };
            seed.Productos.Add(producto);
            await seed.SaveChangesAsync();
            productoId = producto.Id;
        }

        await using var context = new AppDbContext(options);
        var venta = new Venta
        {
            NumeroVenta = $"R2-PERSISTED-NON-SELLABLE-{tipoPersistido}",
            ClienteNombre = "Prueba R2",
            Estado = EstadoDocumento.Borrador
        };
        venta.Detalles.Add(new VentaDetalle
        {
            ProductoId = productoId,
            Cantidad = 1,
            PrecioUnitario = 1m,
            CostoUnitarioSnapshot = 1m,
            Subtotal = 1m,
            UtilidadBruta = 0m,
            ProductoNombreSnapshot = "Tipo persistido no vendible",
            ProductoMarcaSnapshot = "Interna",
            ProductoModeloSnapshot = "Persistido"
        });
        context.Ventas.Add(venta);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => context.SaveChangesAsync());
        Assert.Contains("no puede venderse ni facturarse", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.VentaDetalles.AsNoTracking().ToListAsync());
    }
}
