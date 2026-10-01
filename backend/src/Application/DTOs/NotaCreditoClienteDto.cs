namespace Solqaryn.Application.DTOs;

public sealed class CreateNotaCreditoClienteDto
{
    public int FacturaId { get; set; }
    public decimal MontoCredito { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public sealed class NotaCreditoClienteDto
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public int VentaId { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public decimal MontoCredito { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
}
