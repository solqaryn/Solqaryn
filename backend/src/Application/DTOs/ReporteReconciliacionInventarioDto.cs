namespace Solqaryn.Application.DTOs;

public enum OrigenReconciliacionInventario
{
    Conteo = 1,
    Transferencia = 2,
    Ajuste = 3
}

public class ReporteInventarioReconciliacionFiltroDto : ReporteInventarioFiltroBaseDto
{
    public int? ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? Sku { get; set; }
    public OrigenReconciliacionInventario? Origen { get; set; }
}

public class ReporteInventarioReconciliacionDto
{
    public DateTime Fecha { get; set; }
    public int? AlmacenId { get; set; }
    public string? AlmacenNombre { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public string? UbicacionNombre { get; set; }

    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? Sku { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Color { get; set; }
    public string? Talla { get; set; }

    public OrigenReconciliacionInventario Origen { get; set; }
    public int DocumentoId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }

    public int CantidadEsperada { get; set; }
    public int CantidadObservada { get; set; }
    public int DiferenciaNormalizada { get; set; }

    public int? CantidadFaltante { get; set; }
    public int? CantidadSobrante { get; set; }
    public int? CantidadDanada { get; set; }

    public decimal? CostoUnitario { get; set; }
    public decimal? ImpactoCosto { get; set; }

    public int? UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public string? Observaciones { get; set; }
}
