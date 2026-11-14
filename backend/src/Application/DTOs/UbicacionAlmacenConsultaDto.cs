namespace Solqaryn.Application.DTOs;

public sealed class UbicacionAlmacenFiltroDto
{
    public string? Buscar { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionPadreId { get; set; }
    public bool SoloRaiz { get; set; }
    public string? Tipo { get; set; }
    public bool? Activa { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public sealed class UbicacionAlmacenPaginaDto
{
    public IReadOnlyList<UbicacionAlmacenDto> Items { get; set; } = Array.Empty<UbicacionAlmacenDto>();
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}
