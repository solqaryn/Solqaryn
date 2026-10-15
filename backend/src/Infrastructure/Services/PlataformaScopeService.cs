using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Validación por solicitud de autoridad global vigente en MySQL.
/// JWT solo identifica al usuario: nunca autoriza por claims rol/esAdministrador,
/// por Usuario.RolId ni por X-Empresa-Id.
/// </summary>
public sealed class PlataformaScopeService : IPlataformaScopeService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;

    public PlataformaScopeService(AppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<UsuarioPlataformaScopeActual?> ObtenerActualAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_current.EstaAutenticado ||
            _current.UsuarioId is not > 0 ||
            string.IsNullOrWhiteSpace(_current.NombreUsuario))
            return null;

        var usuarioId = _current.UsuarioId.Value;
        // IgnoreQueryFilters solo para tablas de IDENTIDAD: comprobar estado
        // explícitamente. No se lee ni modifica ningún dato comercial.
        var matching = await (
            from asignacion in _db.Set<UsuarioRolPlataforma>().IgnoreQueryFilters()
            join usuario in _db.Usuarios.IgnoreQueryFilters()
                on asignacion.UsuarioId equals usuario.Id
            join rol in _db.Roles.IgnoreQueryFilters()
                on asignacion.RolId equals rol.Id
            where asignacion.UsuarioId == usuarioId &&
                  asignacion.Activa &&
                  usuario.Activo && !usuario.Bloqueado && !usuario.Eliminado &&
                  rol.Activo && !rol.Eliminado && rol.EsAdministrador &&
                  rol.Ambito == AmbitoAutorizacion.Plataforma &&
                  rol.EmpresaId == null
            select new UsuarioPlataformaScopeActual(
                usuario.Id, rol.Id, usuario.NombreUsuario, rol.Nombre)
        ).AsNoTracking().Take(2).ToArrayAsync(cancellationToken);

        // Múltiples roles globales administrativos o JWT de identidad obsoleta:
        // fallo cerrado, no se escoge arbitrariamente una asignación.
        return matching.Length == 1 &&
               string.Equals(matching[0].NombreUsuario,
                   _current.NombreUsuario, StringComparison.Ordinal)
            ? matching[0]
            : null;
    }
}
