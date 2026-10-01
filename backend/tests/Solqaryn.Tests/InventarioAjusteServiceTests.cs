using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class InventarioAjusteServiceTests
{
    private readonly Mock<IAjusteInventarioService> _ajustes = new();
    private readonly InventarioAjusteService _service;

    public InventarioAjusteServiceTests()
    {
        _service = new InventarioAjusteService(_ajustes.Object);
    }

    [Fact]
    public async Task AjustarProductoAsync_DelegaEnAutoridadFormal_YConservaContratoLegacy()
    {
        var request = new AjusteStockRequest
        {
            CantidadActualEsperada = 8,
            CantidadNueva = 5,
            Motivo = "Conteo físico"
        };
        var esperado = new AjusteStockResultadoDto
        {
            ProductoId = 10,
            CantidadAnterior = 8,
            CantidadNueva = 5,
            Diferencia = -3,
            Motivo = request.Motivo
        };

        _ajustes
            .Setup(x => x.AjustarStockCompatibilidadAsync(10, null, request))
            .ReturnsAsync(esperado);

        var resultado = await _service.AjustarProductoAsync(10, request);

        Assert.Same(esperado, resultado);
        _ajustes.Verify(
            x => x.AjustarStockCompatibilidadAsync(10, null, request),
            Times.Once);
    }

    [Fact]
    public async Task AjustarVarianteAsync_DelegaVarianteEnAutoridadFormal()
    {
        var request = new AjusteStockRequest
        {
            CantidadActualEsperada = 8,
            CantidadNueva = 5,
            Motivo = "Conteo"
        };
        var esperado = new AjusteStockResultadoDto
        {
            ProductoId = 10,
            ProductoVarianteId = 4,
            CantidadAnterior = 8,
            CantidadNueva = 5,
            Diferencia = -3,
            Motivo = request.Motivo
        };

        _ajustes
            .Setup(x => x.AjustarStockCompatibilidadAsync(10, 4, request))
            .ReturnsAsync(esperado);

        var resultado = await _service.AjustarVarianteAsync(10, 4, request);

        Assert.Same(esperado, resultado);
        _ajustes.Verify(
            x => x.AjustarStockCompatibilidadAsync(10, 4, request),
            Times.Once);
    }

    [Fact]
    public async Task AjustarVarianteAsync_PropagaConflictoDeStockDeLaAutoridadFormal()
    {
        var request = new AjusteStockRequest
        {
            CantidadActualEsperada = 8,
            CantidadNueva = 5,
            Motivo = "Conteo"
        };

        _ajustes
            .Setup(x => x.AjustarStockCompatibilidadAsync(10, 4, request))
            .ThrowsAsync(new BusinessRuleException(
                "El stock actual cambió desde la lectura del cliente."));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AjustarVarianteAsync(10, 4, request));

        _ajustes.Verify(
            x => x.AjustarStockCompatibilidadAsync(10, 4, request),
            Times.Once);
    }
}
