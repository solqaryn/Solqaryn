using Solqaryn.Application.DTOs;
using Solqaryn.Application.Validators;
using Solqaryn.Tests.TestHelpers;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoImagenesValidatorTests
{
    private readonly CreateProductoValidator _validator = new();

    private static CreateProductoDto DtoBase(List<FakeFormFile>? imagenes = null) => new()
    {
        Nombre = "Teclado mecánico",
        Marca = "Redragon",
        Modelo = "K552",
        Cantidad = 10,
        Costo = 20,
        Precio = 45,
        Imagenes = imagenes?.Cast<Microsoft.AspNetCore.Http.IFormFile>().ToList()
    };

    [Fact]
    public void Producto_Con_Hasta_5_Imagenes_Es_Valido()
    {
        var imagenes = Enumerable.Range(1, 5)
            .Select(i => new FakeFormFile($"foto{i}.jpg", "image/jpeg", 1024))
            .ToList();

        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Producto_Con_Mas_De_5_Imagenes_Es_Rechazado()
    {
        var imagenes = Enumerable.Range(1, 6)
            .Select(i => new FakeFormFile($"foto{i}.jpg", "image/jpeg", 1024))
            .ToList();

        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Imagenes");
    }

    [Fact]
    public void Imagen_Con_Tipo_Invalido_Es_Rechazada()
    {
        var imagenes = new List<FakeFormFile> { new("archivo.pdf", "application/pdf", 1024) };

        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Imagen_Mayor_A_10Mb_Es_Rechazada()
    {
        var imagenes = new List<FakeFormFile>
        {
            new("foto.jpg", "image/jpeg", ImagenValidationHelper.MaxBytesPorImagen + 1)
        };

        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.False(resultado.IsValid);
    }
}
