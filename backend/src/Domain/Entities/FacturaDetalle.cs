namespace Solqaryn.Domain.Entities;

public class FacturaDetalle
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? ProductoCodigo { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoMarca { get; set; } = string.Empty;
    public string ProductoModelo { get; set; } = string.Empty;
    public string? VarianteColor { get; set; }
    public string? VarianteTalla { get; set; }
    public string? VarianteSku { get; set; }

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalLinea { get; set; }
    public string? Observaciones { get; set; }
}
