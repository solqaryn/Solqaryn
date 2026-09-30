using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

/// <summary>
/// N7.7.D/N7.7.F: contrato HTTP tenant-safe para cola de correo empresarial durable.
/// El tenant autorizado por el filtro debe coincidir con la ruta y la capa de
/// aplicación vuelve a comprobar membresía server-side antes de persistir.
/// La ruta del backend no incluye el prefijo /api: el proxy Vercel usa /api en
/// navegador y lo elimina antes de reenviar la solicitud al servicio Render.
/// </summary>
[ApiController]
[Authorize]
[Route("email-empresarial/tenants/{empresaId:int}")]
public sealed class EmailEmpresarialController : ControllerBase
{
    private const string IndiceIdempotencia = "UX_EmailsEmpresariales_Empresa_Idempotencia";
    private readonly IEmailEmpresarialService _service;

    public EmailEmpresarialController(
        AppDbContext db,
        IUsuarioScopeService usuarioScopeService,
        IAuditoriaService auditoriaService)
    {
        _service = new EmailEmpresarialService(
            new EmailEmpresarialRepository(db),
            usuarioScopeService,
            auditoriaService);
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Crear)]
    public async Task<IActionResult> Registrar(
        int empresaId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] RegistrarEmailEmpresarialRequest request,
        CancellationToken cancellationToken)
    {
        if (!await TenantCoincideAsync(empresaId, cancellationToken))
            return Problema(403, "Contexto tenant inconsistente", "El tenant autorizado no coincide con la ruta.", "EMAIL_TENANT_CONTEXT_MISMATCH");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problema(400, "Idempotency-Key requerida", "Idempotency-Key es obligatoria para registrar correo empresarial.", "EMAIL_IDEMPOTENCY_KEY_REQUIRED");

        try
        {
            var resultado = await _service.RegistrarAsync(
                empresaId,
                request,
                idempotencyKey.Trim(),
                cancellationToken);
            return Accepted(ApiResponse<EmailEmpresarialResultado>.Ok(resultado));
        }
        catch (ConflictException)
        {
            return Problema(409, "Conflicto de idempotencia", "La clave de idempotencia ya representa otra solicitud.", "EMAIL_IDEMPOTENCY_CONFLICT");
        }
        catch (ForbiddenAccessException)
        {
            return Problema(403, "Acceso denegado", "No existe autoridad tenant activa para la empresa solicitada.", "EMAIL_TENANT_FORBIDDEN");
        }
        catch (ArgumentException)
        {
            return Problema(400, "Solicitud inválida", "Los datos del correo no cumplen el contrato requerido.", "EMAIL_INVALID_REQUEST");
        }
        catch (DbUpdateException ex) when (EsColisionIdempotencia(ex))
        {
            return Problema(409, "Conflicto de idempotencia", "La clave fue consumida concurrentemente por otra solicitud.", "EMAIL_IDEMPOTENCY_RACE");
        }
    }

    [HttpGet("{mensajeId:guid}")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> Obtener(
        int empresaId,
        Guid mensajeId,
        CancellationToken cancellationToken)
    {
        if (!await TenantCoincideAsync(empresaId, cancellationToken))
            return Problema(403, "Contexto tenant inconsistente", "El tenant autorizado no coincide con la ruta.", "EMAIL_TENANT_CONTEXT_MISMATCH");

        try
        {
            var resultado = await _service.ObtenerAsync(empresaId, mensajeId, cancellationToken);
            return resultado is null
                ? Problema(404, "Correo no encontrado", "No existe el correo solicitado dentro del tenant autorizado.", "EMAIL_NOT_FOUND")
                : Ok(ApiResponse<EmailEmpresarialResultado>.Ok(resultado));
        }
        catch (ForbiddenAccessException)
        {
            return Problema(403, "Acceso denegado", "No existe autoridad tenant activa para la empresa solicitada.", "EMAIL_TENANT_FORBIDDEN");
        }
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> Listar(
        int empresaId,
        [FromQuery] EstadoEntregaEmail? estado,
        [FromQuery] string? correlationId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 50,
        CancellationToken cancellationToken = default)
    {
        if (!await TenantCoincideAsync(empresaId, cancellationToken))
            return Problema(403, "Contexto tenant inconsistente", "El tenant autorizado no coincide con la ruta.", "EMAIL_TENANT_CONTEXT_MISMATCH");

        try
        {
            var resultado = await _service.ListarAsync(
                empresaId,
                estado,
                correlationId,
                pagina,
                tamano,
                cancellationToken);
            return Ok(ApiResponse<PaginaEmailEmpresarial>.Ok(resultado));
        }
        catch (ForbiddenAccessException)
        {
            return Problema(403, "Acceso denegado", "No existe autoridad tenant activa para la empresa solicitada.", "EMAIL_TENANT_FORBIDDEN");
        }
        catch (ArgumentException)
        {
            return Problema(400, "Consulta inválida", "Los filtros o la paginación no son válidos.", "EMAIL_INVALID_QUERY");
        }
    }

    private async Task<bool> TenantCoincideAsync(int empresaId, CancellationToken cancellationToken) =>
        await TenantPermissionContext.RequireEmpresaIdAsync(HttpContext, cancellationToken) == empresaId;

    private static bool EsColisionIdempotencia(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(IndiceIdempotencia, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private ObjectResult Problema(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
