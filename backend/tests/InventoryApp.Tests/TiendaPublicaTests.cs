using InventoryApp.API.Controllers;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class TiendaPublicaTests
{
    [Fact]
    public void Controller_EsIndependienteYPermiteConsultaAnonima()
    {
        var route = typeof(TiendaController).GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>().Single();
        var allowAnonymous = typeof(TiendaController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault();

        Assert.Equal("tienda", route.Template);
        Assert.NotNull(allowAnonymous);
        AssertEndpoint(nameof(TiendaController.GetProductos), "productos");
        AssertEndpoint(nameof(TiendaController.GetProducto), "productos/{slug}");
        AssertEndpoint(nameof(TiendaController.GetCategorias), "categorias");
        AssertEndpoint(nameof(TiendaController.GetCategoria), "categorias/{slug}");
    }

    [Fact]
    public async Task GetProductos_OfertaVigenteYStockBajo_SeProyectanEnProductoYVariante()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetPagedAsync(It.IsAny<PagedRequest>())).ReturnsAsync(new PagedResult<ProductoDto>
        {
            Items = new List<ProductoDto>
            {
                new()
                {
                    Id = 31, Nombre = "Oferta publica", Activo = true, CategoriaId = 4,
                    Precio = 1000, PrecioMinimo = 1000, Cantidad = 2, TieneStockBajo = true,
                    Variantes = new List<ProductoVarianteDto>
                    {
                        new() { Id = 301, ProductoId = 31, Activo = true, Precio = 1000, Cantidad = 2, TieneStockBajo = true }
                    }
                }
            },
            Page = 1, PageSize = 48, TotalCount = 1
        });
        var promociones = new Mock<IPromocionPublicaService>();
        promociones.Setup(x => x.ResolverAsync(31, 4, 1000m, It.IsAny<DateTime>()))
            .ReturnsAsync(new OfertaPublicaDto
            {
                PrecioNormal = 1000m, PrecioOferta = 800m, Ahorro = 200m,
                PorcentajeAhorro = 20m, Nombre = "Promo septiembre"
            });

        var controller = CrearController(productos: productos, promociones: promociones);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetProductos(new ProductoPagedRequest { PageSize = 48 }));
        var response = Assert.IsType<ApiResponse<PagedResult<TiendaProductoResumenDto>>>(ok.Value);
        var producto = Assert.Single(response.Data!.Items);
        var variante = Assert.Single(producto.Modelos);

        Assert.True(producto.OfertaActiva);
        Assert.Equal(800m, producto.PrecioOferta);
        Assert.Equal(200m, producto.Ahorro);
        Assert.Equal("Últimas unidades", producto.EstadoDisponibilidad);
        Assert.True(variante.OfertaActiva);
        Assert.Equal(800m, variante.PrecioOferta);
        Assert.Equal("Últimas unidades", variante.EstadoDisponibilidad);
    }

    [Fact]
    public async Task ValidarCheckout_UsaOfertaVigenteYStockCeroBloquea()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetByIdAsync(41)).ReturnsAsync(new ProductoDto
        {
            Id = 41, Nombre = "Con oferta", Activo = true, CategoriaId = 5,
            Variantes = new List<ProductoVarianteDto>
            {
                new() { Id = 401, ProductoId = 41, Activo = true, Precio = 500m, Cantidad = 2, ModeloId = 7, ModeloNombre = "M" }
            }
        });
        productos.Setup(x => x.GetByIdAsync(42)).ReturnsAsync(new ProductoDto
        {
            Id = 42, Nombre = "Agotado", Activo = true,
            Variantes = new List<ProductoVarianteDto>
            {
                new() { Id = 402, ProductoId = 42, Activo = true, Precio = 700m, Cantidad = 0, ModeloId = 8, ModeloNombre = "Z" }
            }
        });
        var promociones = new Mock<IPromocionPublicaService>();
        promociones.Setup(x => x.ResolverAsync(41, 5, 500m, It.IsAny<DateTime>()))
            .ReturnsAsync(new OfertaPublicaDto { PrecioNormal = 500m, PrecioOferta = 400m, Ahorro = 100m, PorcentajeAhorro = 20m, Nombre = "Promo" });

        var controller = CrearController(productos: productos, promociones: promociones);
        var ok = Assert.IsType<OkObjectResult>(await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items = new List<CheckoutTiendaItemRequestDto>
            {
                new() { ProductoId = 41, ProductoVarianteId = 401, ModeloId = 7, ModeloNombre = "M", Unidades = 1 }
            }
        }));
        var validado = Assert.IsType<ApiResponse<CheckoutTiendaValidadoDto>>(ok.Value).Data!;
        Assert.Equal(400m, Assert.Single(validado.Lineas).PrecioUnitario);
        Assert.Equal(400m, validado.Total);

        var conflicto = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items = new List<CheckoutTiendaItemRequestDto>
            {
                new() { ProductoId = 42, ProductoVarianteId = 402, ModeloId = 8, ModeloNombre = "Z", Unidades = 1 }
            }
        });
        Assert.IsType<ConflictObjectResult>(conflicto);
    }

    [Fact]
    public async Task ValidarCheckout_MismaVarianteConMetadatosDistintos_NoPuedeSuperarStockTotal()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetByIdAsync(61)).ReturnsAsync(new ProductoDto
        {
            Id = 61, Nombre = "Variante autoritativa", Activo = true,
            Variantes = new List<ProductoVarianteDto>
            {
                new() { Id = 601, ProductoId = 61, Activo = true, Precio = 100m, Cantidad = 99, ModeloId = 4, ModeloNombre = "Real" }
            }
        });

        var inventario = new Mock<IInventarioPublicoService>();
        inventario.Setup(x => x.ObtenerPorVariantesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, InventarioPublicoVarianteDto>
            {
                [601] = new() { ProductoVarianteId = 601, CantidadDisponible = 5, TieneFuenteAutoritativa = true }
            });

        var controller = CrearController(productos: productos, inventario: inventario);
        var resultado = await controller.ValidarCheckout(new ValidarCheckoutTiendaDto
        {
            Items = new List<CheckoutTiendaItemRequestDto>
            {
                new() { ProductoId = 61, ProductoVarianteId = 601, ModeloId = 4, ModeloNombre = "Real", MarcaNombre = "A", Unidades = 3 },
                new() { ProductoId = 61, ProductoVarianteId = 601, ModeloId = 999, ModeloNombre = "Manipulado", MarcaNombre = "B", Unidades = 3 }
            }
        });

        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public void PublicSlug_EsLegibleYResuelvePorIdEstable()
    {
        Assert.Equal("cafe-especial-14-27", PublicSlug.Create("Café Especial 14\"", 27));
        Assert.True(PublicSlug.TryGetId("nombre-anterior-27", out var id));
        Assert.Equal(27, id);
        Assert.False(PublicSlug.TryGetId("sin-id", out _));
        Assert.False(PublicSlug.TryGetId("producto-0", out _));
    }

    [Fact]
    public async Task GetProductos_ExponeSoloLaProyeccionComercialActiva()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetPagedAsync(It.IsAny<PagedRequest>()))
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Items = new List<ProductoDto>
                {
                    new()
                    {
                        Id = 7,
                        Nombre = "Producto público",
                        Activo = true,
                        CategoriaId = 3,
                        CategoriaNombre = "Electrónica",
                        Precio = 1200,
                        Costo = 600,
                        Cantidad = 103,
                        FechaCreacion = new DateTime(2026, 1, 2),
                        CreadoPorNombreUsuario = "dato-reservado",
                        Variantes = new List<ProductoVarianteDto>
                        {
                            new() { Activo = true, Sku = "PUB-001", Cantidad = 3, Precio = 1200 },
                            new() { Activo = false, Sku = "NO-PUBLICO", Cantidad = 100, Precio = 1 }
                        }
                    },
                    new() { Id = 8, Nombre = "Producto inactivo", Activo = false }
                },
                Page = 1,
                PageSize = 48,
                TotalCount = 1
            });

        var controller = CrearController(productos: productos);
        var result = await controller.GetProductos(new ProductoPagedRequest { PageSize = 48 });
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<TiendaProductoResumenDto>>>(ok.Value);
        var producto = Assert.Single(response.Data!.Items);

        Assert.Equal(7, producto.Id);
        Assert.Equal("producto-publico-7", producto.Slug);
        Assert.Equal(3, producto.CategoriaId);
        Assert.Equal("PUB-001", Assert.Single(producto.Modelos).Sku);
        Assert.Equal(1200, producto.Precio);
        Assert.Null(producto.PrecioOferta);
        Assert.False(producto.EsDestacado);
        Assert.Equal(3, producto.CantidadDisponible);
        Assert.Null(typeof(TiendaProductoResumenDto).GetProperty("Imagenes"));
        Assert.Null(typeof(TiendaProductoVarianteResumenDto).GetProperty("Imagenes"));
    }

    [Fact]
    public async Task GetProducto_ResuelveSlugConIdYDevuelveSlugCanonico()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetByIdAsync(21)).ReturnsAsync(new ProductoDto
        {
            Id = 21,
            Nombre = "Cámara Wi-Fi",
            Activo = true,
            Cantidad = 2,
            Precio = 899
        });

        var controller = CrearController(productos: productos);
        var result = await controller.GetProducto("nombre-viejo-21");
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ProductoCatalogoPublicoDto>>(ok.Value);

        Assert.Equal("camara-wi-fi-21", response.Data!.Slug);
        Assert.Equal(21, response.Data.Id);
    }

    [Fact]
    public async Task GetProducto_NoExponeInactivoONoValido()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(new ProductoDto
        {
            Id = 9,
            Nombre = "Oculto",
            Activo = false
        });
        var controller = CrearController(productos: productos);

        Assert.IsType<NotFoundObjectResult>(await controller.GetProducto("oculto-9"));
        Assert.IsType<NotFoundObjectResult>(await controller.GetProducto("slug-invalido"));
    }

    [Fact]
    public async Task GetCategorias_ExponeSoloContratoPublicoActivoSinInventarConteo()
    {
        var categorias = new Mock<ICategoriaService>();
        categorias.Setup(x => x.GetActivasAsync()).ReturnsAsync(new List<CategoriaDto>
        {
            // El servicio de activas no garantiza que este conteo se haya calculado.
            new() { Id = 4, Nombre = "Audio y Vídeo", Descripcion = "Entretenimiento", Activa = true, TotalProductos = 8 },
            new() { Id = 5, Nombre = "Oculta", Activa = false, TotalProductos = 99 }
        });

        var controller = CrearController(categorias: categorias);
        var result = await controller.GetCategorias();
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<CategoriaCatalogoPublicoDto>>>(ok.Value);
        var categoria = Assert.Single(response.Data!);

        Assert.Equal(4, categoria.Id);
        Assert.Equal("audio-y-video-4", categoria.Slug);
        Assert.Null(categoria.TotalProductos);
    }

    [Fact]
    public async Task GetCategoria_ResuelvePorSlugYBloqueaInactiva()
    {
        var categorias = new Mock<ICategoriaService>();
        categorias.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(new CategoriaDto
        {
            Id = 4,
            Nombre = "Computadoras",
            Activa = true,
            TotalProductos = 5
        });
        categorias.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(new CategoriaDto
        {
            Id = 8,
            Nombre = "Interna",
            Activa = false
        });
        var controller = CrearController(categorias: categorias);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetCategoria("computadoras-4"));
        var response = Assert.IsType<ApiResponse<CategoriaCatalogoPublicoDto>>(ok.Value);
        Assert.Equal("computadoras-4", response.Data!.Slug);
        Assert.Null(response.Data.TotalProductos);
        Assert.IsType<NotFoundObjectResult>(await controller.GetCategoria("interna-8"));
    }

    [Fact]
    public async Task StockAutoritativo_CeroBloqueaAunqueCantidadLegacySeaPositiva()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetPagedAsync(It.IsAny<PagedRequest>())).ReturnsAsync(new PagedResult<ProductoDto>
        {
            Items = new List<ProductoDto>
            {
                new()
                {
                    Id = 55, Nombre = "Legacy con stock fantasma", Activo = true,
                    Variantes = new List<ProductoVarianteDto>
                    {
                        new() { Id = 550, ProductoId = 55, Activo = true, Precio = 100m, Cantidad = 9 }
                    }
                }
            },
            Page = 1, PageSize = 48, TotalCount = 1
        });
        var inventario = new Mock<IInventarioPublicoService>();
        inventario.Setup(x => x.ObtenerPorVariantesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, InventarioPublicoVarianteDto>
            {
                [550] = new() { ProductoVarianteId = 550, CantidadDisponible = 0, EstaAgotada = true, TieneFuenteAutoritativa = false }
            });

        var controller = CrearController(productos: productos, inventario: inventario);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetProductos(new ProductoPagedRequest { PageSize = 48 }));
        var response = Assert.IsType<ApiResponse<PagedResult<TiendaProductoResumenDto>>>(ok.Value);
        var variante = Assert.Single(Assert.Single(response.Data!.Items).Modelos);

        Assert.Equal(0, variante.CantidadDisponible);
        Assert.True(variante.EstaAgotado);
        Assert.Equal("Agotado", variante.EstadoDisponibilidad);
    }

    private static TiendaController CrearController(
        Mock<IProductoService>? productos = null,
        Mock<ICategoriaService>? categorias = null,
        Mock<IPromocionPublicaService>? promociones = null,
        Mock<IInventarioPublicoService>? inventario = null)
    {
        if (promociones is null)
        {
            promociones = new Mock<IPromocionPublicaService>();
            promociones.Setup(x => x.ResolverAsync(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
                .ReturnsAsync((OfertaPublicaDto?)null);
        }

        if (inventario is null)
        {
            inventario = new Mock<IInventarioPublicoService>();
            inventario.Setup(x => x.ObtenerPorVariantesAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, InventarioPublicoVarianteDto>());
        }

        return new TiendaController(
            (productos ?? new Mock<IProductoService>()).Object,
            (categorias ?? new Mock<ICategoriaService>()).Object,
            promociones.Object,
            inventario.Object);
    }

    private static void AssertEndpoint(string metodo, string plantilla)
    {
        var endpoint = typeof(TiendaController).GetMethod(metodo);
        Assert.NotNull(endpoint);
        Assert.Equal(plantilla, endpoint!.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>().Single().Template);
    }
}
