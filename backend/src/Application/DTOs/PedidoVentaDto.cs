using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class PedidoVentaDto
{
    public int Id { get; set; }
    public int? CotizacionId { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombreSnapshot { get; set; } = string.Empty;
    public string? ClienteDocumentoSnapshot { get; set; }
    public string? Observaciones { get; set; }
    public EstadoPedidoVenta Estado { get; set; }
    public decimal Total { get; set; }
    public DateTime? FechaConfirmacionUtc { get; set; }
    public int? ConfirmadoPorUsuarioId { get; set; }
    public DateTime? FechaAnulacionUtc { get; set; }
    public int? AnuladoPorUsuarioId { get; set; }
    public string? MotivoAnulacion { get; set; }
    public List<PedidoVentaDetalleDto> Detalles { get; set; } = new();
}

public sealed class PedidoVentaDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
}

public sealed class PedidoVentaFiltroDto : PagedRequest
{
    public int? CotizacionId { get; set; }
    public int? ClienteId { get; set; }
    public EstadoPedidoVenta? Estado { get; set; }
    public DateTime? FechaDesdeUtc { get; set; }
    public DateTime? FechaHastaUtc { get; set; }
}

public sealed class CreatePedidoVentaDto
{
    public int CotizacionId { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class UpdatePedidoVentaDto
{
    public int Id { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class ConfirmarPedidoVentaDto
{
    public List<AsignacionReservaPedidoDto> Asignaciones { get; set; } = new();
}

public sealed class AsignacionReservaPedidoDto
{
    public int ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int Cantidad { get; set; }
}

public sealed class AnularPedidoVentaDto
{
    public string Motivo { get; set; } = string.Empty;
}
