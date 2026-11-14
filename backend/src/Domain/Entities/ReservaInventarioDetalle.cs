using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class ReservaInventarioDetalle : AuditableEntity
{
    public int ReservaInventarioId { get; set; }
    public ReservaInventario ReservaInventario { get; set; } = null!;

    public int ProductoVarianteId { get; set; }
    public ProductoVariante ProductoVariante { get; set; } = null!;

    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;

    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public int CantidadReservada { get; private set; }
    public int CantidadConsumida { get; private set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    public void EstablecerCantidadReservada(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad reservada debe ser mayor que cero.");

        CantidadReservada = cantidad;
    }

    public void RegistrarConsumo(int cantidad)
    {
        if (CantidadReservada <= 0)
            throw new InvalidOperationException("La cantidad reservada debe materializarse antes de consumir la reserva.");
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad consumida debe ser mayor que cero.");
        if (CantidadConsumida > 0)
            throw new InvalidOperationException("La línea de reserva ya fue consumida.");
        if (cantidad != CantidadReservada)
            throw new InvalidOperationException("El consumo de una reserva activa debe corresponder exactamente a la cantidad reservada.");

        CantidadConsumida = cantidad;
    }

    public void ValidarClaveFisica()
    {
        if (ProductoVarianteId <= 0)
            throw new InvalidOperationException("La variante de producto es obligatoria para reservar inventario.");
        if (AlmacenId <= 0)
            throw new InvalidOperationException("El almacén es obligatorio para reservar inventario.");
        if (UbicacionAlmacenId.HasValue && UbicacionAlmacenId.Value <= 0)
            throw new InvalidOperationException("La ubicación de almacén debe ser válida cuando se especifica.");
    }

    public bool EstaConsumida => CantidadReservada > 0 && CantidadConsumida == CantidadReservada;
}
