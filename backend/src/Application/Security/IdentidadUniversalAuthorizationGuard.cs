using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.Security;

/// <summary>
/// C01-01.M03.03: guardia de autorizacion Application-only.
/// Solo admite contexto previamente verificado y una instantanea actual
/// recargada desde el servidor; NO configura endpoints ni concede permisos.
/// </summary>
public sealed class IdentidadUniversalAuthorizationGuard
{
    private readonly IIdentidadUniversalSnapshotSource _source;

    public IdentidadUniversalAuthorizationGuard(IIdentidadUniversalSnapshotSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public async Task<ContextoTenantActual> ExigirAccesoActualAsync(
        ContextoTenantActual autenticado,
        int empresaId,
        ModuloSistema modulo,
        AccionPermiso accion,
        int? sucursalId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(autenticado);

        // Antes de hacer I/O, nunca permitir que un actor del tenant A
        // consulte un recurso o membresia del tenant B.
        autenticado.ExigirEmpresa(empresaId);

        if (sucursalId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(sucursalId));

        if (!Enum.IsDefined(modulo) || !Enum.IsDefined(accion))
            throw new InvalidOperationException("Operacion no reconocida.");

        var snapshot = await _source.ObtenerActualAsync(
            autenticado.UsuarioId, empresaId, sucursalId, cancellationToken)
            ?? throw new InvalidOperationException("Sin membresia vigente para la operacion.");

        var actual = IdentidadUniversalReglas.CrearContextoVerificado(
            snapshot.Usuario, snapshot.Empresa, snapshot.Membresia, snapshot.Rol);

        // Los roles legacy del usuario, cookies, JWTs obsoletos y datos de
        // entrada no pueden ampliar el rol de la membresia autenticada.
        if (actual.UsuarioId != autenticado.UsuarioId ||
            actual.EmpresaId != autenticado.EmpresaId ||
            actual.RolId != autenticado.RolId)
            throw new InvalidOperationException("La identidad o el rol vigente ha cambiado.");

        if (sucursalId.HasValue)
        {
            if (snapshot.Sucursal is null ||
                snapshot.Sucursal.Id != sucursalId.Value)
                throw new InvalidOperationException("La sucursal no esta verificada.");

            IdentidadUniversalReglas.ExigirSucursal(actual, snapshot.Sucursal);
        }
        else if (snapshot.Sucursal is not null)
        {
            throw new InvalidOperationException(
                "La fuente no debe incluir sucursales no solicitadas.");
        }

        // Requiere una asignacion RELACIONAL explicita y un permiso activo
        // del plano empresarial. No existe bypass para rol administrador.
        var autorizado = snapshot.Rol.Permisos?.Any(asignacion =>
            asignacion.RolId == actual.RolId &&
            asignacion.PermisoId > 0 &&
            asignacion.Permiso is
            {
                Id: > 0,
                Activo: true,
                Eliminado: false,
                Ambito: AmbitoAutorizacion.Empresa
            } permiso &&
            permiso.Id == asignacion.PermisoId &&
            permiso.Modulo == modulo &&
            permiso.Accion == accion) ?? false;

        if (!autorizado)
            throw new InvalidOperationException("Permiso empresarial vigente no acreditado.");

        return actual;
    }
}
