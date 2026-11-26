using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Middleware;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class C0104CaseFileEnterpriseHttpMySqlTests
{
    private sealed class FixtureAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    [Fact]
    public async Task SignedHttpSessionUsesActualWarehousesAndIndependentLiveGrants()
    {
        var origin = new MySqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException("Ephemeral MySQL required."));
        if (origin.Server is not ("127.0.0.1" or "localhost") || origin.Port != 3306)
            throw new InvalidOperationException("Only isolated localhost MySQL allowed.");
        origin.Database = "v44_casefile_http_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(origin.ConnectionString, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        await using var setup = new AppDbContext(options);
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
            var a = new Empresa("Expediente QA A");
            var b = new Empresa("Expediente QA B");
            setup.Set<Empresa>().AddRange(a, b);
            await setup.SaveChangesAsync();
            var branchA = new Sucursal { EmpresaId = a.Id, Codigo = "EXP_A", Nombre = "QA A" };
            var branchB = new Sucursal { EmpresaId = b.Id, Codigo = "EXP_B", Nombre = "QA B" };
            var role = new Rol { Nombre = "QA Casefile HTTP", NombreNormalizado = "QA_CASEFILE_HTTP",
                EsSistema = true, Activo = true };
            setup.Set<Sucursal>().AddRange(branchA, branchB);
            setup.Roles.Add(role);
            await setup.SaveChangesAsync();
            var user = new Usuario { NombreUsuario = "qa-casefile-http", NombreCompleto = "QA Casefile HTTP",
                PasswordHash = "fixture-no-login", RolId = role.Id, Activo = true };
            setup.Usuarios.Add(user);
            await setup.SaveChangesAsync();
            setup.UsuarioEmpresas.AddRange(new UsuarioEmpresa(user.Id, a.Id, role.Id),
                new UsuarioEmpresa(user.Id, b.Id, role.Id));
            await setup.SaveChangesAsync();

            async Task<Permiso> PermissionAsync(ModuloSistema module, AccionPermiso action)
            {
                var existing = await setup.Permisos.IgnoreQueryFilters()
                    .SingleOrDefaultAsync(p => p.Modulo == module && p.Accion == action);
                if (existing is not null) return existing;
                var p = new Permiso { Codigo = $"QA.{module}.{action}", Nombre = $"QA {module} {action}",
                    Modulo = module, Accion = action, Ambito = AmbitoAutorizacion.Empresa, Activo = true };
                setup.Permisos.Add(p);
                await setup.SaveChangesAsync();
                return p;
            }
            var branchPermission = await PermissionAsync(ModuloSistema.Sucursales, AccionPermiso.Ver);
            var warehousePermission = await PermissionAsync(ModuloSistema.Almacenes, AccionPermiso.Ver);
            var readPermission = await PermissionAsync(ModuloSistema.Auditoria, AccionPermiso.ReadAuditCorrelation);
            var writePermission = await PermissionAsync(ModuloSistema.Auditoria, AccionPermiso.WriteCaseFileVersion);
            var readerGrant = new RolPermiso { RolId = role.Id, PermisoId = readPermission.Id };
            var warehouseGrant = new RolPermiso { RolId = role.Id, PermisoId = warehousePermission.Id };
            setup.RolPermisos.AddRange(readerGrant, warehouseGrant,
                new RolPermiso { RolId = role.Id, PermisoId = branchPermission.Id });
            await setup.SaveChangesAsync();

            async Task<int> WarehouseAsync(int tenant, int branch, string code)
            {
                var h = new DefaultHttpContext();
                h.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenant;
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: new FixtureAccessor { HttpContext = h });
                var warehouse = new Almacen { SucursalId = branch, Codigo = code, Nombre = code,
                    Tipo = TipoAlmacen.Bodega, Activo = true };
                db.Set<Almacen>().Add(warehouse);
                await db.SaveChangesAsync();
                return warehouse.Id;
            }
            var warehouseA = await WarehouseAsync(a.Id, branchA.Id, "EXP_A1");
            var warehouseA2 = await WarehouseAsync(a.Id, branchA.Id, "EXP_A2");
            var warehouseB = await WarehouseAsync(b.Id, branchB.Id, "EXP_B1");

            var jwtSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            const string issuer = "casefile-http-fixture";
            const string audience = "casefile-http-fixture";
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseTestServer();
            builder.Configuration["CaseFile:EnableEnterpriseIntegration"] = "true";
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<AppDbContext>(o => o.UseMySQL(origin.ConnectionString,
                x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")));
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
            builder.Services.AddScoped<IRolPermisoRepository, RolPermisoRepository>();
            builder.Services.AddScoped<IRolRepository, RolRepository>();
            builder.Services.AddScoped<IPermisoRepository, PermisoRepository>();
            // Not used by the permission lookup. All authentication, membership,
            // grants, resource ownership and receipts use the real services/SQL.
            builder.Services.AddSingleton<IAuditoriaService>(Mock.Of<IAuditoriaService>());
            builder.Services.AddScoped<IPermisoService, PermisoService>();
            builder.Services.AddScoped<V5CaseFileAuditCorrelationAuthority>();
            builder.Services.AddScoped<V5CaseFileEnterpriseService>();
            builder.Services.AddAuthentication(TenantSessionToken.Scheme)
                .AddJwtBearer(TenantSessionToken.Scheme, o => TenantSessionBearer.Configure(o, issuer, audience, key));
            builder.Services.AddAuthorization();
            await using var app = builder.Build();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<UnscopedCommercialTenantGuardMiddleware>();
            Assert.True(V5CaseFileEnterpriseEndpoints.ShouldMap(app.Environment, app.Configuration));
            V5CaseFileEnterpriseEndpoints.Map(app);
            await app.StartAsync();
            using var client = app.GetTestClient();

            string Token(int tenant, int branch) => new JwtSecurityTokenHandler().WriteToken(
                new JwtSecurityToken(issuer, TenantSessionToken.Audience(audience),
                    new[] {
                        new Claim("id", user.Id.ToString(CultureInfo.InvariantCulture)),
                        new Claim("rolId", role.Id.ToString(CultureInfo.InvariantCulture)),
                        new Claim("tenantId", tenant.ToString(CultureInfo.InvariantCulture)),
                        new Claim("branchId", branch.ToString(CultureInfo.InvariantCulture)),
                        new Claim("tenantRoleId", role.Id.ToString(CultureInfo.InvariantCulture))
                    }, expires: DateTime.UtcNow.AddMinutes(5),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
            var tokenA = Token(a.Id, branchA.Id);
            var tokenB = Token(b.Id, branchB.Id);
            async Task<HttpResponseMessage> Post(string token, int caseId, string suffix, object body,
                int? forgedTenant = null, HttpClient? actualClient = null)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post,
                    $"/__dev/expedientes/almacenes/{caseId}/{suffix}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (forgedTenant.HasValue) req.Headers.Add("X-Empresa-Id",
                    forgedTenant.Value.ToString(CultureInfo.InvariantCulture));
                req.Content = JsonContent.Create(body);
                return await (actualClient ?? client).SendAsync(req);
            }
            async Task Expect(HttpStatusCode status, string token, int caseId, string suffix, object body,
                int? forgedTenant = null)
            {
                using var result = await Post(token, caseId, suffix, body, forgedTenant);
                Assert.Equal(status, result.StatusCode);
            }
            object Version(int resource, long? expected = null) => new { AlmacenId = resource, ExpectedVersion = expected };

            Assert.True(Solqaryn.Application.Common.CatalogoPermisosBase.RequiereGrantExplicito(AccionPermiso.WriteCaseFileVersion));
            Assert.DoesNotContain(Solqaryn.Application.Common.CatalogoPermisosBase.NuevosPermisosAdministrador,
                x => x.Accion == AccionPermiso.WriteCaseFileVersion);
            // API12 alone cannot create a case or append a version.
            await Expect(HttpStatusCode.Forbidden, tokenA, 1, "versiones", Version(warehouseA));
            var writerGrant = new RolPermiso { RolId = role.Id, PermisoId = writePermission.Id };
            setup.RolPermisos.Add(writerGrant);
            await setup.SaveChangesAsync();
            await Expect(HttpStatusCode.OK, tokenA, 1, "versiones", Version(warehouseA));
            await Expect(HttpStatusCode.OK, tokenA, 2, "versiones", Version(warehouseA2));
            await Expect(HttpStatusCode.OK, tokenB, 1, "versiones", Version(warehouseB));
            await Expect(HttpStatusCode.Forbidden, tokenA, 3, "versiones", Version(warehouseB));
            await Expect(HttpStatusCode.Forbidden, tokenA, 3, "versiones", Version(warehouseA), b.Id);
            await Expect(HttpStatusCode.Forbidden, Token(a.Id, branchB.Id), 3, "versiones", Version(warehouseA));
            await Expect(HttpStatusCode.Forbidden, tokenA, 1, "versiones", Version(warehouseA2, 1));

            using var record = await Post(tokenA, 1, "auditoria/registrar", new { });
            Assert.Equal(HttpStatusCode.OK, record.StatusCode);
            using var recorded = JsonDocument.Parse(await record.Content.ReadAsStringAsync());
            var correlation = recorded.RootElement.GetProperty("correlation").GetString();
            Assert.True(Guid.TryParseExact(correlation, "N", out _));
            await Expect(HttpStatusCode.OK, tokenA, 1, "versiones", Version(warehouseA, 1));
            await Expect(HttpStatusCode.Conflict, tokenA, 1, "versiones", Version(warehouseA, 1));
            var read = new { Correlation = correlation };
            using var response = await Post(tokenA, 1, "auditoria/correlacion", read);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var linked = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(1L, linked.RootElement.GetProperty("targetVersion").GetInt64());
            Assert.False(linked.RootElement.TryGetProperty("receiptId", out _));
            await Expect(HttpStatusCode.Forbidden, tokenA, 2, "auditoria/correlacion", read);
            await Expect(HttpStatusCode.Forbidden, tokenB, 1, "auditoria/correlacion", read);
            await Expect(HttpStatusCode.Forbidden, tokenA, 999, "auditoria/correlacion", read);
            await Expect(HttpStatusCode.Unauthorized, "not-a-jwt", 1, "auditoria/correlacion", read);

            // Exercise the compiled application's actual composition root, not a
            // second hand-built DI container. The same disposable DB and explicit
            // fixture grants are used, with a newly signed token for each host.
            async Task AssertRouteDisabledAsync(HttpClient actual)
            {
                using var denied = await Post(Token(a.Id, branchA.Id), 1, "versiones",
                    Version(warehouseA, 2), actualClient: actual);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, denied.StatusCode);
                using var problem = JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
                Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED",
                    problem.RootElement.GetProperty("code").GetString());
            }
            await WithRealProgramAsync(origin.ConnectionString, jwtSecret, issuer, audience,
                "Development", false, AssertRouteDisabledAsync);
            await WithRealProgramAsync(origin.ConnectionString, jwtSecret, issuer, audience,
                "CaseFileNegativeFixture", true, AssertRouteDisabledAsync);
            await WithRealProgramAsync(origin.ConnectionString, jwtSecret, issuer, audience,
                "Development", true, async actual =>
                {
                    var freshA = Token(a.Id, branchA.Id);
                    var freshB = Token(b.Id, branchB.Id);
                    using var written = await Post(freshA, 1, "versiones",
                        Version(warehouseA, 2), actualClient: actual);
                    Assert.Equal(HttpStatusCode.OK, written.StatusCode);
                    using var actualReceipt = await Post(freshA, 1, "auditoria/registrar",
                        new { }, actualClient: actual);
                    Assert.Equal(HttpStatusCode.OK, actualReceipt.StatusCode);
                    using var actualRecorded = JsonDocument.Parse(await actualReceipt.Content.ReadAsStringAsync());
                    var actualCorrelation = actualRecorded.RootElement.GetProperty("correlation").GetString();
                    Assert.True(Guid.TryParseExact(actualCorrelation, "N", out _));
                    var actualRead = new { Correlation = actualCorrelation };
                    using var actualResponse = await Post(freshA, 1, "auditoria/correlacion",
                        actualRead, actualClient: actual);
                    Assert.Equal(HttpStatusCode.OK, actualResponse.StatusCode);
                    Assert.True(actualResponse.Headers.CacheControl?.NoStore == true);
                    using var actualLinked = JsonDocument.Parse(await actualResponse.Content.ReadAsStringAsync());
                    Assert.Equal(3L, actualLinked.RootElement.GetProperty("targetVersion").GetInt64());
                    Assert.False(actualLinked.RootElement.TryGetProperty("receiptId", out _));
                    using var foreign = await Post(freshB, 1, "auditoria/correlacion",
                        actualRead, actualClient: actual);
                    Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
                    Assert.True(await setup.Set<RegistroAuditoria>().IgnoreQueryFilters().AsNoTracking()
                        .AnyAsync(x => x.EmpresaId == a.Id && x.CorrelationId == actualCorrelation));
                    setup.RolPermisos.Remove(writerGrant);
                    await setup.SaveChangesAsync();
                    using var deniedWrite = await Post(freshA, 1, "versiones",
                        Version(warehouseA, 3), actualClient: actual);
                    Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
                    using var stillReadable = await Post(freshA, 1, "auditoria/correlacion",
                        actualRead, actualClient: actual);
                    Assert.Equal(HttpStatusCode.OK, stillReadable.StatusCode);
                });

            // Writer was revoked through the actual Program host above.
            tokenA = Token(a.Id, branchA.Id);
            await Expect(HttpStatusCode.Forbidden, tokenA, 1, "versiones", Version(warehouseA, 3));
            await Expect(HttpStatusCode.OK, tokenA, 1, "auditoria/correlacion", read);
            setup.RolPermisos.Remove(warehouseGrant);
            await setup.SaveChangesAsync();
            await Expect(HttpStatusCode.Forbidden, tokenA, 1, "auditoria/correlacion", read);
            setup.RolPermisos.Remove(readerGrant);
            await setup.SaveChangesAsync();
            await Expect(HttpStatusCode.Forbidden, tokenA, 1, "auditoria/correlacion", read);
            Assert.True(await setup.Set<RegistroAuditoria>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(x => x.EmpresaId == a.Id && x.CorrelationId == correlation));
        }
        finally { await setup.Database.EnsureDeletedAsync(); }
    }

    private static async Task WithRealProgramAsync(string connection, string jwtSecret,
        string issuer, string audience, string environment, bool enabled, Func<HttpClient, Task> verify)
    {
        var db = new MySqlConnectionStringBuilder(connection);
        if (db.Server is not ("127.0.0.1" or "localhost") || db.Port != 3306 ||
            !db.Database.StartsWith("v44_casefile_http_", StringComparison.Ordinal))
            throw new InvalidOperationException("Only the isolated case-file fixture is permitted.");

        var output = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = output.Parent?.Name ?? throw new InvalidOperationException("Missing build configuration.");
        DirectoryInfo? backend = output;
        while (backend is not null && !File.Exists(Path.Combine(backend.FullName, "Solqaryn.sln")))
            backend = backend.Parent;
        if (backend is null) throw new InvalidOperationException("Backend solution not found.");
        var api = Path.Combine(backend.FullName, "src", "API", "bin", configuration, "net10.0", "Solqaryn.API.dll");
        Assert.True(File.Exists(api), "Compiled Program artifact missing.");

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(api)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(api);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add("http://127.0.0.1:0");
        // Do not inherit connection strings, cloud configuration, secrets or host flags.
        start.Environment.Clear();
        foreach (var name in new[] { "PATH", "HOME", "DOTNET_ROOT", "DOTNET_ROOT_X64", "DOTNET_CLI_HOME",
            "TMPDIR", "TEMP", "TMP", "LANG", "LC_ALL", "LD_LIBRARY_PATH", "SYSTEMROOT", "WINDIR", "USERPROFILE" })
            if (Environment.GetEnvironmentVariable(name) is string value)
                start.Environment[name] = value;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        start.Environment["RENDER"] = "false";
        start.Environment["ConnectionStrings__DefaultConnection"] = connection;
        start.Environment["Database__ServerVersion"] = "8.4.0";
        start.Environment["Database__ApplyMigrationsOnStartup"] = "false";
        start.Environment["Jwt__Secret"] = jwtSecret;
        start.Environment["Jwt__Issuer"] = issuer;
        start.Environment["Jwt__Audience"] = audience;
        start.Environment["Cors__AllowedOrigins__0"] = "http://127.0.0.1:4200";
        start.Environment["Maintenance__Enabled"] = "false";
        start.Environment["CaseFile__EnableEnterpriseIntegration"] = enabled ? "true" : "false";
        start.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        start.Environment["Logging__Console__FormatterName"] = "simple";
        start.Environment["Logging__Console__FormatterOptions__ColorBehavior"] = "Disabled";
        start.Environment["Cloudinary__CloudName"] = "casefile-fixture";
        start.Environment["Cloudinary__ApiKey"] = "fixture-only";
        start.Environment["Cloudinary__ApiSecret"] = "fixture-only";
        start.Environment["Smtp__Host"] = "127.0.0.1";
        start.Environment["Smtp__Port"] = "1";

        using var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        var listening = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputDataReceived += (_, e) =>
        {
            const string marker = "Now listening on: ";
            var line = e.Data?.Trim();
            if (line?.StartsWith(marker, StringComparison.Ordinal) == true &&
                Uri.TryCreate(line[marker.Length..].Trim(), UriKind.Absolute, out var uri) &&
                uri.Scheme == "http" && uri.Host == "127.0.0.1" && uri.Port > 0)
                listening.TrySetResult(uri);
        };
        process.ErrorDataReceived += (_, _) => { }; // Drain, never export raw process output.
        process.Exited += (_, _) => listening.TrySetException(new InvalidOperationException("Fixture Program exited before readiness."));
        Assert.True(process.Start());
        try
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var endpoint = await listening.Task.WaitAsync(TimeSpan.FromSeconds(60));
            using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
            {
                BaseAddress = endpoint,
                Timeout = TimeSpan.FromSeconds(20)
            };
            using var health = await client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            await verify(client);
        }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) when (process.HasExited) { }
            }
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}
