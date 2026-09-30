using System.Security.Cryptography;
using System.Text;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

/// <summary>
/// N7.6.D - boundary HTTP seguro para WhatsApp Business.
/// No fabrica QR ni degrada a un fallback inseguro cuando el proveedor de sesión
/// todavía no está conectado; expone el estado verificable de la configuración tenant.
/// </summary>
[ApiController]
[Route("whatsapp")]
public sealed class WhatsAppController : ControllerBase
{
    private const string VerifyTokenHeader = "X-WhatsApp-Verify-Token";

    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        AppDbContext db,
        IConfiguration configuration,
        IUsuarioScopeService usuarioScope,
        ILogger<WhatsAppController> logger)
    {
        _db = db;
        _configuration = configuration;
        _usuarioScope = usuarioScope;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public IActionResult Webhook()
    {
        var expected = _configuration["WA_WEBHOOK_VERIFY_TOKEN"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            _logger.LogError("Webhook WhatsApp no disponible: WA_WEBHOOK_VERIFY_TOKEN no configurado.");
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Webhook de WhatsApp no configurado.",
                detail: "La validación del origen no está disponible.");
        }

        var supplied = Request.Headers[VerifyTokenHeader].ToString();
        if (!FixedTimeEquals(expected, supplied))
        {
            _logger.LogWarning(
                "Webhook WhatsApp rechazado por token de verificación inválido. Correlación {CorrelationId}",
                HttpContext.TraceIdentifier);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Webhook de WhatsApp no autorizado.",
                detail: "El token de verificación es ausente o inválido.");
        }

        return Ok(new WhatsAppWebhookResponse("accepted"));
    }

    [HttpGet("publico")]
    [AllowAnonymous]
    [PublicHttpCache(PublicHttpCacheProfile.Identity)]
    public async Task<IActionResult> GetPublicoAsync(CancellationToken cancellationToken)
    {
        var configuraciones = await _db.Set<ConfiguracionWhatsAppEmpresa>()
            .AsNoTracking()
            .Where(x => x.Activa)
            .Select(x => new
            {
                x.NumeroTelefonoE164,
                EmpresaNombre = x.Empresa.Nombre
            })
            .ToListAsync(cancellationToken);

        var numeros = configuraciones
            .Select(x => x.NumeroTelefonoE164)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (numeros.Count == 1)
            return Ok(new WhatsAppPublicoResponse(numeros[0], true));

        if (numeros.Count > 1)
        {
            var identidadPublica = await _db.Set<EmpresaConfiguracion>()
                .AsNoTracking()
                .Where(x => x.Activa)
                .Select(x => new { x.NombreComercial, x.NombreVisibleSistema })
                .SingleOrDefaultAsync(cancellationToken);

            if (identidadPublica is not null)
            {
                var nombresPublicos = new[] { identidadPublica.NombreComercial, identidadPublica.NombreVisibleSistema }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var coincidencias = configuraciones
                    .Where(x => nombresPublicos.Any(nombre =>
                        string.Equals(nombre, x.EmpresaNombre?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .Select(x => x.NumeroTelefonoE164)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (coincidencias.Count == 1)
                    return Ok(new WhatsAppPublicoResponse(coincidencias[0], true));
            }

            _logger.LogWarning(
                "WhatsApp público no expuesto: existen varias configuraciones activas y ninguna coincide de forma inequívoca con la identidad pública. Correlación {CorrelationId}",
                HttpContext.TraceIdentifier);
        }

        return Ok(new WhatsAppPublicoResponse(null, false));
    }

    [HttpPost("iniciar-whatsapp")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> IniciarAsync(
        [FromBody] IniciarWhatsAppRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EmpresaId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Empresa inválida.",
                detail: "EmpresaId debe ser positivo.");
        }

        var scope = await _usuarioScope.ObtenerActualAsync(request.EmpresaId, cancellationToken);
        if (scope is null)
        {
            _logger.LogWarning(
                "Inicio WhatsApp rechazado por scope tenant inválido para empresa {EmpresaId}. Correlación {CorrelationId}",
                request.EmpresaId,
                HttpContext.TraceIdentifier);
            return Forbid();
        }

        var configuracion = await _db.Set<ConfiguracionWhatsAppEmpresa>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EmpresaId == request.EmpresaId && x.Activa,
                cancellationToken);

        if (configuracion is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "WhatsApp no configurado.",
                detail: "La empresa no tiene una configuración activa de WhatsApp Business.");
        }

        // N7.6.D no inventa una sesión ni un QR. La conexión real al proveedor debe
        // materializar un QR verificable; hasta entonces el contrato retorna estado
        // explícito y QR nulo en lugar de un fallback inseguro.
        return Ok(new IniciarWhatsAppResponse(
            Status: "CONFIGURADA_SIN_SESION",
            Qr: null,
            NumeroTelefonoE164: configuracion.NumeroTelefonoE164,
            RequiereProveedorSesion: true));
    }

    internal static bool FixedTimeEquals(string expected, string supplied)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

public sealed record WhatsAppPublicoResponse(string? NumeroTelefonoE164, bool Disponible);
public sealed record IniciarWhatsAppRequest(int EmpresaId);
public sealed record IniciarWhatsAppResponse(
    string Status,
    string? Qr,
    string NumeroTelefonoE164,
    bool RequiereProveedorSesion);
public sealed record WhatsAppWebhookResponse(string Status);
