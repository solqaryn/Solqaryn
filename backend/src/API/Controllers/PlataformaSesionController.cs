using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Punto de entrada a la sesión del propietario del SaaS. No activa ningún
/// contexto empresarial: una cuenta sin empresas activas también puede entrar.
/// La autorización se verifica en MySQL en CADA solicitud y no se cachea.
/// </summary>
[ApiController]
[Authorize]
[Route("plataforma/sesion")]
public sealed class PlataformaSesionController : ControllerBase
{
    private readonly IPlataformaScopeService _scope;
    private readonly ICurrentUserService _current;
    private readonly AppDbContext _db;

    public PlataformaSesionController(
        IPlataformaScopeService scope,
        ICurrentUserService current,
        AppDbContext db)
    {
        _scope = scope;
        _current = current;
        _db = db;
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ObtenerActual(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";

        if (User.Identity?.IsAuthenticated != true ||
            !_current.EstaAutenticado || _current.UsuarioId is not > 0)
            return Forbid();

        var authority = await _scope.ObtenerActualAsync(cancellationToken);
        await RegistrarDecisionAsync(authority, cancellationToken);

        if (authority is null)
            return Forbid();

        return Ok(ApiResponse<PlataformaSesionDto>.Ok(
            new PlataformaSesionDto(
                authority.NombreUsuario,
                authority.RolNombre,
                "Plataforma",
                true),
            "Autoridad de plataforma verificada."));
    }

    private async Task RegistrarDecisionAsync(
        UsuarioPlataformaScopeActual? authority,
        CancellationToken cancellationToken)
    {
        // Auditoría estricta: nunca devolver autoridad si no se pudo registrar
        // el resultado. No usar X-Empresa-Id ni otras marcas empresariales.
        var username = _current.NombreUsuario;
        _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria
        {
            UsuarioId = authority?.UsuarioId,
            NombreUsuario = string.IsNullOrWhiteSpace(username)
                ? "Sesion desconocida" : username[..Math.Min(150, username.Length)],
            EmpresaId = null,
            AmbitoAutoridad = authority is null ? null : AmbitoAutorizacion.Plataforma,
            OrigenAutorizacion = authority is null ? null : OrigenAutorizacion.RolPlataforma,
            Modulo = ModuloSistema.Configuracion,
            Accion = AccionPermiso.ConsultarHistorial,
            Entidad = "SesionPlataforma",
            Descripcion = authority is null
                ? "Verificación de autoridad global denegada."
                : "Verificación de autoridad global concedida desde MySQL.",
            CorrelationId = HttpContext.TraceIdentifier,
            Resultado = authority is null ? "Rechazado" : "Exito"
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record PlataformaSesionDto(
    string NombreUsuario,
    string RolNombre,
    string Ambito,
    bool EsAdministrador);
