namespace Solqaryn.Application.DTOs;

/// <summary>
/// Promocion publica ya resuelta para un precio normal. El cliente nunca
/// recalcula reglas administrativas: recibe un precio vigente y reproducible.
/// </summary>
public sealed class OfertaPublicaDto
{
    public decimal PrecioNormal { get; init; }
    public decimal PrecioOferta { get; init; }
    public decimal Ahorro { get; init; }
    public decimal PorcentajeAhorro { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateTime? VigenteDesdeUtc { get; init; }
    public DateTime? VigenteHastaUtc { get; init; }
}
