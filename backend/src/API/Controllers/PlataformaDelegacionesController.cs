using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Controllers;

public sealed class DelegacionPlataformaRequest
{
    [Range(1, int.MaxValue)]
    public int UsuarioId { get; init; }

    [Required, StringLength(300, MinimumLength = 10)]
    public string Motivo { get; init; } = string.Empty;
}

public sealed class RevocarDelegacionPlataformaRequest
{
    [Required, StringLength(300, MinimumLength = 10)]
    public string Motivo { get; init; } = string.Empty;
}

/// <summary>
/// Única delegación P1-004.A: consulta auditada del directorio de empresas.
/// Los grants no se escogen mediante DTO: solo un superadmin con rol de sistema
/// vivo puede crear/revocar la capacidad fija. No se concede acceso ERP.
/// </summary>
[ApiController]
[Authorize]
[Route("plataforma/delegaciones")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PlataformaDelegacionesController : ControllerBase
{
    private const string RolDelegado = "Delegado plataforma consulta";
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IPlataformaScopeService _superadmin;
    private readonly IPlataformaDelegacionService _delegado;

    public PlataformaDelegacionesController(
        AppDbContext db,
        ICurrentUserService current,
        IPlataformaScopeService superadmin,
        IPlataformaDelegacionService delegado)
    {
        _db = db;
        _current = current;
        _superadmin = superadmin;
        _delegado = delegado;
    }

    private async Task<UsuarioPlataformaScopeActual?> SuperadminAsync(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true || !_current.EstaAutenticado ||
            _current.UsuarioId is not > 0)
            return null;
        var scope = await _superadmin.ObtenerActualAsync(ct);
        return scope is not null && scope.UsuarioId == _current.UsuarioId
            ? scope : null;
    }

    private void Auditar(
        UsuarioPlataformaScopeActual actor, int? referencia, AccionPermiso accion,
        string motivo, string descripcion, string resultado)
    {
        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            UsuarioId = actor.UsuarioId,
            NombreUsuario = actor.NombreUsuario,
            EmpresaId = null, // La delegación NO es una membresía empresarial.
            AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
            OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
            Modulo = ModuloSistema.Configuracion,
            Accion = accion,
            Entidad = nameof(UsuarioRolPlataforma),
            ReferenciaId = referencia,
            Motivo = motivo,
            Descripcion = descripcion,
            CorrelationId = HttpContext.TraceIdentifier,
            Resultado = resultado
        });
    }

    [HttpPost]
    public async Task<IActionResult> Conceder(
        [FromBody] DelegacionPlataformaRequest dto, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        var actor = await SuperadminAsync(ct);
        if (actor is null) return Forbid();
        if (dto is null || dto.UsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(dto.Motivo) ||
            dto.Motivo.Trim().Length is < 10 or > 300)
            return BadRequest(ApiResponse<object>.Fail("Delegación o motivo inválido."));
        var motivo = dto.Motivo.Trim();

        // Nunca crear autoridad para el propio solicitante, ni siquiera si
        // el cuerpo intenta incluir permisos/roles no soportados por el DTO.
        if (dto.UsuarioId == actor.UsuarioId)
        {
            Auditar(actor, dto.UsuarioId, AccionPermiso.AsignarRol, motivo,
                "Autodelegación global rechazada.", "Denegado");
            await _db.SaveChangesAsync(ct);
            return Forbid();
        }

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        try
        {
            var target = await _db.Usuarios.IgnoreQueryFilters().SingleOrDefaultAsync(
                x => x.Id == dto.UsuarioId && x.Activo && !x.Bloqueado && !x.Eliminado, ct);
            if (target is null)
                return BadRequest(ApiResponse<object>.Fail("Usuario de destino no habilitado."));

            var existingAssignments = await _db.UsuarioRolesPlataforma
                .IgnoreQueryFilters().Where(x => x.UsuarioId == target.Id).ToArrayAsync(ct);
            if (existingAssignments.Any(x => x.Activa))
                return Conflict(ApiResponse<object>.Fail("El usuario ya posee un rol global activo."));

            var rol = await _db.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(
                x => x.NombreNormalizado == PoliticaDelegacionPlataforma.RolNombreNormalizado, ct);
            if (rol is null)
            {
                rol = new Rol
                {
                    Nombre = RolDelegado,
                    NombreNormalizado = PoliticaDelegacionPlataforma.RolNombreNormalizado,
                    Descripcion = "Delegación limitada de lectura del directorio de empresas.",
                    EsSistema = false,
                    EsAdministrador = true,
                    Ambito = AmbitoAutorizacion.Plataforma,
                    EmpresaId = null,
                    Activo = true,
                    CreadoPorUsuarioId = actor.UsuarioId
                };
                _db.Roles.Add(rol);
            }
            else if (!rol.Activo || rol.Eliminado || rol.EsSistema ||
                     !rol.EsAdministrador || rol.EmpresaId is not null ||
                     rol.Ambito != AmbitoAutorizacion.Plataforma)
                return Conflict(ApiResponse<object>.Fail("Rol delegado incompatible o revocado."));

            var permiso = await _db.Permisos.IgnoreQueryFilters().SingleOrDefaultAsync(
                p => p.Codigo == PoliticaDelegacionPlataforma.CodigoPermiso, ct);
            if (permiso is null)
            {
                if (await _db.Permisos.IgnoreQueryFilters().AnyAsync(
                    p => p.Modulo == ModuloSistema.Configuracion &&
                         p.Accion == AccionPermiso.ConsultarHistorial, ct))
                    return Conflict(ApiResponse<object>.Fail("Existe permiso incompatible con la delegación."));
                permiso = new Permiso
                {
                    Codigo = PoliticaDelegacionPlataforma.CodigoPermiso,
                    Nombre = "Directorio de empresas (solo lectura)",
                    Descripcion = "Consulta limitada de metadatos de empresas desde plataforma.",
                    Modulo = ModuloSistema.Configuracion,
                    Accion = AccionPermiso.ConsultarHistorial,
                    Ambito = AmbitoAutorizacion.Plataforma,
                    EsSistema = true,
                    Activo = true,
                    CreadoPorUsuarioId = actor.UsuarioId
                };
                _db.Permisos.Add(permiso);
            }
            else if (!permiso.Activo || permiso.Eliminado ||
                     permiso.Ambito != AmbitoAutorizacion.Plataforma ||
                     permiso.Modulo != ModuloSistema.Configuracion ||
                     permiso.Accion != AccionPermiso.ConsultarHistorial)
                return Conflict(ApiResponse<object>.Fail("Permiso delegado incompatible o revocado."));

            await _db.SaveChangesAsync(ct);

            if (!await _db.RolPermisos.IgnoreQueryFilters().AnyAsync(
                x => x.RolId == rol.Id && x.PermisoId == permiso.Id, ct))
                _db.RolPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = permiso.Id });

            var assignment = existingAssignments.SingleOrDefault(x => x.RolId == rol.Id);
            if (assignment is null)
            {
                assignment = new UsuarioRolPlataforma(target.Id, rol.Id)
                {
                    CreadoPorUsuarioId = actor.UsuarioId,
                    CreadoPorNombreUsuario = actor.NombreUsuario
                };
                _db.UsuarioRolesPlataforma.Add(assignment);
            }
            else
            {
                assignment.Activar();
                assignment.ActualizadoPorUsuarioId = actor.UsuarioId;
                assignment.ActualizadoPorNombreUsuario = actor.NombreUsuario;
            }

            Auditar(actor, target.Id, AccionPermiso.AsignarRol, motivo,
                "Concedió exclusivamente la consulta global limitada de empresas.", "Exito");
            await _db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);

            return Created("/plataforma/delegaciones/" + target.Id,
                new { usuarioId = target.Id, capacidad = PoliticaDelegacionPlataforma.CodigoPermiso });
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException)
        {
            return StatusCode(503, ApiResponse<object>.Fail("No se pudo confirmar la delegación."));
        }
    }

    [HttpDelete("{usuarioId:int}")]
    public async Task<IActionResult> Revocar(int usuarioId,
        [FromBody] RevocarDelegacionPlataformaRequest dto, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        var actor = await SuperadminAsync(ct);
        if (actor is null) return Forbid();
        if (usuarioId <= 0 || dto is null ||
            string.IsNullOrWhiteSpace(dto.Motivo) ||
            dto.Motivo.Trim().Length is < 10 or > 300)
            return BadRequest(ApiResponse<object>.Fail("Revocación o motivo inválido."));

        if (usuarioId == actor.UsuarioId)
        {
            Auditar(actor, usuarioId, AccionPermiso.Desactivar, dto.Motivo.Trim(),
                "Intento de autorevocación de autoridad global.", "Denegado");
            await _db.SaveChangesAsync(ct);
            return Forbid();
        }

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        try
        {
            var asignacion = await (
                from a in _db.UsuarioRolesPlataforma.IgnoreQueryFilters()
                join rol in _db.Roles.IgnoreQueryFilters() on a.RolId equals rol.Id
                where a.UsuarioId == usuarioId && a.Activa &&
                    rol.NombreNormalizado == PoliticaDelegacionPlataforma.RolNombreNormalizado &&
                    rol.Ambito == AmbitoAutorizacion.Plataforma && !rol.EsSistema
                select a).SingleOrDefaultAsync(ct);
            if (asignacion is null)
                return NotFound(ApiResponse<object>.Fail("Delegación activa no encontrada."));

            asignacion.Desactivar();
            asignacion.ActualizadoPorUsuarioId = actor.UsuarioId;
            asignacion.ActualizadoPorNombreUsuario = actor.NombreUsuario;
            Auditar(actor, usuarioId, AccionPermiso.Desactivar, dto.Motivo.Trim(),
                "Revocó el permiso delegado de lectura del directorio.", "Exito");
            await _db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);

            return Ok(new { usuarioId, revocada = true });
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException)
        {
            return StatusCode(503, ApiResponse<object>.Fail("No se pudo confirmar la revocación."));
        }
    }

    [HttpGet("empresas")]
    public async Task<IActionResult> Directorio(CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        var delegado = await _delegado.ObtenerDelegadoActualAsync(ct);
        if (delegado is null)
            return Forbid();

        // Solo metadatos de plataforma; ninguna tabla comercial con EmpresaId.
        var empresas = await _db.Set<Empresa>().IgnoreQueryFilters()
            .AsNoTracking().Where(x => x.Activa)
            .OrderBy(x => x.Id)
            .Select(x => new { empresaId = x.Id, nombre = x.Nombre })
            .Take(50).ToArrayAsync(ct);
        // Revalidar después de la consulta para limitar la ventana de revocación.
        if (await _delegado.ObtenerDelegadoActualAsync(ct) is not { } current ||
            current.UsuarioId != delegado.UsuarioId ||
            current.RolId != delegado.RolId)
            return Forbid();

        Auditar(delegado, null, AccionPermiso.ConsultarHistorial,
            "Lectura delegada explícita",
            "Consulta auditada del directorio empresarial sin acceder al ERP.", "Exito");
        // Si la auditoría no puede persistirse, no devolver datos.
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(empresas));
    }
}
