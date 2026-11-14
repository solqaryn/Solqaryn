using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class ProductoVarianteTecnicaLifecycleTests
{
    private readonly Mock<IProductoVarianteRepository> _variantes = new();
    private readonly Mock<IProductoRepository> _productos = new();
    private readonly Mock<ICatalogoProductoService> _catalogos = new();
    private readonly Mock<ICurrentUserService> _usuario = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    private ProductoVarianteService CrearServicio()
    {
        _usuario.SetupGet(x => x.UsuarioId).Returns(7);
        _usuario.SetupGet(x => x.NombreUsuario).Returns("pruebas");
        _variantes.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _productos.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        return new ProductoVarianteService(
            _variantes.Object,
            _productos.Object,
            _catalogos.Object,
            _usuario.Object,
            new FakeUnitOfWork(),
            _auditoria.Object);
    }

    [Fact]
    public async Task AsegurarTecnica_CreaUnaSolaVarianteConDatosDelProducto()
    {
        var producto = new Producto
        {
            Id = 42,
            Nombre = "Producto simple",
            Cantidad = 8,
            Costo = 50m,
            Precio = 90m,
            UmbralStockBajo = 2,
            Activo = true
        };
        ProductoVariante? creada = null;
        _productos.Setup(x => x.GetByIdForUpdateAsync(42)).ReturnsAsync(producto);
        _variantes.Setup(x => x.GetByProductoIdAsync(42, true))
            .ReturnsAsync(new List<ProductoVariante>());
        _variantes.Setup(x => x.GetTecnicaByProductoIdAsync(42, true))
            .ReturnsAsync((ProductoVariante?)null);
        _variantes.Setup(x => x.AddAsync(It.IsAny<ProductoVariante>()))
            .Callback<ProductoVariante>(v => { v.Id = 100; creada = v; })
            .Returns(Task.CompletedTask);

        var resultado = await CrearServicio().AsegurarTecnicaAsync(42);

        Assert.NotNull(creada);
        Assert.True(creada!.EsTecnica);
        Assert.Null(creada.ColorId);
        Assert.Equal("TEC-0000000042", creada.Sku);
        Assert.Equal(8, creada.Cantidad);
        Assert.Equal(50m, creada.Costo);
        Assert.Equal(90m, creada.Precio);
        Assert.Equal(2, creada.UmbralStockBajo);
        Assert.True(resultado.EsTecnica);
    }

    [Fact]
    public async Task AsegurarTecnica_RechazaProductoConVariantesComerciales()
    {
        var producto = new Producto { Id = 5, Nombre = "Comercial", Precio = 10m };
        _productos.Setup(x => x.GetByIdForUpdateAsync(5)).ReturnsAsync(producto);
        _variantes.Setup(x => x.GetByProductoIdAsync(5, true))
            .ReturnsAsync(new List<ProductoVariante>
            {
                new() { Id = 9, ProductoId = 5, ColorId = 2, EsTecnica = false }
            });

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => CrearServicio().AsegurarTecnicaAsync(5));

        Assert.Contains("variantes comerciales", error.Message);
    }

    [Fact]
    public async Task RetirarTecnica_RechazaConversionConStock()
    {
        var producto = new Producto { Id = 3, Nombre = "Simple", Precio = 10m };
        var tecnica = new ProductoVariante
        {
            Id = 8,
            ProductoId = 3,
            EsTecnica = true,
            Cantidad = 1
        };
        _productos.Setup(x => x.GetByIdForUpdateAsync(3)).ReturnsAsync(producto);
        _variantes.Setup(x => x.GetTecnicaByProductoIdAsync(3, false)).ReturnsAsync(tecnica);
        _variantes.Setup(x => x.GetByIdForUpdateAsync(8)).ReturnsAsync(tecnica);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => CrearServicio().RetirarTecnicaParaConversionAsync(3));

        Assert.Contains("stock a cero", error.Message);
        Assert.False(tecnica.Eliminado);
    }

    [Fact]
    public async Task EliminarManual_RechazaVarianteTecnica()
    {
        var producto = new Producto { Id = 10, Nombre = "Simple", Precio = 10m };
        var tecnica = new ProductoVariante
        {
            Id = 11,
            ProductoId = 10,
            EsTecnica = true,
            Cantidad = 0
        };
        _productos.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(producto);
        _variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(tecnica);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => CrearServicio().DeleteAsync(10, 11));

        Assert.Contains("no puede eliminarse manualmente", error.Message);
    }
}
