namespace Solqaryn.Application.Common;

/// <summary>
/// Reglas transversales del cierre ERP-N0.4.
/// </summary>
public static class RbacN04Authority
{
    public const string Modelo = "Usuario.RolId -> Rol -> RolPermiso.PermisoId -> Permiso";
}
