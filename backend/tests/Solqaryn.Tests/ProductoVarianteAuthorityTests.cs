using Solqaryn.Application.DTOs;
using Solqaryn.Application.Mappings;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoVarianteAuthorityTests
{
    [Fact]
    public void Mapper_Deriva_Resumen_Desde_Variantes_Y_No_Desde_Legacy()
    {
        var marca = new Marca { Id = 7, Nombre = "Marca normalizada" };
        var producto = new Producto
        {
            Id = 10,
            Nombre = "Producto",
            Marca = "Marca legacy",
            Modelo = "Modelo legacy",
            Cantidad = 999,
            Costo = 999m,
            Precio = 999m,
            UmbralStockBajo = 999,
            Activo = true,
            Variantes = new List<ProductoVariante>
            {
                new()
                {
                    Id = 1,
                    ProductoId = 10,
                    MarcaId = 7,
                    Marca = marca,
                    ColorId = 1,
                    Color = new Color { Id = 1, Nombre = "Negro" },
                    Cantidad = 2,
                    Costo = 10m,
                    Precio = 20m,
                    UmbralStockBajo = 1,
                    Activo = true
                },
                new()
                {
                    Id = 2,
                    ProductoId = 10,
                    MarcaId = 7,
                    Marca = marca,
                    ColorId = 2,
                    Color = new Color { Id = 2, Nombre = "Blanco" },
                    Cantidad = 3,
                    Costo = 20m,
                    Precio = 30m,
                    UmbralStockBajo = 2,
                    Activo = true
                }
            }
        };

        var dto = ProductoMapper.ToDto(producto);

        Assert.Equal(5, dto.Cantidad);
        Assert.Equal(16m, dto.Costo);
        Assert.Equal(20m, dto.Precio);
        Assert.Equal(20m, dto.PrecioMinimo);
        Assert.Equal(30m, dto.PrecioMaximo);
        Assert.Equal(3, dto.UmbralStockBajo);
        Assert.Equal(7, dto.MarcaId);
        Assert.Equal("Marca normalizada", dto.Marca);
        Assert.Null(dto.ColorId);
    }

    [Fact]
    public void Mapper_Variante_Tecnica_Es_Autoridad_De_Stock_Costo_Y_Precio()
    {
        var producto = new Producto
        {
            Id = 20,
            Nombre = "Simple",
            Marca = "Legacy",
            Modelo = "Legacy",
            Cantidad = 100,
            Costo = 100m,
            Precio = 100m,
            Activo = true,
            Variantes = new List<ProductoVariante>
            {
                new()
                {
                    Id = 3,
                    ProductoId = 20,
                    EsTecnica = true,
                    Cantidad = 4,
                    Costo = 12.50m,
                    Precio = 25m,
                    UmbralStockBajo = 2,
                    Activo = true
                }
            }
        };

        var dto = ProductoMapper.ToDto(producto);

        Assert.Equal(4, dto.Cantidad);
        Assert.Equal(12.50m, dto.Costo);
        Assert.Equal(25m, dto.Precio);
        Assert.Null(dto.MarcaId);
        Assert.Null(dto.ModeloId);
        Assert.Null(dto.ColorId);
        Assert.Null(dto.TallaId);
    }

    [Fact]
    public void Validator_Acepta_Variantes_Sin_Exigir_Campos_Legacy()
    {
        var dto = new CreateProductoDto
        {
            Nombre = "Con variante",
            Variantes = new List<ProductoVarianteFormularioDto>
            {
                new()
                {
                    MarcaId = 1,
                    Cantidad = 0,
                    Costo = 0m,
                    Precio = 1m,
                    UmbralStockBajo = 0,
                    Activo = true
                }
            }
        };

        var resultado = new CreateProductoValidator().Validate(dto);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void ProductoMapper_UsaExclusivamenteVarianteParaInventarioEconomiaYDimensiones()
    {
        var marca = new Marca { Id = 11, Nombre = "Marca Variante" };
        var modelo = new Modelo { Id = 12, MarcaId = 11, Nombre = "Modelo Variante" };
        var color = new Color { Id = 13, Nombre = "Negro" };
        var talla = new Talla { Id = 14, Nombre = "M" };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Producto",
            Marca = "LEGACY INCORRECTA",
            Modelo = "LEGACY INCORRECTO",
            MarcaId = 91,
            ModeloId = 92,
            ColorId = 93,
            TallaId = 94,
            Cantidad = 999,
            Costo = 999m,
            Precio = 999m,
            UmbralStockBajo = 999,
            Activo = true
        };
        producto.Variantes.Add(new ProductoVariante
        {
            Id = 2,
            ProductoId = 1,
            MarcaId = 11,
            Marca = marca,
            ModeloId = 12,
            Modelo = modelo,
            ColorId = 13,
            Color = color,
            TallaId = 14,
            Talla = talla,
            Sku = "SKU-N03",
            CodigoBarras = "123456789",
            Cantidad = 7,
            Costo = 10m,
            Precio = 20m,
            UmbralStockBajo = 2,
            Activo = true
        });

        var dto = ProductoMapper.ToDto(producto);

        Assert.Equal(7, dto.Cantidad);
        Assert.Equal(10m, dto.Costo);
        Assert.Equal(20m, dto.Precio);
        Assert.Equal(2, dto.UmbralStockBajo);
        Assert.Equal(11, dto.MarcaId);
        Assert.Equal(12, dto.ModeloId);
        Assert.Equal(13, dto.ColorId);
        Assert.Equal(14, dto.TallaId);
        Assert.Equal("Marca Variante", dto.Marca);
        Assert.Equal("Modelo Variante", dto.Modelo);
    }
}
