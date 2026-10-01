namespace Solqaryn.Domain.Entities;

/// <summary>
/// Grant relacional explícito entre un rol y un permiso.
/// La existencia de la fila significa permitido; su ausencia significa denegado.
/// </summary>
public class RolPermiso
{
    public int Id { get; set; }

    public int RolId { get; set; }
    public Rol RolEntidad { get; set; } = null!;

    public int PermisoId { get; set; }
    public Permiso Permiso { get; set; } = null!;
}
