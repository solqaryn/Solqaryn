using Solqaryn.Domain.ValueObjects;

namespace Solqaryn.Application.DTOs;

public sealed class ValidarCheckoutTiendaDto
{
    public List<CheckoutTiendaItemRequestDto> Items { get; set; } = new();
    public CheckoutTiendaEnvioRequestDto? Envio { get; set; }
}

// Coverage only. No client price, date, company or tariff ID.
public sealed class CheckoutTiendaEnvioRequestDto
{
    public string? Departamento { get; set; }
    public string? Ciudad { get; set; }
    public string? Zona { get; set; }
    public string? Modalidad { get; set; }
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
    public FiscalCheckoutSnapshot? Fiscal { get; set; }
    public string? HuellaFiscal { get; set; }
    // Null means delivery is not quoted; a configured zero is valid.
    public decimal? CostoEnvio { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int? CostoEnvioId { get; set; }
    public string? TarifaEnvio { get; set; }
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
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal PrecioBase { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int? CategoriaId { get; set; }
    public decimal Total { get; set; }
}
