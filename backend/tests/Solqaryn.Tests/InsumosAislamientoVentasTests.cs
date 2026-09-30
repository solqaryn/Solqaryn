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
}
