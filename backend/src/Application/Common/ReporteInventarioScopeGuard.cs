using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;

namespace Solqaryn.Application.Common;

/// <summary>
/// Fail-closed guard for explicit physical-scope filters in analítica de inventario.
/// The current canonical user scope exposes administrator/global access and
/// row ownership, but no independent sucursal/almacen grant model. Therefore
/// a non-admin request cannot safely assert an explicit physical scope: it must
/// use the unfiltered endpoint, which is still restricted server-side by the
/// row-level scope in the repository/service query.
/// </summary>
public static class ReporteInventarioScopeGuard
{
    public static bool HasExplicitPhysicalScope(ReporteInventarioFiltroBaseDto filtro) =>
        filtro.SucursalId.HasValue || filtro.AlmacenId.HasValue || filtro.UbicacionAlmacenId.HasValue;

    public static async Task<bool> CanUseExplicitPhysicalScopeAsync(
        ReporteInventarioFiltroBaseDto filtro,
        IUsuarioScopeService usuarioScope)
    {
        if (!HasExplicitPhysicalScope(filtro))
            return true;

        var alcance = await usuarioScope.ObtenerActualAsync();
        return alcance is not null && alcance.EsAdministrador;
    }
}
