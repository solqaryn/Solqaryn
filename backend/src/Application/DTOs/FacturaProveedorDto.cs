using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class FacturaProveedorDetalleInputDto
{
    [Range(1, int.MaxValue)]
    public int OrdenCompraDetalleId { get; set; }

    [Range(typeof(decimal), "0.0001", "99999999999999")]
    public decimal CantidadFacturada { get; set; }

    [Range(typeof(decimal), "0", "99999999999999")]
    public decimal PrecioUnitario { get; set; }

    [Range(typeof(decimal), "0", "99999999999999")]
    public decimal Descuento { get; set; }

    [Range(typeof(decimal), "0", "99999999999999")]
    public decimal Impuesto { get; set; }

    [MaxLength(500)]
    public string? Observacion { get; set; }
}

public class CreateFacturaProveedorDto
{
    [Range(1, int.MaxValue)]
    public int ProveedorId { get; set; }

    [Range(1, int.MaxValue)]
    public int OrdenCompraId { get; set; }

    [Required, MaxLength(80)]
    public string NumeroFactura { get; set; } = string.Empty;

    public DateTime FechaEmisionUtc { get; set; }
    public DateTime? FechaVencimientoUtc { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Moneda { get; set; } = "HNL";

    [MaxLength(120)]
    public string? ReferenciaFiscal { get; set; }

    [MaxLength(1000)]
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<FacturaProveedorDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class UpdateFacturaProveedorDto : CreateFacturaProveedorDto
{
}

public sealed class AnularFacturaProveedorDto
{
    [Required, MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class FacturaProveedorFiltroDto
{
    public EstadoFacturaProveedor? Estado { get; set; }
    public int? ProveedorId { get; set; }
    public int? OrdenCompraId { get; set; }
    public string? Numero { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class FacturaProveedorDetalleDto
{
    public int Id { get; set; }
    public int OrdenCompraDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public decimal CantidadFacturada { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public string? Observacion { get; set; }
}

public sealed class FacturaProveedorDto
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public int OrdenCompraId { get; set; }
    public string ProveedorNombreSnapshot { get; set; } = string.Empty;
    public string? ProveedorDocumentoSnapshot { get; set; }
    public string Moneda { get; set; } = "HNL";
    public DateTime FechaEmisionUtc { get; set; }
    public DateTime? FechaVencimientoUtc { get; set; }
    public string? ReferenciaFiscal { get; set; }
    public string? Observaciones { get; set; }
    public EstadoFacturaProveedor Estado { get; set; }
    public DateTime? FechaRegistroUtc { get; set; }
    public int? RegistradaPorUsuarioId { get; set; }
    public string? RegistradaPorNombreSnapshot { get; set; }
    public DateTime? FechaAnulacionUtc { get; set; }
    public int? AnuladaPorUsuarioId { get; set; }
    public string? MotivoAnulacion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public bool EsEditable { get; set; }
    public IReadOnlyList<FacturaProveedorDetalleDto> Detalles { get; set; } = Array.Empty<FacturaProveedorDetalleDto>();
}
