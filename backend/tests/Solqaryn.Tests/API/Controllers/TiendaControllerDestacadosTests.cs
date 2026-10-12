using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class TiendaControllerDestacadosTests
{
    [Fact]
    public async Task Destacados_UsaContratoLigeroDedicado()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        var categorias = new Mock<ICategoriaService>(MockBehavior.Strict);
        var promociones = new Mock<IPromocionPublicaService>(MockBehavior.Strict);
        var inventario = new Mock<IInventarioPublicoService>(MockBehavior.Strict);
        var catalogo = new Mock<ICatalogoPublicoService>(MockBehavior.Strict);
        catalogo.Setup(service => service.ObtenerDestacadosAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TiendaProductoResumenDto>
            {
                new()
                {
                    Id = 501,
                    Slug = "laptop-real-destacada-501",
                    Nombre = "Laptop Real Destacada",
                    EsDestacado = true,
                    Precio = 12345m,
                    CantidadDisponible = 5,
                    ImagenPrincipalUrl = "https://cdn.example/producto.jpg"
                }
            });

        var controller = new TiendaController(
            productos.Object,
            categorias.Object,
            promociones.Object,
            inventario.Object,
            catalogo.Object,
            new Mock<ITiendaBootstrapService>().Object);

        var result = await controller.GetProductosDestacados(99);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<TiendaProductoResumenDto>>>(ok.Value);
        var producto = Assert.Single(response.Data!);
        Assert.True(response.Success);
        Assert.Equal(501, producto.Id);
        Assert.True(producto.EsDestacado);
        Assert.Equal("Laptop Real Destacada", producto.Nombre);
        Assert.Null(typeof(TiendaProductoResumenDto).GetProperty("Imagenes"));
        catalogo.VerifyAll();
        productos.VerifyNoOtherCalls();
        categorias.VerifyNoOtherCalls();
        promociones.VerifyNoOtherCalls();
        inventario.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task Bootstrap_UsaCasoDeUsoDedicadoSinConsultarControladorPorPartes()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        var categorias = new Mock<ICategoriaService>(MockBehavior.Strict);
        var promociones = new Mock<IPromocionPublicaService>(MockBehavior.Strict);
        var inventario = new Mock<IInventarioPublicoService>(MockBehavior.Strict);
        var catalogo = new Mock<ICatalogoPublicoService>(MockBehavior.Strict);
        var bootstrap = new Mock<ITiendaBootstrapService>(MockBehavior.Strict);
        bootstrap.Setup(service => service.ObtenerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TiendaBootstrapDto
            {
                Identidad = new TiendaIdentidadPublicaDto { NombreComercial = "Tienda", Moneda = "HNL" },
                Destacados = { new TiendaProductoResumenDto { Id = 1, Slug = "producto-1", Nombre = "Producto", Precio = 10m } }
            });

        var controller = new TiendaController(
            productos.Object,
            categorias.Object,
            promociones.Object,
            inventario.Object,
            catalogo.Object,
            bootstrap.Object);

        var result = await controller.GetBootstrap();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TiendaBootstrapDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Tienda", response.Data!.Identidad.NombreComercial);
        Assert.Single(response.Data.Destacados);
        bootstrap.VerifyAll();
        productos.VerifyNoOtherCalls();
        categorias.VerifyNoOtherCalls();
        promociones.VerifyNoOtherCalls();
        inventario.VerifyNoOtherCalls();
        catalogo.VerifyNoOtherCalls();
    }

    [Fact]
    public void ModeloOracleEf_ConstruyeProductoConEsDestacado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=phase7_model_only;User=root;SslMode=Disabled;")
            .Options;

        using var context = new AppDbContext(options);
        var model = context.GetService<IDesignTimeModel>().Model;

        var producto = model.FindEntityType("Solqaryn.Domain.Entities.Producto");
        Assert.NotNull(producto);
        Assert.NotNull(producto!.FindProperty("EsDestacado"));
        Assert.NotNull(producto.FindProperty("Activo"));
        Assert.Contains(producto.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "EsDestacado", "Activo" }));

        var banco = model.FindEntityType("Solqaryn.Domain.Entities.Catalogos.Banco");
        Assert.NotNull(banco);
        Assert.Null(banco!.FindProperty("EsDestacado"));
    }
}
