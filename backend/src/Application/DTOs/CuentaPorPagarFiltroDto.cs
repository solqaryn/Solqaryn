using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class CuentaPorPagarFiltroDto
{
    public EstadoCuentaPorPagar? Estado { get; set; }
    public CondicionPagoProveedor? CondicionPago { get; set; }
    public int? ProveedorId { get; set; }
    public int? FacturaProveedorId { get; set; }
    public DateTime? VenceDesdeUtc { get; set; }
    public DateTime? VenceHastaUtc { get; set; }
    public string? Moneda { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
