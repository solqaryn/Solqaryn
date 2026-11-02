using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;

namespace Solqaryn.API.Controllers;

public sealed class CrearSesionSoporteRequest
{
    [Range(1, int.MaxValue)]
    public int EmpresaId { get; init; }

    [EnumDataType(typeof(ModuloSistema))]
    public ModuloSistema Modulo { get; init; }

    [Required, StringLength(500, MinimumLength = 10)]
    public string Motivo { get; init; } = string.Empty;

    [Range(1, 30)]
    public int DuracionMinutos { get; init; } = 15;
}

[ApiController]
[Authorize]
[Route("plataforma/soporte/sesiones")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PlataformaSoporteController : ControllerBase
{
    private readonly IPlataformaSoporteSessionService _sessions;

    public PlataformaSoporteController(IPlataformaSoporteSessionService sessions) =>
        _sessions = sessions;

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearSesionSoporteRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();
        var result = await _sessions.CreateAsync(
            request.EmpresaId, request.Modulo, request.Motivo,
            request.DuracionMinutos, CreateAuditContext(), cancellationToken);

        return StatusCode(result.StatusCode, new
        {
            success = result.Succeeded,
            code = result.Code,
            sessionId = result.SessionId,
            token = result.Token,
            expiresUtc = result.ExpiresUtc
        });
    }

    [HttpDelete("{sessionId:int}")]
    public async Task<IActionResult> Revocar(
        [FromRoute] int sessionId,
        CancellationToken cancellationToken)
    {
        SetNoStore();
        var result = await _sessions.RevokeAsync(
            sessionId, CreateAuditContext(), cancellationToken);

        return StatusCode(result.StatusCode, new
        {
            success = result.Succeeded,
            code = result.Code,
            sessionId = result.SessionId
        });
    }

    private void SetNoStore()
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        Response.Headers.Pragma = "no-cache";
    }

    private PlatformSupportAuditContext CreateAuditContext() => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString(),
        HttpContext.TraceIdentifier);
}