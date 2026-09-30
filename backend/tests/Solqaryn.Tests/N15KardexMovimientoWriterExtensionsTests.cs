using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N15KardexMovimientoWriterExtensionsTests
{
    [Theory]
    [InlineData("compra", false, "compra:17:confirmar")]
    [InlineData("compra", true, "compra:17:anular")]
    [InlineData("venta", false, "venta:17:confirmar")]
    [InlineData("venta", true, "venta:17:anular")]
    [InlineData("consumo", false, "consumo:17:confirmar")]
    [InlineData("consumo", true, "consumo:17:anular")]
    public async Task OperacionTipada_PropagaOrigenYCorrelationIdEsperados(
        string modulo,
        bool anular,
        string correlationIdEsperado)
    {
        var writer = new Mock<IKardexMovimientoWriter>(MockBehavior.Strict);
        var movimiento = new MovimientoInventario { ProductoId = 3 };

        writer
            .Setup(w => w.RegistrarCorrelacionadoAsync(
                movimiento,
                It.Is<OrigenMovimientoInventario>(o => o.DocumentoId == 17),
                correlationIdEsperado))
            .Returns(Task.CompletedTask);

        if (modulo == "compra")
        {
            if (anular)
                await writer.Object.RegistrarCompraAnuladaAsync(movimiento, 17);
            else
                await writer.Object.RegistrarCompraConfirmadaAsync(movimiento, 17);
        }
        else if (modulo == "venta")
        {
            if (anular)
                await writer.Object.RegistrarVentaAnuladaAsync(movimiento, 17);
            else
                await writer.Object.RegistrarVentaConfirmadaAsync(movimiento, 17);
        }
        else
        {
            if (anular)
                await writer.Object.RegistrarConsumoAnuladoAsync(movimiento, 17);
            else
                await writer.Object.RegistrarConsumoConfirmadoAsync(movimiento, 17);
        }

        writer.VerifyAll();
    }
}
