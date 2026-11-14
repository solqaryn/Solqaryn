using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Domain;

public class ThreeWayMatchTests
{
    private OrdenCompra CrearOrdenCompra(int ocId, decimal cantidad, decimal precio, decimal descuento, decimal impuesto)
    {
        var oc = new OrdenCompra { Id = ocId, ProveedorId = 1, ProveedorNombreSnapshot = "Prov", NumeroOrden = "OC-1" };
        oc.EstablecerIdempotencia("key", new string('a', 64));
        var detalle = new OrdenCompraDetalle { Id = 1, OrdenCompraId = ocId, ProductoId = 1 };
        detalle.EstablecerValores(cantidad, precio, descuento, impuesto);
        oc.Detalles.Add(detalle);
        return oc;
    }

    private RecepcionCompra CrearRecepcion(int rcId, int ocDetalleId, decimal cantidadAceptada)
    {
        var rc = new RecepcionCompra { Id = rcId, OrdenCompraId = 1, NumeroRecepcion = $"RC-{rcId}" };
        rc.EstablecerIdempotencia($"key{rcId}", new string('b', 64));
        var detalle = new RecepcionCompraDetalle { Id = rcId, OrdenCompraDetalleId = ocDetalleId, ProductoId = 1, AlmacenId = 1 };
        detalle.EstablecerCantidades(cantidadAceptada);
        rc.Detalles.Add(detalle);
        rc.Confirmar(1, "Tester", DateTime.UtcNow);
        return rc;
    }

    private FacturaProveedor CrearFactura(int fpId, int ocDetalleId, decimal cantidadFacturada, decimal precio, decimal descuento, decimal impuesto)
    {
        var fp = new FacturaProveedor { Id = fpId, ProveedorId = 1, OrdenCompraId = 1, ProveedorNombreSnapshot = "Prov", NumeroFactura = $"FP-{fpId}", FechaEmisionUtc = DateTime.UtcNow };
        var detalle = new FacturaProveedorDetalle { Id = fpId, OrdenCompraDetalleId = ocDetalleId, ProductoId = 1, ProductoNombreSnapshot = "Prod" };
        detalle.EstablecerValores(cantidadFacturada, precio, descuento, impuesto);
        fp.Detalles.Add(detalle);
        fp.Registrar(1, "Tester", DateTime.UtcNow);
        return fp;
    }

    [Fact]
    public void Evaluar_CantidadesPreciosExactos_RetornaAprobado()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 15m);
        var rc1 = CrearRecepcion(1, 1, 5m);
        var rc2 = CrearRecepcion(2, 1, 5m);
        var fp1 = CrearFactura(1, 1, 6m, 100m, 6m, 9m);
        var fp2 = CrearFactura(2, 1, 4m, 100m, 4m, 6m);

        var resultado = ThreeWayMatchResult.Evaluar(oc, new[] { rc1, rc2 }, new[] { fp1, fp2 });

        Assert.Equal(ThreeWayMatchStatus.Aprobado, resultado.Estado);
        Assert.Empty(resultado.Discrepancias);
    }

    [Fact]
    public void Evaluar_DiscrepanciaCantidad_FaltaRecepcion_RetornaDiscrepancia()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 15m);
        var rc = CrearRecepcion(1, 1, 9m);
        var fp = CrearFactura(1, 1, 10m, 100m, 10m, 15m);

        var resultado = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });

        Assert.Equal(ThreeWayMatchStatus.Discrepancia, resultado.Estado);
        Assert.Single(resultado.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Cantidad);
    }

    [Fact]
    public void Evaluar_DiscrepanciaPrecio_RetornaDiscrepancia()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 0m, 0m);
        var rc = CrearRecepcion(1, 1, 10m);
        var fp = CrearFactura(1, 1, 10m, 105m, 0m, 0m);

        var resultado = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });

        Assert.Equal(ThreeWayMatchStatus.Discrepancia, resultado.Estado);
        Assert.Single(resultado.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Precio);
    }

    [Fact]
    public void Evaluar_DiscrepanciaDescuento_RetornaDiscrepancia()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 0m);
        var rc = CrearRecepcion(1, 1, 10m);
        var fp = CrearFactura(1, 1, 10m, 100m, 8m, 0m);

        var resultado = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });

        Assert.Equal(ThreeWayMatchStatus.Discrepancia, resultado.Estado);
        Assert.Single(resultado.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Descuento);
    }

    [Fact]
    public void Evaluar_DocumentoAnulado_SeIgnoraYRetornaDiscrepanciaSiFaltaCantidad()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 0m, 0m);
        var rc = CrearRecepcion(1, 1, 10m);
        var fp = CrearFactura(1, 1, 10m, 100m, 0m, 0m);
        fp.Anular(1, "Prueba", DateTime.UtcNow);

        var resultado = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });

        Assert.Equal(ThreeWayMatchStatus.Discrepancia, resultado.Estado);
        Assert.Contains(resultado.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Cantidad);
    }

    [Fact]
    public void Evaluar_FacturaConMonedaDistinta_RetornaDiscrepancia()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 0m, 0m);
        oc.Moneda = "HNL";
        var rc = CrearRecepcion(1, 1, 10m);
        var fp = CrearFactura(1, 1, 10m, 100m, 0m, 0m);
        fp.Moneda = "USD";

        var resultado = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });

        Assert.Equal(ThreeWayMatchStatus.Discrepancia, resultado.Estado);
        Assert.Contains(resultado.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Moneda);
    }

    [Fact]
    public void Evaluar_DocumentosBorrador_NoCuentanComoEvidencia()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 0m, 0m);
        var rc = new RecepcionCompra { Id = 1, OrdenCompraId = 1, NumeroRecepcion = "RC-BORRADOR" };
        rc.Detalles.Add(new RecepcionCompraDetalle { Id = 1, OrdenCompraDetalleId = 1, ProductoId = 1, AlmacenId = 1 });
        rc.Detalles.First().EstablecerCantidades(10m);
        var fp = new FacturaProveedor { Id = 1, ProveedorId = 1, OrdenCompraId = 1, ProveedorNombreSnapshot = "Prov", NumeroFactura = "FP-BORRADOR", FechaEmisionUtc = DateTime.UtcNow };
        var fd = new FacturaProveedorDetalle { Id = 1, OrdenCompraDetalleId = 1, ProductoId = 1, ProductoNombreSnapshot = "Prod" };
        fd.EstablecerValores(10m, 100m, 0m, 0m);
        fp.Detalles.Add(fd);

        var resultado = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });

        Assert.Equal(ThreeWayMatchStatus.Discrepancia, resultado.Estado);
        Assert.Contains(resultado.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Cantidad);
    }
}
