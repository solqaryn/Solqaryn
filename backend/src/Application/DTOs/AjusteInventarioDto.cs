using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class AjusteInventarioDetalleInputDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int CantidadObjetivo { get; set; }
}

public sealed class CreateAjusteInventarioDto
{
    public DateTime? FechaAjuste { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<AjusteInventarioDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class UpdateAjusteInventarioDto
{
    public DateTime? FechaAjuste { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<AjusteInventarioDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class AnularAjusteInventarioDto
{
    public string MotivoAnulacion { get; set; } = string.Empty;
}

public sealed class AjusteInventarioFiltroDto : PagedRequest
{
    public AjusteInventarioFiltroDto()
    {
        SortBy = "FechaAjuste";
        SortDirection = "desc";
    }

    public EstadoAjusteInventario? Estado { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
}

public sealed class AjusteInventarioDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int CantidadObjetivo { get; set; }
    public int? CantidadAnteriorSnapshot { get; set; }
    public int? CantidadNuevaSnapshot { get; set; }
    public int? DiferenciaSnapshot { get; set; }
    public decimal? CostoUnitarioSnapshot { get; set; }
    public decimal? ImpactoCostoSnapshot { get; set; }
    public string? NombreSnapshot { get; set; }
    public string? SkuSnapshot { get; set; }
    public string? MarcaSnapshot { get; set; }
    public string? ModeloSnapshot { get; set; }
    public string? ColorSnapshot { get; set; }
    public string? TallaSnapshot { get; set; }
}

public sealed class AjusteInventarioDto
{
    public int Id { get; set; }
    public string NumeroAjuste { get; set; } = string.Empty;
    public DateTime FechaAjuste { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
    public decimal? ImpactoCostoTotalSnapshot { get; set; }
    public List<AjusteInventarioDetalleDto> Detalles { get; set; } = new();
}
