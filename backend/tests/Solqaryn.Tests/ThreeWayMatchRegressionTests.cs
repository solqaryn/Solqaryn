using System;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class ThreeWayMatchRegressionTests
{
    private OrdenCompra CrearOrdenCompra(int ocId, decimal cantidad, decimal precio, decimal descuento, decimal impuesto)
    {
        var oc = new OrdenCompra { Id = ocId, ProveedorId = 1, ProveedorNombreSnapshot = "Prov", NumeroOrden = "OC-1", Moneda = "USD" };
        oc.EstablecerIdempotencia("key", new string('a', 64));
        var detalle = new OrdenCompraDetalle { Id = 1, OrdenCompraId = ocId, ProductoId = 1 };
        detalle.EstablecerValores(cantidad, precio, descuento, impuesto);
        oc.Detalles.Add(detalle);
        return oc;
    }

    private RecepcionCompra CrearRecepcion(int rcId, int ocDetalleId, decimal cantidadAceptada, EstadoRecepcionCompra estado = EstadoRecepcionCompra.Recibida)
    {
        var rc = new RecepcionCompra { Id = rcId, OrdenCompraId = 1, NumeroRecepcion = $"RC-{rcId}" };
        rc.EstablecerIdempotencia($"key{rcId}", new string('b', 64));
        var detalle = new RecepcionCompraDetalle { Id = rcId, OrdenCompraDetalleId = ocDetalleId, ProductoId = 1, AlmacenId = 1 };
        detalle.EstablecerCantidades(cantidadAceptada);
        rc.Detalles.Add(detalle);

        if (estado == EstadoRecepcionCompra.Recibida || estado == EstadoRecepcionCompra.Anulada)
            rc.Confirmar(1, "Tester", DateTime.UtcNow);
        if (estado == EstadoRecepcionCompra.Anulada)
            rc.Anular(1, "Motivo", DateTime.UtcNow);
        return rc;
    }

    private FacturaProveedor CrearFactura(int fpId, int ocDetalleId, decimal cantidadFacturada, decimal precio, decimal descuento, decimal impuesto, EstadoFacturaProveedor estado = EstadoFacturaProveedor.Registrada, string moneda = "USD")
    {
        var fp = new FacturaProveedor { Id = fpId, ProveedorId = 1, OrdenCompraId = 1, ProveedorNombreSnapshot = "Prov", NumeroFactura = $"FP-{fpId}", FechaEmisionUtc = DateTime.UtcNow, Moneda = moneda };
        var detalle = new FacturaProveedorDetalle { Id = fpId, OrdenCompraDetalleId = ocDetalleId, ProductoId = 1, ProductoNombreSnapshot = "Prod" };
        detalle.EstablecerValores(cantidadFacturada, precio, descuento, impuesto);
        fp.Detalles.Add(detalle);
        if (estado == EstadoFacturaProveedor.Registrada || estado == EstadoFacturaProveedor.Anulada)
            fp.Registrar(1, "Tester", DateTime.UtcNow);
        if (estado == EstadoFacturaProveedor.Anulada)
            fp.Anular(1, "Prueba", DateTime.UtcNow);
        return fp;
    }

    [Fact]
    public void ExactMatch_Aprobado()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 15m);
        var result = ThreeWayMatchResult.Evaluar(oc, new[] { CrearRecepcion(1, 1, 10m) }, new[] { CrearFactura(1, 1, 10m, 100m, 10m, 15m) });
        Assert.Equal(ThreeWayMatchStatus.Aprobado, result.Estado);
    }

    [Fact]
    public void OnlyRecibidaAndRegistradaAreEligible_AnuladaIgnored()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 15m);
        var result = ThreeWayMatchResult.Evaluar(oc, new[] { CrearRecepcion(1, 1, 10m, EstadoRecepcionCompra.Anulada) }, new[] { CrearFactura(1, 1, 10m, 100m, 10m, 15m, EstadoFacturaProveedor.Anulada) });
        Assert.Equal(ThreeWayMatchStatus.Discrepancia, result.Estado);
        Assert.Contains(result.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Cantidad);
    }

    [Fact]
    public void HeaderDiscrepancy_CurrencyMismatch_SentinelBehavior()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 15m);
        var result = ThreeWayMatchResult.Evaluar(oc, new[] { CrearRecepcion(1, 1, 10m) }, new[] { CrearFactura(1, 1, 10m, 100m, 10m, 15m, moneda: "HNL") });
        Assert.Equal(ThreeWayMatchStatus.Discrepancia, result.Estado);
        Assert.Contains(result.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Moneda && d.OrdenCompraDetalleId == 0);
    }

    [Fact]
    public void DeterministicRepeatedEvaluation_NoSideEffects()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 15m);
        var rc = CrearRecepcion(1, 1, 10m);
        var fp = CrearFactura(1, 1, 10m, 100m, 10m, 15m);
        var r1 = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });
        var r2 = ThreeWayMatchResult.Evaluar(oc, new[] { rc }, new[] { fp });
        Assert.Equal(r1.Estado, r2.Estado);
        Assert.Equal(r1.Discrepancias.Count, r2.Discrepancias.Count);
    }

    [Fact]
    public void NoInventedTolerance_SmallDifferenceFails()
    {
        var oc = CrearOrdenCompra(1, 10m, 100m, 10m, 15m);
        var result = ThreeWayMatchResult.Evaluar(oc, new[] { CrearRecepcion(1, 1, 10m) }, new[] { CrearFactura(1, 1, 10m, 100.01m, 10m, 15m) });
        Assert.Equal(ThreeWayMatchStatus.Discrepancia, result.Estado);
        Assert.Contains(result.Discrepancias, d => d.Tipo == ThreeWayMatchDiscrepancyType.Precio);
    }
}
