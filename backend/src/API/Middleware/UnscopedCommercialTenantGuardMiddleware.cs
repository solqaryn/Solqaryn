using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Middleware;

/// <summary>
/// Contención P0 temporal: los maestros y documentos comerciales heredados todavía
/// carecen de EmpresaId. No exponer NINGÚN endpoint de datos si hay más de un
/// registro Empresa, aunque la otra empresa esté inactiva.
/// Se retirará únicamente tras migración y certificación de ownership por fila.
/// </summary>
public sealed class UnscopedCommercialTenantGuardMiddleware
{
    private readonly RequestDelegate _next;

    public UnscopedCommercialTenantGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, AppDbContext db)
    {
        if (EsRutaDeSeguridadPermitida(httpContext.Request))
        {
            await _next(httpContext);
            return;
        }

        // Impide crear una segunda empresa mientras el catálogo comercial permanezca
        // sin tenant owner. Es bloqueo por seguridad, no un éxito de onboarding.
        if (HttpMethods.IsPost(httpContext.Request.Method) &&
            EsRutaExacta(httpContext.Request.Path, "/empresas"))
        {
            await RechazarAsync(httpContext);
            return;
        }

        // Se cuentan también empresas desactivadas: podrían tener datos históricos.
        // Sin caché: altas/cambios de empresas se detectan en la siguiente petición.
        var empresas = await db.Empresas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(empresa => empresa.Id)
            .Select(empresa => empresa.Id)
            .Take(2)
            .ToArrayAsync(httpContext.RequestAborted);

        if (empresas.Length != 1)
        {
            await RechazarAsync(httpContext);
            return;
        }

        await _next(httpContext);
    }

    private static bool EsRutaDeSeguridadPermitida(HttpRequest request)
    {
        if (HttpMethods.IsOptions(request.Method))
            return true;

        var path = request.Path;
        if (HttpMethods.IsGet(request.Method) &&
            (EsRutaExacta(path, "/health") || EsRutaExacta(path, "/health/ready")))
            return true;

        if (HttpMethods.IsPost(request.Method) &&
            (EsRutaExacta(path, "/auth/login") || EsRutaExacta(path, "/auth/renovar")))
            return true;

        // Permite a la sesión autenticada consultar su membresía tenant válida, pero
        // NO leer ni modificar registros comerciales de otro tenant.
        if (HttpMethods.IsGet(request.Method) &&
            path.StartsWithSegments("/tenant-context", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool EsRutaExacta(PathString path, string expected) =>
        string.Equals(path.Value, expected, StringComparison.OrdinalIgnoreCase);

    private static async Task RechazarAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "private, no-store, max-age=0";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            code = "TENANT_DATA_ISOLATION_REQUIRED",
            message = "Esta operación está temporalmente bloqueada por seguridad de los datos empresariales."
        }, context.RequestAborted);
    }
}
