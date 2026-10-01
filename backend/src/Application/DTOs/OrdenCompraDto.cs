using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class OrdenCompraDetalleInputDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public decimal CantidadOrdenada { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public string? Observacion { get; set; }
}

public class CreateOrdenCompraDto
{
    public int? SolicitudCompraId { get; set; }
    public int ProveedorId { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string? CondicionesCompra { get; set; }
    public DateTime? FechaEsperadaUtc { get; set; }
    public string? Observaciones { get; set; }
    public List<OrdenCompraDetalleInputDto> Detalles { get; set; } = new();
}

public class UpdateOrdenCompraDto : CreateOrdenCompraDto
{
}

public class OrdenCompraDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public decimal CantidadOrdenada { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string? Observacion { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
}

public class OrdenCompraDto
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public EstadoOrdenCompra Estado { get; set; }
    public int? SolicitudCompraId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string Moneda { get; set; } = string.Empty;
    public string? CondicionesCompra { get; set; }
    public DateTime? FechaEsperadaUtc { get; set; }
    public string? Observaciones { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public DateTime? FechaEnvioAprobacionUtc { get; set; }
    public DateTime? FechaAprobacionUtc { get; set; }
    public DateTime? FechaCancelacionUtc { get; set; }
    public List<OrdenCompraDetalleDto> Detalles { get; set; } = new();
}

public class OrdenCompraFiltroDto : PagedRequest
{
    public EstadoOrdenCompra? Estado { get; set; }
    public int? ProveedorId { get; set; }
    public int? SolicitudCompraId { get; set; }
    public string? Numero { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public OrdenCompraFiltroDto()
    {
        SortBy = "FechaCreacion";
        SortDirection = "desc";
    }
}

public class CancelarOrdenCompraDto
{
    public string Motivo { get; set; } = string.Empty;
}
