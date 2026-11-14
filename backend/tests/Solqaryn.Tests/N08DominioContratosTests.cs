using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N08DominioContratosTests
{
    [Fact]
    public void Compra_Expone_Contrato_Relacional_De_MetodoPago_Sin_Romper_Legacy()
    {
        var compra = new Compra
        {
            MetodoPago = MetodoPago.Transferencia,
            MetodoPagoId = 7
        };

        Assert.Equal(MetodoPago.Transferencia, compra.MetodoPago);
        Assert.Equal(7, compra.MetodoPagoId);
    }

    [Fact]
    public void MovimientoInventario_Resuelve_Un_Origen_Tipado_Unico()
    {
        var movimiento = new MovimientoInventario
        {
            CompraId = 25,
            ReferenciaTipo = "snapshot-legacy-no-autoritativo",
            ReferenciaId = 999
        };

        var origen = movimiento.OrigenTipado;

        Assert.NotNull(origen);
        Assert.Equal(TipoOrigenMovimientoInventario.Compra, origen!.Tipo);
        Assert.Equal(25, origen.DocumentoId);
        Assert.Equal(25, origen.CompraId);
        Assert.Null(origen.VentaId);
        Assert.Null(origen.ConsumoInsumoId);
        Assert.Null(origen.AjusteInventarioId);
    }

    [Fact]
    public void MovimientoInventario_Rechaza_Mas_De_Un_Origen_Tipado()
    {
        var movimiento = new MovimientoInventario
        {
            CompraId = 1,
            VentaId = 2
        };

        Assert.Throws<InvalidOperationException>(() => _ = movimiento.OrigenTipado);
    }

    [Fact]
    public void MovimientoInventario_Sin_Fk_Tipada_Conserva_Compatibilidad_Transitoria()
    {
        var movimiento = new MovimientoInventario
        {
            ReferenciaTipo = "Compra",
            ReferenciaId = 10
        };

        Assert.Null(movimiento.OrigenTipado);
        Assert.Equal("Compra", movimiento.ReferenciaTipo);
        Assert.Equal(10, movimiento.ReferenciaId);
    }
}
