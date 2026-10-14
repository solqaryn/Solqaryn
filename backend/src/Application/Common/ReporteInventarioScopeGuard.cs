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

    // La sobrecarga histórica NO es autoridad para seleccionar un almacén:
    // UsuarioScopeActual proviene del rol global legado, no de UsuarioEmpresa.
    public static Task<bool> CanUseExplicitPhysicalScopeAsync(
        ReporteInventarioFiltroBaseDto filtro,
        IUsuarioScopeService usuarioScope) =>
        Task.FromResult(!HasExplicitPhysicalScope(filtro));

    /// <summary>
    /// Autoriza dimensiones físicas solo tras verificar membresía activa en
    /// la empresa ya autorizada por el filtro RBAC. Nunca acepta rol global.
    /// </summary>
    public static async Task<bool> CanUseExplicitPhysicalScopeAsync(
        ReporteInventarioFiltroBaseDto filtro,
        IUsuarioScopeService usuarioScope,
        int empresaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        ArgumentNullException.ThrowIfNull(usuarioScope);
        if (!HasExplicitPhysicalScope(filtro))
            return true;
        if (empresaId <= 0)
            return false;

        var alcance = await usuarioScope.ObtenerActualAsync(empresaId, cancellationToken);
        return alcance is { EsAdministrador: true } && alcance.EmpresaId == empresaId;
    }
}
