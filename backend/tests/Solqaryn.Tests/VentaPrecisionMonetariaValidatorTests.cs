using Solqaryn.Application.DTOs;
using Solqaryn.Application.Validators;
using Xunit;

namespace Solqaryn.Tests;

public class VentaPrecisionMonetariaValidatorTests
{
    [Theory]
    [InlineData("10")]
    [InlineData("10.1")]
    [InlineData("10.01")]
    public void CreateVenta_PrecioHastaDosDecimales_EsValido(string precioTexto)
    {
        var validator = new CreateVentaValidator();
        var dto = CrearDto(decimal.Parse(precioTexto, System.Globalization.CultureInfo.InvariantCulture));

        var resultado = validator.Validate(dto);

        Assert.DoesNotContain(resultado.Errors, e => e.PropertyName.EndsWith("PrecioUnitario"));
    }

    [Fact]
    public void CreateVenta_PrecioConTresDecimales_EsRechazado()
    {
        var validator = new CreateVentaValidator();
        var dto = CrearDto(10.005m);

        var resultado = validator.Validate(dto);

        Assert.Contains(resultado.Errors, e =>
            e.PropertyName.EndsWith("PrecioUnitario") &&
            e.ErrorMessage.Contains("2 decimales", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateVenta_PrecioConTresDecimales_EsRechazado()
    {
        var validator = new UpdateVentaValidator();
        var dto = new UpdateVentaDto
        {
            Detalles = new List<VentaDetalleInputDto>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 19.999m }
            }
        };

        var resultado = validator.Validate(dto);

        Assert.Contains(resultado.Errors, e =>
            e.PropertyName.EndsWith("PrecioUnitario") &&
            e.ErrorMessage.Contains("2 decimales", StringComparison.OrdinalIgnoreCase));
    }

    private static CreateVentaDto CrearDto(decimal precio) => new()
    {
        Detalles = new List<VentaDetalleInputDto>
        {
            new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = precio }
        }
    };
}
