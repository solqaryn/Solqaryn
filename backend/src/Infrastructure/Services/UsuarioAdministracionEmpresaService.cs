using System.Data;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// P1-005.A: operaciones administrativas limitadas al tenant. Revalida
/// membresia/rol vivo desde MySQL; un JWT o cabecera no conceden autoridad.
/// El alta vincula usuario y membresia en la misma transaccion MySQL.
/// </summary>
public sealed class UsuarioAdministracionEmpresaService : IUsuarioAdministracionEmpresaService
{
    private readonly AppDbContext _db;
    private readonly IUsuarioService _usuarios;
    private readonly IUsuarioScopeService _scope;
    private readonly IPermisoService _permisos;
    private readonly ICurrentUserService _actual;
    private readonly IAuditoriaService _auditoria;

    public UsuarioAdministracionEmpresaService(
        AppDbContext db, IUsuarioService usuarios, IUsuarioScopeService scope,
        IPermisoService permisos, ICurrentUserService actual, IAuditoriaService auditoria)
    {
        _db = db;
        _usuarios = usuarios;
        _scope = scope;
        _permisos = permisos;
        _actual = actual;
        _auditoria = auditoria;
    }

    private async Task<int> ExigirAdministradorAsync(
        int empresaId, AccionPermiso accion, CancellationToken ct)
    {
        if (empresaId <= 0 || !_actual.EstaAutenticado || _actual.UsuarioId is not > 0)
            throw new ForbiddenAccessException("Se requiere empresa y sesión válida.");

        var actor = await _scope.ObtenerActualAsync(empresaId, ct);
        if (actor is null || !actor.EsAdministrador || actor.UsuarioId != _actual.UsuarioId ||
            actor.EmpresaId != empresaId)
            throw new ForbiddenAccessException("Solo un administrador vigente de esta empresa puede administrar usuarios.");

        await _permisos.VerificarPermisoAsync(
            empresaId, ModuloSistema.Usuarios, accion);
        return actor.UsuarioId;
    }

    private async Task ExigirRolEmpresarialAsync(
        int? rolId, int empresaId, CancellationToken ct)
    {
        if (rolId is not > 0)
            throw new BusinessRuleException("Seleccione un rol empresarial válido.");

        var rol = await _db.Roles.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == rolId.Value, ct);
        if (rol is null || !rol.EsAsignableAEmpresa(empresaId))
            throw new ForbiddenAccessException(
                "No se puede utilizar un rol de otra empresa, de plataforma o sin dueño.");
    }

    public async Task<UsuarioDto> CrearAsync(
        CreateUsuarioDto dto, int empresaIdAutorizada, CancellationToken ct = default)
    {
        // SERIALIZABLE: autorización, titularidad y mutación comparten snapshot
        // transaccional; una revocación concurrente no deja un alta huérfana.
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var actor = await ExigirAdministradorAsync(
            empresaIdAutorizada, AccionPermiso.Crear, ct);
        await _permisos.VerificarPermisoAsync(
            empresaIdAutorizada, ModuloSistema.Usuarios, AccionPermiso.AsignarRol);
        await ExigirRolEmpresarialAsync(dto.RolId, empresaIdAutorizada, ct);

        // Usuario es global: no resolver ni reutilizar una identidad de otro tenant.
        var nombre = dto.NombreUsuario?.Trim().ToLowerInvariant() ?? string.Empty;
        if (await _db.Usuarios.IgnoreQueryFilters().AsNoTracking().AnyAsync(
                u => !u.Eliminado && u.NombreUsuario.ToLower() == nombre, ct))
            throw new BusinessRuleException("El nombre de usuario no está disponible.");

        var creado = await _usuarios.CreateAsync(dto);
        var membresia = new UsuarioEmpresa(
            creado.Id, empresaIdAutorizada, dto.RolId!.Value)
        {
            CreadoPorUsuarioId = actor
        };
        _db.UsuarioEmpresas.Add(membresia);
        // Auditoria crítica: si falla, la transacción revierte alta y membresía.
        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.AsignarRol,
            "Alta administrativa de usuario y primera membresía empresarial.",
            creado.Id, entidad: "UsuarioEmpresa",
            valoresNuevos: new { EmpresaId = empresaIdAutorizada, RolId = dto.RolId });
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return creado;
    }

    public async Task<UsuarioDto?> ActualizarAsync(
        int usuarioId, UpdateUsuarioDto dto, int empresaIdAutorizada,
        CancellationToken ct = default)
    {
        if (usuarioId <= 0)
            throw new ForbiddenAccessException("Usuario inválido.");

        // El scope vivo, las membresías (incluidas inactivas) y el cambio de
        // rol se leen/aplican en una única transacción serializable.
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        await ExigirAdministradorAsync(
            empresaIdAutorizada, AccionPermiso.Editar, ct);
        await ExigirRolEmpresarialAsync(dto.RolId, empresaIdAutorizada, ct);

        // El usuario puede tener identidad global, pero un administrador de A
        // nunca modifica atributos compartidos de alguien con membresía en B,
        // incluso si esa segunda membresía está inactiva.
        var membresias = await _db.UsuarioEmpresas.IgnoreQueryFilters()
            .AsNoTracking().Where(m => m.UsuarioId == usuarioId)
            .Select(m => new { m.EmpresaId, m.Activa }).ToListAsync(ct);
        if (membresias.Count != 1 ||
            membresias[0].EmpresaId != empresaIdAutorizada ||
            !membresias[0].Activa)
            throw new ForbiddenAccessException(
                "No se puede editar una identidad ajena o compartida con otra empresa.");

        // No administrar ni restaurar credenciales de identidades con autoridad
        // de plataforma, aunque tengan simultáneamente una membresía tenant.
        if (await _db.UsuarioRolesPlataforma.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(g => g.UsuarioId == usuarioId, ct))
            throw new ForbiddenAccessException(
                "Una identidad de plataforma no puede editarse desde una empresa.");

        var usuario = await _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId && !u.Eliminado, ct);
        if (usuario is null) return null;
        if (await _db.Roles.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(r => r.Id == usuario.RolId &&
                r.Ambito == AmbitoAutorizacion.Plataforma, ct))
            throw new ForbiddenAccessException(
                "Una identidad con rol global heredado no puede editarse desde una empresa.");

        if (usuario.RolId != dto.RolId)
            await _permisos.VerificarPermisoAsync(
                empresaIdAutorizada, ModuloSistema.Usuarios, AccionPermiso.AsignarRol);
        if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
            await _permisos.VerificarPermisoAsync(
                empresaIdAutorizada, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena);

        var cambioRol = usuario.RolId != dto.RolId;
        var actualizado = await _usuarios.UpdateAsync(usuarioId, dto);
        if (actualizado is null) return null;

        if (cambioRol)
        {
            // El rol efectivo procede de UsuarioEmpresa y no de Usuario.RolId.
            // Actualizar ambos dentro de la MISMA transacción preserva RBAC vivo.
            var membresia = await _db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == usuarioId && m.EmpresaId == empresaIdAutorizada, ct);
            membresia.CambiarRol(dto.RolId!.Value);
            membresia.ActualizadoPorUsuarioId = _actual.UsuarioId;
            membresia.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.Editar,
            "Edición administrativa de identidad empresarial propia.",
            usuarioId, entidad: "Usuario",
            valoresNuevos: new { EmpresaId = empresaIdAutorizada, RolId = dto.RolId });
        if (tx is not null) await tx.CommitAsync(ct);
        return actualizado;
    }
    /// <summary>
    /// Soft-delete a user that belongs exclusively to the authorized tenant.
    /// Never use the legacy global-user repository for tenant deletion: its
    /// unspecialized read scope triggers DEV quarantine (HTTP 503). Verify all
    /// memberships and platform provenance before bypassing global read filters.
    /// </summary>
    public async Task<bool> EliminarAsync(
        int usuarioId, int empresaIdAutorizada, CancellationToken ct = default)
    {
        if (usuarioId <= 0)
            throw new ForbiddenAccessException("Usuario inválido.");

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var actorId = await ExigirAdministradorAsync(
            empresaIdAutorizada, AccionPermiso.EliminarLogico, ct);
        if (usuarioId == actorId)
            throw new BusinessRuleException("No puedes eliminar tu propia cuenta.");

        // Membership is global, so check EVERY tenant, including inactive ones,
        // before touching a shared identity. A JWT/header alone never suffices.
        var memberships = await _db.UsuarioEmpresas.IgnoreQueryFilters()
            .Where(m => m.UsuarioId == usuarioId).ToListAsync(ct);
        if (memberships.Count != 1 ||
            memberships[0].EmpresaId != empresaIdAutorizada ||
            !memberships[0].Activa)
            throw new ForbiddenAccessException(
                "No se puede eliminar una identidad ajena o compartida con otra empresa.");

        // Even inactive platform grants encode platform provenance.
        if (await _db.UsuarioRolesPlataforma.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(g => g.UsuarioId == usuarioId, ct))
            throw new ForbiddenAccessException(
                "Una identidad de plataforma no puede eliminarse desde una empresa.");

        var usuario = await _db.Usuarios.IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Id == usuarioId && !u.Eliminado, ct);
        if (usuario is null) return false;
        if (await _db.Roles.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(r => r.Id == usuario.RolId &&
                r.Ambito == AmbitoAutorizacion.Plataforma, ct))
            throw new ForbiddenAccessException(
                "Una identidad con rol global heredado no puede eliminarse desde una empresa.");

        // ExigirAdministradorAsync already guarantees an independent ACTIVE
        // administrator in this same tenant (the actor is never the target).
        var now = DateTime.UtcNow;
        usuario.Eliminado = true;
        usuario.Activo = false;
        usuario.FechaEliminacion = now;
        usuario.EliminadoPorUsuarioId = actorId;
        usuario.ActualizadoPorUsuarioId = actorId;
        usuario.FechaActualizacion = now;
        memberships[0].Desactivar();
        memberships[0].ActualizadoPorUsuarioId = actorId;
        memberships[0].FechaActualizacion = now;

        // Durable audit and both revoked states share the same transaction.
        await _db.SaveChangesAsync(ct);
        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.EliminarLogico,
            "Eliminación lógica de usuario empresarial exclusivo.",
            usuarioId, entidad: "Usuario",
            valoresNuevos: new { EmpresaId = empresaIdAutorizada,
                Eliminado = true, Activo = false, MembresiaActiva = false });
        if (tx is not null) await tx.CommitAsync(ct);
        return true;
    }

}
