using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaKardexCancelacionTests
{
    [Fact]
    public async Task CancelacionEnTransito_EmiteEntradaEnOrigenConCorrelationPropia()
    {
        var variante = new ProductoVariante { Id = 91, ProductoId = 44, Sku = "SKU-CAN", Costo = 9.75m, Activo = true };
        var transferencia = new TransferenciaInventario
        {
            Id = 31,
            Numero = "TRF-CAN-31",
            AlmacenOrigenId = 10,
            AlmacenDestinoId = 20,
            Detalles = new List<TransferenciaInventarioDetalle>
            {
                new()
                {
                    Id = 301,
                    ProductoVarianteId = 91,
                    ProductoVariante = variante,
                    UbicacionOrigenId = 101,
                    UbicacionDestinoId = 202,
                    ProductoSkuSnapshot = variante.Sku,
                    CreadoPorUsuarioId = 7
                }
            }
        };
        var writer = new FakeWriter();
        var registrar = new TransferenciaKardexMovimientoRegistrar(writer);
        var transiciones = new[]
        {
            new TransferenciaInventarioTransitoTransicion(new InventarioExistenciaClave(91, 10, 101), 7, 12, 0, 0, 5),
            new TransferenciaInventarioTransitoTransicion(new InventarioExistenciaClave(91, 20, 202), 3, 3, 5, 0, 0)
        };

        await registrar.RegistrarCancelacionAsync(transferencia, transiciones, 7, "tester");

        var registro = Assert.Single(writer.Registros);
        Assert.Equal(TipoMovimientoInventario.Entrada, registro.Movimiento.Tipo);
        Assert.Equal(CausaMovimientoInventario.TransferenciaCancelacion, registro.Movimiento.Causa);
        Assert.Equal(5, registro.Movimiento.Cantidad);
        Assert.Equal(7, registro.Movimiento.StockAnterior);
        Assert.Equal(12, registro.Movimiento.StockNuevo);
        Assert.Equal(31, registro.Origen.TransferenciaInventarioId);
        Assert.Equal("transferencia:31:cancelar", registro.Contexto.CorrelationId);
        Assert.Equal(10, registro.Contexto.AlmacenId);
        Assert.Equal(101, registro.Contexto.UbicacionAlmacenId);
    }

    private sealed class FakeWriter : IKardexMovimientoWriter
    {
        public List<Registro> Registros { get; } = new();

        public Task RegistrarCorrelacionadoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen, string correlationId) =>
            throw new NotSupportedException();

        public Task RegistrarFisicoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen, ContextoFisicoMovimientoInventario contexto)
        {
            Registros.Add(new Registro(movimiento, origen, contexto));
            return Task.CompletedTask;
        }
    }

    private sealed record Registro(MovimientoInventario Movimiento, OrigenMovimientoInventario Origen, ContextoFisicoMovimientoInventario Contexto);
}
