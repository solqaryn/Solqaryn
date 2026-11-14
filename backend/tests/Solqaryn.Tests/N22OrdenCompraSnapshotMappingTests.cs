using System.Reflection;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N22OrdenCompraSnapshotMappingTests
{
    [Fact]
    public void Map_expone_snapshots_completos_de_variante_sin_consultar_catalogos_vivos()
    {
        var detalle = new OrdenCompraDetalle
        {
            Id = 7,
            ProductoId = 11,
            ProductoVarianteId = 19,
            ProductoSkuSnapshot = "SKU-19",
            ProductoNombreSnapshot = "Producto histórico",
            ProductoMarcaSnapshot = "Marca histórica",
            ProductoModeloSnapshot = "Modelo histórico",
            ProductoColorSnapshot = "Azul histórico",
            ProductoTallaSnapshot = "M histórica"
        };
        detalle.EstablecerValores(2m, 100m, 5m, 28.50m);

        var orden = new OrdenCompra
        {
            Id = 3,
            NumeroOrden = "OC-TEST-SNAPSHOT",
            ProveedorId = 4,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            Detalles = new List<OrdenCompraDetalle> { detalle }
        };

        var map = typeof(OrdenCompraService).GetMethod("Map", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(map);

        var dto = Assert.IsType<OrdenCompraDto>(map!.Invoke(null, new object[] { orden }));
        var mapped = Assert.Single(dto.Detalles);

        Assert.Equal("SKU-19", mapped.ProductoSkuSnapshot);
        Assert.Equal("Producto histórico", mapped.ProductoNombreSnapshot);
        Assert.Equal("Marca histórica", mapped.ProductoMarcaSnapshot);
        Assert.Equal("Modelo histórico", mapped.ProductoModeloSnapshot);
        Assert.Equal("Azul histórico", mapped.ProductoColorSnapshot);
        Assert.Equal("M histórica", mapped.ProductoTallaSnapshot);
    }
}
