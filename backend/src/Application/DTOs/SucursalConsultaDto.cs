namespace Solqaryn.Application.DTOs;

public sealed class SucursalFiltroDto
{
    public string? Buscar { get; set; }
    public bool? Activa { get; set; }
    public int? EmpresaId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public sealed class SucursalPaginaDto
{
    public IReadOnlyList<SucursalDto> Items { get; set; } = Array.Empty<SucursalDto>();
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}
