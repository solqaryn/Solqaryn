namespace Solqaryn.Application.Common;

/// <summary>
/// Marcador técnico de ERP-N0.4. La autorización efectiva se resuelve exclusivamente
/// mediante Usuario.RolId -> RolPermiso.PermisoId -> Permiso.
/// </summary>
internal static class RbacN04Marker
{
    public const string ModeloAutoridad = "Usuario.RolId->RolPermiso.PermisoId->Permiso";
}
