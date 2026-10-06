using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class ProductoAutocompleteIntegrationTests
{
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;", mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

    [Fact]
    public async Task Autocomplete_MySql84_FiltraStockVenta_PermiteStockCeroCompra_YBuscaPorCamposOperativos()
    {
        var dbName = $"test_autocomplete_2c5_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.MigrateAsync();

            var marca = new Marca
            {
                Nombre = "Storefront",
                Activo = true,
                Eliminado = false
            };
            setup.Marcas.Add(marca);
            await setup.SaveChangesAsync();

            var modeloBuds = new Modelo
            {
                MarcaId = marca.Id,
                Nombre = "2C5",
                Activo = true,
                Eliminado = false
            };
            var modeloCable = new Modelo
            {
                MarcaId = marca.Id,
                Nombre = "USB-C",
                Activo = true,
                Eliminado = false
            };
            setup.Modelos.AddRange(modeloBuds, modeloCable);
            await setup.SaveChangesAsync();

            var conStock = CrearProducto("Buds Pro Remoto", "Storefront", "2C5", 4);
            var sinStock = CrearProducto("Cable Remoto", "Storefront", "USB-C", 0);
            setup.Productos.AddRange(conStock, sinStock);
            await setup.SaveChangesAsync();

            setup.ProductoVariantes.AddRange(
                CrearVariante(conStock.Id, marca.Id, modeloBuds.Id, "SKU-BUDS-2C5", "000000002501", 4, 120m, 220m),
                CrearVariante(sinStock.Id, marca.Id, modeloCable.Id, "SKU-CABLE-2C5", "000000002502", 0, 35m, 80m));
            await setup.SaveChangesAsync();
        }

        await using var context = new AppDbContext(options);
        try
        {
            var service = new ProductoEscanerService(new ProductoVarianteRepository(context));

            var ventaPorNombre = await service.BuscarParaVentaAsync("buds pro");
            Assert.Single(ventaPorNombre);
            Assert.Equal("SKU-BUDS-2C5", ventaPorNombre[0].Sku);
            Assert.Equal(4, ventaPorNombre[0].CantidadDisponible);

            var ventaSinStock = await service.BuscarParaVentaAsync("cable remoto");
            Assert.Empty(ventaSinStock);

            var compraSinStock = await service.BuscarParaCompraAsync("sku-cable");
            Assert.Single(compraSinStock);
            Assert.Equal(0, compraSinStock[0].CantidadDisponible);
            Assert.Equal(35m, compraSinStock[0].Costo);

            var compraPorCodigoConCeros = await service.BuscarParaCompraAsync("000000002502");
            Assert.Single(compraPorCodigoConCeros);
            Assert.Equal("000000002502", compraPorCodigoConCeros[0].CodigoBarras);

            var ventaPorMarca = await service.BuscarParaVentaAsync("storefront");
            Assert.Single(ventaPorMarca);
            Assert.Equal("Storefront", ventaPorMarca[0].MarcaNombre);
            Assert.Equal("2C5", ventaPorMarca[0].ModeloNombre);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static Producto CrearProducto(string nombre, string marca, string modelo, int cantidad) =>
        new()
        {
            Nombre = nombre,
            Marca = marca,
            Modelo = modelo,
            TipoInventario = TipoInventario.MercaderiaVenta,
            Cantidad = cantidad,
            Costo = 40m,
            Precio = 80m,
            UmbralStockBajo = 2,
            Activo = true,
            Eliminado = false
        };

    private static ProductoVariante CrearVariante(
        int productoId,
        int marcaId,
        int modeloId,
        string sku,
        string codigoBarras,
        int cantidad,
        decimal costo,
        decimal precio) =>
        new()
        {
            ProductoId = productoId,
            MarcaId = marcaId,
            ModeloId = modeloId,
            Sku = sku,
            CodigoBarras = codigoBarras,
            Cantidad = cantidad,
            Costo = costo,
            Precio = precio,
            UmbralStockBajo = 2,
            Activo = true,
            Eliminado = false
        };
}