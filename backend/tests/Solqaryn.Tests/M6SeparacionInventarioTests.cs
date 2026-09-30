using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class M6SeparacionInventarioTests
{
    [Fact]
    public async Task ProductoRepository_Separa_Mercaderia_E_Insumos_En_Conteo_Y_Valoracion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var mercaderia = new Producto
        {
            Nombre = "Cable USB-C",
            Marca = "Marca",
            Modelo = "M1",
            TipoInventario = TipoInventario.MercaderiaVenta,
            Costo = 80m,
            Precio = 150m,
            Cantidad = 3,
            Activo = true
        };
        mercaderia.Variantes.Add(new ProductoVariante
        {
            Sku = "M6-MERC-001",
            Cantidad = 3,
            Costo = 80m,
            Precio = 150m,
            Activo = true
        });

        var insumo = new Producto
        {
            Nombre = "Bolsa de empaque",
            Marca = "Interno",
            Modelo = "B1",
            TipoInventario = TipoInventario.InsumoAdministrativo,
            Costo = 5m,
            Precio = 5m,
            Cantidad = 10,
            Activo = true
        };
        insumo.Variantes.Add(new ProductoVariante
        {
            Sku = "M6-INS-001",
            Cantidad = 10,
            Costo = 5m,
            Precio = 5m,
            Activo = true
        });

        context.Productos.AddRange(mercaderia, insumo);
        await context.SaveChangesAsync();

        var repository = new ProductoRepository(context);

        Assert.Equal(1, await repository.GetTotalProductosPorTipoAsync(TipoInventario.MercaderiaVenta));
        Assert.Equal(1, await repository.GetTotalProductosPorTipoAsync(TipoInventario.InsumoAdministrativo));
        Assert.Equal(3, await repository.GetTotalUnidadesPorTipoAsync(TipoInventario.MercaderiaVenta));
        Assert.Equal(10, await repository.GetTotalUnidadesPorTipoAsync(TipoInventario.InsumoAdministrativo));
        Assert.Equal(240m, await repository.GetValorTotalCostoPorTipoAsync(TipoInventario.MercaderiaVenta));
        Assert.Equal(50m, await repository.GetValorTotalCostoPorTipoAsync(TipoInventario.InsumoAdministrativo));
        Assert.Equal(450m, await repository.GetValorTotalPrecioPorTipoAsync(TipoInventario.MercaderiaVenta));
    }
}
