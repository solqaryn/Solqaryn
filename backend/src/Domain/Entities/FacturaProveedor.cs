using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;

namespace Solqaryn.Domain.Entities;

public class FacturaProveedor : AuditableEntity
{
    public string NumeroFactura { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public int OrdenCompraId { get; set; }
    public OrdenCompra OrdenCompra { get; set; } = null!;

    public string ProveedorNombreSnapshot { get; set; } = string.Empty;
    public string? ProveedorDocumentoSnapshot { get; set; }
    public string Moneda { get; set; } = "HNL";
    public DateTime FechaEmisionUtc { get; set; }
    public DateTime? FechaVencimientoUtc { get; set; }
    public string? ReferenciaFiscal { get; set; }
    public string? Observaciones { get; set; }

    public EstadoFacturaProveedor Estado { get; private set; } = EstadoFacturaProveedor.Borrador;
    public DateTime? FechaRegistroUtc { get; private set; }
    public int? RegistradaPorUsuarioId { get; private set; }
    public string? RegistradaPorNombreSnapshot { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }
    public int? AnuladaPorUsuarioId { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public ICollection<FacturaProveedorDetalle> Detalles { get; set; } = new List<FacturaProveedorDetalle>();

    public bool EsEditable => Estado == EstadoFacturaProveedor.Borrador;
    public decimal Subtotal => Detalles.Sum(x => x.SubtotalSnapshot);
    public decimal Descuento => Detalles.Sum(x => x.DescuentoSnapshot);
    public decimal Impuesto => Detalles.Sum(x => x.ImpuestoSnapshot);
    public decimal Total => Detalles.Sum(x => x.TotalSnapshot);
    public FacturaProveedorMontos Montos => FacturaProveedorMontos.Crear(Subtotal, Descuento, Impuesto, Total);

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una factura de proveedor en borrador puede modificarse.");
    }

    public void Registrar(int usuarioId, string? usuarioNombre, DateTime fechaUtc)
    {
        if (Estado != EstadoFacturaProveedor.Borrador)
            throw new InvalidOperationException("Solo una factura de proveedor en borrador puede registrarse.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();
        _ = Montos;

        Estado = EstadoFacturaProveedor.Registrada;
        FechaRegistroUtc = fechaUtc;
        RegistradaPorUsuarioId = usuarioId;
        RegistradaPorNombreSnapshot = Normalizar(usuarioNombre);
    }

    public void Anular(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoFacturaProveedor.Registrada)
            throw new InvalidOperationException("Solo una factura de proveedor registrada puede anularse.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        ValidarUsuario(usuarioId);

        Estado = EstadoFacturaProveedor.Anulada;
        FechaAnulacionUtc = fechaUtc;
        AnuladaPorUsuarioId = usuarioId;
        MotivoAnulacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(NumeroFactura))
            throw new InvalidOperationException("El número de factura del proveedor es obligatorio.");
        if (ProveedorId <= 0)
            throw new InvalidOperationException("El proveedor es obligatorio.");
        if (OrdenCompraId <= 0)
            throw new InvalidOperationException("La orden de compra es obligatoria.");
        if (string.IsNullOrWhiteSpace(ProveedorNombreSnapshot))
            throw new InvalidOperationException("El snapshot del proveedor es obligatorio.");
        if (string.IsNullOrWhiteSpace(Moneda) || Moneda.Trim().Length != 3)
            throw new InvalidOperationException("La moneda debe usar un código ISO de tres caracteres.");
        if (FechaEmisionUtc == default)
            throw new InvalidOperationException("La fecha de emisión es obligatoria.");
        if (FechaVencimientoUtc is not null && FechaVencimientoUtc.Value < FechaEmisionUtc)
            throw new InvalidOperationException("La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La factura de proveedor debe contener al menos un detalle.");

        foreach (var detalle in Detalles)
            detalle.Validar();

        var lineasDuplicadas = Detalles
            .GroupBy(x => x.OrdenCompraDetalleId)
            .Any(grupo => grupo.Count() > 1);
        if (lineasDuplicadas)
            throw new InvalidOperationException("Una línea de orden de compra no puede repetirse dentro de la misma factura de proveedor.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
