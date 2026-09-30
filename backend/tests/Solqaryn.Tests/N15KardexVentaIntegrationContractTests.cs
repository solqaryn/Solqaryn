using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexVentaIntegrationContractTests
{
    [Fact]
    public void Registrar_de_venta_debe_consumir_writer_canonico_de_Kardex()
    {
        var constructor = typeof(VentaKardexMovimientoRegistrar)
            .GetConstructors()
            .Single();

        Assert.Contains(
            constructor.GetParameters(),
            parametro => parametro.ParameterType == typeof(IKardexMovimientoWriter));
    }

    [Fact]
    public async Task Confirmacion_debe_persistir_correlacion_deterministica()
    {
        var writer = new WriterSpy();
        var registrar = new VentaKardexMovimientoRegistrar(writer);
        var movimiento = new MovimientoInventario();

        await registrar.RegistrarConfirmacionAsync(31, movimiento);

        Assert.Same(movimiento, writer.Movimiento);
        Assert.Equal("venta:31:confirmar", writer.CorrelationId);
        Assert.NotNull(writer.Origen);
    }

    [Fact]
    public async Task Anulacion_debe_persistir_correlacion_deterministica()
    {
        var writer = new WriterSpy();
        var registrar = new VentaKardexMovimientoRegistrar(writer);
        var movimiento = new MovimientoInventario();

        await registrar.RegistrarAnulacionAsync(31, movimiento);

        Assert.Same(movimiento, writer.Movimiento);
        Assert.Equal("venta:31:anular", writer.CorrelationId);
        Assert.NotNull(writer.Origen);
    }

    [Fact]
    public async Task Registrar_debe_fallar_cerrado_si_venta_no_esta_persistida()
    {
        var registrar = new VentaKardexMovimientoRegistrar(new WriterSpy());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            registrar.RegistrarConfirmacionAsync(0, new MovimientoInventario()));
    }

    private sealed class WriterSpy : IKardexMovimientoWriter
    {
        public MovimientoInventario? Movimiento { get; private set; }
        public OrigenMovimientoInventario? Origen { get; private set; }
        public string? CorrelationId { get; private set; }

        public Task RegistrarCorrelacionadoAsync(
            MovimientoInventario movimiento,
            OrigenMovimientoInventario origen,
            string correlationId)
        {
            Movimiento = movimiento;
            Origen = origen;
            CorrelationId = correlationId;
            return Task.CompletedTask;
        }

        public Task RegistrarFisicoAsync(
            MovimientoInventario movimiento,
            OrigenMovimientoInventario origen,
            ContextoFisicoMovimientoInventario contexto) =>
            throw new NotSupportedException();
    }
}
