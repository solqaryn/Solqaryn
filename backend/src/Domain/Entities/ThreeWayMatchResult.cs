using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;

namespace Solqaryn.Domain.Entities;

public class ThreeWayMatchResult : AuditableEntity
{
    public int OrdenCompraId { get; private set; }
    public ThreeWayMatchStatus Estado { get; private set; }

    private readonly List<ThreeWayMatchLineDiscrepancy> _discrepancias = new();
    public IReadOnlyCollection<ThreeWayMatchLineDiscrepancy> Discrepancias => _discrepancias.AsReadOnly();

    private ThreeWayMatchResult()
    {
    }

    private ThreeWayMatchResult(int ordenCompraId, ThreeWayMatchStatus estado, IEnumerable<ThreeWayMatchLineDiscrepancy> discrepancias)
    {
        OrdenCompraId = ordenCompraId;
        Estado = estado;
        _discrepancias.AddRange(discrepancias);
    }

    public static ThreeWayMatchResult Evaluar(
        OrdenCompra ordenCompra,
        IEnumerable<RecepcionCompra> recepciones,
        IEnumerable<FacturaProveedor> facturas)
    {
        if (ordenCompra == null) throw new ArgumentNullException(nameof(ordenCompra));
        if (recepciones == null) throw new ArgumentNullException(nameof(recepciones));
        if (facturas == null) throw new ArgumentNullException(nameof(facturas));

        var discrepancias = new List<ThreeWayMatchLineDiscrepancy>();
        var listaRecepciones = recepciones.ToList();
        var listaFacturas = facturas.ToList();

        if (listaRecepciones.Any(r => r.OrdenCompraId != ordenCompra.Id))
            throw new InvalidOperationException("Todas las recepciones deben pertenecer a la orden evaluada.");
        if (listaFacturas.Any(f => f.OrdenCompraId != ordenCompra.Id))
            throw new InvalidOperationException("Todas las facturas deben pertenecer a la orden evaluada.");

        // Solo documentos materializados participan del match. Borradores no constituyen
        // evidencia operativa y los anulados dejan de ser evidencia vigente.
        var recepcionesValidas = listaRecepciones.Where(r => r.Estado == EstadoRecepcionCompra.Recibida).ToList();
        var facturasValidas = listaFacturas.Where(f => f.Estado == EstadoFacturaProveedor.Registrada).ToList();

        foreach (var factura in facturasValidas)
        {
            if (!string.Equals(factura.Moneda?.Trim(), ordenCompra.Moneda?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                discrepancias.Add(new ThreeWayMatchLineDiscrepancy(
                    0,
                    ThreeWayMatchDiscrepancyType.Moneda,
                    0m,
                    0m,
                    0m,
                    $"Discrepancia de moneda: orden {ordenCompra.Moneda} / factura {factura.Moneda}.",
                    ordenCompra.Moneda,
                    factura.Moneda));
            }
        }

        var recepcionesDetalles = recepcionesValidas
            .SelectMany(r => r.Detalles)
            .GroupBy(d => d.OrdenCompraDetalleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var facturasDetalles = facturasValidas
            .SelectMany(f => f.Detalles)
            .GroupBy(d => d.OrdenCompraDetalleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var lineaOrden in ordenCompra.Detalles)
        {
            var lineasRecepcion = recepcionesDetalles.GetValueOrDefault(lineaOrden.Id, new List<RecepcionCompraDetalle>());
            var lineasFactura = facturasDetalles.GetValueOrDefault(lineaOrden.Id, new List<FacturaProveedorDetalle>());
            var cantidadOrdenada = lineaOrden.CantidadOrdenada;
            var sumaCantidadAceptada = lineasRecepcion.Sum(r => r.CantidadAceptada);
            var sumaCantidadFacturada = lineasFactura.Sum(f => f.CantidadFacturada);

            if (cantidadOrdenada != sumaCantidadAceptada || cantidadOrdenada != sumaCantidadFacturada)
            {
                discrepancias.Add(new ThreeWayMatchLineDiscrepancy(
                    lineaOrden.Id,
                    ThreeWayMatchDiscrepancyType.Cantidad,
                    cantidadOrdenada,
                    sumaCantidadAceptada,
                    sumaCantidadFacturada,
                    "Discrepancia en cantidad."));
            }

            if (lineasFactura.Any(f => f.PrecioUnitarioSnapshot != lineaOrden.PrecioUnitario))
            {
                var primerPrecioDiferente = lineasFactura
                    .First(f => f.PrecioUnitarioSnapshot != lineaOrden.PrecioUnitario)
                    .PrecioUnitarioSnapshot;
                discrepancias.Add(new ThreeWayMatchLineDiscrepancy(
                    lineaOrden.Id,
                    ThreeWayMatchDiscrepancyType.Precio,
                    lineaOrden.PrecioUnitario,
                    0m,
                    primerPrecioDiferente,
                    "Discrepancia en precio unitario."));
            }

            var sumaDescuentoFacturado = lineasFactura.Sum(f => f.DescuentoSnapshot);
            var sumaImpuestoFacturado = lineasFactura.Sum(f => f.ImpuestoSnapshot);

            if (lineaOrden.Descuento != sumaDescuentoFacturado)
            {
                discrepancias.Add(new ThreeWayMatchLineDiscrepancy(
                    lineaOrden.Id,
                    ThreeWayMatchDiscrepancyType.Descuento,
                    lineaOrden.Descuento,
                    0m,
                    sumaDescuentoFacturado,
                    "Discrepancia en descuento total."));
            }

            if (lineaOrden.Impuesto != sumaImpuestoFacturado)
            {
                discrepancias.Add(new ThreeWayMatchLineDiscrepancy(
                    lineaOrden.Id,
                    ThreeWayMatchDiscrepancyType.Impuesto,
                    lineaOrden.Impuesto,
                    0m,
                    sumaImpuestoFacturado,
                    "Discrepancia en impuesto total."));
            }
        }

        var estado = discrepancias.Any() ? ThreeWayMatchStatus.Discrepancia : ThreeWayMatchStatus.Aprobado;
        return new ThreeWayMatchResult(ordenCompra.Id, estado, discrepancias);
    }
}
