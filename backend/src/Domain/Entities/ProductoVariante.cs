using System.ComponentModel.DataAnnotations.Schema;
using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class ProductoVariante : AuditableEntity
{
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }
    public int? ModeloId { get; set; }
    public Modelo? Modelo { get; set; }
    public int? ColorId { get; set; }
    public Color? Color { get; set; }
    public int? TallaId { get; set; }
    public Talla? Talla { get; set; }
    public string? Sku { get; set; }
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public decimal? Costo { get; set; }
    public decimal? Precio { get; set; }
    public bool EsTecnica { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    // N1.9.C — política opt-in persistida. Las variantes existentes conservan
    // comportamiento legacy con todos los controles desactivados por defecto.
    public bool ControlaLote { get; private set; }
    public bool ControlaNumeroSerie { get; private set; }
    public bool ControlaFechaVencimiento { get; private set; }
    public int? DiasAlertaVencimiento { get; private set; }

    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
    public bool TieneStockBajo => Activo && !Eliminado && Cantidad > 0 && Cantidad < UmbralStockBajo;
    public bool EstaAgotada => Activo && !Eliminado && Cantidad <= 0;
    [NotMapped]
    public bool RequiereTrazabilidad => ControlaLote || ControlaNumeroSerie || ControlaFechaVencimiento;

    public void ConfigurarTrazabilidad(
        bool controlaLote,
        bool controlaNumeroSerie,
        bool controlaFechaVencimiento,
        int? diasAlertaVencimiento = null)
    {
        if (controlaFechaVencimiento && !controlaLote)
            throw new InvalidOperationException("El control de vencimiento requiere control de lote para preservar una identidad logística durable.");
        if (diasAlertaVencimiento.HasValue && diasAlertaVencimiento.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(diasAlertaVencimiento), "Los días de alerta de vencimiento no pueden ser negativos.");
        if (!controlaFechaVencimiento && diasAlertaVencimiento.HasValue)
            throw new InvalidOperationException("Los días de alerta sólo aplican cuando el control de vencimiento está habilitado.");

        ControlaLote = controlaLote;
        ControlaNumeroSerie = controlaNumeroSerie;
        ControlaFechaVencimiento = controlaFechaVencimiento;
        DiasAlertaVencimiento = controlaFechaVencimiento ? diasAlertaVencimiento : null;
    }
}
