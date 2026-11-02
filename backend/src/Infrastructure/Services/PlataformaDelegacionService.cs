using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Capacidad global delegada de lectura, verificada contra tablas vivas de
/// identidad/RBAC de MySQL en cada solicitud. No permite acceso ERP a tenants.
/// Un rol global de sistema queda fuera de esta autorización de delegado.
/// </summary>
public sealed class PlataformaDelegacionService : IPlataformaDelegacionService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;

    public PlataformaDelegacionService(AppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<UsuarioPlataformaScopeActual?> ObtenerDelegadoActualAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_current.EstaAutenticado || _current.UsuarioId is not > 0 ||
            string.IsNullOrWhiteSpace(_current.NombreUsuario))
            return null;

        var usuarioId = _current.UsuarioId.Value;
        // Una única asignación global viva. También se rechazan dos roles
        // simultáneos aunque solo uno tenga el grant delegado.
        var asignaciones = await _db.Set<UsuarioRolPlataforma>()
            .IgnoreQueryFilters().AsNoTracking()
            .CountAsync(x => x.UsuarioId == usuarioId && x.Activa, cancellationToken);
        if (asignaciones != 1)
            return null;

        // IgnoreQueryFilters se limita a identidad y RBAC con todos sus estados
        // explícitos; nunca omite filtros de consultas comerciales.
        var autorizaciones = await (
            from asignacion in _db.Set<UsuarioRolPlataforma>()
                .IgnoreQueryFilters().AsNoTracking()
            join usuario in _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
                on asignacion.UsuarioId equals usuario.Id
            join rol in _db.Roles.IgnoreQueryFilters().AsNoTracking()
                on asignacion.RolId equals rol.Id
            join grant in _db.RolPermisos.IgnoreQueryFilters().AsNoTracking()
                on rol.Id equals grant.RolId
            join permiso in _db.Permisos.IgnoreQueryFilters().AsNoTracking()
                on grant.PermisoId equals permiso.Id
            where asignacion.UsuarioId == usuarioId && asignacion.Activa
                && usuario.Activo && !usuario.Bloqueado && !usuario.Eliminado
                && rol.Activo && !rol.Eliminado && rol.EsAdministrador
                && !rol.EsSistema && rol.EmpresaId == null
                && rol.Ambito == AmbitoAutorizacion.Plataforma
                && rol.NombreNormalizado == PoliticaDelegacionPlataforma.RolNombreNormalizado
                && permiso.Activo && !permiso.Eliminado
                && permiso.Codigo == PoliticaDelegacionPlataforma.CodigoPermiso
                && permiso.Ambito == AmbitoAutorizacion.Plataforma
                && permiso.Modulo == ModuloSistema.Configuracion
                && permiso.Accion == AccionPermiso.ConsultarHistorial
            select new UsuarioPlataformaScopeActual(
                usuario.Id, rol.Id, usuario.NombreUsuario, rol.Nombre)
        ).Take(2).ToArrayAsync(cancellationToken);

        return autorizaciones.Length == 1 &&
               string.Equals(autorizaciones[0].NombreUsuario,
                   _current.NombreUsuario, StringComparison.Ordinal)
            ? autorizaciones[0] : null;
    }
}
