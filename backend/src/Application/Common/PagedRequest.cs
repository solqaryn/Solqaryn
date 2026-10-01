using System.Text.Json.Serialization;

namespace Solqaryn.Application.Common;

public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 200);
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; } = "Nombre";
    public string? SortDirection { get; set; } = "asc"; // asc | desc

    /// <summary>
    /// Alcance interno aplicado por los servicios para usuarios no administradores.
    /// Nunca se acepta desde la petición HTTP: el servicio lo sobrescribe usando
    /// ICurrentUserService.UsuarioId antes de consultar el repositorio.
    /// </summary>
    [JsonIgnore]
    public int? UsuarioIdScope { get; set; }
}
