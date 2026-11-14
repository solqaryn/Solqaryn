using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class PreparacionPedidoVentaDto
{
    public int Id { get; set; }
    public int PedidoVentaId { get; set; }
    public int ReservaInventarioId { get; set; }
    public EstadoPreparacionPedidoVenta Estado { get; set; }
    public DateTime? FechaPickingCompletadoUtc { get; set; }
    public DateTime? FechaPackingCompletadoUtc { get; set; }
    public DateTime? FechaDespachoUtc { get; set; }
    public DateTime? FechaEntregaUtc { get; set; }
    public DateTime? FechaCancelacionUtc { get; set; }
    public string? MotivoCancelacion { get; set; }
    public IReadOnlyList<PreparacionPedidoVentaDetalleDto> Detalles { get; set; } = Array.Empty<PreparacionPedidoVentaDetalleDto>();
}

public sealed class PreparacionPedidoVentaDetalleDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int CantidadPreparar { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
}

public sealed class CancelarPreparacionPedidoVentaDto
{
    public string Motivo { get; set; } = string.Empty;
}
