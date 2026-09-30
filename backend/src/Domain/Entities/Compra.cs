using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Domain.Entities;

public class Compra : ConfirmableEntity
{
    public string NumeroCompra { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorDocumento { get; set; }
    public string? DocumentoReferencia { get; set; }

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Borrador;
    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;

    // Snapshot/bridge ERP-N0: se conserva mientras los contratos legacy sigan
    // necesitando un valor representable por enum. No es la autoridad relacional.
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    // Autoridad relacional materializada en N0.8.C. Permanece nullable a nivel de
    // transición hasta que N0.8.D migre todas las escrituras de aplicación.
    public int? MetodoPagoId { get; set; }
    public CatalogoMetodoPago? MetodoPagoCatalogo { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public string? Notas { get; set; }

    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
    public ICollection<CompraImpuesto> ImpuestosAplicados { get; set; } = new List<CompraImpuesto>();
}
