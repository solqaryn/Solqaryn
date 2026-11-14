using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N15KardexCorrelationWriterTests
{
    [Fact]
    public async Task WriterCorrelacionado_PersisteCorrelationId_Normalizado_SinInventarContextoFisico()
    {
        var repository = new Mock<IMovimientoInventarioRepository>(MockBehavior.Strict);
        MovimientoInventario? persistido = null;
        var origen = OrigenMovimientoInventario.DesdeCompra(17);

        repository
            .Setup(r => r.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), origen))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((movimiento, _) => persistido = movimiento)
            .Returns(Task.CompletedTask);

        var movimiento = new MovimientoInventario
        {
            ProductoId = 11,
            Tipo = TipoMovimientoInventario.Entrada,
            Cantidad = 2,
            StockAnterior = 3,
            StockNuevo = 5,
            Descripcion = "Compra correlacionada"
        };

        await repository.Object.AddConOrigenTipadoCorrelacionadoAsync(
            movimiento,
            origen,
            "  compra:17:confirmar:abc-123  ");

        Assert.Same(movimiento, persistido);
        Assert.Equal("compra:17:confirmar:abc-123", movimiento.CorrelationId);
        Assert.Null(movimiento.AlmacenId);
        Assert.Null(movimiento.UbicacionAlmacenId);
        repository.VerifyAll();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("compra/17")]
    [InlineData("venta 25")]
    public async Task WriterCorrelacionado_RechazaCorrelationIdInvalido(string correlationId)
    {
        var repository = new Mock<IMovimientoInventarioRepository>(MockBehavior.Strict);
        var movimiento = new MovimientoInventario { ProductoId = 1 };
        var origen = OrigenMovimientoInventario.DesdeVenta(25);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repository.Object.AddConOrigenTipadoCorrelacionadoAsync(movimiento, origen, correlationId));

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WriterCorrelacionado_RechazaCorrelationIdMayorAlContrato()
    {
        var repository = new Mock<IMovimientoInventarioRepository>(MockBehavior.Strict);
        var movimiento = new MovimientoInventario { ProductoId = 1 };
        var origen = OrigenMovimientoInventario.DesdeConsumoInsumo(9);
        var correlationId = new string('a', ContextoFisicoMovimientoInventario.MaxCorrelationIdLength + 1);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repository.Object.AddConOrigenTipadoCorrelacionadoAsync(movimiento, origen, correlationId));

        repository.VerifyNoOtherCalls();
    }
}
