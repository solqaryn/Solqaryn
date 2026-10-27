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
        IPublicStoreHostTenantResolver? publicHostResolver = null,
        IPublicStoreProofReplayStore? replayStore = null)
    {
        if (EsRutaDeSeguridadPermitida(httpContext.Request))
        {
            await _next(httpContext);
            return;
        }

        // Excepción mínima de descubrimiento: GET /permisos/mis-permisos/empresa/{id>0}.
        // No cambia RBAC: MVC exige JWT y PermisoService relee la membresía real
        // del tenant solicitado. Bloquea subrutas, otros verbos y anónimos.
        if (EsRutaPermisosEmpresaConsulta(httpContext.Request))
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                await _next(httpContext);
                return;
            }
            await RechazarAsync(httpContext);
            return;
        }

        // Autogestión de perfil: únicamente GET exacto y con JWT ya autenticado.
        // El controlador no recibe UsuarioId ni EmpresaId; PerfilService relee la
        // cuenta propia por claim y exige que siga activa, no bloqueada y no eliminada.
        // Ningún verbo vecino, subruta de perfil o endpoint /permisos se habilita aquí.
        if (HttpMethods.IsGet(httpContext.Request.Method) &&
            EsRutaExacta(httpContext.Request.Path, "/perfil"))
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                await _next(httpContext);
                return;
            }

            await RechazarAsync(httpContext);
            return;
        }

        // Sesión de plataforma: GET exacto, sin EmpresaId ni opt-in comercial.
        // La autenticación JWT y el rol global vigente se verifican en MVC/MySQL.
        // No habilitar ninguna otra ruta /plataforma, ni otros métodos.
        if (HttpMethods.IsGet(httpContext.Request.Method) &&
            EsRutaExacta(httpContext.Request.Path, "/plataforma/sesion"))
        {
            await _next(httpContext);
            return;
        }

        // Catálogo de plataforma: solo GET exacto. MVC vuelve a exigir JWT y
        // autoridad global desde MySQL; no concede privilegios tenant.
        if (HttpMethods.IsGet(httpContext.Request.Method) &&
            EsRutaExacta(httpContext.Request.Path, "/plataforma/empresas"))
        {
            await _next(httpContext);
            return;
        }

        // Gestión de identidad estricta de plataforma. Nunca se libera el
        // resto de la API comercial; MVC exige JWT y grant global vigente.
        // Los verbos y los segmentos numéricos son una allowlist exacta.
        if (EsRutaGestionBasicaPlataforma(httpContext.Request))
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                await _next(httpContext);
                return;
            }
            await RechazarAsync(httpContext);
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

        // Alta de borrador SIN operación comercial: JWT más autoridad global
        // verificada en el controlador. El tenant nace suspendido y no se
        // convierte automáticamente en una tienda pública.
        if (HttpMethods.IsPost(httpContext.Request.Method) &&
            EsRutaExacta(httpContext.Request.Path, "/plataforma/empresas/borrador"))
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
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
            // Un proxy Vercel autorizado prueba el origen público y, si
            // procede, la clave de /tiendas/{storeKey}. Solo esa prueba puede
            // aportar el Host original: Request.Host en Render es el host de la API.
            // No aceptar X-Forwarded-Host, X-Empresa-Id ni rutas inventadas.
            var configPublica = httpContext.RequestServices?.GetService<IConfiguration>();
            int? empresaPublica;
            var tieneProof = PublicStoreProxyProof.HasProofHeaders(httpContext.Request);
            // Identidad pública y binding sólo se habilitan con prueba HMAC.
            // Nunca heredan el fallback monoempresa ni confían en cabeceras del cliente.
            var rutaIdentidad = EsRutaExacta(httpContext.Request.Path, "/tienda/configuracion");
            if ((!tieneProof && (rutaIdentidad ||
                EsRutaExacta(httpContext.Request.Path, "/tienda/binding"))) ||
                (rutaIdentidad && !HttpMethods.IsGet(httpContext.Request.Method)))
            {
                await RechazarAsync(httpContext);
                return;
            }
            if (tieneProof)
            {
                if (publicHostResolver?.IsEnabled != true || configPublica is null ||
                    !PublicStoreProxyProof.TryReadVerified(httpContext.Request, configPublica,
                        out var verifiedHost, out var verifiedStoreKey))
                {
                    await RechazarAsync(httpContext);
                    return;
                }
                // Defensa adicional a Render: aunque alguien consiguiera una
                // prueba HMAC válida no puede utilizar rutas comerciales para
                // una tienda secundaria durante esta microfase read-only.
                if (verifiedStoreKey is not null &&
                    (!(HttpMethods.IsGet(httpContext.Request.Method) ||
                       HttpMethods.IsHead(httpContext.Request.Method)) ||
                     !(EsRutaExacta(httpContext.Request.Path, "/tienda/binding") ||
                       (HttpMethods.IsGet(httpContext.Request.Method) && rutaIdentidad))))
                {
                    await RechazarAsync(httpContext);
                    return;
                }
                empresaPublica = publicHostResolver.ResolveEmpresaId(verifiedHost, verifiedStoreKey);
                // Para nuevas tiendas, el registro indexado de MySQL reemplaza
                // la lista manual de variables por empresa, pero sólo con
                // opt-in explícito y proof HMAC previamente verificado.
                if (verifiedStoreKey is not null &&
                    configPublica.GetValue<bool>("PublicStore:EnableDatabaseBindings"))
                {
                    empresaPublica = await db.PublicStoreBindings.IgnoreQueryFilters().AsNoTracking()
                        .Where(x => x.Activa && x.Host == verifiedHost.Host &&
                                    x.StoreKey == verifiedStoreKey)
                        .Select(x => (int?)x.EmpresaId)
                        .SingleOrDefaultAsync(httpContext.RequestAborted);
                }
                // Mientras se certifica la identidad/caché por tienda, no permitir
                // almacenar el mismo path interno /tienda en un CDN compartido.
                // El filtro MVC debe conocer la restricción antes de evaluar 304.
                VerifiedPublicStoreTenant.RequirePrivateNoStore(httpContext);
                httpContext.Response.OnStarting(() =>
                {
                    httpContext.Response.Headers.CacheControl = "private, no-store, max-age=0";
                    httpContext.Response.Headers.Remove("ETag");
                    return Task.CompletedTask;
                });
            }
            else
            {
                // Opt-in de seguridad para múltiples tiendas. Con prueba obligatoria
                // no existe fallback a Host Render ni al modo una sola empresa.
                if (publicHostResolver?.IsEnabled == true &&
                    configPublica?.GetValue<bool>("PublicStore:RequireVerifiedProxyProof") == true)
                {
                    // Ventana de migración sin interrupción: sólo /tienda legacy,
                    // empresa explícita y host upstream exacto, jamás el tenant
                    // por X-Forwarded-Host, cookie o X-Empresa-Id. La prueba
                    // firmada sigue siendo obligatoria para /tiendas/{clave}.
                    var legacyHost = configPublica["PublicStore:LegacyUpstreamHost"];
                    // La empresa legacy está fijada en MySQL antes de crear
                    // otros tenants; no requiere ID manual por nuevo cliente.
                    var persistedLegacyId = string.IsNullOrWhiteSpace(legacyHost) ? null :
                        await db.PublicStoreBindings.IgnoreQueryFilters().AsNoTracking()
                            .Where(x => x.Host == legacyHost && x.StoreKey == "" && x.Activa)
                            .Select(x => (int?)x.EmpresaId)
                            .SingleOrDefaultAsync(httpContext.RequestAborted);
                    var configuredLegacyId = configPublica.GetValue<int>("PublicStore:LegacyRootEmpresaId");
                    // Compatibilidad con instalaciones que ya tienen owner
                    // explícito en variables, sin elegir dinámicamente un ID.
                    int? explicitLegacyOwnerId = persistedLegacyId ??
                        (configuredLegacyId > 0 ? configuredLegacyId : null);
                    if (explicitLegacyOwnerId is not > 0 ||
                        (persistedLegacyId.HasValue && configuredLegacyId > 0 && configuredLegacyId != persistedLegacyId) ||
                        string.IsNullOrWhiteSpace(legacyHost) ||
                        !string.Equals(httpContext.Request.Host.Host, legacyHost,
                            StringComparison.OrdinalIgnoreCase) ||
                        httpContext.Request.Host.Port is not null)
                    {
                        await RechazarAsync(httpContext);
                        return;
                    }
                    empresaPublica = explicitLegacyOwnerId;
                    // Un owner legacy fijado tampoco puede recibir ETag/304 público.
                    VerifiedPublicStoreTenant.RequirePrivateNoStore(httpContext);
                    httpContext.Response.OnStarting(() =>
                    {
                        httpContext.Response.Headers.CacheControl = "private, no-store, max-age=0";
                        httpContext.Response.Headers.Remove("ETag");
                        return Task.CompletedTask;
                    });
                }
                else
                {
                    // Compatibilidad original intacta mientras no haya opt-in.
                    empresaPublica = publicHostResolver?.ResolveEmpresaId(httpContext.Request.Host);
                }
            }
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

                // Consume only proofs for an already-approved, active company:
                // unknown URLs cannot populate the replay ledger through the proxy.
                if (tieneProof && (replayStore is null ||
                    !await replayStore.TryConsumeAsync(
                        httpContext.Request.Headers[PublicStoreProxyProof.ProofHeader].ToString(),
                        httpContext.RequestAborted)))
                {
                    await RechazarAsync(httpContext);
                    return;
                }

                httpContext.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] =
                    empresaPublica.Value;
                await _next(httpContext);
                return;
            }

            // Antes de existir una segunda empresa se conserva la tienda
            // monoempresa original. Con 2+ empresas no es seguro elegir una
            // por orden/estado: exigir propietario LEGACY fijado por plataforma
            // antes del primer borrador, jamás reasignarlo automáticamente.
            if (publicHostResolver?.IsEnabled == true)
            {
                await RechazarAsync(httpContext);
                return;
            }
            int? legacyId;
            if (empresas.Length == 1)
            {
                legacyId = empresas[0].Activa ? empresas[0].Id : null;
            }
            else
            {
                legacyId = await db.PublicStoreBindings.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Host == httpContext.Request.Host.Host &&
                                x.StoreKey == "" && x.Activa)
                    .Select(x => (int?)x.EmpresaId)
                    .SingleOrDefaultAsync(httpContext.RequestAborted);
            }
            if (legacyId is not > 0 ||
                !await db.Set<Empresa>().IgnoreQueryFilters().AsNoTracking()
                    .AnyAsync(e => e.Id == legacyId && e.Activa,
                        httpContext.RequestAborted))
            {
                await RechazarAsync(httpContext);
                return;
            }
            httpContext.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] =
                legacyId.Value;
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
        // Sin DI válida no se autoriza ningún piloto empresarial. El guard
        // explícito evita CS8604 bajo -warnaserror y permanece fail-closed.
        if (http.User.Identity?.IsAuthenticated != true || services is null)
            return false;
        var config = services.GetService<IConfiguration>();
        if (config is null)
            return false;

        var lectura = HttpMethods.IsGet(http.Request.Method) &&
                      config.GetValue<bool>("MultiEmpresa:PrivateReadPilotEnabled");
        var alta = HttpMethods.IsPost(http.Request.Method) &&
                   config.GetValue<bool>("MultiEmpresa:PrivateWritePilotEnabled");
        // Edición de maestros: opt-in separado, nunca habilita otros PUT.
        var editarMaestros = HttpMethods.IsPut(http.Request.Method) &&
                           config.GetValue<bool>("MultiEmpresa:PrivateMasterEditPilotEnabled");
        // No activa mutaciones de inventario ni confirmaciones fiscales.
        // El piloto adicional sólo admite altas de producto SIN archivos
        // y borradores de venta/compra; nunca altera el modo por defecto.
        var borrador = HttpMethods.IsPost(http.Request.Method) &&
                       config.GetValue<bool>("MultiEmpresa:PrivateDraftPilotEnabled");
        var confirmar = HttpMethods.IsPost(http.Request.Method) &&
                        config.GetValue<bool>("MultiEmpresa:PrivateConfirmPilotEnabled");
        // Pago y anulación requieren autoridad específica y body JSON acotado.
        var finanzas = HttpMethods.IsPost(http.Request.Method) &&
                       config.GetValue<bool>("MultiEmpresa:PrivateFinancialPilotEnabled");
        // Productos/compra: archivos y metadatos en rutas exactas con
        // verificación doble del dueño + storage tenant (nunca global).
        var archivos = (HttpMethods.IsGet(http.Request.Method) ||
                        HttpMethods.IsPost(http.Request.Method) ||
                        HttpMethods.IsPut(http.Request.Method) ||
                        HttpMethods.IsDelete(http.Request.Method)) &&
                       config.GetValue<bool>("MultiEmpresa:PrivateCommercialFilesPilotEnabled");
        var pdf = HttpMethods.IsGet(http.Request.Method) &&
                  config.GetValue<bool>("MultiEmpresa:PrivatePdfPilotEnabled");
        var reportes = HttpMethods.IsGet(http.Request.Method) &&
                       config.GetValue<bool>("MultiEmpresa:PrivateReportPilotEnabled");
        var inventario = HttpMethods.IsGet(http.Request.Method) &&
                         config.GetValue<bool>("MultiEmpresa:PrivateInventarioReadPilotEnabled");
        // Exportación binaria de imágenes, nunca subidas ni escritura Cloudinary.
        var media = HttpMethods.IsGet(http.Request.Method) &&
                    config.GetValue<bool>("MultiEmpresa:PrivateMediaExportPilotEnabled");
        // Reportes administrativos aislados: no expone listas de otros
        // tenants ni permite exportaciones sin el RBAC específico.
        var adminReport = HttpMethods.IsGet(http.Request.Method) &&
                          config.GetValue<bool>("MultiEmpresa:PrivateAdminReportPilotEnabled");
        if (!lectura && !alta && !editarMaestros && !borrador && !confirmar &&
            !pdf && !reportes && !inventario && !media && !adminReport &&
            !finanzas && !archivos)
            return false;

        ModuloSistema modulo;
        AccionPermiso accion;
        var esBorrador = false;
        var esEdicionMaestros = false;
        var esConfirmacion = false;
        var esPdf = false;
        var esMedia = false;
        var esAdminExport = false;
        var accionFinanciera = (AccionPermiso?)null;
        var accionArchivos = (AccionPermiso?)null;
        var esCargaArchivos = false;
        var esCargaComprobante = false;
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
        else if (pdf && EsRutaAccionPositiva(http.Request.Path, "/facturas", "pdf"))
        {
            modulo = ModuloSistema.Facturacion;
            esPdf = true;
        }
        else if (media &&
                (EsRutaAccionPositiva(http.Request.Path, "/productos", "imagenes/descargar-todas") ||
                 EsRutaDescargaImagenPositiva(http.Request.Path)))
        {
            modulo = ModuloSistema.Productos;
            esMedia = true;
        }
        else if (reportes && (EsRutaExacta(http.Request.Path, "/ventas/reportes/resumen") ||
                             EsRutaExacta(http.Request.Path, "/ventas/reportes/detalle")))
            modulo = ModuloSistema.Ventas;
        else if (reportes && EsRutaExacta(http.Request.Path, "/compras/reportes/detalle"))
            modulo = ModuloSistema.Compras;
        else if (adminReport && EsRutaExportacionAdministrativa(http.Request.Path))
        {
            modulo = ModuloSistema.ReportesAdministrativos;
            esAdminExport = true;
        }
        else if (adminReport && (EsRutaExacta(http.Request.Path, "/reportes-administrativos/resumen") ||
                                 EsRutaExacta(http.Request.Path, "/reportes-administrativos/usuarios-accesos") ||
                                 EsRutaExacta(http.Request.Path, "/reportes-administrativos/roles-permisos") ||
                                 EsRutaExacta(http.Request.Path, "/reportes-administrativos/auditoria-resumen")))
            modulo = ModuloSistema.ReportesAdministrativos;
        else if (inventario && (EsRutaExacta(http.Request.Path, "/existencias-variante") ||
                                 EsRutaDetallePositiva(http.Request.Path, "/existencias-variante")))
            modulo = ModuloSistema.Inventario;
        else if (inventario && (EsRutaExacta(http.Request.Path, "/inventario/movimientos") ||
                                 EsRutaExacta(http.Request.Path, "/inventario/movimientos/paged")))
            modulo = ModuloSistema.MovimientosInventario;
        else if (alta && EsRutaExacta(http.Request.Path, "/tipo-clientes"))
            modulo = ModuloSistema.TiposClientes;
        else if (alta && EsRutaExacta(http.Request.Path, "/clientes"))
            modulo = ModuloSistema.Clientes;
        else if (alta && EsRutaExacta(http.Request.Path, "/proveedores"))
            modulo = ModuloSistema.Proveedores;
        else if (editarMaestros && EsRutaDetallePositiva(http.Request.Path, "/clientes"))
        {
            modulo = ModuloSistema.Clientes;
            esEdicionMaestros = true;
        }
        else if (editarMaestros && EsRutaDetallePositiva(http.Request.Path, "/proveedores"))
        {
            modulo = ModuloSistema.Proveedores;
            esEdicionMaestros = true;
        }
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
        else if (finanzas && EsRutaAccionPositiva(http.Request.Path, "/ventas", "anular"))
        {
            modulo = ModuloSistema.Ventas;
            accionFinanciera = AccionPermiso.Anular;
        }
        else if (finanzas && EsRutaAccionPositiva(http.Request.Path, "/compras", "anular"))
        {
            modulo = ModuloSistema.Compras;
            accionFinanciera = AccionPermiso.Anular;
        }
        else if (finanzas && EsRutaAccionPositiva(http.Request.Path, "/facturas", "pagos"))
        {
            modulo = ModuloSistema.Facturacion;
            accionFinanciera = AccionPermiso.Aplicar;
        }
        else if (finanzas && EsRutaAnularPagoPositiva(http.Request.Path))
        {
            modulo = ModuloSistema.Facturacion;
            accionFinanciera = AccionPermiso.Anular;
        }
        else if (archivos && HttpMethods.IsGet(http.Request.Method) &&
                 EsRutaAccionPositiva(http.Request.Path, "/compras", "documentos"))
        {
            modulo = ModuloSistema.Compras;
            accionArchivos = AccionPermiso.Ver;
        }
        else if (archivos && HttpMethods.IsGet(http.Request.Method) &&
                 EsRutaCompraDocumentoConId(http.Request.Path, descargar: true))
        {
            modulo = ModuloSistema.Compras;
            accionArchivos = AccionPermiso.Exportar;
        }
        else if (archivos && HttpMethods.IsPost(http.Request.Method) &&
                 EsRutaAccionPositiva(http.Request.Path, "/compras", "documentos"))
        {
            modulo = ModuloSistema.Compras;
            accionArchivos = AccionPermiso.Editar;
            esCargaArchivos = true;
            esCargaComprobante = true;
        }
        else if (archivos && HttpMethods.IsDelete(http.Request.Method) &&
                 EsRutaCompraDocumentoConId(http.Request.Path, descargar: false))
        {
            modulo = ModuloSistema.Compras;
            accionArchivos = AccionPermiso.Editar;
        }
        else if (archivos && HttpMethods.IsPut(http.Request.Method) &&
                 EsRutaDetallePositiva(http.Request.Path, "/productos"))
        {
            modulo = ModuloSistema.Productos;
            accionArchivos = AccionPermiso.Editar;
            esCargaArchivos = true;
        }
        else
            return false;

        accion = accionArchivos ?? accionFinanciera ??
            (esEdicionMaestros ? AccionPermiso.Editar
            : esPdf || esMedia || esAdminExport ? AccionPermiso.Exportar
            : lectura || reportes || inventario || adminReport ? AccionPermiso.Ver
            : esConfirmacion ? AccionPermiso.Confirmar : AccionPermiso.Crear);
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

        if (esCargaArchivos)
        {
            // El servicio real recodifica imágenes y valida MIME/magic bytes;
            // limitar antes de deserializar reduce abuso y rechaza otros forms.
            var maxContentBytes = esCargaComprobante ? 12L * 1024 * 1024
                                                     : 15L * 1024 * 1024;
            var length = http.Request.ContentLength;
            if (length is null || length <= 0 || length > maxContentBytes ||
                !http.Request.HasFormContentType)
                return false;
            try
            {
                var form = await http.Request.ReadFormAsync(http.RequestAborted);
                var files = form.Files;
                if (esCargaComprobante ? files.Count != 1 : files.Count > 5)
                    return false;
                if (files.Any(file => file.Length <= 0 || file.Length > 10L * 1024 * 1024))
                    return false;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        if (accionFinanciera.HasValue)
        {
            // Todas las mutaciones contables requieren JSON sin archivos,
            // excepto confirmaciones que permanecen en otro piloto.
            const int maxJsonBytes = 32 * 1024;
            var length = http.Request.ContentLength;
            if (length is null || length <= 0 || length > maxJsonBytes ||
                http.Request.ContentType?.StartsWith(
                    "application/json", StringComparison.OrdinalIgnoreCase) != true)
                return false;
        }

        if (esEdicionMaestros)
        {
            // Solo JSON pequeño: ID propio, RBAC Editar y filtro EF físico.
            const int maxJsonBytes = 32 * 1024;
            var length = http.Request.ContentLength;
            if (length is null || length <= 0 || length > maxJsonBytes ||
                http.Request.ContentType?.StartsWith(
                    "application/json", StringComparison.OrdinalIgnoreCase) != true)
                return false;
        }

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

    private static bool EsRutaGestionBasicaPlataforma(HttpRequest request)
    {
        var s = request.Path.Value?.Split('/', StringSplitOptions.None);
        if (s is null || s.Length < 4 || s[0] != "" ||
            !s[1].Equals("plataforma", StringComparison.OrdinalIgnoreCase) ||
            !s[2].Equals("gestion", StringComparison.OrdinalIgnoreCase))
            return false;
        static bool Igual(string a, string b) =>
            a.Equals(b, StringComparison.OrdinalIgnoreCase);
        static bool IdPositivo(string v) =>
            v.Length > 0 && v.All(c => c is >= '0' and <= '9') &&
            int.TryParse(v, out var id) && id > 0;

        if (s.Length == 4)
        {
            if (HttpMethods.IsGet(request.Method) && Igual(s[3], "roles")) return true;
            if (HttpMethods.IsPost(request.Method) && Igual(s[3], "usuarios")) return true;
        }
        if (s.Length == 5 && HttpMethods.IsGet(request.Method) &&
            Igual(s[3], "usuarios") && Igual(s[4], "buscar"))
            return true;

        if (s.Length >= 6 && Igual(s[3], "empresas") && IdPositivo(s[4]))
        {
            if (s.Length == 6)
            {
                if (Igual(s[5], "tiendas") && HttpMethods.IsGet(request.Method))
                    return true;
                if (Igual(s[5], "membresias") &&
                    (HttpMethods.IsGet(request.Method) || HttpMethods.IsPost(request.Method)))
                    return true;
                if (HttpMethods.IsPut(request.Method) &&
                    (Igual(s[5], "nombre") || Igual(s[5], "estado")))
                    return true;
            }
            if (s.Length == 7 && HttpMethods.IsPut(request.Method) &&
                Igual(s[5], "tiendas") && s[6].Length >= 2 && s[6].Length <= 48 &&
                s[6][0] is >= 'a' and <= 'z' &&
                s[6][^1] is >= 'a' and <= 'z' or >= '0' and <= '9' &&
                s[6].All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
                return true;
            if (s.Length == 8 && HttpMethods.IsPut(request.Method) &&
                Igual(s[5], "membresias") && IdPositivo(s[6]) &&
                (Igual(s[7], "rol") || Igual(s[7], "estado")))
                return true;
        }
        return false;
    }

    private static bool EsRutaPermisosEmpresaConsulta(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
            return false;
        var partes = request.Path.Value?.Split('/', StringSplitOptions.None);
        if (partes is not { Length: 5 } || partes[0] != "" ||
            !partes[1].Equals("permisos", StringComparison.OrdinalIgnoreCase) ||
            !partes[2].Equals("mis-permisos", StringComparison.OrdinalIgnoreCase) ||
            !partes[3].Equals("empresa", StringComparison.OrdinalIgnoreCase))
            return false;
        return partes[4].Length > 0 &&
               partes[4].All(c => c is >= '0' and <= '9') &&
               int.TryParse(partes[4], out var id) && id > 0;
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
    // La imagen individual lleva DOS IDs positivos; nunca autorizar por
    // StartsWith("/productos") ni aceptar otros sufijos (SSRF/export accidental).
    private static bool EsRutaExportacionAdministrativa(PathString path)
    {
        // Whitelist de los únicos tipos admitidos por el generador existente.
        // Nunca admitir un segmento arbitrario ni sufijos o subrutas.
        var valor = path.Value;
        return valor is not null && new[]
        {
            "/reportes-administrativos/exportar/usuarios",
            "/reportes-administrativos/exportar/usuarios-accesos",
            "/reportes-administrativos/exportar/roles",
            "/reportes-administrativos/exportar/roles-permisos",
            "/reportes-administrativos/exportar/auditoria"
        }.Any(permitida => string.Equals(valor, permitida, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EsRutaDescargaImagenPositiva(PathString path)
    {
        var segmentos = path.Value?.Split('/', StringSplitOptions.None);
        if (segmentos is not { Length: 6 } ||
            segmentos[0].Length != 0 ||
            !segmentos[1].Equals("productos", StringComparison.OrdinalIgnoreCase) ||
            !segmentos[3].Equals("imagenes", StringComparison.OrdinalIgnoreCase) ||
            !segmentos[5].Equals("descargar", StringComparison.OrdinalIgnoreCase))
            return false;

        static bool Positivo(string valor) =>
            valor.Length > 0 &&
            valor.All(x => x is >= '0' and <= '9') &&
            int.TryParse(valor, out var n) && n > 0;

        return Positivo(segmentos[2]) && Positivo(segmentos[4]);
    }

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

    private static bool EsRutaCompraDocumentoConId(PathString path, bool descargar)
    {
        var ruta = path.Value;
        if (ruta is null) return false;
        var partes = ruta.Split('/', StringSplitOptions.None);
        if (partes.Length != (descargar ? 6 : 5) ||
            partes[0].Length != 0 ||
            !string.Equals(partes[1], "compras", StringComparison.OrdinalIgnoreCase) ||
            !NumeroPositivoASCII(partes[2]) ||
            !string.Equals(partes[3], "documentos", StringComparison.OrdinalIgnoreCase) ||
            !NumeroPositivoASCII(partes[4]))
            return false;

        return !descargar ||
               string.Equals(partes[5], "descargar", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsRutaAnularPagoPositiva(PathString path)
    {
        // /facturas/{id positivo}/pagos/{pagoId positivo}/anular: nunca
        // aceptar IDs parciales, rutas alias o sufijos.
        var ruta = path.Value;
        if (ruta is null) return false;
        var partes = ruta.Split('/', StringSplitOptions.None);
        return partes.Length == 6 &&
               partes[0].Length == 0 &&
               string.Equals(partes[1], "facturas", StringComparison.OrdinalIgnoreCase) &&
               NumeroPositivoASCII(partes[2]) &&
               string.Equals(partes[3], "pagos", StringComparison.OrdinalIgnoreCase) &&
               NumeroPositivoASCII(partes[4]) &&
               string.Equals(partes[5], "anular", StringComparison.OrdinalIgnoreCase);
    }

    private static bool NumeroPositivoASCII(string segmento) =>
        segmento.Length > 0 &&
        segmento.All(caracter => caracter >= '0' && caracter <= '9') &&
        int.TryParse(segmento, out var id) && id > 0;

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
