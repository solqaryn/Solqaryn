using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Administración básica del MVP exclusivamente por autoridad global viva.
/// Ninguna de estas acciones otorga autoridad de Plataforma ni consulta datos
/// comerciales de tenants. Las operaciones comerciales siguen en cuarentena.
/// </summary>
[ApiController]
[Authorize]
[Route("plataforma/gestion")]
public sealed class PlataformaGestionMvpController : ControllerBase
{
    private static readonly Regex UsuarioValido = new(
        "^[a-zA-Z0-9._-]{3,50}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IPlataformaScopeService _plataforma;

    public PlataformaGestionMvpController(
        AppDbContext db, ICurrentUserService current,
        IPlataformaScopeService plataforma)
    {
        _db = db;
        _current = current;
        _plataforma = plataforma;
    }

    private async Task<UsuarioPlataformaScopeActual?> AutoridadAsync(CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        if (User.Identity?.IsAuthenticated != true ||
            !_current.EstaAutenticado || _current.UsuarioId is not > 0)
            return null;
        return await _plataforma.ObtenerActualAsync(ct);
    }

    private void Registrar(UsuarioPlataformaScopeActual actor, string entidad,
        int? referencia, int? empresaId, AccionPermiso accion, string descripcion)
    {
        _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria
        {
            UsuarioId = actor.UsuarioId,
            NombreUsuario = actor.NombreUsuario,
            EmpresaId = empresaId,
            AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
            OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
            Modulo = ModuloSistema.Configuracion,
            Accion = accion,
            Entidad = entidad,
            ReferenciaId = referencia,
            Descripcion = descripcion,
            CorrelationId = HttpContext.TraceIdentifier,
            Resultado = "Exito"
        });
    }

    private async Task<Rol?> RolEmpresarialAsync(int rolId, int empresaId, CancellationToken ct)
    {
        if (rolId <= 0 || empresaId <= 0) return null;
        return await _db.Roles.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(r =>
                r.Id == rolId && r.Activo && !r.Eliminado &&
                r.Ambito == AmbitoAutorizacion.Empresa &&
                ((r.EmpresaId == null && r.EsSistema) || r.EmpresaId == empresaId), ct);
    }

    private Task<Empresa?> EmpresaAsync(int empresaId, CancellationToken ct) =>
        _db.Set<Empresa>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(e => e.Id == empresaId, ct);

    private async Task<int> AdministradoresActivosAsync(int empresaId, CancellationToken ct)
    {
        return await (
            from m in _db.Set<UsuarioEmpresa>().IgnoreQueryFilters()
            join u in _db.Usuarios.IgnoreQueryFilters() on m.UsuarioId equals u.Id
            join r in _db.Roles.IgnoreQueryFilters() on m.RolId equals r.Id
            where m.EmpresaId == empresaId && m.Activa &&
                  u.Activo && !u.Bloqueado && !u.Eliminado &&
                  r.Ambito == AmbitoAutorizacion.Empresa &&
                  (r.EmpresaId == empresaId || (r.EmpresaId == null && r.EsSistema)) &&
                  r.EsAdministrador && r.Activo && !r.Eliminado
            select m.Id).CountAsync(ct);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles([FromQuery] int empresaId = 0,
        CancellationToken cancellationToken = default)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        if (empresaId < 0) return BadRequest(ApiResponse<object>.Fail("Empresa inválida."));
        if (empresaId > 0 && await EmpresaAsync(empresaId, cancellationToken) is null)
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));

        var roles = await _db.Roles.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Activo && !r.Eliminado &&
                        r.Ambito == AmbitoAutorizacion.Empresa &&
                        ((r.EmpresaId == null && r.EsSistema) ||
                         (empresaId > 0 && r.EmpresaId == empresaId)))
            .OrderBy(r => r.Nombre).ThenBy(r => r.Id)
            .Select(r => new { id = r.Id, nombre = r.Nombre,
                esAdministrador = r.EsAdministrador, empresaId = r.EmpresaId })
            .Take(100).ToArrayAsync(cancellationToken);
        Registrar(actor, nameof(Rol), null, empresaId > 0 ? empresaId : null,
            AccionPermiso.ConsultarHistorial, "Consulta de catálogo RBAC empresarial.");
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(roles));
    }

    [HttpGet("usuarios/buscar")]
    public async Task<IActionResult> BuscarUsuario([FromQuery] string nombreUsuario,
        CancellationToken cancellationToken = default)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        var nombre = nombreUsuario?.Trim() ?? string.Empty;
        if (!UsuarioValido.IsMatch(nombre))
            return BadRequest(ApiResponse<object>.Fail("Nombre de usuario inválido."));
        var usuario = await _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.NombreUsuario == nombre && !u.Eliminado)
            .Select(u => new { id = u.Id, nombreUsuario = u.NombreUsuario,
                nombreCompleto = u.NombreCompleto, activo = u.Activo,
                bloqueado = u.Bloqueado })
            .FirstOrDefaultAsync(cancellationToken);
        Registrar(actor, nameof(Usuario), usuario?.id, null,
            AccionPermiso.ConsultarHistorial, "Búsqueda exacta de identidad empresarial.");
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(usuario));
    }

    [HttpGet("empresas/{empresaId:int}/membresias")]
    public async Task<IActionResult> Miembros(int empresaId,
        CancellationToken cancellationToken)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        if (empresaId <= 0 || await EmpresaAsync(empresaId, cancellationToken) is null)
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));
        var membresias = await (
            from m in _db.Set<UsuarioEmpresa>().IgnoreQueryFilters()
            join u in _db.Usuarios.IgnoreQueryFilters() on m.UsuarioId equals u.Id
            join r in _db.Roles.IgnoreQueryFilters() on m.RolId equals r.Id
            where m.EmpresaId == empresaId
            orderby u.NombreUsuario
            select new {
                usuarioId = u.Id, nombreUsuario = u.NombreUsuario,
                nombreCompleto = u.NombreCompleto, usuarioActivo = u.Activo && !u.Bloqueado && !u.Eliminado,
                rolId = r.Id, rolNombre = r.Nombre,
                esAdministrador = r.EsAdministrador && r.Ambito == AmbitoAutorizacion.Empresa &&
                    (r.EmpresaId == empresaId || (r.EmpresaId == null && r.EsSistema)),
                activa = m.Activa
            }).AsNoTracking().Take(200).ToArrayAsync(cancellationToken);
        Registrar(actor, nameof(UsuarioEmpresa), null, empresaId,
            AccionPermiso.ConsultarHistorial, "Consulta de membresías de una empresa.");
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(membresias));
    }

    [HttpPost("usuarios")]
    public async Task<IActionResult> CrearUsuario([FromBody] AltaUsuarioPlataformaMvpRequest dto,
        CancellationToken cancellationToken)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        var nombre = dto?.NombreUsuario?.Trim() ?? string.Empty;
        var completo = dto?.NombreCompleto?.Trim() ?? string.Empty;
        var password = dto?.Password ?? string.Empty;
        if (!UsuarioValido.IsMatch(nombre) || completo.Length is < 3 or > 150 ||
            password.Length is < 10 or > 128 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            return BadRequest(ApiResponse<object>.Fail("Usuario, nombre o contraseña inválidos."));

        var rolBase = await _db.Roles.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Ambito == AmbitoAutorizacion.Empresa &&
                        r.EmpresaId == null && r.EsSistema &&
                        !r.EsAdministrador && r.Activo && !r.Eliminado)
            .OrderBy(r => r.Id).Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (rolBase <= 0)
            return StatusCode(503, ApiResponse<object>.Fail("No existe rol empresarial base no administrador."));
        if (await _db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.NombreUsuario == nombre,
                cancellationToken))
            return Conflict(ApiResponse<object>.Fail("El nombre de usuario ya existe."));

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var nuevo = new Usuario {
            NombreUsuario = nombre, NombreCompleto = completo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            RolId = rolBase,
            Activo = true, CreadoPorUsuarioId = actor.UsuarioId
        };
        _db.Usuarios.Add(nuevo);
        await _db.SaveChangesAsync(cancellationToken);
        Registrar(actor, nameof(Usuario), nuevo.Id, null,
            AccionPermiso.Crear, "Creación de identidad sin privilegios globales ni empresa asignada.");
        await _db.SaveChangesAsync(cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return Created("/plataforma/gestion/usuarios/" + nuevo.Id,
            ApiResponse<object>.Ok(new { id = nuevo.Id, nombreUsuario = nuevo.NombreUsuario }));
    }

    [HttpPost("empresas/{empresaId:int}/membresias")]
    public async Task<IActionResult> Asignar(int empresaId,
        [FromBody] AsignarMembresiaPlataformaMvpRequest dto,
        CancellationToken cancellationToken)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        if (empresaId <= 0 || dto is null || dto.UsuarioId <= 0 || dto.RolId <= 0)
            return BadRequest(ApiResponse<object>.Fail("Empresa, usuario o rol inválido."));
        if (await EmpresaAsync(empresaId, cancellationToken) is null)
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));
        var rol = await RolEmpresarialAsync(dto.RolId, empresaId, cancellationToken);
        if (rol is null) return BadRequest(ApiResponse<object>.Fail("Rol fuera del ámbito de esta empresa."));
        var usuario = await _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == dto.UsuarioId &&
                u.Activo && !u.Bloqueado && !u.Eliminado, cancellationToken);
        if (usuario is null) return BadRequest(ApiResponse<object>.Fail("Usuario inactivo o inexistente."));
        if (await _db.Set<UsuarioEmpresa>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(m => m.EmpresaId == empresaId && m.UsuarioId == dto.UsuarioId,
                    cancellationToken))
            return Conflict(ApiResponse<object>.Fail("La membresía ya existe; modifica su estado o rol."));

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        _db.Set<UsuarioEmpresa>().Add(new UsuarioEmpresa(usuario.Id, empresaId, rol.Id) {
            CreadoPorUsuarioId = actor.UsuarioId, CreadoPorNombreUsuario = actor.NombreUsuario
        });
        Registrar(actor, nameof(UsuarioEmpresa), usuario.Id, empresaId,
            AccionPermiso.AsignarRol, "Asignación empresarial con rol validado por tenant.");
        await _db.SaveChangesAsync(cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new {
            empresaId, usuarioId = usuario.Id, rolId = rol.Id, activa = true
        }));
    }

    [HttpPut("empresas/{empresaId:int}/membresias/{usuarioId:int}/rol")]
    public async Task<IActionResult> CambiarRol(int empresaId, int usuarioId,
        [FromBody] CambiarRolPlataformaMvpRequest dto, CancellationToken cancellationToken)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        if (dto is null || empresaId <= 0 || usuarioId <= 0)
            return BadRequest(ApiResponse<object>.Fail("Solicitud inválida."));
        var rol = await RolEmpresarialAsync(dto.RolId, empresaId, cancellationToken);
        if (rol is null) return BadRequest(ApiResponse<object>.Fail("Rol fuera de empresa."));
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var miembro = await _db.Set<UsuarioEmpresa>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.EmpresaId == empresaId &&
                m.UsuarioId == usuarioId, cancellationToken);
        if (miembro is null) return NotFound(ApiResponse<object>.Fail("Membresía inexistente."));
        var rolAnterior = await RolEmpresarialAsync(miembro.RolId, empresaId, cancellationToken);
        if (miembro.Activa && rolAnterior?.EsAdministrador == true && !rol.EsAdministrador &&
            await AdministradoresActivosAsync(empresaId, cancellationToken) <= 1)
            return Conflict(ApiResponse<object>.Fail("No se permite retirar el último administrador."));
        miembro.CambiarRol(rol.Id);
        miembro.ActualizadoPorUsuarioId = actor.UsuarioId;
        miembro.ActualizadoPorNombreUsuario = actor.NombreUsuario;
        Registrar(actor, nameof(UsuarioEmpresa), usuarioId, empresaId,
            AccionPermiso.AsignarRol, "Cambio de rol empresarial; sin concesión global.");
        await _db.SaveChangesAsync(cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { empresaId, usuarioId,
            rolId = rol.Id, activa = miembro.Activa }));
    }

    [HttpPut("empresas/{empresaId:int}/membresias/{usuarioId:int}/estado")]
    public async Task<IActionResult> CambiarEstadoMembresia(int empresaId, int usuarioId,
        [FromBody] CambiarEstadoPlataformaMvpRequest dto, CancellationToken cancellationToken)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        if (dto is null || empresaId <= 0 || usuarioId <= 0)
            return BadRequest(ApiResponse<object>.Fail("Solicitud inválida."));
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var miembro = await _db.Set<UsuarioEmpresa>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.EmpresaId == empresaId &&
                m.UsuarioId == usuarioId, cancellationToken);
        if (miembro is null) return NotFound(ApiResponse<object>.Fail("Membresía inexistente."));
        if (dto.Activa)
        {
            var usuario = await _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(u => u.Id == usuarioId && u.Activo && !u.Bloqueado && !u.Eliminado,
                    cancellationToken);
            if (!usuario || await RolEmpresarialAsync(miembro.RolId, empresaId,
                    cancellationToken) is null)
                return BadRequest(ApiResponse<object>.Fail("Usuario o rol no vigente."));
        }
        else if (miembro.Activa &&
                 (await RolEmpresarialAsync(miembro.RolId, empresaId, cancellationToken))?.EsAdministrador == true &&
                 await AdministradoresActivosAsync(empresaId, cancellationToken) <= 1)
            return Conflict(ApiResponse<object>.Fail("No se permite quitar al último administrador."));
        if (dto.Activa) miembro.Activar(); else miembro.Desactivar();
        miembro.ActualizadoPorUsuarioId = actor.UsuarioId;
        miembro.ActualizadoPorNombreUsuario = actor.NombreUsuario;
        Registrar(actor, nameof(UsuarioEmpresa), usuarioId, empresaId,
            dto.Activa ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            "Cambio de estado de membresía por autoridad global.");
        await _db.SaveChangesAsync(cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { empresaId, usuarioId, activa = miembro.Activa }));
    }

    [HttpPut("empresas/{empresaId:int}/nombre")]
    public async Task<IActionResult> CambiarNombre(int empresaId,
        [FromBody] CambiarNombreEmpresaPlataformaMvpRequest dto,
        CancellationToken cancellationToken)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        var nombre = dto?.Nombre?.Trim() ?? string.Empty;
        if (empresaId <= 0 || nombre.Length is < 2 or > 200)
            return BadRequest(ApiResponse<object>.Fail("Nombre inválido."));
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var empresa = await EmpresaAsync(empresaId, cancellationToken);
        if (empresa is null) return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));
        if (await _db.Set<Empresa>().IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(e => e.Id != empresaId && e.Nombre == nombre, cancellationToken))
            return Conflict(ApiResponse<object>.Fail("Ya existe otra empresa con ese nombre."));
        empresa.CambiarNombre(nombre);
        empresa.ActualizadoPorUsuarioId = actor.UsuarioId;
        empresa.ActualizadoPorNombreUsuario = actor.NombreUsuario;
        Registrar(actor, nameof(Empresa), empresaId, empresaId,
            AccionPermiso.Editar, "Actualización de identidad comercial empresarial.");
        await _db.SaveChangesAsync(cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id = empresa.Id, nombre = empresa.Nombre,
            activa = empresa.Activa }));
    }

    [HttpPut("empresas/{empresaId:int}/estado")]
    public async Task<IActionResult> CambiarEstadoEmpresa(int empresaId,
        [FromBody] CambiarEstadoPlataformaMvpRequest dto,
        CancellationToken cancellationToken)
    {
        var actor = await AutoridadAsync(cancellationToken);
        if (actor is null) return Forbid();
        if (dto is null || empresaId <= 0)
            return BadRequest(ApiResponse<object>.Fail("Solicitud inválida."));
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var empresa = await EmpresaAsync(empresaId, cancellationToken);
        if (empresa is null) return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));
        if (dto.Activa && await AdministradoresActivosAsync(empresaId, cancellationToken) == 0)
            return Conflict(ApiResponse<object>.Fail("Se requiere administrador empresarial activo antes de reactivar."));
        if (dto.Activa) empresa.Activar(); else empresa.Desactivar();
        empresa.ActualizadoPorUsuarioId = actor.UsuarioId;
        empresa.ActualizadoPorNombreUsuario = actor.NombreUsuario;
        Registrar(actor, nameof(Empresa), empresaId, empresaId,
            dto.Activa ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            "Cambio explícito de estado empresarial. Sin bypass de cuarentena comercial.");
        await _db.SaveChangesAsync(cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id = empresa.Id, nombre = empresa.Nombre,
            activa = empresa.Activa }));
    }
}

public sealed class AltaUsuarioPlataformaMvpRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
public sealed class AsignarMembresiaPlataformaMvpRequest
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
}
public sealed class CambiarRolPlataformaMvpRequest
{
    public int RolId { get; set; }
}
public sealed class CambiarEstadoPlataformaMvpRequest
{
    public bool Activa { get; set; }
}
public sealed class CambiarNombreEmpresaPlataformaMvpRequest
{
    public string Nombre { get; set; } = string.Empty;
}
