using System.Reflection;
using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoDtoNullabilityContractTests
{
    [Theory]
    [InlineData(typeof(CreateProductoDto), nameof(CreateProductoDto.Marca))]
    [InlineData(typeof(CreateProductoDto), nameof(CreateProductoDto.Modelo))]
    [InlineData(typeof(UpdateProductoDto), nameof(UpdateProductoDto.Marca))]
    [InlineData(typeof(UpdateProductoDto), nameof(UpdateProductoDto.Modelo))]
    public void CamposLegadosMarcaModelo_SonOpcionalesParaModelBinding(Type dtoType, string propertyName)
    {
        var property = dtoType.GetProperty(propertyName);

        Assert.NotNull(property);

        var nullability = new NullabilityInfoContext().Create(property!);
        Assert.Equal(NullabilityState.Nullable, nullability.WriteState);
    }

    [Fact]
    public void CreateProductoDto_PermiteOmitirModeloSinInventarValorLegado()
    {
        var dto = new CreateProductoDto
        {
            Nombre = "UAT Producto 001",
            MarcaId = 1,
            ModeloId = null
        };

        Assert.Null(dto.Modelo);
        Assert.Null(dto.ModeloId);
    }
}
