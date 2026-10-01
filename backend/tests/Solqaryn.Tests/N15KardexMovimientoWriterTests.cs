using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N15KardexMovimientoWriterTests
{
    [Fact]
    public async Task RegistrarCorrelacionado_DelegaSinInventarContextoFisico()
    {
        var repository = new Mock<IMovimientoInventarioRepository>(MockBehavior.Strict);
        var origen = OrigenMovimientoInventario.DesdeCompra(41);
        MovimientoInventario? persistido = null;

        repository
            .Setup(r => r.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), origen))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((movimiento, _) => persistido = movimiento)
            .Returns(Task.CompletedTask);

        var writer = new KardexMovimientoWriter(repository.Object);
        var movimiento = new MovimientoInventario
        {
            ProductoId = 7,
            Tipo = TipoMovimientoInventario.Entrada,
            Cantidad = 3,
            StockAnterior = 4,
            StockNuevo = 7
        };

        await writer.RegistrarCorrelacionadoAsync(
            movimiento,
            origen,
            KardexCorrelationId.CompraConfirmar(41));

        Assert.Same(movimiento, persistido);
        Assert.Equal("compra:41:confirmar", movimiento.CorrelationId);
        Assert.Null(movimiento.AlmacenId);
        Assert.Null(movimiento.UbicacionAlmacenId);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RegistrarFisico_PropagaClaveFisicaYCorrelacion()
    {
        var repository = new Mock<IMovimientoInventarioRepository>(MockBehavior.Strict);
        var origen = OrigenMovimientoInventario.DesdeVenta(52);
        MovimientoInventario? persistido = null;

        repository
            .Setup(r => r.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), origen))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((movimiento, _) => persistido = movimiento)
            .Returns(Task.CompletedTask);

        var writer = new KardexMovimientoWriter(repository.Object);
        var movimiento = new MovimientoInventario
        {
            ProductoId = 8,
            Tipo = TipoMovimientoInventario.Salida,
            Cantidad = 1,
            StockAnterior = 5,
            StockNuevo = 4
        };
        var contexto = ContextoFisicoMovimientoInventario.Crear(9, 2, 6, "venta:52:confirmar");

        await writer.RegistrarFisicoAsync(movimiento, origen, contexto);

        Assert.Same(movimiento, persistido);
        Assert.Equal(9, movimiento.ProductoVarianteId);
        Assert.Equal(2, movimiento.AlmacenId);
        Assert.Equal(6, movimiento.UbicacionAlmacenId);
        Assert.Equal("venta:52:confirmar", movimiento.CorrelationId);
        repository.VerifyAll();
    }
}
