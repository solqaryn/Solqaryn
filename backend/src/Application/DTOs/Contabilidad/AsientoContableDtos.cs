namespace Solqaryn.Application.DTOs.Contabilidad;

public sealed class CrearAsientoContableDto
{
    public DateTime? Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? Numero { get; set; }
    public int? DocumentoOrigenId { get; set; }
    public string? TipoDocumentoOrigen { get; set; }
    public List<CrearAsientoDetalleDto> Detalles { get; set; } = new();
}

public sealed class CrearAsientoDetalleDto
{
    public int CuentaContableId { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public string? Referencia { get; set; }
}

public sealed class AsientoContableDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? Numero { get; set; }
    public int? DocumentoOrigenId { get; set; }
    public string? TipoDocumentoOrigen { get; set; }
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }
    public IReadOnlyList<AsientoDetalleDto> Detalles { get; set; } = Array.Empty<AsientoDetalleDto>();
}

public sealed class AsientoDetalleDto
{
    public int Id { get; set; }
    public int CuentaContableId { get; set; }
    public string? CuentaCodigo { get; set; }
    public string? CuentaNombre { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public string? Referencia { get; set; }
}
