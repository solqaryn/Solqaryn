using System.ComponentModel.DataAnnotations;
using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioQueryValidationTests
{
    [Fact]
    public void DesdePosteriorAHasta_EsInvalido()
    {
        var dto = new ConteoInventarioQueryDto
        {
            Desde = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc),
            Hasta = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc)
        };

        var resultados = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true);

        Assert.False(valido);
        Assert.Contains(resultados, x => x.MemberNames.Contains(nameof(ConteoInventarioQueryDto.Desde)));
        Assert.Contains(resultados, x => x.MemberNames.Contains(nameof(ConteoInventarioQueryDto.Hasta)));
    }

    [Fact]
    public void RangoCronologicoValido_EsValido()
    {
        var dto = new ConteoInventarioQueryDto
        {
            Desde = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc),
            Hasta = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc)
        };

        var resultados = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true);

        Assert.True(valido);
        Assert.Empty(resultados);
    }
}
