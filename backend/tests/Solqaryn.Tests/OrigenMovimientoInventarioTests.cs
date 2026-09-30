using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class OrigenMovimientoInventarioTests
{
    [Theory]
    [InlineData(TipoOrigenMovimientoInventario.Compra, 11)]
    [InlineData(TipoOrigenMovimientoInventario.Venta, 22)]
    [InlineData(TipoOrigenMovimientoInventario.ConsumoInsumo, 33)]
    [InlineData(TipoOrigenMovimientoInventario.AjusteInventario, 44)]
    [InlineData(TipoOrigenMovimientoInventario.TransferenciaInventario, 55)]
    public void Factory_CreaOrigenTipadoValido(TipoOrigenMovimientoInventario tipo, int id)
    {
        var origen = tipo switch
        {
            TipoOrigenMovimientoInventario.Compra => OrigenMovimientoInventario.DesdeCompra(id),
            TipoOrigenMovimientoInventario.Venta => OrigenMovimientoInventario.DesdeVenta(id),
            TipoOrigenMovimientoInventario.ConsumoInsumo => OrigenMovimientoInventario.DesdeConsumoInsumo(id),
            TipoOrigenMovimientoInventario.AjusteInventario => OrigenMovimientoInventario.DesdeAjusteInventario(id),
            TipoOrigenMovimientoInventario.TransferenciaInventario => OrigenMovimientoInventario.DesdeTransferenciaInventario(id),
            _ => throw new InvalidOperationException()
        };

        Assert.Equal(tipo, origen.Tipo);
        Assert.Equal(id, origen.DocumentoId);
        Assert.Equal(id, origen.Tipo switch
        {
            TipoOrigenMovimientoInventario.Compra => origen.CompraId,
            TipoOrigenMovimientoInventario.Venta => origen.VentaId,
            TipoOrigenMovimientoInventario.ConsumoInsumo => origen.ConsumoInsumoId,
            TipoOrigenMovimientoInventario.AjusteInventario => origen.AjusteInventarioId,
            TipoOrigenMovimientoInventario.TransferenciaInventario => origen.TransferenciaInventarioId,
            _ => null
        });
    }

    [Fact]
    public void DesdeIds_FallaSiNoHayOrigen()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => OrigenMovimientoInventario.DesdeIds(null, null, null, null, null));
        Assert.Contains("exactamente un origen", exception.Message);
    }

    [Fact]
    public void DesdeIds_FallaSiHayMasDeUnOrigen()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => OrigenMovimientoInventario.DesdeIds(
                compraId: 1,
                ventaId: null,
                consumoInsumoId: null,
                ajusteInventarioId: null,
                transferenciaInventarioId: 2));
        Assert.Contains("exactamente un origen", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Factory_FallaSiIdNoEsPositivo(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrigenMovimientoInventario.DesdeCompra(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrigenMovimientoInventario.DesdeTransferenciaInventario(id));
    }
}
