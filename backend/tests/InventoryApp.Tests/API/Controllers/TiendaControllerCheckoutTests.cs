using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class TiendaControllerCheckoutTests
{
    [Fact]
    public void CheckoutPublico_ConservaRutaAnonimaSinPermisosAdministrativos()
    {
        var type = typeof(TiendaController);
        Assert.NotNull(type.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("tienda", type.GetCustomAttribute<RouteAttribute>()?.Template);

        var method = type.GetMethod(nameof(TiendaController.ValidarCheckout));
        Assert.NotNull(method);
        var post = method!.GetCustomAttribute<HttpPostAttribute>();
        Assert.Equal("checkout/validar", post?.Template);
        Assert.Null(method.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public async Task ValidarCheckout_RechazaCarritoVacioSinConsultarCatalogo()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Null(response.Data);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidarCheckout_RecalculaPrecioStockYTotalDesdeServidorYAgrupaDuplicados()
    {
        var producto = new ProductoDto
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
                    Sku = "AUD-501",
                    Cantidad = 5,
                    Precio = 123.45m,
                    Activo = true
                }
            }
        };
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(501)).ReturnsAsync(producto);
        var controller = CrearController(productos);
        var inicio = DateTime.UtcNow;

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items =
            {
                new CheckoutTiendaItemRequestDto { ProductoId = 501, ModeloId = 77, ModeloNombre = "16 GB / 512 GB", MarcaNombre = "Audit", Unidades = 1 },
                new CheckoutTiendaItemRequestDto { ProductoId = 501, ModeloId = 77, ModeloNombre = "16 GB / 512 GB", MarcaNombre = "Audit", Unidades = 2 }
            }
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(ok.Value);
        Assert.True(response.Success);
        var validado = Assert.IsType<CheckoutTiendaValidadoDto>(response.Data);
        Assert.Matches("^[a-f0-9]{32}$", validado.ValidacionId);
        Assert.True(validado.ExpiraUtc > inicio.AddMinutes(9));
        Assert.True(validado.ExpiraUtc <= DateTime.UtcNow.AddMinutes(11));
        Assert.Equal(370.35m, validado.Subtotal);
        Assert.Equal(validado.Subtotal, validado.Total);

        var linea = Assert.Single(validado.Lineas);
        Assert.Equal(501, linea.ProductoId);
        Assert.Equal(77, linea.ModeloId);
        Assert.Equal("Laptop auditada", linea.Nombre);
        Assert.Equal("16 GB / 512 GB", linea.Modelo);
        Assert.Equal("AUD-501", linea.Sku);
        Assert.Equal(3, linea.Unidades);
        Assert.Equal(5, linea.StockDisponible);
        Assert.Equal(123.45m, linea.PrecioUnitario);
        Assert.Equal(370.35m, linea.Total);
        productos.Verify(service => service.GetByIdAsync(501), Times.Once);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidarCheckout_DesambiguaGruposConModeloIdNuloSinMezclarStockNiPrecio()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(700)).ReturnsAsync(new ProductoDto
        {
            Id = 700,
            Nombre = "Producto técnico",
            Activo = true,
            Variantes =
            {
                new ProductoVarianteDto
                {
                    Id = 7001,
                    ProductoId = 700,
                    ModeloId = null,
                    ModeloNombre = "Edición A",
                    MarcaNombre = "Marca A",
                    Sku = "TEC-A",
                    Cantidad = 1,
                    Precio = 100m,
                    Activo = true
                },
                new ProductoVarianteDto
                {
                    Id = 7002,
                    ProductoId = 700,
                    ModeloId = null,
                    ModeloNombre = "Edición B",
                    MarcaNombre = "Marca B",
                    Sku = "TEC-B",
                    Cantidad = 20,
                    Precio = 10m,
                    Activo = true
                }
            }
        });
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items =
            {
                new CheckoutTiendaItemRequestDto
                {
                    ProductoId = 700,
                    ModeloId = null,
                    ModeloNombre = "Edición A",
                    MarcaNombre = "Marca A",
                    Unidades = 1
                }
            }
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(ok.Value);
        var validado = Assert.IsType<CheckoutTiendaValidadoDto>(response.Data);
        var linea = Assert.Single(validado.Lineas);
        Assert.Equal(1, linea.StockDisponible);
        Assert.Equal(100m, linea.PrecioUnitario);
        Assert.Equal(100m, linea.Total);
        Assert.Equal("Edición A", linea.Modelo);
        Assert.Equal("TEC-A", linea.Sku);
        productos.Verify(service => service.GetByIdAsync(700), Times.Once);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidarCheckout_NormalizaNullYVacioAntesDeAcumularCantidadContraStock()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(701)).ReturnsAsync(new ProductoDto
        {
            Id = 701,
            Nombre = "Variante sin etiquetas",
            Activo = true,
            Variantes =
            {
                new ProductoVarianteDto
                {
                    Id = 7011,
                    ProductoId = 701,
                    ModeloId = null,
                    ModeloNombre = null,
                    MarcaNombre = null,
                    Sku = "NULL-701",
                    Cantidad = 5,
                    Precio = 50m,
                    Activo = true
                }
            }
        });
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items =
            {
                new CheckoutTiendaItemRequestDto { ProductoId = 701, ModeloId = null, ModeloNombre = null, MarcaNombre = null, Unidades = 3 },
                new CheckoutTiendaItemRequestDto { ProductoId = 701, ModeloId = null, ModeloNombre = "", MarcaNombre = "", Unidades = 3 }
            }
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(conflict.Value);
        Assert.False(response.Success);
        Assert.Contains("existencia", response.Message, StringComparison.OrdinalIgnoreCase);
        productos.Verify(service => service.GetByIdAsync(701), Times.Once);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidarCheckout_RechazaCantidadFueraDeLimiteAntesDeConsultarCatalogo()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items = { new CheckoutTiendaItemRequestDto { ProductoId = 1, ModeloId = null, Unidades = 1000 } }
        });

        Assert.IsType<BadRequestObjectResult>(result);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidarCheckout_DetieneElFlujoSiCambioLaExistencia()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(9)).ReturnsAsync(new ProductoDto
        {
            Id = 9,
            Nombre = "Producto con poco stock",
            Activo = true,
            Precio = 250m,
            PrecioMinimo = 250m,
            Cantidad = 1
        });
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items = { new CheckoutTiendaItemRequestDto { ProductoId = 9, ModeloId = null, Unidades = 2 } }
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(conflict.Value);
        Assert.False(response.Success);
        Assert.Contains("existencia", response.Message, StringComparison.OrdinalIgnoreCase);
        productos.Verify(service => service.GetByIdAsync(9), Times.Once);
        productos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidarCheckout_DetieneElFlujoSiLaVarianteYaNoExiste()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos.Setup(service => service.GetByIdAsync(12)).ReturnsAsync(new ProductoDto
        {
            Id = 12,
            Nombre = "Producto variante",
            Activo = true,
            Variantes =
            {
                new ProductoVarianteDto
                {
                    Id = 1201,
                    ProductoId = 12,
                    ModeloId = 10,
                    ModeloNombre = "Modelo vigente",
                    Sku = "VIG-10",
                    Cantidad = 4,
                    Precio = 400m,
                    Activo = true
                }
            }
        });
        var controller = CrearController(productos);

        var result = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items = { new CheckoutTiendaItemRequestDto { ProductoId = 12, ModeloId = 99, ModeloNombre = "Modelo inexistente", Unidades = 1 } }
        });

        Assert.IsType<ConflictObjectResult>(result);
        productos.Verify(service => service.GetByIdAsync(12), Times.Once);
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
}
