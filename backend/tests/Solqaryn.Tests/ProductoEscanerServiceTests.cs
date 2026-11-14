using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class ProductoEscanerServiceTests
{
    private readonly Mock<IProductoVarianteRepository> _repository = new();

    [Fact]
    public async Task ResolverParaVenta_CodigoVacio_DevuelveEntradaInvalidaSinConsultarRepositorio()
    {
        var service = new ProductoEscanerService(_repository.Object);

        var resultado = await service.ResolverParaVentaAsync("   ");

        Assert.Equal(EstadoResolucionProductoEscaner.EntradaInvalida, resultado.Estado);
        Assert.Null(resultado.Dato);
        _repository.Verify(
            x => x.BuscarPorCodigoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolverParaVenta_Sku_NormalizaMayusculasYNoExponeCosto()
    {
        string? skuRecibido = null;
        string? codigoRecibido = null;
        var variante = CrearVariante(sku: "SKU-VENTA", codigoBarras: "001122", cantidad: 5);
        _repository
            .Setup(x => x.BuscarPorCodigoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sku, codigo, _) =>
            {
                skuRecibido = sku;
                codigoRecibido = codigo;
            })
            .ReturnsAsync(new List<ProductoVariante> { variante });

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.ResolverParaVentaAsync("  sku-venta  ");

        Assert.Equal(EstadoResolucionProductoEscaner.Encontrado, resultado.Estado);
        Assert.Equal("SKU-VENTA", skuRecibido);
        Assert.Equal("sku-venta", codigoRecibido);
        Assert.Equal(variante.Id, resultado.Dato!.ProductoVarianteId);
        Assert.Equal(90m, resultado.Dato.Precio);
        Assert.DoesNotContain(
            typeof(ProductoEscaneadoVentaDto).GetProperties(),
            propiedad => propiedad.Name.Equals("Costo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ResolverParaCompra_CodigoBarras_ConservaCerosIniciales()
    {
        string? codigoRecibido = null;
        var variante = CrearVariante(sku: "SKU-COMPRA", codigoBarras: "0000123456", cantidad: 0);
        _repository
            .Setup(x => x.BuscarPorCodigoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, codigo, _) => codigoRecibido = codigo)
            .ReturnsAsync(new List<ProductoVariante> { variante });

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.ResolverParaCompraAsync(" 0000123456 ");

        Assert.Equal(EstadoResolucionProductoEscaner.Encontrado, resultado.Estado);
        Assert.Equal("0000123456", codigoRecibido);
        Assert.Equal(40m, resultado.Dato!.Costo);
        Assert.Equal(0, resultado.Dato.CantidadDisponible);
    }

    [Fact]
    public async Task Resolver_CoincidenciaDoble_DevuelveConflicto()
    {
        _repository
            .Setup(x => x.BuscarPorCodigoAsync(
                "CRUCE",
                "CRUCE",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductoVariante>
            {
                CrearVariante(1, "CRUCE", "111", 3),
                CrearVariante(2, "OTRO", "CRUCE", 4)
            });

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.ResolverParaVentaAsync("CRUCE");

        Assert.Equal(EstadoResolucionProductoEscaner.Conflicto, resultado.Estado);
        Assert.Null(resultado.Dato);
    }

    [Fact]
    public async Task ResolverParaVenta_SinStock_DevuelveNoOperativo_PeroCompraLoPermite()
    {
        var variante = CrearVariante(cantidad: 0);
        _repository
            .Setup(x => x.BuscarPorCodigoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductoVariante> { variante });

        var service = new ProductoEscanerService(_repository.Object);
        var venta = await service.ResolverParaVentaAsync(variante.Sku!);
        var compra = await service.ResolverParaCompraAsync(variante.Sku!);

        Assert.Equal(EstadoResolucionProductoEscaner.NoOperativo, venta.Estado);
        Assert.Equal(EstadoResolucionProductoEscaner.Encontrado, compra.Estado);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Resolver_ProductoOVarianteNoOperativa_DevuelveNoOperativo(
        bool productoActivo,
        bool varianteActiva,
        bool productoEliminado)
    {
        var variante = CrearVariante();
        variante.Activo = varianteActiva;
        variante.Producto.Activo = productoActivo;
        variante.Producto.Eliminado = productoEliminado;
        _repository
            .Setup(x => x.BuscarPorCodigoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductoVariante> { variante });

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.ResolverParaCompraAsync(variante.Sku!);

        Assert.Equal(EstadoResolucionProductoEscaner.NoOperativo, resultado.Estado);
    }

    [Fact]
    public async Task ResolverParaVenta_VarianteTecnica_DevuelveIdentificadoresCanonicos()
    {
        var variante = CrearVariante(id: 77, sku: "TEC-0000000042", cantidad: 8);
        variante.ProductoId = 42;
        variante.Producto.Id = 42;
        variante.EsTecnica = true;
        variante.ColorId = null;
        variante.Color = null;
        _repository
            .Setup(x => x.BuscarPorCodigoAsync(
                "TEC-0000000042",
                "TEC-0000000042",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductoVariante> { variante });

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.ResolverParaVentaAsync("TEC-0000000042");

        Assert.Equal(EstadoResolucionProductoEscaner.Encontrado, resultado.Estado);
        Assert.Equal(42, resultado.Dato!.ProductoId);
        Assert.Equal(77, resultado.Dato.ProductoVarianteId);
        Assert.True(resultado.Dato.EsVarianteTecnica);
        Assert.Null(resultado.Dato.ColorId);
    }

    [Fact]
    public async Task Resolver_CodigoInexistente_DevuelveNoEncontrado()
    {
        _repository
            .Setup(x => x.BuscarPorCodigoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductoVariante>());

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.ResolverParaVentaAsync("NO-EXISTE");

        Assert.Equal(EstadoResolucionProductoEscaner.NoEncontrado, resultado.Estado);
    }

    private static ProductoVariante CrearVariante(
        int id = 10,
        string sku = "SKU-001",
        string? codigoBarras = "000001",
        int cantidad = 5) =>
        new()
        {
            Id = id,
            ProductoId = 1,
            Producto = new Producto
            {
                Id = 1,
                Nombre = "Producto de prueba",
                Marca = "Solqaryn",
                Modelo = "2C3",
                Costo = 35m,
                Precio = 80m,
                Activo = true,
                Eliminado = false
            },
            Sku = sku,
            CodigoBarras = codigoBarras,
            Cantidad = cantidad,
            Costo = 40m,
            Precio = 90m,
            Activo = true,
            Eliminado = false
        };
}
