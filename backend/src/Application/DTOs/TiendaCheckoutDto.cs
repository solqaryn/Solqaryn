namespace Solqaryn.Application.DTOs;

public sealed class ValidarCheckoutTiendaDto
{
    public List<CheckoutTiendaItemRequestDto> Items { get; set; } = new();
}

public sealed class CheckoutTiendaItemRequestDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int? ModeloId { get; set; }
    public string? ModeloNombre { get; set; }
    public string? MarcaNombre { get; set; }
    public int Unidades { get; set; }
}

public sealed class CheckoutTiendaValidadoDto
{
    public string ValidacionId { get; set; } = string.Empty;
    public DateTime ExpiraUtc { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public List<CheckoutTiendaLineaDto> Lineas { get; set; } = new();
}

public sealed class CheckoutTiendaLineaDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int? ModeloId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Modelo { get; set; }
    public string? Sku { get; set; }
    public int Unidades { get; set; }
    public int StockDisponible { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
}
