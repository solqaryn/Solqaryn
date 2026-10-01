using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N37NotaCreditoClienteDomainTests
{
    [Fact]
    public void CrearDesdeFactura_Elegible_PreservaAutoridadFacturaYVenta()
    {
        var factura = CrearFactura(EstadoFactura.Emitida, total: 1000m);

        var nota = NotaCreditoCliente.CrearDesdeFactura(factura, 250m, "Ajuste comercial", " observación ");

        Assert.Equal(factura.Id, nota.FacturaId);
        Assert.Same(factura, nota.Factura);
        Assert.Equal(factura.VentaId, nota.VentaId);
        Assert.Equal("HNL", nota.Moneda);
        Assert.Equal(250m, nota.MontoCredito);
        Assert.Equal("Ajuste comercial", nota.Motivo);
        Assert.Equal("observación", nota.Observaciones);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
        Assert.Equal(0m, factura.TotalPagado);
    }

    [Theory]
    [InlineData(EstadoFactura.Borrador)]
    [InlineData(EstadoFactura.Anulada)]
    [InlineData(EstadoFactura.Cancelada)]
    public void CrearDesdeFactura_NoElegible_FallaCerrado(EstadoFactura estado)
    {
        var factura = CrearFactura(estado, 1000m);
        Assert.Throws<InvalidOperationException>(() =>
            NotaCreditoCliente.CrearDesdeFactura(factura, 100m, "motivo"));
    }

    [Fact]
    public void CrearDesdeFactura_MontoMayorQueFactura_FallaCerrado()
    {
        var factura = CrearFactura(EstadoFactura.Pagada, 500m);
        Assert.Throws<InvalidOperationException>(() =>
            NotaCreditoCliente.CrearDesdeFactura(factura, 500.01m, "motivo"));
    }

    [Fact]
    public void CrearDesdeFactura_FacturaNoPersistidaOFueraDeVenta_FallaCerrado()
    {
        var sinId = CrearFactura(EstadoFactura.Emitida, 500m);
        sinId.Id = 0;
        Assert.Throws<InvalidOperationException>(() => NotaCreditoCliente.CrearDesdeFactura(sinId, 100m, "motivo"));

        var sinVenta = CrearFactura(EstadoFactura.Emitida, 500m);
        sinVenta.VentaId = 0;
        Assert.Throws<InvalidOperationException>(() => NotaCreditoCliente.CrearDesdeFactura(sinVenta, 100m, "motivo"));
    }

    [Fact]
    public void CrearDesdeFactura_NoMutaFacturaNiMaterializaEfectosFisicosOFinancieros()
    {
        var factura = CrearFactura(EstadoFactura.ParcialmentePagada, 1000m);
        factura.TotalPagado = 200m;
        factura.SaldoPendiente = 800m;

        var nota = NotaCreditoCliente.CrearDesdeFactura(factura, 150m, "Crédito comercial");

        Assert.Equal(150m, nota.MontoCredito);
        Assert.Equal(EstadoFactura.ParcialmentePagada, factura.Estado);
        Assert.Equal(200m, factura.TotalPagado);
        Assert.Equal(800m, factura.SaldoPendiente);
    }

    private static Factura CrearFactura(EstadoFactura estado, decimal total) => new()
    {
        Id = 700,
        VentaId = 600,
        NumeroFactura = "F-700",
        Estado = estado,
        Moneda = "HNL",
        ClienteNombre = "Cliente",
        EmpresaNombre = "Storefront",
        Total = total,
        TotalPagado = 0m,
        SaldoPendiente = total
    };
}
