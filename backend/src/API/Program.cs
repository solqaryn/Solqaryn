using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using InventoryApp.API.Middleware;
using InventoryApp.API.Observability;
using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Interfaces.Services;
using InventoryApp.Application.Services;
using InventoryApp.Application.Validators;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT");
var isRender = string.Equals(Environment.GetEnvironmentVariable("RENDER"), "true", StringComparison.OrdinalIgnoreCase);
if (!string.IsNullOrWhiteSpace(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.Services.AddControllers(options => options.Filters.Add<InventoryApp.API.Filters.MedirRendimientoBusquedaFilter>());
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 30 * 1024 * 1024);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductoValidator>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurado.");
var mysqlServerVersion = Version.Parse(builder.Configuration["Database:ServerVersion"] ?? "8.4.3");
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, new MySqlServerVersion(mysqlServerVersion)));
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection("Observability"));
builder.Services.AddSingleton<RequestObservability>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
builder.Services.AddScoped<IMensajeOutboxRepository, MensajeOutboxRepository>();
builder.Services.AddScoped<IMensajeOutboxService, MensajeOutboxService>();
builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
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
builder.Services.AddScoped<IOperacionBancariaService, InventoryApp.Application.Bancos.OperacionBancariaService>();
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
builder.Services.AddSwaggerGen(options => { options.SwaggerDoc("v1", new OpenApiInfo { Title = "InventoryApp API", Version = "v1" }); options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Ingresa: Bearer {tu token}" }); options.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } }); });
var app = builder.Build();
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
app.UseMiddleware<CorrelationIdMiddleware>();
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
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "InventoryApp API" })).ExcludeFromDescription();
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken cancellationToken) => { var databaseReady = false; try { databaseReady = await db.Database.CanConnectAsync(cancellationToken); } catch { databaseReady = false; } return databaseReady ? Results.Ok(new { status = "ready", database = "connected" }) : Results.Json(new { status = "not_ready", database = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable); }).ExcludeFromDescription();
app.MapControllers();
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
