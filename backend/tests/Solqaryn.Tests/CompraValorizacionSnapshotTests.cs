using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class CompraValorizacionSnapshotTests
{
    [Fact]
    public async Task SaveChanges_Confirmacion_CapturaSnapshotsOriginalesYNuevos()
    {
        await using var context = CrearContexto();
        var (producto, variante, compra, detalle) = await SembrarBorradorAsync(context);

        variante.Cantidad = 15;
        variante.Costo = 13.33m;
        producto.Cantidad = 15;
        producto.Costo = 13.33m;
        compra.Estado = EstadoDocumento.Confirmada;

        await context.SaveChangesAsync();

        Assert.Equal(10, detalle.StockProductoAnteriorSnapshot);
        Assert.Equal(10m, detalle.CostoProductoAnteriorSnapshot);
        Assert.Equal(15, detalle.StockProductoNuevoSnapshot);
        Assert.Equal(13.33m, detalle.CostoProductoNuevoSnapshot);
        Assert.Equal(10, detalle.StockVarianteAnteriorSnapshot);
        Assert.Equal(10m, detalle.CostoVarianteAnteriorSnapshot);
        Assert.Equal(15, detalle.StockVarianteNuevoSnapshot);
        Assert.Equal(13.33m, detalle.CostoVarianteNuevoSnapshot);
    }

    [Fact]
    public async Task SaveChanges_Anulacion_RestauraVarianteAfectadaYRecalculaProductoConOtraVarianteActual()
    {
        await using var context = CrearContexto();
        var (producto, variante, compra, _) = await SembrarBorradorAsync(context);

        variante.Cantidad = 15;
        variante.Costo = 13.33m;
        producto.Cantidad = 15;
        producto.Costo = 13.33m;
        compra.Estado = EstadoDocumento.Confirmada;
        await context.SaveChangesAsync();

        var otra = new ProductoVariante
        {
            ProductoId = producto.Id,
            ColorId = 2,
            Sku = "OTRA",
            Cantidad = 4,
            Costo = 20m,
            Precio = 25m,
            Activo = true
        };
        context.ProductoVariantes.Add(otra);
        producto.Cantidad = 19;
        producto.Costo = Math.Round(((13.33m * 15) + (20m * 4)) / 19m, 2, MidpointRounding.AwayFromZero);
        await context.SaveChangesAsync();

        // Simula la deducción que CompraService realiza antes del SaveChanges final.
        variante.Cantidad -= 5;
        producto.Cantidad -= 5;
        compra.Estado = EstadoDocumento.Anulada;
        await context.SaveChangesAsync();

        Assert.Equal(10, variante.Cantidad);
        Assert.Equal(10m, variante.Costo);
        Assert.Equal(4, otra.Cantidad);
        Assert.Equal(20m, otra.Costo);
        Assert.Equal(14, producto.Cantidad);
        Assert.Equal(12.86m, producto.Costo);
    }

    [Fact]
    public async Task SaveChanges_Anulacion_RestauraCostoAnteriorNuloDeVariante()
    {
        await using var context = CrearContexto();
        var (producto, variante, compra, detalle) = await SembrarBorradorAsync(context);
        variante.Costo = null;
        producto.Costo = 0m;
        await context.SaveChangesAsync();

        variante.Cantidad = 15;
        variante.Costo = 6.67m;
        producto.Cantidad = 15;
        producto.Costo = 6.67m;
        compra.Estado = EstadoDocumento.Confirmada;
        await context.SaveChangesAsync();

        Assert.Null(detalle.CostoVarianteAnteriorSnapshot);
        Assert.Equal(6.67m, detalle.CostoVarianteNuevoSnapshot);

        variante.Cantidad -= 5;
        producto.Cantidad -= 5;
        compra.Estado = EstadoDocumento.Anulada;
        await context.SaveChangesAsync();

        Assert.Equal(10, variante.Cantidad);
        Assert.Null(variante.Costo);
        Assert.Equal(10, producto.Cantidad);
        Assert.Equal(0m, producto.Costo);
    }

    [Fact]
    public async Task SaveChanges_AnulacionHistoricaSinSnapshots_EsBloqueada()
    {
        await using var context = CrearContexto();
        var producto = CrearProducto();
        var variante = CrearVariante(producto);
        var compra = new Compra
        {
            NumeroCompra = "COM-HIST-001",
            ProveedorNombre = "Proveedor histórico",
            Estado = EstadoDocumento.Confirmada,
            MetodoPago = MetodoPago.Efectivo,
            EstadoPago = EstadoPago.Pendiente
        };
        compra.Detalles.Add(new CompraDetalle
        {
            Producto = producto,
            ProductoId = producto.Id,
            ProductoVariante = variante,
            ProductoVarianteId = variante.Id,
            Cantidad = 5,
            CostoUnitario = 15m,
            Subtotal = 75m,
            ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = producto.Marca,
            ProductoModeloSnapshot = producto.Modelo
        });

        context.AddRange(producto, variante, compra);
        await context.SaveChangesAsync();

        compra.Estado = EstadoDocumento.Anulada;

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => context.SaveChangesAsync());
        Assert.Contains("snapshots", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(Producto Producto, ProductoVariante Variante, Compra Compra, CompraDetalle Detalle)>
        SembrarBorradorAsync(AppDbContext context)
    {
        var producto = CrearProducto();
        var variante = CrearVariante(producto);
        var compra = new Compra
        {
            NumeroCompra = "COM-SNAP-001",
            ProveedorNombre = "Proveedor",
            Estado = EstadoDocumento.Borrador,
            MetodoPago = MetodoPago.Efectivo,
            EstadoPago = EstadoPago.Pendiente
        };
        var detalle = new CompraDetalle
        {
            Producto = producto,
            ProductoId = producto.Id,
            ProductoVariante = variante,
            ProductoVarianteId = variante.Id,
            Cantidad = 5,
            CostoUnitario = 20m,
            Subtotal = 100m,
            ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = producto.Marca,
            ProductoModeloSnapshot = producto.Modelo,
            ProductoSkuSnapshot = variante.Sku
        };
        compra.Detalles.Add(detalle);

        context.AddRange(producto, variante, compra);
        await context.SaveChangesAsync();
        return (producto, variante, compra, detalle);
    }

    private static Producto CrearProducto() => new()
    {
        Nombre = "Producto snapshot",
        Marca = "Marca",
        Modelo = "Modelo",
        Cantidad = 10,
        Costo = 10m,
        Precio = 20m,
        Activo = true
    };

    private static ProductoVariante CrearVariante(Producto producto) => new()
    {
        Producto = producto,
        ProductoId = producto.Id,
        ColorId = 1,
        Sku = "SNAP-001",
        Cantidad = 10,
        Costo = 10m,
        Precio = 20m,
        Activo = true
    };

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"compra-snapshots-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
