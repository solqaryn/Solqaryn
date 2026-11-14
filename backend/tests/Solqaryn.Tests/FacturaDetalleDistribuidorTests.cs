using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class FacturaDetalleDistribuidorTests
{
    [Fact]
    public void Aplicar_Envio80_ImpuestoIncluido2870_Descuento20_ConciliaTotal280()
    {
        var factura = CrearFactura(
            importeBruto: 300m,
            descuento: 20m,
            impuesto: 28.70m,
            envio: 80m,
            total: 280m,
            300m);

        FacturaDetalleDistribuidor.Aplicar(factura);

        var linea = Assert.Single(factura.Detalles);
        Assert.Equal(20m, linea.Descuento);
        Assert.Equal(28.70m, linea.Impuesto);
        Assert.Equal(200m, linea.TotalLinea);
        Assert.Equal(280m, linea.TotalLinea + factura.CostoEnvio);
    }

    [Fact]
    public void Aplicar_ImpuestoAdicional_SeIncluyeEnTotalLinea()
    {
        var factura = CrearFactura(
            importeBruto: 100m,
            descuento: 0m,
            impuesto: 15m,
            envio: 0m,
            total: 115m,
            100m);

        FacturaDetalleDistribuidor.Aplicar(factura);

        var linea = Assert.Single(factura.Detalles);
        Assert.Equal(0m, linea.Descuento);
        Assert.Equal(15m, linea.Impuesto);
        Assert.Equal(115m, linea.TotalLinea);
    }

    [Fact]
    public void Aplicar_ImpuestoMixto_ConservaIncluidoYAdicionalEnSnapshotDeLinea()
    {
        var factura = CrearFactura(
            importeBruto: 115m,
            descuento: 0m,
            impuesto: 30m,
            envio: 0m,
            total: 130m,
            115m);

        FacturaDetalleDistribuidor.Aplicar(factura);

        var linea = Assert.Single(factura.Detalles);
        Assert.Equal(30m, linea.Impuesto);
        Assert.Equal(130m, linea.TotalLinea);
    }

    [Fact]
    public void Aplicar_ResiduoDeCentavo_SeAsignaDeterministicamenteALaPrimeraLineaMayor()
    {
        var factura = CrearFactura(
            importeBruto: 3m,
            descuento: 1m,
            impuesto: 0m,
            envio: 0m,
            total: 2m,
            1m, 1m, 1m);

        FacturaDetalleDistribuidor.Aplicar(factura);

        var lineas = factura.Detalles.ToList();
        Assert.Equal(0.34m, lineas[0].Descuento);
        Assert.Equal(0.33m, lineas[1].Descuento);
        Assert.Equal(0.33m, lineas[2].Descuento);
        Assert.Equal(2m, lineas.Sum(x => x.TotalLinea));
    }

    [Fact]
    public void Aplicar_SnapshotFiscalInconsistente_EsRechazado()
    {
        var factura = CrearFactura(
            importeBruto: 100m,
            descuento: 0m,
            impuesto: 5m,
            envio: 0m,
            total: 120m,
            100m);

        var error = Assert.Throws<BusinessRuleException>(() =>
            FacturaDetalleDistribuidor.Aplicar(factura));

        Assert.Contains("impuestos", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Factura CrearFactura(
        decimal importeBruto,
        decimal descuento,
        decimal impuesto,
        decimal envio,
        decimal total,
        params decimal[] subtotales)
    {
        var factura = new Factura
        {
            VentaId = 1,
            NumeroFactura = "FAC-TMP-2D",
            EmpresaNombre = "Storefront",
            ClienteNombre = "Cliente",
            VendedorUsuarioId = 1,
            VendedorNombreUsuario = "tester",
            ImporteBruto = importeBruto,
            Subtotal = importeBruto,
            Descuento = descuento,
            Impuesto = impuesto,
            CostoEnvio = envio,
            Total = total,
            SaldoPendiente = total
        };

        for (var i = 0; i < subtotales.Length; i++)
        {
            factura.Detalles.Add(new FacturaDetalle
            {
                ProductoId = i + 1,
                ProductoNombre = $"Producto {i + 1}",
                ProductoMarca = "Marca",
                ProductoModelo = "Modelo",
                Cantidad = 1,
                PrecioUnitario = subtotales[i],
                Subtotal = subtotales[i]
            });
        }

        return factura;
    }
}
