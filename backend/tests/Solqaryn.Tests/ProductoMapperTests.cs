using Solqaryn.Application.Mappings;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoMapperTests
{
    [Fact]
    public void ToDto_ProductoSimpleConVarianteTecnica_ConservaDimensionesNormalizadasFamiliares()
    {
        var marca = new Marca { Id = 10, Nombre = "Marca simple" };
        var modelo = new Modelo { Id = 20, Nombre = "Modelo simple", MarcaId = marca.Id, Marca = marca };
        var color = new Color { Id = 30, Nombre = "Negro", CodigoVisual = "#000000" };
        var talla = new Talla { Id = 40, Nombre = "M" };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Producto simple",
            MarcaId = marca.Id,
            MarcaCatalogo = marca,
            ModeloId = modelo.Id,
            ModeloCatalogo = modelo,
            ColorId = color.Id,
            ColorCatalogo = color,
            TallaId = talla.Id,
            TallaCatalogo = talla,
            Cantidad = 4,
            Costo = 100m,
            Precio = 160m,
            UmbralStockBajo = 1,
            Variantes = new List<ProductoVariante>
            {
                new()
                {
                    Id = 100,
                    ProductoId = 1,
                    EsTecnica = true,
                    Cantidad = 4,
                    Costo = 100m,
                    Precio = 160m,
                    UmbralStockBajo = 1
                }
            }
        };

        var dto = ProductoMapper.ToDto(producto);

        Assert.Equal(marca.Id, dto.MarcaId);
        Assert.Equal(modelo.Id, dto.ModeloId);
        Assert.Equal(color.Id, dto.ColorId);
        Assert.Equal(talla.Id, dto.TallaId);
        Assert.Equal(marca.Nombre, dto.MarcaNombre);
        Assert.Equal(modelo.Nombre, dto.ModeloNombre);
        Assert.Equal(color.Nombre, dto.ColorNombre);
        Assert.Equal(talla.Nombre, dto.TallaNombre);
        Assert.Equal(4, dto.Cantidad);
        Assert.Single(dto.Variantes);
        Assert.True(dto.Variantes[0].EsTecnica);
    }

    [Fact]
    public void ToDto_ProductoConVariantesComerciales_UsaDimensionesDeLasVariantes()
    {
        var marcaFamilia = new Marca { Id = 10, Nombre = "Marca familia" };
        var marcaVariante = new Marca { Id = 11, Nombre = "Marca variante" };
        var colorFamilia = new Color { Id = 30, Nombre = "Negro" };
        var colorVariante = new Color { Id = 31, Nombre = "Azul" };
        var producto = new Producto
        {
            Id = 2,
            Nombre = "Producto con variante",
            MarcaId = marcaFamilia.Id,
            MarcaCatalogo = marcaFamilia,
            ColorId = colorFamilia.Id,
            ColorCatalogo = colorFamilia,
            Variantes = new List<ProductoVariante>
            {
                new()
                {
                    Id = 200,
                    ProductoId = 2,
                    EsTecnica = false,
                    MarcaId = marcaVariante.Id,
                    Marca = marcaVariante,
                    ColorId = colorVariante.Id,
                    Color = colorVariante,
                    Cantidad = 2,
                    Costo = 50m,
                    Precio = 80m
                }
            }
        };

        var dto = ProductoMapper.ToDto(producto);

        Assert.Equal(marcaVariante.Id, dto.MarcaId);
        Assert.Equal(colorVariante.Id, dto.ColorId);
        Assert.Equal(marcaVariante.Nombre, dto.MarcaNombre);
        Assert.Equal(colorVariante.Nombre, dto.ColorNombre);
    }
}
