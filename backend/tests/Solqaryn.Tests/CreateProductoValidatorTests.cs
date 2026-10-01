using Solqaryn.Application.DTOs;
using Solqaryn.Application.Validators;
using Xunit;

namespace Solqaryn.Tests;

public class CreateProductoValidatorTests
{
    private readonly CreateProductoValidator _validator = new();

    [Fact]
    public void Producto_Valido_No_Genera_Errores()
    {
        var dto = new CreateProductoDto
        {
            Nombre = "Mouse Inalámbrico",
            Marca = "Logitech",
            Modelo = "M185",
            Cantidad = 10,
            Costo = 5,
            Precio = 12,
            UmbralStockBajo = 3
        };

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Nombre_Vacio_Genera_Error()
    {
        var dto = new CreateProductoDto
        {
            Nombre = "",
            Marca = "Logitech",
            Modelo = "M185",
            Cantidad = 10,
            Costo = 5,
            Precio = 12
        };

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Nombre");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Costo_Cero_O_Negativo_Genera_Error(decimal costo)
    {
        var dto = new CreateProductoDto
        {
            Nombre = "Teclado",
            Marca = "HP",
            Modelo = "K100",
            Cantidad = 5,
            Costo = costo,
            Precio = 20
        };

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Costo");
    }

    [Fact]
    public void Cantidad_Negativa_Genera_Error()
    {
        var dto = new CreateProductoDto
        {
            Nombre = "Teclado",
            Marca = "HP",
            Modelo = "K100",
            Cantidad = -1,
            Costo = 10,
            Precio = 20
        };

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Cantidad");
    }
}
