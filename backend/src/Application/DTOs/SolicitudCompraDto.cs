using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class SolicitudCompraDetalleInputDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public decimal? CostoEstimadoUnitario { get; set; }
    public string? Observacion { get; set; }
}

public sealed class CreateSolicitudCompraDto
{
    public int? ProveedorId { get; set; }
    public string? Notas { get; set; }
    public List<SolicitudCompraDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class UpdateSolicitudCompraDto
{
    public int? ProveedorId { get; set; }
    public string? Notas { get; set; }
    public List<SolicitudCompraDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class RechazarSolicitudCompraDto
{
    public string Motivo { get; set; } = string.Empty;
}

public sealed class SolicitudCompraFiltroDto : PagedRequest
{
    public SolicitudCompraFiltroDto()
    {
        SortBy = "FechaCreacion";
        SortDirection = "desc";
    }

    public EstadoSolicitudCompra? Estado { get; set; }
    public int? ProveedorId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Numero { get; set; }
}

public sealed class SolicitudCompraDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public decimal? CostoEstimadoUnitario { get; set; }
    public string? Observacion { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
}

public sealed class SolicitudCompraDto
{
    public int Id { get; set; }
    public string NumeroSolicitud { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? Notas { get; set; }
    public DateTime? FechaSolicitudUtc { get; set; }
    public int? SolicitadaPorUsuarioId { get; set; }
    public string? SolicitadaPorNombreSnapshot { get; set; }
    public DateTime? FechaDecisionUtc { get; set; }
    public int? DecididaPorUsuarioId { get; set; }
    public string? DecididaPorNombreSnapshot { get; set; }
    public string? MotivoRechazo { get; set; }
    public List<SolicitudCompraDetalleDto> Detalles { get; set; } = new();
}
