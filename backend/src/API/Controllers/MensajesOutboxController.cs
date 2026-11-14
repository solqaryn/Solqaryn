using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("api/outbox/tenants/{empresaId:int}")]
public sealed class MensajesOutboxController : ControllerBase
{
    private const string IndiceIdempotencia = "UX_MensajesOutbox_EmpresaId_ClaveIdempotencia";

    private readonly IMensajeOutboxService _service;
    private readonly AppDbContext _db;
    private readonly IAuditoriaService? _auditoria;

    public MensajesOutboxController(
        IMensajeOutboxService service,
        AppDbContext db,
        IAuditoriaService? auditoria = null)
    {
        _service = service;
        _db = db;
        _auditoria = auditoria;
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Crear)]
    public async Task<IActionResult> Registrar(
        int empresaId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] RegistrarMensajeOutboxRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problema(
                StatusCodes.Status400BadRequest,
                "Idempotency-Key requerida",
                "Idempotency-Key es obligatoria para registrar un mensaje outbox.",
                "OUTBOX_IDEMPOTENCY_KEY_REQUIRED");
        }

        // El filtro de permiso autoriza el tenant indicado en X-Empresa-Id. La ruta
        // no puede apuntar a otro tenant: de lo contrario un usuario con permiso en
        // A y mera membresía en B podría reutilizar el permiso de A para escribir en B.
        var empresaAutorizada = await TenantPermissionContext.RequireEmpresaIdAsync(HttpContext, cancellationToken);
        if (empresaAutorizada != empresaId)
        {
            if (_auditoria is not null)
            {
                await _auditoria.RegistrarAsync(
                    ModuloSistema.Configuracion,
                    AccionPermiso.Crear,
                    "Intento rechazado de registrar un mensaje outbox con tenant de ruta distinto al tenant autorizado.",
                    entidad: "MensajeOutbox",
                    valoresNuevos: new { EmpresaSolicitada = empresaId, EmpresaAutorizada = empresaAutorizada },
                    resultado: "Rechazado",
                    error: "OUTBOX_TENANT_CONTEXT_MISMATCH");
            }

            return Problema(
                StatusCodes.Status403Forbidden,
                "Contexto tenant inconsistente",
                "El tenant autorizado no coincide con la empresa solicitada.",
                "OUTBOX_TENANT_CONTEXT_MISMATCH");
        }

        try
        {
            var mensaje = await _service.RegistrarAsync(
                empresaId,
                request,
                idempotencyKey.Trim(),
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            if (_auditoria is not null)
            {
                await _auditoria.RegistrarAsync(
                    ModuloSistema.Configuracion,
                    AccionPermiso.Crear,
                    "Mensaje outbox registrado para despacho posterior.",
                    entidad: "MensajeOutbox",
                    valoresNuevos: new
                    {
                        mensaje.EventoId,
                        mensaje.EmpresaId,
                        mensaje.TipoEvento,
                        mensaje.Estado,
                        mensaje.CorrelationId
                    });
            }

            return Accepted(ApiResponse<MensajeOutboxRegistroResponse>.Ok(
                new MensajeOutboxRegistroResponse(
                    mensaje.EventoId,
                    mensaje.EmpresaId,
                    mensaje.Estado.ToString())));
        }
        catch (ConflictException)
        {
            return Problema(
                StatusCodes.Status409Conflict,
                "Conflicto de idempotencia",
                "La clave de idempotencia ya fue utilizada para esta empresa.",
                "OUTBOX_IDEMPOTENCY_CONFLICT");
        }
        catch (ForbiddenAccessException)
        {
            return Problema(
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                "No existe autoridad tenant activa para la empresa solicitada.",
                "OUTBOX_TENANT_FORBIDDEN");
        }
        catch (ArgumentException)
        {
            return Problema(
                StatusCodes.Status400BadRequest,
                "Solicitud inválida",
                "Los datos del mensaje outbox no cumplen el contrato requerido.",
                "OUTBOX_INVALID_REQUEST");
        }
        catch (DbUpdateException ex) when (EsColisionIdempotencia(ex))
        {
            // La restricción única (EmpresaId, ClaveIdempotencia) es la última
            // defensa ante carreras interleaved. El detalle del provider nunca sale al cliente.
            return Problema(
                StatusCodes.Status409Conflict,
                "Conflicto de idempotencia",
                "No fue posible registrar el mensaje porque la clave ya fue consumida.",
                "OUTBOX_IDEMPOTENCY_RACE");
        }
    }

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

public sealed record MensajeOutboxRegistroResponse(Guid EventoId, int EmpresaId, string Estado);
