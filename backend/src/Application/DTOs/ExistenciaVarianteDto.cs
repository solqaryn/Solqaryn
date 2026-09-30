using Solqaryn.Application.Common;

namespace Solqaryn.Application.DTOs;

public sealed class ExistenciaVarianteDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string VarianteSku { get; set; } = string.Empty;
    public int AlmacenId { get; set; }
    public string AlmacenCodigo { get; set; } = string.Empty;
    public string AlmacenNombre { get; set; } = string.Empty;
    public int? UbicacionAlmacenId { get; set; }
    public string? UbicacionCodigo { get; set; }
    public string? UbicacionNombre { get; set; }
    public int StockFisico { get; set; }
    public int StockReservado { get; set; }
    public int StockDisponible { get; set; }
    public int StockTransito { get; set; }
    public int StockMinimo { get; set; }
    public int? StockMaximo { get; set; }
    public bool TieneStockBajo { get; set; }
    public bool EstaAgotada { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public sealed class CreateExistenciaVarianteDto
{
    public int ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int StockFisico { get; set; }
    public int StockReservado { get; set; }
    public int StockTransito { get; set; }
    public int StockMinimo { get; set; }
    public int? StockMaximo { get; set; }
}

/// <summary>
/// Configuración operativa de una existencia. StockDisponible no aparece como
/// input porque es derivado. Las mutaciones transaccionales de stock vivo se
/// implementarán mediante casos de uso explícitos en N1.4.D.
/// </summary>
public sealed class UpdateExistenciaVarianteConfiguracionDto
{
    public int? UbicacionAlmacenId { get; set; }
    public int StockMinimo { get; set; }
    public int? StockMaximo { get; set; }
}

public sealed class ExistenciaVarianteFiltroDto : PagedRequest
{
    public ExistenciaVarianteFiltroDto()
    {
        SortBy = "ProductoVarianteId";
        SortDirection = "asc";
    }

    public int? ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public bool? SoloRaizAlmacen { get; set; }
    public bool? StockBajo { get; set; }
    public bool? Agotada { get; set; }
}

public readonly record struct ExistenciaVarianteClaveDto(
    int ProductoVarianteId,
    int AlmacenId,
    int? UbicacionAlmacenId);
