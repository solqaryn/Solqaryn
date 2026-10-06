using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class ProductoEscanerIntegrationTests
{
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;", mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

    [Fact]
    public async Task ResolverEscaner_MySql84_CubreTecnicaCodigoConCerosConflictoYStockCero()
    {
        var dbName = $"test_escaner_2c3_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);
        int simpleId;

        await using (var setup = new AppDbContext(options))
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);

            var simple = CrearProducto("Producto simple", 6);
            var barras = CrearProducto("Producto con barras", 3);
            var cruceSku = CrearProducto("Cruce SKU", 2);
            var cruceBarras = CrearProducto("Cruce barras", 2);
            var sinStock = CrearProducto("Producto sin stock", 0);
            setup.Productos.AddRange(simple, barras, cruceSku, cruceBarras, sinStock);
            await setup.SaveChangesAsync();
            simpleId = simple.Id;

            setup.ProductoVariantes.AddRange(
                CrearVariante(simple, $"TEC-{simple.Id:D10}", null, 6, esTecnica: true),
                CrearVariante(barras, "SKU-BARRAS", "0000123456", 3),
                CrearVariante(cruceSku, "CRUCE-2C3", "111111", 2),
                CrearVariante(cruceBarras, "SKU-OTRO", "CRUCE-2C3", 2),
                CrearVariante(sinStock, "SKU-SIN-STOCK", "999999", 0));
            await setup.SaveChangesAsync();
        }

        await using var context = new AppDbContext(options);
        try
        {
            var service = new ProductoEscanerService(new ProductoVarianteRepository(context));

            var tecnica = await service.ResolverParaVentaAsync($"tec-{simpleId:D10}");
            Assert.Equal(EstadoResolucionProductoEscaner.Encontrado, tecnica.Estado);
            Assert.True(tecnica.Dato!.EsVarianteTecnica);
            Assert.Equal(6, tecnica.Dato.CantidadDisponible);

            var codigoConCeros = await service.ResolverParaCompraAsync("0000123456");
            Assert.Equal(EstadoResolucionProductoEscaner.Encontrado, codigoConCeros.Estado);
            Assert.Equal("0000123456", codigoConCeros.Dato!.CodigoBarras);
            Assert.Equal(45m, codigoConCeros.Dato.Costo);

            var conflicto = await service.ResolverParaVentaAsync("CRUCE-2C3");
            Assert.Equal(EstadoResolucionProductoEscaner.Conflicto, conflicto.Estado);

            var ventaSinStock = await service.ResolverParaVentaAsync("SKU-SIN-STOCK");
            var compraSinStock = await service.ResolverParaCompraAsync("SKU-SIN-STOCK");
            Assert.Equal(EstadoResolucionProductoEscaner.NoOperativo, ventaSinStock.Estado);
            Assert.Equal(EstadoResolucionProductoEscaner.Encontrado, compraSinStock.Estado);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static Producto CrearProducto(string nombre, int cantidad) =>
        new()
        {
            Nombre = nombre,
            Marca = "Solqaryn",
            Modelo = "2C3",
            Cantidad = cantidad,
            Costo = 40m,
            Precio = 80m,
            UmbralStockBajo = 2,
            Activo = true,
            Eliminado = false
        };

    private static ProductoVariante CrearVariante(
        Producto producto,
        string sku,
        string? codigoBarras,
        int cantidad,
        bool esTecnica = false) =>
        new()
        {
            ProductoId = producto.Id,
            Producto = producto,
            Sku = sku,
            CodigoBarras = codigoBarras,
            Cantidad = cantidad,
            Costo = 45m,
            Precio = 85m,
            UmbralStockBajo = 2,
            EsTecnica = esTecnica,
            Activo = true,
            Eliminado = false
        };
}
