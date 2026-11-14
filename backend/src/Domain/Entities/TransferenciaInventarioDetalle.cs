using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class TransferenciaInventarioDetalle : AuditableEntity
{
    public int TransferenciaInventarioId { get; set; }
    public TransferenciaInventario TransferenciaInventario { get; set; } = null!;

    public int ProductoVarianteId { get; set; }
    public ProductoVariante ProductoVariante { get; set; } = null!;

    public int? UbicacionOrigenId { get; set; }
    public UbicacionAlmacen? UbicacionOrigen { get; set; }
    public int? UbicacionDestinoId { get; set; }
    public UbicacionAlmacen? UbicacionDestino { get; set; }

    public int CantidadSolicitada { get; private set; }
    public int CantidadAprobada { get; private set; }
    public int CantidadDespachada { get; private set; }
    public int CantidadRecibida { get; private set; }
    public int CantidadFaltante { get; private set; }
    public int CantidadSobrante { get; private set; }
    public int CantidadDanada { get; private set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    public void EstablecerCantidadSolicitada(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad solicitada debe ser mayor que cero.");

        CantidadSolicitada = cantidad;
    }

    public void AprobarCantidad(int cantidad)
    {
        if (CantidadSolicitada <= 0)
            throw new InvalidOperationException("La cantidad solicitada debe definirse antes de aprobar.");
        if (cantidad <= 0 || cantidad > CantidadSolicitada)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad aprobada debe ser mayor que cero y no superar lo solicitado.");

        CantidadAprobada = cantidad;
    }

    public void RegistrarDespacho(int cantidad)
    {
        if (CantidadAprobada <= 0)
            throw new InvalidOperationException("La cantidad aprobada debe definirse antes del despacho.");
        if (cantidad <= 0 || cantidad > CantidadAprobada)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad despachada debe ser mayor que cero y no superar lo aprobado.");

        CantidadDespachada = cantidad;
    }

    public void RegistrarRecepcion(int recibida, int faltante, int danada, int sobrante)
    {
        if (CantidadDespachada <= 0)
            throw new InvalidOperationException("No puede registrarse recepción sin despacho previo.");
        if (recibida < 0 || faltante < 0 || danada < 0 || sobrante < 0)
            throw new ArgumentOutOfRangeException(nameof(recibida), "Las cantidades de recepción no pueden ser negativas.");
        if (recibida + faltante + danada > CantidadDespachada)
            throw new InvalidOperationException("La suma recibida + faltante + dañada no puede superar lo despachado.");

        CantidadRecibida = recibida;
        CantidadFaltante = faltante;
        CantidadDanada = danada;
        CantidadSobrante = sobrante;
    }

    public bool RecepcionCerrada =>
        CantidadDespachada > 0 &&
        CantidadRecibida + CantidadFaltante + CantidadDanada == CantidadDespachada;
}
