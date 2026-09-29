using InventoryApp.API.Controllers;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class TiendaControllerExactVariantTests
{
    [Fact]
    public async Task ValidarCheckout_ProductoVarianteIdSeleccionaUnaExistenciaFisicaExacta()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(501)).ReturnsAsync(CrearProductoConVariantesAmbiguas());
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items =
            {
                new CheckoutTiendaItemRequestDto
                {
                    ProductoId = 501,
                    ProductoVarianteId = 9002,
                    ModeloId = 77,
                    ModeloNombre = "16 GB / 512 GB",
                    MarcaNombre = "Audit",
                    Unidades = 2
                }
            }
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(ok.Value);
        var validado = Assert.IsType<CheckoutTiendaValidadoDto>(response.Data);
        var linea = Assert.Single(validado.Lineas);
        Assert.Equal(9002, linea.ProductoVarianteId);
        Assert.Equal(77, linea.ModeloId);
        Assert.Equal("AUD-B", linea.Sku);
        Assert.Equal(7, linea.StockDisponible);
        Assert.Equal(200m, linea.PrecioUnitario);
        Assert.Equal(400m, linea.Total);
        productos.Verify(service => service.GetByIdAsync(501), Times.Once);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidarCheckout_SinProductoVarianteIdRechazaSeleccionFisicaAmbigua()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(501)).ReturnsAsync(CrearProductoConVariantesAmbiguas());
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items =
            {
                new CheckoutTiendaItemRequestDto
                {
                    ProductoId = 501,
                    ModeloId = 77,
                    ModeloNombre = "16 GB / 512 GB",
                    MarcaNombre = "Audit",
                    Unidades = 1
                }
            }
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(conflict.Value);
        Assert.False(response.Success);
        Assert.Contains("variante exacta", response.Message, StringComparison.OrdinalIgnoreCase);
        productos.Verify(service => service.GetByIdAsync(501), Times.Once);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProducto_ExponeCadaVarianteFisicaConIdentidadExacta()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(501)).ReturnsAsync(CrearProductoConVariantesAmbiguas());
        var controller = CrearController(productos);

        var result = await controller.GetProducto("laptop-auditada-501");

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ProductoCatalogoPublicoDto>>(ok.Value);
        var producto = Assert.IsType<ProductoCatalogoPublicoDto>(response.Data);
        Assert.Collection(
            producto.Modelos.OrderBy(modelo => modelo.ProductoVarianteId),
            modelo =>
            {
                Assert.Equal(9001, modelo.ProductoVarianteId);
                Assert.Equal("AUD-A", modelo.Sku);
                Assert.Equal(3, modelo.CantidadDisponible);
                Assert.Equal(100m, modelo.Precio);
            },
            modelo =>
            {
                Assert.Equal(9002, modelo.ProductoVarianteId);
                Assert.Equal("AUD-B", modelo.Sku);
                Assert.Equal(7, modelo.CantidadDisponible);
                Assert.Equal(200m, modelo.Precio);
            });
        productos.Verify(service => service.GetByIdAsync(501), Times.Once);
        productos.VerifyNoOtherCalls();
    }

    private static TiendaController CrearController(Mock<IProductoService> productos)
    {
        var categorias = new Mock<ICategoriaService>(MockBehavior.Strict);
        var promociones = new Mock<IPromocionPublicaService>();
        promociones.Setup(service => service.ResolverAsync(
                It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
            .ReturnsAsync((OfertaPublicaDto?)null);
        var inventario = new Mock<IInventarioPublicoService>();
        inventario.Setup(service => service.ObtenerPorVariantesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, InventarioPublicoVarianteDto>());
        return new TiendaController(
            productos.Object,
            categorias.Object,
            promociones.Object,
            inventario.Object,
            new Mock<ICatalogoPublicoService>().Object);
    }

    private static ProductoDto CrearProductoConVariantesAmbiguas() => new()
    {
        Id = 501,
        Nombre = "Laptop auditada",
        Activo = true,
        Variantes =
        {
            new ProductoVarianteDto
            {
                Id = 9001,
                ProductoId = 501,
                ModeloId = 77,
                ModeloNombre = "16 GB / 512 GB",
                MarcaNombre = "Audit",
                Sku = "AUD-A",
                Cantidad = 3,
                Precio = 100m,
                Activo = true
            },
            new ProductoVarianteDto
            {
                Id = 9002,
                ProductoId = 501,
                ModeloId = 77,
                ModeloNombre = "16 GB / 512 GB",
                MarcaNombre = "Audit",
                Sku = "AUD-B",
                Cantidad = 7,
                Precio = 200m,
                Activo = true
            }
        }
    };
}
