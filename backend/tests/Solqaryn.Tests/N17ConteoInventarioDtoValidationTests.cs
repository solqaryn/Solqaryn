using System.ComponentModel.DataAnnotations;
using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioDtoValidationTests
{
    [Fact]
    public void Create_ProductoVarianteIdsNulo_EsInvalido()
    {
        var dto = new CreateConteoInventarioDto
        {
            AlmacenId = 1,
            ProductoVarianteIds = null!
        };

        Assert.False(EsValido(dto));
    }

    [Fact]
    public void Update_ProductoVarianteIdsNulo_EsInvalido()
    {
        var dto = new UpdateConteoInventarioDto
        {
            AlmacenId = 1,
            ProductoVarianteIds = null!
        };

        Assert.False(EsValido(dto));
    }

    [Fact]
    public void CapturaLote_LineasNulas_EsInvalida()
    {
        var dto = new CapturarConteoInventarioLoteDto
        {
            Lineas = null!
        };

        Assert.False(EsValido(dto));
    }

    private static bool EsValido(object instance)
    {
        var resultados = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), resultados, validateAllProperties: true);
    }
}
