namespace Solqaryn.Application.DTOs;

public sealed class AjusteStockRequest
{
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int CantidadActualEsperada { get; set; }
    public int CantidadNueva { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class AjusteStockResultadoDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int CantidadAnterior { get; set; }
    public int CantidadNueva { get; set; }
    public int Diferencia { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
