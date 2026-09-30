namespace Solqaryn.Application.DTOs;

public sealed class AlmacenFiltroDto
{
    public string? Buscar { get; set; }
    public int? SucursalId { get; set; }
    public string? Tipo { get; set; }
    public bool? Activo { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public sealed class AlmacenPaginaDto
{
    public IReadOnlyList<AlmacenDto> Items { get; set; } = Array.Empty<AlmacenDto>();
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}
