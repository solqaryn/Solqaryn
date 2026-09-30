namespace Solqaryn.Application.DTOs.Contabilidad;

public sealed class EstadoFinancieroDto
{
    public string Nombre { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    public IReadOnlyList<EstadoFinancieroLineaDto> Lineas { get; init; } = Array.Empty<EstadoFinancieroLineaDto>();
    public IReadOnlyList<EstadoFinancieroTotalDto> Totales { get; init; } = Array.Empty<EstadoFinancieroTotalDto>();
}

public sealed class EstadoFinancieroLineaDto
{
    public int CuentaContableId { get; init; }
    public string? CuentaCodigo { get; init; }
    public string? CuentaNombre { get; init; }
    public decimal Saldo { get; init; }
    public bool EsRaiz { get; init; }
}

public sealed class EstadoFinancieroTotalDto
{
    public string Etiqueta { get; init; } = string.Empty;
    public decimal Valor { get; init; }
}
