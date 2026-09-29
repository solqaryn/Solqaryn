using InventoryApp.API.Controllers;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class TiendaControllerDestacadosTests
{
    [Fact]
    public async Task Destacados_ConsultaSoloMarcadosActivosConLimiteSeguroYOrdenDeterminista()
    {
        var productos = new Mock<IProductoService>(MockBehavior.Strict);
        productos
            .Setup(service => service.GetPagedAsync(It.Is<ProductoPagedRequest>(request =>
                request.Page == 1
                && request.PageSize == 4
                && request.Activo == true
                && request.EsDestacado == true
                && request.UsuarioIdScope == null
                && request.SortBy == "FechaCreacion"
                && request.SortDirection == "desc")))
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Page = 1,
                PageSize = 4,
                TotalCount = 1,
                Items =
                {
                    new ProductoDto
                    {
                        Id = 501,
                        Nombre = "Laptop Real Destacada",
                        Descripcion = "Producto destacado persistido",
                        Activo = true,
                        EsDestacado = true,
                        Cantidad = 5,
                        Precio = 12345m,
                        PrecioMinimo = 12345m,
                        FechaCreacion = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc)
                    }
                }
            });

        var categorias = new Mock<ICategoriaService>(MockBehavior.Strict);
        var promociones = new Mock<IPromocionPublicaService>();
        promociones.Setup(service => service.ResolverAsync(
                It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
            .ReturnsAsync((OfertaPublicaDto?)null);
        var inventario = new Mock<IInventarioPublicoService>();
        inventario.Setup(service => service.ObtenerPorVariantesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, InventarioPublicoVarianteDto>());
        var controller = new TiendaController(productos.Object, categorias.Object, promociones.Object, inventario.Object);

        var result = await controller.GetProductosDestacados(99);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<TiendaProductoResumenDto>>>(ok.Value);
        Assert.True(response.Success);
        var producto = Assert.Single(response.Data!);
        Assert.Equal(501, producto.Id);
        Assert.True(producto.EsDestacado);
        Assert.Equal("Laptop Real Destacada", producto.Nombre);
        productos.VerifyAll();
        productos.VerifyNoOtherCalls();
        categorias.VerifyNoOtherCalls();
    }
    [Fact]
    public void SnapshotEf_ConstruyeProductoConEsDestacadoSinModeloPendiente()
    {
        var snapshotType = typeof(AppDbContext).Assembly.GetType(
            "InventoryApp.Infrastructure.Migrations.AppDbContextModelSnapshot",
            throwOnError: true)!;
        var snapshot = Activator.CreateInstance(snapshotType, nonPublic: true)!;
        var modelProperty = snapshotType.GetProperty(
            "Model",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        var model = Assert.IsAssignableFrom<IModel>(modelProperty.GetValue(snapshot));
        var producto = model.FindEntityType("InventoryApp.Domain.Entities.Producto");
        Assert.NotNull(producto);
        Assert.NotNull(producto!.FindProperty("EsDestacado"));
        Assert.NotNull(producto.FindProperty("Activo"));
        Assert.Contains(producto.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "EsDestacado", "Activo" }));

        var banco = model.FindEntityType("InventoryApp.Domain.Entities.Catalogos.Banco");
        Assert.NotNull(banco);
        Assert.Null(banco!.FindProperty("EsDestacado"));
    }
}
