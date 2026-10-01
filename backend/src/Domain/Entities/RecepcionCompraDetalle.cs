using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class RecepcionCompraDetalle : AuditableEntity
{
    public int RecepcionCompraId { get; set; }
    public RecepcionCompra RecepcionCompra { get; set; } = null!;

    public int OrdenCompraDetalleId { get; set; }
    public OrdenCompraDetalle OrdenCompraDetalle { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;

    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public decimal CantidadRecibida { get; private set; }
    public decimal CantidadDanada { get; private set; }
    public decimal CantidadFaltante { get; private set; }
    public decimal CantidadSobrante { get; private set; }
    public decimal CostoUnitarioSnapshot { get; set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    // Unidades dañadas y sobrantes se mantienen explícitas fuera del stock aceptado.
    // La política para aceptar sobrantes pertenece a la materialización transaccional de N2.3.D.
    public decimal CantidadAceptada => CantidadRecibida - CantidadDanada - CantidadSobrante;
    public bool TieneActividadFisica => CantidadRecibida > 0 || CantidadFaltante > 0;

    public void EstablecerCantidades(
        decimal cantidadRecibida,
        decimal cantidadDanada = 0m,
        decimal cantidadFaltante = 0m,
        decimal cantidadSobrante = 0m)
    {
        ValidarCantidades(cantidadRecibida, cantidadDanada, cantidadFaltante, cantidadSobrante);

        CantidadRecibida = cantidadRecibida;
        CantidadDanada = cantidadDanada;
        CantidadFaltante = cantidadFaltante;
        CantidadSobrante = cantidadSobrante;
    }

    public void Validar()
    {
        if (OrdenCompraDetalleId <= 0)
            throw new InvalidOperationException("La línea de orden de compra es obligatoria.");
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (AlmacenId <= 0)
            throw new InvalidOperationException("El almacén destino es obligatorio.");
        if (UbicacionAlmacenId is <= 0)
            throw new InvalidOperationException("La ubicación debe ser válida cuando se especifica.");
        if (CostoUnitarioSnapshot < 0)
            throw new InvalidOperationException("El costo unitario snapshot no puede ser negativo.");

        ValidarCantidades(CantidadRecibida, CantidadDanada, CantidadFaltante, CantidadSobrante);
    }

    private static void ValidarCantidades(
        decimal cantidadRecibida,
        decimal cantidadDanada,
        decimal cantidadFaltante,
        decimal cantidadSobrante)
    {
        if (cantidadRecibida < 0 || cantidadDanada < 0 || cantidadFaltante < 0 || cantidadSobrante < 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadRecibida), "Las cantidades de recepción no pueden ser negativas.");
        if (cantidadDanada > cantidadRecibida)
            throw new ArgumentOutOfRangeException(nameof(cantidadDanada), "La cantidad dañada no puede superar la cantidad recibida.");
        if (cantidadSobrante > cantidadRecibida)
            throw new ArgumentOutOfRangeException(nameof(cantidadSobrante), "La cantidad sobrante debe formar parte de la cantidad físicamente recibida.");
        if (cantidadDanada + cantidadSobrante > cantidadRecibida)
            throw new InvalidOperationException("Las cantidades dañada y sobrante no pueden superar conjuntamente la cantidad físicamente recibida.");
        if (cantidadRecibida == 0 && cantidadFaltante == 0)
            throw new InvalidOperationException("El detalle debe registrar recepción física o faltante.");
    }
}