using System;
using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class EvaluacionProveedor : AuditableEntity
{
    public int ProveedorId { get; private set; }
    public Proveedor Proveedor { get; private set; } = null!;

    public int OrdenCompraId { get; private set; }
    public OrdenCompra OrdenCompra { get; private set; } = null!;

    public int RecepcionCompraId { get; private set; }
    public RecepcionCompra RecepcionCompra { get; private set; } = null!;

    public DateTime FechaEsperadaUtc { get; private set; }
    public DateTime FechaRecepcionUtc { get; private set; }

    public decimal CantidadOrdenada { get; private set; }
    public decimal CantidadAceptada { get; private set; }
    public decimal CantidadDanada { get; private set; }
    public decimal CantidadSobrante { get; private set; }

    protected EvaluacionProveedor() { }

    public EvaluacionProveedor(
        int proveedorId,
        int ordenCompraId,
        int recepcionCompraId,
        DateTime fechaEsperadaUtc,
        DateTime fechaRecepcionUtc)
    {
        if (proveedorId <= 0) throw new ArgumentException("ProveedorId is required", nameof(proveedorId));
        if (ordenCompraId <= 0) throw new ArgumentException("OrdenCompraId is required", nameof(ordenCompraId));
        if (recepcionCompraId <= 0) throw new ArgumentException("RecepcionCompraId is required", nameof(recepcionCompraId));

        ProveedorId = proveedorId;
        OrdenCompraId = ordenCompraId;
        RecepcionCompraId = recepcionCompraId;

        ConfigurarDesviacionEntrega(fechaEsperadaUtc, fechaRecepcionUtc);
    }

    public void ConfigurarDesviacionEntrega(DateTime fechaEsperadaUtc, DateTime fechaRecepcionUtc)
    {
        FechaEsperadaUtc = fechaEsperadaUtc;
        FechaRecepcionUtc = fechaRecepcionUtc;
    }

    public void EstablecerCantidades(decimal cantidadOrdenada, decimal cantidadRecibida, decimal cantidadAceptada, decimal cantidadDanada, decimal cantidadSobrante)
    {
        if (cantidadOrdenada < 0 || cantidadRecibida < 0 || cantidadAceptada < 0 || cantidadDanada < 0 || cantidadSobrante < 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadOrdenada), "Las cantidades no pueden ser negativas.");

        if (cantidadAceptada + cantidadDanada + cantidadSobrante != cantidadRecibida)
            throw new InvalidOperationException("La cantidad aceptada no es coherente con la recibida, dañada y sobrante.");

        if (cantidadDanada > cantidadRecibida)
            throw new ArgumentOutOfRangeException(nameof(cantidadDanada), "La cantidad dañada no puede superar la cantidad recibida.");

        if (cantidadSobrante > cantidadRecibida)
            throw new ArgumentOutOfRangeException(nameof(cantidadSobrante), "La cantidad sobrante debe formar parte de la cantidad físicamente recibida.");

        if (cantidadDanada + cantidadSobrante > cantidadRecibida)
            throw new InvalidOperationException("Las cantidades dañada y sobrante no pueden superar conjuntamente la cantidad físicamente recibida.");

        CantidadOrdenada = cantidadOrdenada;
        CantidadAceptada = cantidadAceptada;
        CantidadDanada = cantidadDanada;
        CantidadSobrante = cantidadSobrante;
    }
}
