namespace Solqaryn.Application.DTOs;

public class PermisoCatalogoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public bool EsSistema { get; set; }
    public bool Activo { get; set; }
    public int CantidadRolesAsignados { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public class CrearPermisoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
}

public class ActualizarPermisoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
