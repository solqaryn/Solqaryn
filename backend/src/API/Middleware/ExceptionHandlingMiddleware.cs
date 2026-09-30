using System.Net;
using System.Text.Json;
using FluentValidation;
using Solqaryn.Application.Common;
using Solqaryn.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Petición cancelada por el cliente. Referencia {Referencia}",
                context.TraceIdentifier);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Error de validación");
            await EscribirAsync(
                context,
                HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail("Error de validación.", ex.Errors.Select(e => e.ErrorMessage).ToList()));
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Regla de negocio violada");
            await EscribirAsync(context, HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido en la operación");
            await EscribirProblemaAsync(context, HttpStatusCode.BadRequest, "Solicitud inválida", ex.Message);
        }
        catch (ForbiddenAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso denegado por permisos");
            await EscribirAsync(context, HttpStatusCode.Forbidden, ApiResponse<object>.Fail(ex.Message));
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogInformation(ex, "Recurso no encontrado");
            await EscribirProblemaAsync(context, HttpStatusCode.NotFound, "Recurso no encontrado", ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflicto de operación");
            await EscribirProblemaAsync(context, HttpStatusCode.Conflict, "Conflicto de operación", ex.Message);
        }
        catch (UniqueConstraintViolationException ex)
        {
            _logger.LogWarning(ex, "Conflicto de unicidad {Constraint}", ex.ConstraintName);
            await EscribirProblemaAsync(context, HttpStatusCode.Conflict, "Conflicto de unicidad", ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflicto de concurrencia optimista");
            await EscribirProblemaAsync(
                context,
                HttpStatusCode.Conflict,
                "Conflicto de concurrencia",
                "La información cambió desde la última lectura. Recarga e intenta nuevamente.");
        }
        catch (DbUpdateException ex)
        {
            var referencia = context.TraceIdentifier;
            var detalleTecnico = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Error de persistencia. Referencia {Referencia}", referencia);

            if (detalleTecnico.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) ||
                detalleTecnico.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase))
            {
                await EscribirAsync(
                    context,
                    HttpStatusCode.Conflict,
                    ApiResponse<object>.Fail($"Ya existe un registro con el mismo color, SKU o código de barras. Referencia: {referencia}."));
                return;
            }

            var estructuraDesactualizada =
                detalleTecnico.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase) ||
                detalleTecnico.Contains("Unknown column", StringComparison.OrdinalIgnoreCase) ||
                detalleTecnico.Contains("no such table", StringComparison.OrdinalIgnoreCase);

            var mensaje = estructuraDesactualizada
                ? $"El entorno de datos se está actualizando. Reintenta en unos minutos. Referencia: {referencia}."
                : $"No fue posible guardar la información. Reintenta nuevamente. Referencia: {referencia}.";

            await EscribirAsync(context, HttpStatusCode.ServiceUnavailable, ApiResponse<object>.Fail(mensaje));
        }
        catch (Exception ex)
        {
            var referencia = context.TraceIdentifier;
            _logger.LogError(ex, "Error no controlado. Referencia {Referencia}", referencia);
            await EscribirAsync(
                context,
                HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail($"Ocurrió un error interno. Intenta nuevamente más tarde. Referencia: {referencia}."));
        }
    }

    private static async Task EscribirProblemaAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static async Task EscribirAsync(HttpContext context, HttpStatusCode statusCode, ApiResponse<object> response)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}