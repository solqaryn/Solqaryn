using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Resuelve el contexto de seguridad vigente desde MySQL. El JWT identifica la
/// sesión, pero la autoridad tenant-aware se deriva siempre de UsuarioEmpresa.
/// </summary>
public sealed class UsuarioScopeService : IUsuarioScopeService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UsuarioScopeService(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Camino legacy conservado para no romper consumidores aún no tenant-aware.
    /// No debe utilizarse para autorizar recursos pertenecientes a una Empresa.
    /// </summary>
    public async Task<UsuarioScopeActual?> ObtenerActualAsync()
    {
        if (!_currentUser.EstaAutenticado || !_currentUser.UsuarioId.HasValue)
            return null;

        var usuarioId = _currentUser.UsuarioId.Value;
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u =>
                u.Id == usuarioId &&
                !u.Eliminado &&
                u.Activo &&
                !u.Bloqueado &&
                u.RolId > 0 &&
                u.RolEntidad.Activo &&
                !u.RolEntidad.Eliminado)
            .Select(u => new UsuarioScopeActual(
                u.Id,
                u.RolId,
                u.RolEntidad.Nombre,
                u.RolEntidad.EsAdministrador))
            .SingleOrDefaultAsync();
    }

    public async Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
        int empresaId,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0 ||
            !_currentUser.EstaAutenticado ||
            !_currentUser.UsuarioId.HasValue)
        {
            return null;
        }

        var usuarioId = _currentUser.UsuarioId.Value;
        return await ContextosTenantValidos(usuarioId, empresaId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UsuarioTenantScopeActual?> ObtenerUnicoActualAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.EstaAutenticado || !_currentUser.UsuarioId.HasValue)
            return null;

        var candidatos = await ContextosTenantValidos(_currentUser.UsuarioId.Value)
            .Take(2)
            .ToListAsync(cancellationToken);

        return candidatos.Count == 1 ? candidatos[0] : null;
    }

    private IQueryable<UsuarioTenantScopeActual> ContextosTenantValidos(
        int usuarioId,
        int? empresaId = null)
    {
        var empresas = _context.Set<Empresa>().AsNoTracking();

        return
            from membresia in _context.UsuarioEmpresas.AsNoTracking()
            join usuario in _context.Usuarios.AsNoTracking()
                on membresia.UsuarioId equals usuario.Id
            join empresa in empresas
                on membresia.EmpresaId equals empresa.Id
            join rol in _context.Roles.AsNoTracking()
                on membresia.RolId equals rol.Id
            where membresia.UsuarioId == usuarioId &&
                  membresia.Activa &&
                  usuario.Activo &&
                  !usuario.Bloqueado &&
                  !usuario.Eliminado &&
                  empresa.Activa &&
                  rol.Activo &&
                  !rol.Eliminado &&
                  (!empresaId.HasValue || membresia.EmpresaId == empresaId.Value)
            select new UsuarioTenantScopeActual(
                usuario.Id,
                empresa.Id,
                rol.Id,
                rol.Nombre,
                rol.EsAdministrador);
    }
}
