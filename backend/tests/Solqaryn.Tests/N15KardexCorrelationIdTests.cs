using Solqaryn.Application.Common;
using Xunit;

namespace Solqaryn.Tests;

public class N15KardexCorrelationIdTests
{
    [Theory]
    [InlineData(17, "compra:17:confirmar")]
    [InlineData(25, "venta:25:confirmar")]
    [InlineData(9, "consumo:9:confirmar")]
    public void Confirmacion_GeneraCorrelationIdDeterministico(int id, string esperado)
    {
        var actual = esperado.StartsWith("compra:", StringComparison.Ordinal)
            ? KardexCorrelationId.CompraConfirmar(id)
            : esperado.StartsWith("venta:", StringComparison.Ordinal)
                ? KardexCorrelationId.VentaConfirmar(id)
                : KardexCorrelationId.ConsumoConfirmar(id);

        Assert.Equal(esperado, actual);
    }

    [Theory]
    [InlineData(17, "compra:17:anular")]
    [InlineData(25, "venta:25:anular")]
    [InlineData(9, "consumo:9:anular")]
    public void Anulacion_GeneraCorrelationIdDistintoYDeterministico(int id, string esperado)
    {
        var actual = esperado.StartsWith("compra:", StringComparison.Ordinal)
            ? KardexCorrelationId.CompraAnular(id)
            : esperado.StartsWith("venta:", StringComparison.Ordinal)
                ? KardexCorrelationId.VentaAnular(id)
                : KardexCorrelationId.ConsumoAnular(id);

        Assert.Equal(esperado, actual);
        Assert.EndsWith(":anular", actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DocumentoInvalido_FallaCerrado(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KardexCorrelationId.CompraConfirmar(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => KardexCorrelationId.VentaConfirmar(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => KardexCorrelationId.ConsumoConfirmar(id));
    }
}
