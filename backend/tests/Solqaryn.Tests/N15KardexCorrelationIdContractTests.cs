using Solqaryn.Application.Common;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexCorrelationIdContractTests
{
    [Theory]
    [InlineData("compra", 17, "confirmar", "compra:17:confirmar")]
    [InlineData("compra", 17, "anular", "compra:17:anular")]
    [InlineData("venta", 25, "confirmar", "venta:25:confirmar")]
    [InlineData("venta", 25, "anular", "venta:25:anular")]
    [InlineData("consumo", 9, "confirmar", "consumo:9:confirmar")]
    [InlineData("consumo", 9, "anular", "consumo:9:anular")]
    public void CorrelationId_es_deterministico_por_documento_y_operacion(
        string modulo,
        int documentoId,
        string operacion,
        string esperado)
    {
        var actual = (modulo, operacion) switch
        {
            ("compra", "confirmar") => KardexCorrelationId.CompraConfirmar(documentoId),
            ("compra", "anular") => KardexCorrelationId.CompraAnular(documentoId),
            ("venta", "confirmar") => KardexCorrelationId.VentaConfirmar(documentoId),
            ("venta", "anular") => KardexCorrelationId.VentaAnular(documentoId),
            ("consumo", "confirmar") => KardexCorrelationId.ConsumoConfirmar(documentoId),
            ("consumo", "anular") => KardexCorrelationId.ConsumoAnular(documentoId),
            _ => throw new InvalidOperationException("Caso de prueba no soportado.")
        };

        Assert.Equal(esperado, actual);
        Assert.DoesNotContain(' ', actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CorrelationId_rechaza_documento_no_persistido(int documentoId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KardexCorrelationId.CompraConfirmar(documentoId));
        Assert.Throws<ArgumentOutOfRangeException>(() => KardexCorrelationId.VentaConfirmar(documentoId));
        Assert.Throws<ArgumentOutOfRangeException>(() => KardexCorrelationId.ConsumoConfirmar(documentoId));
    }
}
