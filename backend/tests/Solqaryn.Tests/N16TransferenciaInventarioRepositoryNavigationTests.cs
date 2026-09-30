using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioRepositoryNavigationTests
{
    [Fact]
    public async Task GetById_CargaProductoVarianteDeCadaDetalle()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n16-transferencia-nav-{Guid.NewGuid():N}")
            .Options;

        await using var context = new AppDbContext(options);
        var producto = new Producto
        {
            Id = 44,
            Nombre = "Producto N1.6",
            Marca = "Marca snapshot",
            Modelo = "Modelo snapshot",
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = 7
        };
        var variante = new ProductoVariante
        {
            Id = 91,
            ProductoId = producto.Id,
            Producto = producto,
            Sku = "SKU-N16",
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = 7
        };
        var origen = new Almacen
        {
            Id = 1,
            SucursalId = 1,
            Codigo = "ALM-N16-O",
            Nombre = "Origen N1.6",
            Tipo = TipoAlmacen.Bodega,
            Activo = true,
            CreadoPorUsuarioId = 7
        };
        var destino = new Almacen
        {
            Id = 2,
            SucursalId = 1,
            Codigo = "ALM-N16-D",
            Nombre = "Destino N1.6",
            Tipo = TipoAlmacen.Bodega,
            Activo = true,
            CreadoPorUsuarioId = 7
        };
        var transferencia = new TransferenciaInventario
        {
            Id = 31,
            Numero = "TRF-NAV-001",
            AlmacenOrigenId = origen.Id,
            AlmacenOrigen = origen,
            AlmacenDestinoId = destino.Id,
            AlmacenDestino = destino,
            CreadoPorUsuarioId = 7,
            Detalles = new List<TransferenciaInventarioDetalle>
            {
                new()
                {
                    Id = 301,
                    ProductoVarianteId = variante.Id,
                    ProductoVariante = variante,
                    CreadoPorUsuarioId = 7
                }
            }
        };

        context.Set<Producto>().Add(producto);
        context.Set<ProductoVariante>().Add(variante);
        context.Set<Almacen>().AddRange(origen, destino);
        context.Set<TransferenciaInventario>().Add(transferencia);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new TransferenciaInventarioRepository(context);
        var encontrada = await repository.GetByIdAsync(transferencia.Id);

        Assert.NotNull(encontrada);
        Assert.Equal("Origen N1.6", encontrada!.AlmacenOrigen.Nombre);
        Assert.Equal("Destino N1.6", encontrada.AlmacenDestino.Nombre);
        var detalle = Assert.Single(encontrada.Detalles);
        Assert.NotNull(detalle.ProductoVariante);
        Assert.Equal(44, detalle.ProductoVariante.ProductoId);
        Assert.Equal("SKU-N16", detalle.ProductoVariante.Sku);
    }
}
