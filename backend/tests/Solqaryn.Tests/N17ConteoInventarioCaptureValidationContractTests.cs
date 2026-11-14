using System.ComponentModel.DataAnnotations;
using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioCaptureValidationContractTests
{
    [Fact]
    public void CapturaIndividual_RechazaCantidadNegativa()
    {
        var dto = new CapturarConteoInventarioDetalleDto { CantidadContada = -1 };

        Assert.False(EsValido(dto));
    }

    [Fact]
    public void CapturaLote_RechazaDetalleNoValidoYCantidadNegativa()
    {
        var linea = new CapturaConteoInventarioLineaDto
        {
            DetalleId = 0,
            CantidadContada = -1
        };

        var resultados = Validar(linea);

        Assert.Equal(2, resultados.Count);
        Assert.Contains(resultados, x => x.MemberNames.Contains(nameof(CapturaConteoInventarioLineaDto.DetalleId)));
        Assert.Contains(resultados, x => x.MemberNames.Contains(nameof(CapturaConteoInventarioLineaDto.CantidadContada)));
    }

    [Fact]
    public void Capturas_AdmitenCeroComoConteoFisicoValido()
    {
        var detalle = new CapturarConteoInventarioDetalleDto { CantidadContada = 0 };
        var linea = new CapturaConteoInventarioLineaDto { DetalleId = 1, CantidadContada = 0 };

        Assert.True(EsValido(detalle));
        Assert.True(EsValido(linea));
    }

    private static bool EsValido(object value) => Validar(value).Count == 0;

    private static List<ValidationResult> Validar(object value)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), resultados, validateAllProperties: true);
        return resultados;
    }
}
