using Microsoft.AspNetCore.Authentication;
using MySql.EntityFrameworkCore.Extensions;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Solqaryn.API.Configuration;
using Solqaryn.API.Security;
using Solqaryn.Application.AutomationV5;
using Solqaryn.API.Middleware;
using Solqaryn.API.Observability;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Interfaces.Services;
using Solqaryn.Application.Services;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT");
var isRender = string.Equals(Environment.GetEnvironmentVariable("RENDER"), "true", StringComparison.OrdinalIgnoreCase);
RenderEnvironmentContractSnapshot? renderEnvironmentContract = null;
if (isRender) renderEnvironmentContract = RenderEnvironmentContractGuard.ValidateProcessEnvironment(builder.Environment.EnvironmentName);
if (!string.IsNullOrWhiteSpace(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Solqaryn.API.Filters.MedirRendimientoBusquedaFilter>();
    options.Filters.Add<Solqaryn.API.Filters.FluentValidationActionFilter>();
});
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 30 * 1024 * 1024);
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductoValidator>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurado.");
if (isRender) EnvironmentDatabaseGuard.ValidateRenderBinding(builder.Environment.EnvironmentName, connectionString);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
// C01-04.M03.06: solo DEV, sink de auditoria tipado; no registrar fuente sin autenticacion real.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<
        Solqaryn.Application.AutomationV5.IV5UniversalCaseFileQaAuditSink,
        Solqaryn.Application.AutomationV5.V5UniversalCaseFileQaStructuredLoggerSink>();
}

// Operación única DEV: OFF por defecto; nunca se registra en QA/PROD o sin Render.
if (isRender && builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Cloudinary:NormalizeDevLegacyImagesOnStartup"))
{
    builder.Services.AddHostedService<DevCloudinaryLegacyNormalizationHostedService>();
}
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/problem+json"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.AddSingleton<IPublicStoreCache, PublicStoreMemoryCache>();
builder.Services.AddScoped<IPublicStoreTenantKeyProvider, PublicStoreTenantKeyProvider>();
// Registro de hosts de tiendas: configuración servidor, nunca headers arbitrarios.
builder.Services.AddSingleton<IPublicStoreHostTenantResolver, ConfiguredPublicStoreHostTenantResolver>();
// A shared MySQL unique key (not instance memory) prevents HMAC replay.
builder.Services.AddScoped<IPublicStoreProofReplayStore, MySqlPublicStoreProofReplayStore>();
builder.Services.AddSingleton<DbQueryTimingInterceptor>();
builder.Services.AddSingleton<SupportReadOnlyCommandInterceptor>();
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options
        .UseMySQL(connectionString, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
        .AddInterceptors(
            serviceProvider.GetRequiredService<DbQueryTimingInterceptor>(),
            serviceProvider.GetRequiredService<SupportReadOnlyCommandInterceptor>()));
// C01-01.M03.05: integra servicios de identidad sin exponer endpoints nuevos.
builder.Services.AddIdentidadUniversalIntegration();
builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection("Observability"));
builder.Services.AddSingleton<RequestObservability>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
// C07-01.M03.03: JWT y UsuarioEmpresa/RolPermiso actuales, DEV y OFF por defecto.
// Fuente solo lectura sin endpoint, concesion o mutacion.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IC0701VerifiedCatalogActorReader,
        C0701CatalogVerifiedActorSource>();
// C04-01.M03.03: JWT + tenant + RolPermiso vigentes; read-only y OFF sin opt-in.
builder.Services.AddScoped<IC0401VerifiedPlanActorSource,
    C0401PlanVerifiedActorSource>();
}
// C02-03.M03.03: genuine JWT + platform RolPermiso/Empresa read-only bridge,
// DEV-only and OFF by default. No controller, lifecycle mutation or deployment.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IC0203VerifiedOperatorEvidenceReader,
        C0203VerifiedPlatformOperatorEvidenceReader>();
    builder.Services.AddScoped<C0203TenantLifecycleAuthorizationGate>();
}
// C11-03.M03.03: real tenant/JWT/RolPermiso preflight is DEV-only and OFF by default.
// No outbox write, producer, relay, provider or production registration.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IC1103OutboxCurrentMembershipReader,
        C1103OutboxVerifiedMembershipReader>();
    builder.Services.AddScoped<C1103OutboxAuthorizationGate>();
}
// C04-04.M03.03: only a read-only DEV preflight, OFF unless opt-in.
// Never register a PSP, charge, refund, financial mutation or PROD endpoint.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IC0404PaymentAuthorizationEvidenceReader,
        C0404PaymentVerifiedEvidenceReader>();
    builder.Services.AddScoped<C0404PaymentAuthorizationGate>();
}
builder.Services.AddScoped<IPlataformaScopeService, PlataformaScopeService>();
// C02-04.M05.02: lectura acotada de checks GitHub para QA global; sin credenciales en frontend.
builder.Services.AddHttpClient("SolqarynPublicGitHubQa", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Solqaryn-Global-QA/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<IPlataformaDelegacionService, PlataformaDelegacionService>();
builder.Services.AddScoped<IPlataformaSoporteSessionService, PlataformaSoporteSessionService>();
builder.Services.AddScoped<IMensajeOutboxRepository, MensajeOutboxRepository>();
builder.Services.AddScoped<IMensajeOutboxService, MensajeOutboxService>();
builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoCatalogoPublicoRepository, ProductoCatalogoPublicoRepository>();
builder.Services.AddScoped<IProductoVarianteRepository, ProductoVarianteRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ISucursalRepository, SucursalRepository>();
builder.Services.AddScoped<ICentroCostoRepository, CentroCostoRepository>();
builder.Services.AddScoped<IAlmacenRepository, AlmacenRepository>();
builder.Services.AddScoped<IUbicacionAlmacenRepository, UbicacionAlmacenRepository>();
builder.Services.AddScoped<IExistenciaVarianteRepository, ExistenciaVarianteRepository>();
builder.Services.AddScoped<IConteoInventarioRepository, ConteoInventarioRepository>();
builder.Services.AddScoped<ITransferenciaInventarioRepository, TransferenciaInventarioRepository>();
builder.Services.AddScoped<ISolicitudCompraRepository, SolicitudCompraRepository>();
builder.Services.AddScoped<IOrdenCompraRepository, OrdenCompraRepository>();
builder.Services.AddScoped<IRecepcionCompraRepository, RecepcionCompraRepository>();
builder.Services.AddScoped<IFacturaProveedorRepository, FacturaProveedorRepository>();
builder.Services.AddScoped<IReservaInventarioRepository, ReservaInventarioRepository>();
builder.Services.AddScoped<IDevolucionClienteRepository, DevolucionClienteRepository>();
builder.Services.AddScoped<ICreditoClienteRepository, CreditoClienteRepository>();
builder.Services.AddScoped<INotaCreditoClienteRepository, NotaCreditoClienteRepository>();
builder.Services.AddScoped<IPreparacionPedidoVentaRepository, PreparacionPedidoVentaRepository>();
builder.Services.AddScoped<ITrazabilidadInventarioRepository, TrazabilidadInventarioRepository>();
builder.Services.AddScoped<IPoliticaCosteoInventarioRepository, PoliticaCosteoInventarioRepository>();
builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ICotizacionRepository, CotizacionRepository>();
builder.Services.AddScoped<IPedidoVentaRepository, PedidoVentaRepository>();
builder.Services.AddScoped<ITipoClienteRepository, TipoClienteRepository>();
builder.Services.AddScoped<IConsumoInsumoRepository, ConsumoInsumoRepository>();
builder.Services.AddScoped<IAjusteInventarioRepository, AjusteInventarioRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IRolPermisoRepository, RolPermisoRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<IPermisoRepository, PermisoRepository>();
builder.Services.AddScoped<IPermisoCatalogoService, PermisoCatalogoService>();
builder.Services.AddScoped<IDescuentoRepository, DescuentoRepository>();
builder.Services.AddScoped<IDescuentoService, DescuentoService>();
builder.Services.AddScoped<IPromocionPublicaService, PromocionPublicaService>();
builder.Services.AddScoped<ICatalogoPublicoService, CatalogoPublicoService>();
builder.Services.AddScoped<IWhatsAppPublicoService, WhatsAppPublicoService>();
builder.Services.AddScoped<ITiendaBootstrapService, TiendaBootstrapService>();
builder.Services.AddScoped<IImpuestoRepository, ImpuestoRepository>();
builder.Services.AddScoped<IImpuestoService, ImpuestoService>();
builder.Services.AddScoped<ICostoEnvioRepository, CostoEnvioRepository>();
builder.Services.AddScoped<ICostoEnvioService, CostoEnvioService>();
builder.Services.AddScoped<ICalculoService, CalculoService>();
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IPerfilImagenStorageService, CloudinaryPerfilImagenStorageService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IProductoVarianteService, ProductoVarianteService>();
builder.Services.AddScoped<IProductoVarianteImagenService, ProductoVarianteImagenService>();
builder.Services.AddScoped<IProductoEscanerService, ProductoEscanerService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<ISucursalService, SucursalService>();
builder.Services.AddScoped<ICentroCostoService, CentroCostoService>();
builder.Services.AddScoped<IAlmacenService, AlmacenService>();
builder.Services.AddScoped<IUbicacionAlmacenService, UbicacionAlmacenService>();
builder.Services.AddScoped<IExistenciaVarianteService, ExistenciaVarianteService>();
builder.Services.AddScoped<IInventarioPublicoService, InventarioPublicoService>();
builder.Services.AddScoped<IConteoInventarioService, ConteoInventarioService>();
builder.Services.AddScoped<IExistenciaVarianteConcurrencyService, ExistenciaVarianteConcurrencyService>();
builder.Services.AddScoped<AjusteInventarioExistenciaCutoverService>();
builder.Services.AddScoped<TransferenciaInventarioExistenciaService>();
builder.Services.AddScoped<RecepcionCompraExistenciaMaterializador>();
builder.Services.AddScoped<ITransferenciaInventarioMovimientoService, TransferenciaInventarioMovimientoService>();
builder.Services.AddScoped<ITransferenciaInventarioService, TransferenciaInventarioService>();
builder.Services.AddScoped<ISolicitudCompraService, SolicitudCompraService>();
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<RecepcionCompraKardexRegistrar>();
builder.Services.AddScoped<IRecepcionCompraService, RecepcionCompraService>();
builder.Services.AddScoped<IFacturaProveedorService, FacturaProveedorService>();
builder.Services.AddScoped<IThreeWayMatchService, ThreeWayMatchService>();
builder.Services.AddScoped<IReservaInventarioService, ReservaInventarioService>();
builder.Services.AddScoped<IDevolucionClienteService, DevolucionClienteService>();
builder.Services.AddScoped<ICreditoClienteService, CreditoClienteService>();
builder.Services.AddScoped<INotaCreditoClienteService, NotaCreditoClienteService>();
builder.Services.AddScoped<IPreparacionPedidoVentaService, PreparacionPedidoVentaService>();
builder.Services.AddScoped<ITrazabilidadInventarioService, TrazabilidadInventarioService>();
builder.Services.AddScoped<IPoliticaCosteoInventarioService, PoliticaCosteoInventarioService>();
builder.Services.AddScoped<ICatalogoProductoService, CatalogoProductoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICotizacionService, CotizacionService>();
builder.Services.AddScoped<IPedidoVentaService, PedidoVentaService>();
builder.Services.AddScoped<ITipoClienteService, TipoClienteService>();
builder.Services.AddScoped<IConsumoInsumoService, ConsumoInsumoService>();
builder.Services.AddScoped<IAjusteInventarioService, AjusteInventarioService>();
builder.Services.AddScoped<IAjusteInventarioConsultaService, AjusteInventarioConsultaService>();
builder.Services.AddScoped<ITipoClientePredeterminadoResolver, TipoClientePredeterminadoResolver>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IPermisoService, PermisoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IUsuarioAdministracionEmpresaService, UsuarioAdministracionEmpresaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
builder.Services.AddScoped<ICompraDocumentoStorageService, CloudinaryCompraDocumentoStorageService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<ICompraDocumentoRepository, CompraDocumentoRepository>();
builder.Services.AddScoped<ICompraDocumentoService, CompraDocumentoService>();
builder.Services.AddScoped<IMovimientoInventarioRepository, MovimientoInventarioRepository>();
builder.Services.AddScoped<IKardexMovimientoWriter, KardexMovimientoWriter>();
builder.Services.AddScoped<IMovimientoFinancieroRepository, MovimientoFinancieroRepository>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IMetodoPagoRepository, MetodoPagoRepository>();
builder.Services.AddScoped<IMetodoPagoService, MetodoPagoService>();
builder.Services.AddScoped<IEmpresaConfiguracionRepository, EmpresaConfiguracionRepository>();
builder.Services.AddScoped<IRevisionFinancieraRepository, RevisionFinancieraRepository>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IReporteVentasService, ReporteVentasService>();
builder.Services.AddScoped<IReporteComprasService, ReporteComprasService>();
builder.Services.AddScoped<IRentabilidadVentasService, RentabilidadVentasService>();
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddScoped<IFacturaPdfService, QuestPdfFacturaService>();
builder.Services.AddScoped<IFacturaCompartirRepository, FacturaCompartirRepository>();
builder.Services.AddScoped<IFacturaCompartirService, FacturaCompartirService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ITemaVisualRepository, TemaVisualRepository>();
builder.Services.AddScoped<ITemaVisualService, TemaVisualService>();
builder.Services.AddScoped<IEmpresaConfiguracionService, EmpresaConfiguracionService>();
builder.Services.AddScoped<IFinanzasService, FinanzasService>();
builder.Services.AddScoped<IMovimientoInventarioService, MovimientoInventarioService>();
builder.Services.AddScoped<ICargaMasivaService, CargaMasivaService>();
builder.Services.AddScoped<IReporteAdministrativoService, ReporteAdministrativoService>();
builder.Services.AddScoped<IInventarioConcurrencyService, InventarioConcurrencyService>();
builder.Services.AddScoped<IInventarioAjusteService, InventarioAjusteService>();
builder.Services.AddScoped<ICajaRepository, CajaRepository>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<ICuentaBancariaRepository, CuentaBancariaRepository>();
builder.Services.AddScoped<ICuentaBancariaService, CuentaBancariaService>();
builder.Services.AddScoped<ICuentaContableRepository, CuentaContableRepository>();
builder.Services.AddScoped<ICuentaContableService, CuentaContableService>();
builder.Services.AddScoped<IPeriodoContableRepository, PeriodoContableRepository>();
builder.Services.AddScoped<IPeriodoContableService, PeriodoContableService>();
builder.Services.AddScoped<IEstadoFinancieroService, EstadoFinancieroService>();
builder.Services.AddScoped<IConciliacionBancariaRepository, ConciliacionBancariaRepository>();
builder.Services.AddScoped<IOperacionBancariaService, Solqaryn.Application.Bancos.OperacionBancariaService>();
builder.Services.AddScoped<IConciliacionBancariaService, ConciliacionBancariaService>();
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret no configurado.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (jwtSecret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) || Encoding.UTF8.GetByteCount(jwtSecret) < 32) throw new InvalidOperationException("Jwt:Secret debe ser un secreto real de al menos 32 bytes.");
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience)) throw new InvalidOperationException("Jwt:Issuer y Jwt:Audience son obligatorios.");
builder.Services.AddAuthentication(options => { options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; }).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = jwtIssuer, ValidAudience = jwtAudience, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ClockSkew = TimeSpan.Zero };
    options.Events = new JwtBearerEvents { OnAuthenticationFailed = async context => { if (context.Exception is SecurityTokenExpiredException) { var auditoria = context.HttpContext.RequestServices.GetService<IAuditoriaService>(); if (auditoria is not null) await auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.ConsultarHistorial, "Intento de uso de token expirado.", entidad: "Sesion", resultado: "Rechazado", error: "Token expirado"); } } };
});
builder.Services.AddAuthorization();
var loginRateLimitPerMinute = Math.Clamp(builder.Configuration.GetValue<int?>("Security:LoginRateLimitPerMinute") ?? 60, 5, 300);
builder.Services.AddRateLimiter(options => { options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; options.OnRejected = async (context, cancellationToken) => { context.HttpContext.Response.ContentType = "application/json"; await context.HttpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Demasiados intentos de acceso. Espera un minuto e intenta nuevamente."), cancellationToken); }; options.AddPolicy("AuthLogin", httpContext => RateLimitPartition.GetFixedWindowLimiter(partitionKey: TrustedClientIpResolver.Resolve(httpContext, isRender), factory: _ => new FixedWindowRateLimiterOptions { AutoReplenishment = true, PermitLimit = loginRateLimitPerMinute, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) })); });
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (corsOrigins.Length == 0 || corsOrigins.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException("Cors:AllowedOrigins debe contener al menos un origen válido.");
builder.Services.AddCors(options => options.AddPolicy("FrontendPolicy", policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Solqaryn API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa: Bearer {tu token}"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
var app = builder.Build();
if (renderEnvironmentContract is not null)
{
    app.Logger.LogInformation(
        "RENDER_ENV_CONTRACT=PASS environment={Environment} managed_keys={ManagedKeyCount} keyset_fingerprint={KeySetFingerprint} shared_fingerprint={SharedConfigurationFingerprint}",
        renderEnvironmentContract.Environment,
        renderEnvironmentContract.ManagedKeyCount,
        renderEnvironmentContract.KeySetFingerprint,
        renderEnvironmentContract.SharedConfigurationFingerprint);
}
if (isRender)
{
    app.Use(async (context, next) =>
    {
        context.Request.Scheme = Uri.UriSchemeHttps;
        await next();
    });
}
else
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, ForwardLimit = 1 };
    app.UseForwardedHeaders(forwardedHeadersOptions);
}
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseResponseCompression();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (!context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
            context.Response.Headers[HeaderNames.CacheControl] = "private, no-store, max-age=0";

        return Task.CompletedTask;
    });

    await next();
});
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRouting();
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.Use(async (context, next) => { context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff"); context.Response.Headers.TryAdd("X-Frame-Options", "DENY"); context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer"); context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()"); await next(); });
var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"); if (swaggerEnabled) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!isRender) app.UseHttpsRedirection();
app.UseCors("FrontendPolicy"); app.UseRateLimiter();
var maintenanceEnabled = app.Configuration.GetValue<bool>("Maintenance:Enabled");
if (maintenanceEnabled)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next();
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers["Retry-After"] = "300";
        await context.Response.WriteAsJsonAsync(new
        {
            status = 503,
            title = "Servicio temporalmente en mantenimiento",
            detail = "Solqaryn se encuentra temporalmente en mantenimiento programado."
        });
    });
}
app.UseAuthentication(); app.UseAuthorization();
// P0: no servir datos comerciales multiempresa sin ownership por fila.
// Este middleware bloquea nuevas empresas y la mezcla de datos legacy de 2+ tenants.
app.UseMiddleware<UnscopedCommercialTenantGuardMiddleware>();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Solqaryn API" })).ExcludeFromDescription();
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken cancellationToken) => { var databaseReady = false; try { databaseReady = await db.Database.CanConnectAsync(cancellationToken); } catch { databaseReady = false; } return databaseReady ? Results.Ok(new { status = "ready", database = "connected" }) : Results.Json(new { status = "not_ready", database = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable); }).ExcludeFromDescription();
app.MapControllers();
// DEV-only, opt-in and read-only; no business mutation and no payment provider.
// This probe does not bypass the existing multiempresa quarantine middleware.
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("PaymentPlanning:EnableReadOnlyAuthorizationProbe"))
{
    app.MapGet("/__dev/payment-planning/{tenantId:int}/{invoiceId:int}",
        async (int tenantId, int invoiceId,
            C0404PaymentAuthorizationGate gate, CancellationToken cancellationToken) =>
        {
            var decision = await gate.InspectAsync(
                new C0404PaymentAuthorizationGate.Request(
                    tenantId, invoiceId,
                    C0404PaymentAuthorizationGate.Operation.CreateLink),
                cancellationToken);
            return decision.MayInspectPureApplicationPlan
                ? Results.Ok(new { code = decision.Code, execute = false })
                : Results.StatusCode(StatusCodes.Status403Forbidden);
        })
        .RequireAuthorization()
        .ExcludeFromDescription();
}
// C11-03.M03.03: opt-in DEV-only read-only authority probe, never outbox persistence.
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("OutboxPlanning:EnableReadOnlyAuthorizationProbe"))
{
    app.MapGet("/__dev/outbox-preflight/{tenantId:int}",
        async (HttpContext http, int tenantId,
            C1103OutboxAuthorizationGate gate, CancellationToken cancellationToken) =>
        {
            var bearer = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (!bearer.Succeeded || bearer.Principal?.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();
            var signedIds = bearer.Principal.FindAll("id").ToArray();
            if (signedIds.Length != 1 ||
                !int.TryParse(signedIds[0].Value, out var actor) || actor <= 0)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            // The Application gate consumes only the identity normalized AFTER
            // signature and lifetime verification by the actual Bearer handler.
            var principal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier,
                        signedIds[0].Value) }, JwtBearerDefaults.AuthenticationScheme));
            var draft = new Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules.EventDraft(
                tenantId,
                Guid.Parse("11111111-2222-4333-8444-555555555555"),
                "c1103-dev-readonly-preflight-123456789",
                "ventas.factura.emitida",
                new string('a', 64),
                DateTime.UtcNow.AddMinutes(-1),
                "c1103-v22", 1, 0, 3);
            var verdict = await gate.InspectAsync(principal, draft, cancellationToken);
            return verdict.PermitsDomainPreflightOnly
                ? Results.Ok(new { code = verdict.Code, mayPersist = false, mayDispatch = false })
                : Results.StatusCode(StatusCodes.Status403Forbidden);
        })
        .RequireAuthorization()
        .ExcludeFromDescription();
}

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var repairService = new ProductionDataRepairService(db);
    var repairResult = await repairService.RepairAsync();
    app.Logger.LogInformation(
        "ProductionDataRepair EmpresasTotales={EmpresasTotales} EmpresaConfiguracionesActivas={EmpresaConfiguracionesActivas} EmpresaMaterializadaId={EmpresaMaterializadaId} MembresiasLegacyCreadas={MembresiasLegacyCreadas}",
        repairResult.EmpresasTotales,
        repairResult.EmpresaConfiguracionesActivas,
        repairResult.EmpresaMaterializadaId,
        repairResult.MembresiasLegacyCreadas);
    var seedPermisoService = new SeedPermisoService(db); await seedPermisoService.SeedDefaultsAsync();
    var adminUsername = app.Configuration["SeedAdmin:Username"]?.Trim(); var adminPassword = app.Configuration["SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminUsername) && !string.IsNullOrWhiteSpace(adminPassword) && !await db.Usuarios.AnyAsync(u => u.NombreUsuario == adminUsername))
    {
        var adminRolId = await db.Roles.Where(r => r.EsAdministrador && r.Activo && !r.Eliminado).OrderBy(r => r.Id).Select(r => r.Id).FirstOrDefaultAsync();
        if (adminRolId <= 0) throw new InvalidOperationException("No existe un rol administrador dinámico activo para SeedAdmin.");
        db.Usuarios.Add(new Usuario { NombreUsuario = adminUsername, NombreCompleto = "Administrador", PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword), RolId = adminRolId, Activo = true, FechaCreacion = DateTime.UtcNow }); await db.SaveChangesAsync();
    }
    var seedFiscalService = new SeedFiscalService(db); await seedFiscalService.SeedDefaultsAsync();
}
await app.RunAsync();

public static class TrustedClientIpResolver
{
    public static string Resolve(HttpContext context, bool isRender)
    {
        if (isRender &&
            context.Request.Headers.TryGetValue("CF-Connecting-IP", out var values) &&
            values.Count == 1)
        {
            var raw = values[0]?.Trim();
            if (!string.IsNullOrWhiteSpace(raw) && IPAddress.TryParse(raw, out var parsed))
                return parsed.ToString();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconocida";
    }
}
