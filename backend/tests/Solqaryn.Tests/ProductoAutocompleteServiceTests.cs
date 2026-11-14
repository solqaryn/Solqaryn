using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class ProductoAutocompleteServiceTests
{
    private readonly Mock<IProductoVarianteRepository> _repository = new();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public async Task BuscarParaVenta_TerminoMenorADosCaracteres_RechazaSinConsultarRepositorio(string termino)
    {
        var service = new ProductoEscanerService(_repository.Object);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.BuscarParaVentaAsync(termino));
        _repository.Verify(x => x.BuscarPorTerminoAsync(
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<TipoInventario?>()), Times.Never);
    }

    [Fact]
    public async Task BuscarParaVenta_NormalizaTermino_LimitaResultados_ExigeStockYNoExponeCosto()
    {
        string? terminoRecibido = null;
        bool? soloConStockRecibido = null;
        int? limiteRecibido = null;
        TipoInventario? tipoRecibido = null;
        var variante = CrearVariante(cantidad: 7);

        _repository.Setup(x => x.BuscarPorTerminoAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<TipoInventario?>()))
            .Callback<string, bool, int, CancellationToken, TipoInventario?>((termino, soloConStock, limite, _, tipo) =>
            { terminoRecibido = termino; soloConStockRecibido = soloConStock; limiteRecibido = limite; tipoRecibido = tipo; })
            .ReturnsAsync(new List<ProductoVariante> { variante });

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.BuscarParaVentaAsync("  BuDs PRO  ", 999);

        Assert.Equal("buds pro", terminoRecibido);
        Assert.True(soloConStockRecibido);
        Assert.Equal(30, limiteRecibido);
        Assert.Equal(TipoInventario.MercaderiaVenta, tipoRecibido);
        Assert.Single(resultado);
        Assert.Equal(variante.Id, resultado[0].ProductoVarianteId);
        Assert.Equal(95m, resultado[0].Precio);
        Assert.DoesNotContain(typeof(ProductoEscaneadoVentaDto).GetProperties(), p => p.Name.Equals("Costo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuscarParaCompra_PermiteStockCero_DevuelveCostoYRespetaLimiteInferior()
    {
        bool? soloConStockRecibido = null;
        int? limiteRecibido = null;
        var variante = CrearVariante(cantidad: 0);

        _repository.Setup(x => x.BuscarPorTerminoAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<TipoInventario?>()))
            .Callback<string, bool, int, CancellationToken, TipoInventario?>((_, soloConStock, limite, _, _) =>
            { soloConStockRecibido = soloConStock; limiteRecibido = limite; })
            .ReturnsAsync(new List<ProductoVariante> { variante });

        var service = new ProductoEscanerService(_repository.Object);
        var resultado = await service.BuscarParaCompraAsync("sku", 0);

        Assert.False(soloConStockRecibido);
        Assert.Equal(1, limiteRecibido);
        Assert.Single(resultado);
        Assert.Equal(45m, resultado[0].Costo);
        Assert.Equal(0, resultado[0].CantidadDisponible);
    }

    [Fact]
    public async Task Buscar_TerminoMayorACienCaracteres_Rechaza()
    {
        var service = new ProductoEscanerService(_repository.Object);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.BuscarParaCompraAsync(new string('x', 101)));
    }

    private static ProductoVariante CrearVariante(int cantidad) => new()
    {
        Id = 31, ProductoId = 12,
        Producto = new Producto { Id = 12, Nombre = "Buds Pro", Marca = "Storefront", Modelo = "3", TipoInventario = TipoInventario.MercaderiaVenta, Costo = 40m, Precio = 90m, Activo = true, Eliminado = false },
        Sku = "SKU-BUDS-NEGRO", CodigoBarras = "000123456789", Cantidad = cantidad, Costo = 45m, Precio = 95m, Activo = true, Eliminado = false
    };
}
