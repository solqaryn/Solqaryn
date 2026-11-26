using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
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

        // La autorización sin EmpresaId sólo pertenece al modelo monoempresa.
        // Si la acción ya pasó el filtro RBAC, el rol efectivo siempre procede
        // de UsuarioEmpresa, nunca de Usuario.RolId heredado.
        var empresaId = _context.AuthorizedCommercialEmpresaId;
        if (empresaId is > 0)
        {
            var scope = await ObtenerActualAsync(empresaId.Value);
            return scope is null || scope.EmpresaId != empresaId.Value
                ? null
                : new UsuarioScopeActual(
                    scope.UsuarioId, scope.RolId, scope.RolNombre, scope.EsAdministrador);
        }

        // Background services y llamadas sin HTTP no tienen marca: con 2+
        // empresas (activas o inactivas) no se puede inferir un propietario.
        var empresas = await _context.Set<Empresa>()
            .IgnoreQueryFilters().AsNoTracking().Take(2).CountAsync();
        if (empresas > 1)
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

    // The base session role and the selected tenant membership role are separate.
    // A signed but stale base role cannot enter or switch tenant context.
    public async Task<bool> SesionBaseVigenteAsync(int actorId, int rolId,
        CancellationToken cancellationToken = default)
    {
        if (actorId <= 0 || rolId <= 0 || !_currentUser.EstaAutenticado ||
            _currentUser.UsuarioId != actorId)
            return false;

        var baseRole = await _context.Usuarios.AsNoTracking()
            .Where(u => u.Id == actorId && u.RolId == rolId &&
                u.Activo && !u.Bloqueado && !u.Eliminado &&
                u.RolEntidad.Id == rolId && u.RolEntidad.Activo && !u.RolEntidad.Eliminado)
            .Select(u => new { u.RolEntidad.Ambito, u.RolEntidad.EmpresaId, u.RolEntidad.EsSistema })
            .SingleOrDefaultAsync(cancellationToken);
        if (baseRole is null)
            return false;

        if (baseRole.Ambito == AmbitoAutorizacion.Plataforma)
            return baseRole.EmpresaId == null && baseRole.EsSistema;
        if (baseRole.Ambito != AmbitoAutorizacion.Empresa ||
            (baseRole.EmpresaId == null && !baseRole.EsSistema) ||
            baseRole.EmpresaId is <= 0)
            return false;

        // Same live, tenant-owned membership predicate as token issuance.
        // Never use a selected tenant's role to resurrect a revoked base session.
        return await ContextosTenantValidos(actorId, baseRole.EmpresaId, rolId)
            .AnyAsync(cancellationToken);
    }

    public async Task<UsuarioTenantScopeActual?> ObtenerSucursalActualAsync(
        int actorId, int rolBaseId, int empresaId, int sucursalId,
        CancellationToken cancellationToken = default)
    {
        if (actorId <= 0 || rolBaseId <= 0 || empresaId <= 0 || sucursalId <= 0 ||
            !_currentUser.EstaAutenticado || _currentUser.UsuarioId != actorId)
            return null;

        async Task<bool> SucursalYPermisoVigentesAsync(int rolId) =>
            await _context.Set<Sucursal>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(x => x.Id == sucursalId && x.EmpresaId == empresaId &&
                    x.Activa && !x.Eliminado, cancellationToken) &&
            await PermisoSucursalVigenteAsync(rolId, cancellationToken);

        if (!await BaseComercialVigenteAsync(actorId, rolBaseId, cancellationToken))
            return null;
        var scope = await ObtenerActualAsync(empresaId, cancellationToken);
        if (scope is null || scope.UsuarioId != actorId || scope.EmpresaId != empresaId ||
            scope.RolId <= 0 || !await SucursalYPermisoVigentesAsync(scope.RolId))
            return null;
        if (!await BaseComercialVigenteAsync(actorId, rolBaseId, cancellationToken))
            return null;
        var fresh = await ObtenerActualAsync(empresaId, cancellationToken);
        if (fresh is null || fresh != scope ||
            !await SucursalYPermisoVigentesAsync(fresh.RolId) ||
            !_currentUser.EstaAutenticado || _currentUser.UsuarioId != actorId)
            return null;

        return fresh;
    }

    private async Task<bool> BaseComercialVigenteAsync(
        int actorId, int rolBaseId, CancellationToken cancellationToken) =>
        await SesionBaseVigenteAsync(actorId, rolBaseId, cancellationToken) &&
        await _context.Usuarios.AsNoTracking().AnyAsync(u =>
            u.Id == actorId && u.RolId == rolBaseId && u.Activo &&
            !u.Bloqueado && !u.Eliminado && u.RolEntidad.Activo &&
            !u.RolEntidad.Eliminado &&
            u.RolEntidad.Ambito == AmbitoAutorizacion.Empresa, cancellationToken);

    private Task<bool> PermisoSucursalVigenteAsync(int rolId, CancellationToken cancellationToken) =>
        (from grant in _context.RolPermisos.IgnoreQueryFilters().AsNoTracking()
         join permiso in _context.Permisos.IgnoreQueryFilters().AsNoTracking()
             on grant.PermisoId equals permiso.Id
         where grant.RolId == rolId && permiso.Activo && !permiso.Eliminado &&
               permiso.Ambito == AmbitoAutorizacion.Empresa &&
               permiso.Modulo == ModuloSistema.Sucursales &&
               permiso.Accion == AccionPermiso.Ver
         select grant.Id).AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<SucursalSeleccionable>?> ListarSucursalesSeleccionablesAsync(
        int actorId, int rolBaseId, int empresaId, CancellationToken cancellationToken = default)
    {
        if (actorId <= 0 || rolBaseId <= 0 || empresaId <= 0 ||
            !_currentUser.EstaAutenticado || _currentUser.UsuarioId != actorId)
            return null;

        async Task<UsuarioTenantScopeActual?> AuthorityAsync()
        {
            if (!await BaseComercialVigenteAsync(actorId, rolBaseId, cancellationToken))
                return null;
            var scope = await ObtenerActualAsync(empresaId, cancellationToken);
            return scope is not null && scope.UsuarioId == actorId &&
                scope.EmpresaId == empresaId && scope.RolId > 0 &&
                await PermisoSucursalVigenteAsync(scope.RolId, cancellationToken)
                ? scope : null;
        }

        Task<List<SucursalSeleccionable>> ChoicesAsync() =>
            _context.Set<Sucursal>().IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Activa && !x.Eliminado)
                .OrderBy(x => x.Id)
                .Select(x => new SucursalSeleccionable(x.Id, empresaId, x.Codigo, x.Nombre))
                .ToListAsync(cancellationToken);

        var authority = await AuthorityAsync();
        if (authority is null)
            return null;
        var choices = await ChoicesAsync();
        var currentAuthority = await AuthorityAsync();
        if (currentAuthority != authority)
            return null;
        var currentChoices = await ChoicesAsync();
        if (!choices.SequenceEqual(currentChoices) ||
            await AuthorityAsync() != authority ||
            !_currentUser.EstaAutenticado || _currentUser.UsuarioId != actorId)
            return null;

        // Selection metadata only. Issuance independently revalidates the chosen branch.
        return currentChoices;
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

    public async Task<IReadOnlyList<EmpresaSeleccionable>> ListarEmpresasSeleccionablesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.EstaAutenticado || !_currentUser.UsuarioId.HasValue)
            return Array.Empty<EmpresaSeleccionable>();

        // El nombre de la empresa y su autorización provienen de la misma
        // consulta relacional. No materializar IDs autorizados y reutilizarlos
        // después de una posible revocación de membresía, rol o empresa.
        var actorId = _currentUser.UsuarioId.Value;
        var empresas = await (
            from membresia in MembresiasTenantValidas(actorId)
            join empresa in _context.Set<Empresa>().AsNoTracking()
                on membresia.EmpresaId equals empresa.Id
            orderby empresa.Nombre, empresa.Id
            select new EmpresaSeleccionable(empresa.Id, empresa.Nombre))
            .TagWith("C0301_SELECTOR_CURRENT_MEMBERSHIPS")
            .ToListAsync(cancellationToken);

        return _currentUser.EstaAutenticado && _currentUser.UsuarioId == actorId
            ? empresas.DistinctBy(empresa => empresa.EmpresaId).ToArray()
            : Array.Empty<EmpresaSeleccionable>();
    }

    private IQueryable<UsuarioTenantScopeActual> ContextosTenantValidos(
        int usuarioId,
        int? empresaId = null,
        int? rolId = null) =>
        from membresia in MembresiasTenantValidas(usuarioId, empresaId, rolId)
        join rol in _context.Roles.AsNoTracking() on membresia.RolId equals rol.Id
        select new UsuarioTenantScopeActual(
            membresia.UsuarioId, membresia.EmpresaId, rol.Id, rol.Nombre,
            rol.EsAdministrador);

    // Predicado compartido antes de cualquier proyección DTO. Mantener todos
    // los joins y filtros de autorización en SQL, también para el selector.
    private IQueryable<UsuarioEmpresa> MembresiasTenantValidas(
        int usuarioId,
        int? empresaId = null,
        int? rolId = null)
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
                  // Nunca aceptar un rol de Plataforma como membresía Empresa,
                  // ni aplicar el rol personalizado de la empresa B sobre A.
                  rol.Ambito == AmbitoAutorizacion.Empresa &&
                  (rol.EmpresaId == empresa.Id ||
                   (rol.EmpresaId == null && rol.EsSistema)) &&
                  (!empresaId.HasValue || membresia.EmpresaId == empresaId.Value) &&
                  (!rolId.HasValue || rol.Id == rolId.Value)
            select membresia;
    }
}
