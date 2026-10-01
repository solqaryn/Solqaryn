using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaKardexMovimientoRegistrarTests
{
    [Fact]
    public async Task Despacho_EmiteSalidaFisicaConOrigenTransferenciaYCorrelationDeterminista()
    {
        var transferencia = CrearTransferencia();
        var writer = new FakeWriter();
        var registrar = new TransferenciaKardexMovimientoRegistrar(writer);
        var clave = new InventarioExistenciaClave(91, 10, 101);
        var transicion = new TransferenciaInventarioTransitoTransicion(clave, 12, 7, 0, 0, -5);

        await registrar.RegistrarDespachoAsync(transferencia, new[] { transicion }, 7, "tester");

        var registro = Assert.Single(writer.Registros);
        Assert.Equal(TipoMovimientoInventario.Salida, registro.Movimiento.Tipo);
        Assert.Equal(CausaMovimientoInventario.TransferenciaDespacho, registro.Movimiento.Causa);
        Assert.Equal(5, registro.Movimiento.Cantidad);
        Assert.Equal(12, registro.Movimiento.StockAnterior);
        Assert.Equal(7, registro.Movimiento.StockNuevo);
        Assert.Equal(transferencia.Id, registro.Origen.TransferenciaInventarioId);
        Assert.Equal("transferencia:31:despachar", registro.Contexto.CorrelationId);
        Assert.Equal(10, registro.Contexto.AlmacenId);
        Assert.Equal(101, registro.Contexto.UbicacionAlmacenId);
    }

    [Fact]
    public async Task Recepcion_EmiteEntradaSoloPorCantidadFisicamenteRecibida()
    {
        var transferencia = CrearTransferencia();
        var writer = new FakeWriter();
        var registrar = new TransferenciaKardexMovimientoRegistrar(writer);
        var clave = new InventarioExistenciaClave(91, 20, 202);
        var transicion = new TransferenciaInventarioTransitoTransicion(clave, 3, 7, 5, 0, 4);

        await registrar.RegistrarRecepcionAsync(transferencia, new[] { transicion }, 8, "receptor");

        var registro = Assert.Single(writer.Registros);
        Assert.Equal(TipoMovimientoInventario.Entrada, registro.Movimiento.Tipo);
        Assert.Equal(CausaMovimientoInventario.TransferenciaRecepcion, registro.Movimiento.Causa);
        Assert.Equal(4, registro.Movimiento.Cantidad);
        Assert.Equal("transferencia:31:recibir", registro.Contexto.CorrelationId);
        Assert.Equal(20, registro.Contexto.AlmacenId);
        Assert.Equal(202, registro.Contexto.UbicacionAlmacenId);
    }

    private static TransferenciaInventario CrearTransferencia()
    {
        var variante = new ProductoVariante
        {
            Id = 91,
            ProductoId = 44,
            Sku = "SKU-TRF",
            Costo = 12.50m,
            Activo = true
        };
        return new TransferenciaInventario
        {
            Id = 31,
            Numero = "TRF-000031",
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
    }

    private sealed class FakeWriter : IKardexMovimientoWriter
    {
        public List<Registro> Registros { get; } = new();

        public Task RegistrarCorrelacionadoAsync(
            MovimientoInventario movimiento,
            OrigenMovimientoInventario origen,
            string correlationId)
        {
            throw new NotSupportedException();
        }

        public Task RegistrarFisicoAsync(
            MovimientoInventario movimiento,
            OrigenMovimientoInventario origen,
            ContextoFisicoMovimientoInventario contexto)
        {
            Registros.Add(new Registro(movimiento, origen, contexto));
            return Task.CompletedTask;
        }
    }

    private sealed record Registro(
        MovimientoInventario Movimiento,
        OrigenMovimientoInventario Origen,
        ContextoFisicoMovimientoInventario Contexto);
}
