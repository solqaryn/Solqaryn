using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// P1-004.A: delegacion de solo lectura a administradores de plataforma
/// NO superadmin. Exige identidad viva verificada y grant relacional exacto.
/// No administra grants ni abre por si misma acceso a recursos empresariales.
/// </summary>
public sealed class PlataformaDelegacionService : IPlataformaDelegacionService
{
    private readonly AppDbContext _db;
    private readonly IPlataformaScopeService _scope;

    public PlataformaDelegacionService(AppDbContext db, IPlataformaScopeService scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<bool> TienePermisoDelegadoAsync(
        ModuloSistema modulo,
        AccionPermiso accion,
        CancellationToken cancellationToken = default)
    {
        // PlataformaScopeService comprueba en MySQL el usuario y la asignacion
        // global viva en CADA llamada: nunca confiar en un claim recibido.
        var actual = await _scope.ObtenerActualAsync(cancellationToken);
        if (actual is null || actual.RolId <= 0 || actual.UsuarioId <= 0)
            return false;

        // La autoridad viva y el grant se comprueban juntos en la consulta
        // final. El scope previo por si solo no autoriza si una asignacion o
        // identidad cambia entre la primera validacion y esta lectura.
        // IgnoreQueryFilters se limita a identidad/RBAC, con estados explicitos.
        return await (
            from asignacion in _db.Set<UsuarioRolPlataforma>().AsNoTracking().IgnoreQueryFilters()
            join usuario in _db.Usuarios.AsNoTracking().IgnoreQueryFilters()
                on asignacion.UsuarioId equals usuario.Id
            join rol in _db.Roles.AsNoTracking().IgnoreQueryFilters()
                on asignacion.RolId equals rol.Id
            join grant in _db.Set<RolPermiso>().AsNoTracking().IgnoreQueryFilters()
                on rol.Id equals grant.RolId
            join permiso in _db.Set<Permiso>().AsNoTracking().IgnoreQueryFilters()
                on grant.PermisoId equals permiso.Id
            where asignacion.UsuarioId == actual.UsuarioId
                  && asignacion.RolId == actual.RolId
                  && asignacion.Activa
                  && usuario.Activo
                  && !usuario.Bloqueado
                  && !usuario.Eliminado
                  && usuario.NombreUsuario == actual.NombreUsuario
                  && rol.Ambito == AmbitoAutorizacion.Plataforma
                  && rol.EmpresaId == null
                  && rol.EsAdministrador
                  && !rol.EsSistema
                  && rol.Activo
                  && !rol.Eliminado
                  && permiso.Ambito == AmbitoAutorizacion.Plataforma
                  && permiso.Activo
                  && !permiso.Eliminado
                  && permiso.Modulo == modulo
                  && permiso.Accion == accion
            select grant.Id
        ).AnyAsync(cancellationToken);
    }
}
