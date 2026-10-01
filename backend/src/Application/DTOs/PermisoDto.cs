namespace Solqaryn.Application.DTOs;

public class PermisoMatrizItemDto
{
    public string Rol { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public bool Permitido { get; set; }
}

public class UpdatePermisoMatrizDto
{
    public List<PermisoMatrizItemDto> Permisos { get; set; } = new();
}

public class MisPermisosDto
{
    public string Rol { get; set; } = string.Empty;
    public bool EsAdministrador { get; set; }
    public List<string> Permisos { get; set; } = new(); // formato "Modulo:Accion"
}
