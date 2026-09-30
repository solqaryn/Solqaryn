namespace Solqaryn.Domain.Entities;

/// Respaldo documental de una compra: factura, recibo o comprobante entregado
/// por el proveedor. El archivo puede ser imagen o PDF y se conserva separado
/// de la factura de venta generada por Solqaryn.
public class CompraDocumento
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "image";

    public bool Eliminado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
}
