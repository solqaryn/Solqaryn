using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoDomainTests
{
    [Theory]
    [InlineData(2, 5, true)]
    [InlineData(5, 5, false)]
    [InlineData(10, 5, false)]
    public void TieneStockBajo_Calcula_Correctamente(int cantidad, int umbral, bool esperado)
    {
        var producto = new Producto { Cantidad = cantidad, UmbralStockBajo = umbral };

        Assert.Equal(esperado, producto.TieneStockBajo);
    }
}
