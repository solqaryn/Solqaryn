namespace Solqaryn.Application.DTOs;

public class RolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsSistema { get; set; }
    public bool EsAdministrador { get; set; }
    public bool Activo { get; set; }
    public int CantidadUsuarios { get; set; }
    public int CantidadPermisos { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public class CrearRolDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsAdministrador { get; set; }
}

public class ActualizarRolDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
