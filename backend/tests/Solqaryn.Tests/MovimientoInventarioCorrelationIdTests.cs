using Solqaryn.Application.Common;
using Solqaryn.Domain.Common;
using Xunit;

namespace Solqaryn.Tests;

public class MovimientoInventarioCorrelationIdTests
{
    [Theory]
    [InlineData("compra")]
    [InlineData("venta")]
    [InlineData("consumo")]
    [InlineData("ajuste")]
    public void Generador_Produce_Id_Durable_Acotado_Y_Con_Origen(string origen)
    {
        var correlationId = origen switch
        {
            "compra" => MovimientoInventarioCorrelationId.NuevaCompra(123),
            "venta" => MovimientoInventarioCorrelationId.NuevaVenta(123),
            "consumo" => MovimientoInventarioCorrelationId.NuevoConsumo(123),
            "ajuste" => MovimientoInventarioCorrelationId.NuevoAjuste(123),
            _ => throw new InvalidOperationException()
        };

        Assert.StartsWith($"{origen}:123:", correlationId);
        Assert.True(correlationId.Length <= ContextoFisicoMovimientoInventario.MaxCorrelationIdLength);
        Assert.DoesNotContain(' ', correlationId);
    }

    [Fact]
    public void Generador_No_Reutiliza_CorrelationId_Entre_Transacciones()
    {
        var primero = MovimientoInventarioCorrelationId.NuevaCompra(10);
        var segundo = MovimientoInventarioCorrelationId.NuevaCompra(10);

        Assert.NotEqual(primero, segundo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Generador_Rechaza_Origen_No_Persistido(int origenId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MovimientoInventarioCorrelationId.NuevaVenta(origenId));
    }
}
