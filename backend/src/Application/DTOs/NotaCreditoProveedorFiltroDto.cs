using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class NotaCreditoProveedorFiltroDto
{
    public EstadoNotaCreditoProveedor? Estado { get; set; }
    public int? ProveedorId { get; set; }
    public int? FacturaProveedorId { get; set; }
    public int? DevolucionProveedorId { get; set; }
    public string? Numero { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
