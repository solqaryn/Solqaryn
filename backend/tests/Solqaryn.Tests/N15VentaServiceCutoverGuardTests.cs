using Solqaryn.Application.Common;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15VentaServiceCutoverGuardTests
{
    [Fact]
    public void VentaCorrelationIdsSonDeterministicosYSeparanConfirmacionDeAnulacion()
    {
        const int ventaId = 314;

        var confirmar1 = KardexCorrelationId.VentaConfirmar(ventaId);
        var confirmar2 = KardexCorrelationId.VentaConfirmar(ventaId);
        var anular = KardexCorrelationId.VentaAnular(ventaId);

        Assert.Equal(confirmar1, confirmar2);
        Assert.Equal("venta:314:confirmar", confirmar1);
        Assert.Equal("venta:314:anular", anular);
        Assert.NotEqual(confirmar1, anular);
    }

    [Fact]
    public void VentaRegistrarRechazaWriterNulo()
    {
        Assert.Throws<ArgumentNullException>(() => new VentaKardexMovimientoRegistrar(null!));
    }
}
