using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Solqaryn.Domain.Enums;
using Solqaryn.Application.Exceptions;
using Solqaryn.API.Filters;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Middleware;

/// <summary>
/// Contención P0: las rutas comerciales privadas siguen en cuarentena con
/// 2+ empresas, incluso si una está inactiva. La única excepción delimitada
/// es /tienda/* con Host exacto previamente aprobado por configuración del
/// servidor y empresa activa comprobada de nuevo contra la base real.
/// No se habilitan exportaciones, archivos ni tareas internas por este cambio.
/// </summary>
public sealed class UnscopedCommercialTenantGuardMiddleware
{
    private readonly RequestDelegate _next;

    public UnscopedCommercialTenantGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, AppDbContext db,
        IPublicStoreHostTenantResolver? publicHostResolver = null)
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
        var empresas = await db.Set<Empresa>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(empresa => empresa.Id)
            .Select(empresa => new { empresa.Id, empresa.Activa })
            .Take(2)
            .ToArrayAsync(httpContext.RequestAborted);

        var rutaTienda = httpContext.Request.Path.StartsWithSegments(
            "/tienda", StringComparison.OrdinalIgnoreCase);
        if (rutaTienda)
        {
            // Un mapping de Host solo tiene autoridad si procede del registro
            // explícito server-side. Ni X-Forwarded-Host ni X-Empresa-Id
            // pueden proporcionar el EmpresaId de una tienda.
            var empresaPublica = publicHostResolver?.ResolveEmpresaId(httpContext.Request.Host);
            if (empresaPublica is > 0)
            {
                // Comprueba el propietario en MySQL y el estado vigente EN CADA
                // solicitud: una tienda suspendida no recibe checkout ni sesión.
                var autorizada = await db.Set<Empresa>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(e => e.Id == empresaPublica.Value && e.Activa,
                        httpContext.RequestAborted);
                if (!autorizada)
                {
                    await RechazarAsync(httpContext);
                    return;
                }

                httpContext.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] =
                    empresaPublica.Value;
                await _next(httpContext);
                return;
            }

            // Con allowlist habilitada, hosts desconocidos fallan cerrados
            // incluso si sólo hay una empresa. Sin allowlist, preservar modo
            // legacy monoempresa exclusivamente.
            if (publicHostResolver?.IsEnabled == true ||
                empresas.Length != 1 || !empresas[0].Activa)
            {
                await RechazarAsync(httpContext);
                return;
            }
            httpContext.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] =
                empresas[0].Id;
            await _next(httpContext);
            return;
        }

        // La autorización administrativa, exports, descargas y jobs NO se
        // desbloquean al habilitar hosts públicos: conservan cuarentena P0.
        if (empresas.Length != 1)
        {
            // Opt-in apagado por defecto: piloto sólo de lecturas de maestros
            // con membresía+RBAC verificadas antes de entrar a MVC/EF.
            // No habilita CRUD, archivos, exportaciones, jobs ni onboarding A/B.
            if (empresas.Length == 2 && await AutorizarLecturaPilotoAsync(httpContext, db))
                return;

            await RechazarAsync(httpContext);
            return;
        }

        await _next(httpContext);
    }

    private async Task<bool> AutorizarLecturaPilotoAsync(HttpContext http, AppDbContext db)
    {
        // Nada se desbloquea accidentalmente al desplegar; requiere opt-in
        // explícito y un principal previamente autenticado por ASP.NET Core.
        var services = http.RequestServices;
        if (services?.GetService<IConfiguration>()?.GetValue<bool>("MultiEmpresa:PrivateReadPilotEnabled") != true ||
            !HttpMethods.IsGet(http.Request.Method) ||
            http.User.Identity?.IsAuthenticated != true)
            return false;

        // Lista exacta: jamás aceptar un prefijo, subruta, descarga o mutación.
        ModuloSistema modulo;
        if (EsRutaExacta(http.Request.Path, "/productos"))
            modulo = ModuloSistema.Productos;
        else if (EsRutaExacta(http.Request.Path, "/clientes"))
            modulo = ModuloSistema.Clientes;
        else if (EsRutaExacta(http.Request.Path, "/proveedores"))
            modulo = ModuloSistema.Proveedores;
        else
            return false;

        var permisos = services.GetService<IPermisoService>();
        if (permisos is null)
            return false;

        int empresaId;
        try
        {
            // X-Empresa-Id sólo selecciona; jamás autoriza por sí solo.
            empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(
                http, http.RequestAborted);
        }
        catch (ForbiddenAccessException)
        {
            return false;
        }

        // La verificación real consulta membresía activa + permisos del usuario.
        if (!await permisos.TienePermisoAsync(empresaId, modulo, AccionPermiso.Ver))
            return false;

        // Empresa desactivada, inexistente o ID falsificado: nunca publicar.
        var activa = await db.Set<Empresa>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(e => e.Id == empresaId && e.Activa, http.RequestAborted);
        if (!activa)
            return false;

        // Requiere el filtro RequierePermiso original nuevamente en MVC.
        // El DbContext filtra registros por esta marca verificada por solicitud.
        TenantPermissionContext.MarkAuthorizedEmpresaId(http, empresaId);
        await _next(http);
        return true;
    }

    private static bool EsRutaDeSeguridadPermitida(HttpRequest request)
    {
        if (HttpMethods.IsOptions(request.Method))
            return true;

        // La ruta de token solo permite atravesar el middleware. El servicio
        // valida hash, vigencia, owner y FacturaId antes de entregar el PDF.
        if (HttpMethods.IsGet(request.Method) &&
            EsRutaFacturaPublicaConToken(request.Path))
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

    private static bool EsRutaFacturaPublicaConToken(PathString path)
    {
        var segmentos = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmentos is not { Length: 4 } ||
            !string.Equals(segmentos[0], "facturas", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(segmentos[1], "publico", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(segmentos[3], "pdf", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = segmentos[2];
        return token.Length is >= 32 and <= 160 &&
               token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
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
