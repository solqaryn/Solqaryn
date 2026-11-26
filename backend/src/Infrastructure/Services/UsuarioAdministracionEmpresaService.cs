using System.Data;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Identidad global y membresía empresarial no son intercambiables. Lecturas
/// proyectan el rol efectivo del tenant; cambios globales sólo sobre identidad
/// exclusiva sin autoridad de plataforma. Mutaciones y auditoría son atómicas.
/// </summary>
public sealed class UsuarioAdministracionEmpresaService : IUsuarioAdministracionEmpresaService
{
    private readonly AppDbContext _db;
    private readonly IUsuarioScopeService _scope;
    private readonly IPermisoService _permisos;
    private readonly ICurrentUserService _actual;

    public UsuarioAdministracionEmpresaService(AppDbContext db, IUsuarioScopeService scope,
        IPermisoService permisos, ICurrentUserService actual)
    {
        _db = db;
        _scope = scope;
        _permisos = permisos;
        _actual = actual;
    }

    private async Task<int> ExigirAsync(int empresaId, AccionPermiso accion, CancellationToken ct, bool administrar = true)
    {
        if (empresaId <= 0 || !_actual.EstaAutenticado || _actual.UsuarioId is not > 0)
            throw new ForbiddenAccessException("Se requiere una empresa y sesión autorizadas.");
        var actor = await _scope.ObtenerActualAsync(empresaId, ct);
        if (actor is null || actor.EmpresaId != empresaId || actor.UsuarioId != _actual.UsuarioId ||
            (administrar && !actor.EsAdministrador))
            throw new ForbiddenAccessException("Se requiere autoridad vigente en esta empresa.");
        if (!await RolesEmpresa(empresaId).AnyAsync(r => r.Id == actor.RolId &&
            r.Activo && !r.Eliminado && (!administrar || r.EsAdministrador), ct))
            throw new ForbiddenAccessException("El rol efectivo no pertenece al ámbito empresarial autorizado.");
        await _permisos.VerificarPermisoAsync(empresaId, ModuloSistema.Usuarios, accion);
        return actor.UsuarioId;
    }

    private IQueryable<Rol> RolesEmpresa(int empresaId) => _db.Roles.Where(r =>
        r.Ambito == AmbitoAutorizacion.Empresa &&
        (r.EmpresaId == empresaId || (r.EmpresaId == null && r.EsSistema)));

    private IQueryable<UsuarioDetalleDto> UsuariosEmpresa(int empresaId) =>
        from m in _db.UsuarioEmpresas.AsNoTracking()
        join u in _db.Usuarios.AsNoTracking() on m.UsuarioId equals u.Id
        join r in RolesEmpresa(empresaId).AsNoTracking() on m.RolId equals r.Id
        where m.EmpresaId == empresaId && !u.Eliminado && !r.Eliminado
        select new UsuarioDetalleDto
        {
            Id = u.Id, NombreUsuario = u.NombreUsuario, NombreCompleto = u.NombreCompleto,
            Rol = r.Nombre, RolId = r.Id, RolNombre = r.Nombre,
            Activo = u.Activo && m.Activa, Bloqueado = u.Bloqueado,
            MotivoBloqueo = _db.UsuarioEmpresas.Count(x => x.UsuarioId == u.Id) == 1 &&
                !_db.UsuarioRolesPlataforma.Any(g => g.UsuarioId == u.Id) ? u.MotivoBloqueo : null,
            FechaBloqueo = u.FechaBloqueo, FechaCreacion = u.FechaCreacion,
            FechaActualizacion = u.FechaActualizacion
        };

    private static UsuarioDto Resumen(UsuarioDetalleDto u) => new()
    {
        Id = u.Id, NombreUsuario = u.NombreUsuario, NombreCompleto = u.NombreCompleto,
        Rol = u.Rol, RolId = u.RolId, Activo = u.Activo,
        Bloqueado = u.Bloqueado, FechaCreacion = u.FechaCreacion
    };

    private static UsuarioDto Resumen(Usuario u, UsuarioEmpresa m, Rol r) => new()
    {
        Id = u.Id, NombreUsuario = u.NombreUsuario, NombreCompleto = u.NombreCompleto,
        Rol = r.Nombre, RolId = m.RolId, Activo = u.Activo && m.Activa,
        Bloqueado = u.Bloqueado, FechaCreacion = u.FechaCreacion
    };

    public async Task<List<UsuarioDto>> GetAllAsync(int empresaId, CancellationToken ct = default)
    {
        await ExigirAsync(empresaId, AccionPermiso.Ver, ct, administrar: false);
        return (await UsuariosEmpresa(empresaId).OrderBy(u => u.NombreUsuario).ThenBy(u => u.Id)
            .ToListAsync(ct)).Select(Resumen).ToList();
    }

    public async Task<PagedResult<UsuarioDto>> GetPagedAsync(int empresaId, PagedRequest request, CancellationToken ct = default)
    {
        await ExigirAsync(empresaId, AccionPermiso.Ver, ct, administrar: false);
        var query = UsuariosEmpresa(empresaId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(u => u.NombreUsuario.Contains(search) || u.NombreCompleto.Contains(search));
        }
        var ordered = (request.SortBy, request.SortDirection?.ToLowerInvariant()) switch
        {
            ("NombreUsuario", "desc") => query.OrderByDescending(u => u.NombreUsuario),
            ("NombreUsuario", _) => query.OrderBy(u => u.NombreUsuario),
            ("FechaCreacion", "desc") => query.OrderByDescending(u => u.FechaCreacion),
            ("FechaCreacion", _) => query.OrderBy(u => u.FechaCreacion),
            (_, "desc") => query.OrderByDescending(u => u.NombreCompleto),
            _ => query.OrderBy(u => u.NombreCompleto)
        };
        var total = await query.CountAsync(ct);
        var skip = (int)Math.Min((long)(request.Page - 1) * request.PageSize, int.MaxValue);
        var rows = await ordered.ThenBy(u => u.Id).Skip(skip).Take(request.PageSize).ToListAsync(ct);
        return new PagedResult<UsuarioDto>
        {
            Items = rows.Select(Resumen).ToList(), Page = request.Page, PageSize = request.PageSize, TotalCount = total
        };
    }

    public async Task<UsuarioDetalleDto?> GetByIdAsync(int empresaId, int id, CancellationToken ct = default)
    {
        await ExigirAsync(empresaId, AccionPermiso.Ver, ct, administrar: false);
        return await UsuariosEmpresa(empresaId).SingleOrDefaultAsync(u => u.Id == id, ct);
    }

    private async Task<Rol> RolValidoAsync(int empresaId, int? id, CancellationToken ct) =>
        await RolesEmpresa(empresaId).SingleOrDefaultAsync(r => r.Id == id && r.Activo && !r.Eliminado, ct)
        ?? throw new ForbiddenAccessException("Seleccione un rol empresarial vigente y autorizado.");

    private async Task<(Usuario Usuario, UsuarioEmpresa Membresia)> ExigirMembresiaAsync(int empresaId, int id, CancellationToken ct)
    {
        var m = await _db.UsuarioEmpresas.SingleOrDefaultAsync(x => x.UsuarioId == id && x.EmpresaId == empresaId, ct);
        if (m is null) throw new ForbiddenAccessException("La membresía no pertenece a la empresa autorizada.");
        var u = await _db.Usuarios.SingleOrDefaultAsync(x => x.Id == id && !x.Eliminado, ct);
        if (u is null) throw new ForbiddenAccessException("La membresía no está disponible.");
        return (u, m);
    }

    private async Task<(Usuario Usuario, UsuarioEmpresa Membresia)> ExigirIdentidadExclusivaAsync(int empresaId, int id, CancellationToken ct)
    {
        var result = await ExigirMembresiaAsync(empresaId, id, ct);
        if (await _db.UsuarioEmpresas.CountAsync(m => m.UsuarioId == id, ct) != 1 ||
            await _db.UsuarioRolesPlataforma.AnyAsync(g => g.UsuarioId == id, ct) ||
            !await RolesEmpresa(empresaId).AnyAsync(r => r.Id == result.Usuario.RolId, ct))
            throw new ForbiddenAccessException("No se puede modificar una identidad ajena, compartida o de plataforma.");
        return result;
    }

    private static string NombreUsuario(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (!Regex.IsMatch(normalized, "^[a-zA-Z0-9._-]{3,50}$", RegexOptions.CultureInvariant))
            throw new BusinessRuleException("El nombre de usuario debe tener entre 3 y 50 caracteres y usar letras, números, punto, guion o guion bajo.");
        return normalized;
    }

    private static string NombreCompleto(string? value)
    {
        var normalized = Regex.Replace(value?.Trim() ?? string.Empty, "\\s+", " ");
        if (normalized.Length < 3 || normalized.Length > 150)
            throw new BusinessRuleException("El nombre completo debe tener entre 3 y 150 caracteres.");
        return normalized;
    }

    private static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10 || password.Length > 128 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new BusinessRuleException("La contraseña debe tener entre 10 y 128 caracteres e incluir mayúscula, minúscula, número y símbolo.");
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private async Task ExigirNombreDisponibleAsync(string nombre, int? id, CancellationToken ct)
    {
        var normalized = nombre.ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => (!id.HasValue || u.Id != id.Value) && u.NombreUsuario.ToLower() == normalized, ct))
            throw new BusinessRuleException("El nombre de usuario no está disponible.");
    }

    private void Audit(int empresaId, int actor, int id, AccionPermiso accion, string entidad, string descripcion, object datos)
    {
        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            UsuarioId = actor, NombreUsuario = _actual.NombreUsuario ?? $"Usuario:{actor}",
            EmpresaId = empresaId, AmbitoAutoridad = AmbitoAutorizacion.Empresa,
            OrigenAutorizacion = OrigenAutorizacion.MembresiaEmpresa,
            Modulo = ModuloSistema.Usuarios, Accion = accion, Entidad = entidad,
            ReferenciaId = id, Descripcion = descripcion, ValoresNuevos = JsonSerializer.Serialize(datos)
        });
    }

    private void Marcar(Usuario u, int actor)
    {
        u.ActualizadoPorUsuarioId = actor;
        u.FechaActualizacion = DateTime.UtcNow;
    }

    private void Marcar(UsuarioEmpresa m, int actor)
    {
        m.ActualizadoPorUsuarioId = actor;
        m.FechaActualizacion = DateTime.UtcNow;
    }

    public async Task<UsuarioDto> CrearAsync(int empresaId, CreateUsuarioDto dto, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational() ?
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var actor = await ExigirAsync(empresaId, AccionPermiso.Crear, ct);
        await _permisos.VerificarPermisoAsync(empresaId, ModuloSistema.Usuarios, AccionPermiso.AsignarRol);
        var r = await RolValidoAsync(empresaId, dto.RolId, ct);
        var nombre = NombreUsuario(dto.NombreUsuario);
        await ExigirNombreDisponibleAsync(nombre, null, ct);
        var u = new Usuario
        {
            NombreUsuario = nombre, NombreCompleto = NombreCompleto(dto.NombreCompleto),
            PasswordHash = HashPassword(dto.Password), RolId = r.Id,
            Activo = true, CreadoPorUsuarioId = actor
        };
        _db.Usuarios.Add(u);
        await _db.SaveChangesAsync(ct);
        var m = new UsuarioEmpresa(u.Id, empresaId, r.Id) { CreadoPorUsuarioId = actor };
        _db.UsuarioEmpresas.Add(m);
        Audit(empresaId, actor, u.Id, AccionPermiso.Crear, nameof(Usuario),
            "Alta de identidad y membresía empresarial.", new { EmpresaId = empresaId, RolId = r.Id });
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return Resumen(u, m, r);
    }

    public async Task<UsuarioDto?> ActualizarAsync(int empresaId, int id, UpdateUsuarioDto dto, CancellationToken ct = default)
    {
        await using var tx = _db.Database.IsRelational() ?
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var actor = await ExigirAsync(empresaId, AccionPermiso.Editar, ct);
        var (u, m) = await ExigirIdentidadExclusivaAsync(empresaId, id, ct);
        if (!m.Activa) throw new ForbiddenAccessException("La membresía empresarial debe estar activa para editar la identidad.");
        var r = await RolValidoAsync(empresaId, dto.RolId, ct);
        if (actor == id && !r.EsAdministrador)
            throw new BusinessRuleException("No puedes retirar tu propia autoridad administrativa.");
        if (m.RolId != r.Id || u.RolId != r.Id)
            await _permisos.VerificarPermisoAsync(empresaId, ModuloSistema.Usuarios, AccionPermiso.AsignarRol);
        var nombre = NombreUsuario(dto.NombreUsuario);
        await ExigirNombreDisponibleAsync(nombre, id, ct);
        var completo = NombreCompleto(dto.NombreCompleto);
        string? hash = null;
        if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
        {
            await _permisos.VerificarPermisoAsync(empresaId, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena);
            if (BCrypt.Net.BCrypt.Verify(dto.NuevaPassword, u.PasswordHash))
                throw new BusinessRuleException("La nueva contraseña debe ser diferente de la actual.");
            hash = HashPassword(dto.NuevaPassword);
        }
        u.NombreUsuario = nombre;
        u.NombreCompleto = completo;
        u.RolId = r.Id;
        if (hash is not null) u.PasswordHash = hash;
        m.CambiarRol(r.Id);
        Marcar(u, actor);
        Marcar(m, actor);
        Audit(empresaId, actor, id, AccionPermiso.Editar, nameof(Usuario),
            "Edición de identidad empresarial exclusiva.", new { EmpresaId = empresaId, RolId = r.Id, PasswordRestablecida = hash is not null });
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return Resumen(u, m, r);
    }

    public Task<UsuarioDto> UpdateEstadoAsync(int empresaId, int id, bool activo, CancellationToken ct = default) =>
        MutarIdentidadAsync(empresaId, id, activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            "Cambio de estado de identidad empresarial exclusiva.", (u, m, actor) =>
            {
                if (!activo && id == actor) throw new BusinessRuleException("No puedes desactivar tu propia cuenta.");
                u.Activo = activo;
            }, ct);

    public Task<UsuarioDto> BloquearAsync(int empresaId, int id, string motivo, CancellationToken ct = default) =>
        MutarIdentidadAsync(empresaId, id, AccionPermiso.Desactivar,
            "Bloqueo de identidad empresarial exclusiva.", (u, m, actor) =>
            {
                if (id == actor) throw new BusinessRuleException("No puedes bloquear tu propia cuenta.");
                if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 300)
                    throw new BusinessRuleException("Indique un motivo de bloqueo válido.");
                u.Bloqueado = true; u.MotivoBloqueo = motivo.Trim();
                u.FechaBloqueo = DateTime.UtcNow; u.BloqueadoPorUsuarioId = actor;
            }, ct);

    public Task<UsuarioDto> DesbloquearAsync(int empresaId, int id, CancellationToken ct = default) =>
        MutarIdentidadAsync(empresaId, id, AccionPermiso.Activar,
            "Desbloqueo de identidad empresarial exclusiva.", (u, m, actor) =>
            {
                u.Bloqueado = false; u.MotivoBloqueo = null;
                u.FechaBloqueo = null; u.BloqueadoPorUsuarioId = null;
            }, ct);

    public async Task EliminarAsync(int empresaId, int id, CancellationToken ct = default)
    {
        await MutarIdentidadAsync(empresaId, id, AccionPermiso.EliminarLogico,
            "Eliminación lógica de identidad empresarial exclusiva.", (u, m, actor) =>
            {
                if (id == actor) throw new BusinessRuleException("No puedes eliminar tu propia cuenta.");
                u.Eliminado = true; u.Activo = false; u.FechaEliminacion = DateTime.UtcNow;
                u.EliminadoPorUsuarioId = actor; m.Desactivar(); Marcar(m, actor);
            }, ct);
    }

    private async Task<UsuarioDto> MutarIdentidadAsync(int empresaId, int id, AccionPermiso accion,
        string descripcion, Action<Usuario, UsuarioEmpresa, int> mutar, CancellationToken ct)
    {
        await using var tx = _db.Database.IsRelational() ?
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var actor = await ExigirAsync(empresaId, accion, ct);
        var (u, m) = await ExigirIdentidadExclusivaAsync(empresaId, id, ct);
        var r = await RolesEmpresa(empresaId).SingleOrDefaultAsync(x => x.Id == m.RolId, ct)
            ?? throw new ForbiddenAccessException("La membresía no posee un rol empresarial válido.");
        mutar(u, m, actor);
        Marcar(u, actor);
        Audit(empresaId, actor, id, accion, nameof(Usuario), descripcion,
            new { EmpresaId = empresaId, u.Activo, u.Bloqueado, u.Eliminado, MembresiaActiva = m.Activa });
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return Resumen(u, m, r);
    }

    private static UsuarioEmpresaDto MembresiaDto(UsuarioEmpresa m) => new()
    { Id = m.Id, UsuarioId = m.UsuarioId, EmpresaId = m.EmpresaId, RolId = m.RolId, Activa = m.Activa };

    public async Task<List<UsuarioEmpresaDto>> GetEmpresasAsync(int empresaId, int id, CancellationToken ct = default)
    {
        await ExigirAsync(empresaId, AccionPermiso.Ver, ct, administrar: false);
        return (await _db.UsuarioEmpresas.AsNoTracking()
            .Where(m => m.EmpresaId == empresaId && m.UsuarioId == id &&
                _db.Usuarios.Any(u => u.Id == id && !u.Eliminado))
            .ToListAsync(ct)).Select(MembresiaDto).ToList();
    }

    // Alta de identidad crea su propia membresía; no se permite apropiarse de
    // identidades ajenas por ID. Una membresía previa del tenant puede restaurarse.
    public Task<UsuarioEmpresaDto> AsignarEmpresaAsync(int empresaId, int id, int rolId, CancellationToken ct = default) =>
        MutarMembresiaAsync(empresaId, id, AccionPermiso.AsignarRol, rolId, true, restaurar: true, ct);

    public Task<UsuarioEmpresaDto> CambiarRolEmpresaAsync(int empresaId, int id, int rolId, CancellationToken ct = default) =>
        MutarMembresiaAsync(empresaId, id, AccionPermiso.AsignarRol, rolId, null, restaurar: false, ct);

    public Task<UsuarioEmpresaDto> CambiarEstadoEmpresaAsync(int empresaId, int id, bool activa, CancellationToken ct = default) =>
        MutarMembresiaAsync(empresaId, id, activa ? AccionPermiso.Activar : AccionPermiso.Desactivar, null, activa, restaurar: false, ct);

    private async Task<UsuarioEmpresaDto> MutarMembresiaAsync(int empresaId, int id, AccionPermiso accion,
        int? rolId, bool? activa, bool restaurar, CancellationToken ct)
    {
        await using var tx = _db.Database.IsRelational() ?
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var actor = await ExigirAsync(empresaId, accion, ct);
        var (u, m) = await ExigirMembresiaAsync(empresaId, id, ct);
        if (restaurar)
        {
            if (m.Activa) throw new BusinessRuleException("La membresía ya está activa.");
            await _permisos.VerificarPermisoAsync(empresaId, ModuloSistema.Usuarios, AccionPermiso.Activar);
        }
        if (activa == true && (!u.Activo || u.Bloqueado))
            throw new BusinessRuleException("No puede activarse una membresía de un usuario inactivo o bloqueado.");
        Rol? r = null;
        if (rolId.HasValue || activa == true)
            r = await RolValidoAsync(empresaId, rolId ?? m.RolId, ct);
        if (actor == id && (activa == false || (r is not null && !r.EsAdministrador)))
            throw new BusinessRuleException("No puedes retirar tu propia membresía o autoridad administrativa.");
        if (rolId.HasValue) m.CambiarRol(r!.Id);
        if (activa == true) m.Activar();
        if (activa == false) m.Desactivar();
        Marcar(m, actor);
        Audit(empresaId, actor, id, accion, nameof(UsuarioEmpresa),
            "Cambio de membresía en la empresa autorizada.", new { EmpresaId = empresaId, m.RolId, m.Activa });
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return MembresiaDto(m);
    }
}
