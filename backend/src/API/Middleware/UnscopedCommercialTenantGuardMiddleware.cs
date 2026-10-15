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
/// Contención P0: con 2+ empresas la API privada permanece en cuarentena.
/// Excepciones mínimas: tienda pública ligada a Host autorizado, lecturas de
/// maestros, tipos de cliente y altas de clientes/proveedores/tipos con
/// opt-in independiente y RBAC. No habilita ventas, compras, facturación, inventario,
/// medios, exportaciones, jobs ni rutas administrativas generales.
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

        // Alta de plataforma: excepción de ruta estricta, NO autoridad.
        // JWT y rol PLATAFORMA se vuelven a verificar en el controlador contra MySQL.
        // Dos flags independientes deben estar activados expresamente; ninguna
        // otra ruta se desbloquea ni se retira la cuarentena comercial.
        if (HttpMethods.IsPost(httpContext.Request.Method) &&
            EsRutaExacta(httpContext.Request.Path, "/plataforma/empresas/alta-controlada"))
        {
            var config = httpContext.RequestServices.GetService<IConfiguration>();
            if (config?.GetValue<bool>("MultiEmpresa:CommercialIsolationCertified") == true &&
                config.GetValue<bool>("MultiEmpresa:PlatformOnboardingEnabled") &&
                httpContext.User.Identity?.IsAuthenticated == true)
            {
                await _next(httpContext);
                return;
            }
            await RechazarAsync(httpContext);
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
            // Pilotos independientes, apagados por defecto. Solo rutas exactas
            // y acción RBAC correspondiente; ninguna bandera habilita CRUD general.
            if (empresas.Length == 2 && await AutorizarOperacionPilotoAsync(httpContext, db))
                return;

            await RechazarAsync(httpContext);
            return;
        }

        await _next(httpContext);
    }

    private async Task<bool> AutorizarOperacionPilotoAsync(HttpContext http, AppDbContext db)
    {
        // Selección de ruta/método ANTES de emitir una marca empresarial.
        // Las dos banderas se controlan por separado y siguen false por defecto.
        var services = http.RequestServices;
        var config = services?.GetService<IConfiguration>();
        if (http.User.Identity?.IsAuthenticated != true || config is null)
            return false;

        var lectura = HttpMethods.IsGet(http.Request.Method) &&
                      config.GetValue<bool>("MultiEmpresa:PrivateReadPilotEnabled");
        var alta = HttpMethods.IsPost(http.Request.Method) &&
                   config.GetValue<bool>("MultiEmpresa:PrivateWritePilotEnabled");
        // No activa mutaciones de inventario ni confirmaciones fiscales.
        // El piloto adicional sólo admite altas de producto SIN archivos
        // y borradores de venta/compra; nunca altera el modo por defecto.
        var borrador = HttpMethods.IsPost(http.Request.Method) &&
                       config.GetValue<bool>("MultiEmpresa:PrivateDraftPilotEnabled");
        var confirmar = HttpMethods.IsPost(http.Request.Method) &&
                        config.GetValue<bool>("MultiEmpresa:PrivateConfirmPilotEnabled");
        if (!lectura && !alta && !borrador && !confirmar)
            return false;

        ModuloSistema modulo;
        AccionPermiso accion;
        var esBorrador = false;
        var esConfirmacion = false;
        if (lectura && (EsRutaExacta(http.Request.Path, "/productos") ||
                        EsRutaDetallePositiva(http.Request.Path, "/productos")))
            modulo = ModuloSistema.Productos;
        else if (lectura && (EsRutaExacta(http.Request.Path, "/clientes") ||
                             EsRutaExacta(http.Request.Path, "/clientes/activos") ||
                             EsRutaExacta(http.Request.Path, "/clientes/buscar") ||
                             EsRutaDetallePositiva(http.Request.Path, "/clientes")))
            modulo = ModuloSistema.Clientes;
        else if (lectura && (EsRutaExacta(http.Request.Path, "/proveedores") ||
                             EsRutaExacta(http.Request.Path, "/proveedores/activos") ||
                             EsRutaExacta(http.Request.Path, "/proveedores/buscar") ||
                             EsRutaDetallePositiva(http.Request.Path, "/proveedores")))
            modulo = ModuloSistema.Proveedores;
        else if (lectura && (EsRutaExacta(http.Request.Path, "/tipo-clientes") ||
                             EsRutaExacta(http.Request.Path, "/tipo-clientes/activos") ||
                             EsRutaDetallePositiva(http.Request.Path, "/tipo-clientes")))
            modulo = ModuloSistema.TiposClientes;
        else if (lectura && (EsRutaExacta(http.Request.Path, "/ventas") ||
                             EsRutaDetallePositiva(http.Request.Path, "/ventas")))
            modulo = ModuloSistema.Ventas;
        else if (lectura && (EsRutaExacta(http.Request.Path, "/compras") ||
                             EsRutaDetallePositiva(http.Request.Path, "/compras")))
            modulo = ModuloSistema.Compras;
        else if (lectura && (EsRutaExacta(http.Request.Path, "/facturas") ||
                             EsRutaDetallePositiva(http.Request.Path, "/facturas") ||
                             EsRutaDetallePositiva(http.Request.Path, "/facturas/venta")))
            modulo = ModuloSistema.Facturacion;
        else if (alta && EsRutaExacta(http.Request.Path, "/tipo-clientes"))
            modulo = ModuloSistema.TiposClientes;
        else if (alta && EsRutaExacta(http.Request.Path, "/clientes"))
            modulo = ModuloSistema.Clientes;
        else if (alta && EsRutaExacta(http.Request.Path, "/proveedores"))
            modulo = ModuloSistema.Proveedores;
        else if (borrador && EsRutaExacta(http.Request.Path, "/productos"))
        {
            modulo = ModuloSistema.Productos;
            esBorrador = true;
        }
        else if (borrador && EsRutaExacta(http.Request.Path, "/ventas"))
        {
            modulo = ModuloSistema.Ventas;
            esBorrador = true;
        }
        else if (borrador && EsRutaExacta(http.Request.Path, "/compras"))
        {
            modulo = ModuloSistema.Compras;
            esBorrador = true;
        }
        else if (confirmar && EsRutaAccionPositiva(http.Request.Path, "/ventas", "confirmar"))
        {
            modulo = ModuloSistema.Ventas;
            esConfirmacion = true;
        }
        else if (confirmar && EsRutaAccionPositiva(http.Request.Path, "/compras", "confirmar"))
        {
            modulo = ModuloSistema.Compras;
            esConfirmacion = true;
        }
        else
            return false;

        accion = lectura ? AccionPermiso.Ver
            : esConfirmacion ? AccionPermiso.Confirmar : AccionPermiso.Crear;
        var permisos = services.GetService<IPermisoService>();
        if (permisos is null)
            return false;

        int empresaId;
        try
        {
            // La cabecera selecciona, pero la membresía y el permiso se
            // verifican server-side: nunca autoriza por sí sola.
            empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(
                http, http.RequestAborted);
        }
        catch (ForbiddenAccessException)
        {
            return false;
        }

        if (!await permisos.TienePermisoAsync(empresaId, modulo, accion))
            return false;

        var activa = await db.Set<Empresa>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(e => e.Id == empresaId && e.Activa, http.RequestAborted);
        if (!activa)
            return false;

        if (esBorrador)
        {
            // Solo formularios pequeños y sin IFormFile. Cloudinary se
            // habilitará separadamente tras pruebas físicas de A/B.
            const int maxPayloadBytes = 256 * 1024;
            var length = http.Request.ContentLength;
            if (length is null || length <= 0 || length > maxPayloadBytes)
                return false;

            if (EsRutaExacta(http.Request.Path, "/productos"))
            {
                if (!http.Request.HasFormContentType)
                    return false;
                try
                {
                    var form = await http.Request.ReadFormAsync(http.RequestAborted);
                    if (form.Files.Count != 0)
                        return false;
                }
                catch (InvalidDataException)
                {
                    return false;
                }
            }
            else if (http.Request.ContentType?.StartsWith(
                         "application/json", StringComparison.OrdinalIgnoreCase) != true)
            {
                return false;
            }
        }

        // Requiere una SEGUNDA autorización en el filtro MVC RequierePermiso.
        // La misma marca controla owner de la nueva fila en los servicios
        // comerciales, filtros de lectura EF y auditoría por tenant.
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

    // Sólo /maestro/{id:int} con un segmento numérico positivo, sin subrutas.
    // Solo `/{id:int positivo}/{accion}`; prohibidas subrutas, slash final,
    // prefijos parecidos, números con signos o percent-encoding ambiguo.
    private static bool EsRutaAccionPositiva(PathString path, string prefijo, string accion)
    {
        var ruta = path.Value;
        var comienzo = prefijo + "/";
        var fin = "/" + accion;
        if (ruta is null ||
            !ruta.StartsWith(comienzo, StringComparison.OrdinalIgnoreCase) ||
            !ruta.EndsWith(fin, StringComparison.OrdinalIgnoreCase))
            return false;

        var segmento = ruta[comienzo.Length..^fin.Length];
        return segmento.Length > 0 &&
               segmento.All(caracter => caracter >= '0' && caracter <= '9') &&
               int.TryParse(segmento, out var id) && id > 0;
    }

    private static bool EsRutaDetallePositiva(PathString path, string prefijo)
    {
        var ruta = path.Value;
        if (ruta is null || !ruta.StartsWith(prefijo + "/", StringComparison.OrdinalIgnoreCase))
            return false;

        var segmento = ruta[(prefijo.Length + 1)..];
        return segmento.Length > 0 &&
               segmento.All(caracter => caracter >= '0' && caracter <= '9') &&
               int.TryParse(segmento, out var id) && id > 0;
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
