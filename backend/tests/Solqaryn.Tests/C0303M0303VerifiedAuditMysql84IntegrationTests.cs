using Solqaryn.Application.DTOs;
using Solqaryn.Application.Common;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using System.Net.Http.Headers;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Services;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;
using Rules = Solqaryn.Domain.Security.C0303SecurityAuditDomainRules;

namespace Solqaryn.Tests;

/// <summary>
/// Physical MySQL/Oracle EF proof for C03-03.M03.03 audit-read preflight.
/// JWT bearer verification is real but HTTP transport is a test context, not
/// a hosted HTTP endpoint. No durable event sink or production grant is proven.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0303M0303VerifiedAuditMysql84IntegrationTests
{
    private static readonly SymmetricSecurityKey Signing = new(
        Encoding.UTF8.GetBytes("solqaryn-c0303-m0303-audit-mysql84-test-only-2026!"));

    [Fact]
    public async Task PhysicalMysql84SignedJwtTenantAbGrantRevocationAndRollbackFailClosed()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var source = new MySqlConnectionStringBuilder(raw);
        if (source.Server is not ("127.0.0.1" or "localhost") ||
            source.Port != 3306 || source.UserID != "root" ||
            !source.Database.StartsWith("solqaryn_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only CI disposable MySQL 8.4 localhost is allowed");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "v25_c0303_m0303_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, o => o.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            try
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT VERSION()";
                var actualVersion = Convert.ToString(await command.ExecuteScalarAsync());
                Assert.StartsWith("8.4.", actualVersion, StringComparison.Ordinal);
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }

            var tenantA = new Empresa("Audit CI tenant A");
            var tenantB = new Empresa("Audit CI tenant B");
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();
            Assert.NotEqual(tenantA.Id, tenantB.Id);

            var roleA = new Rol
            {
                Nombre = "Audit CI Administrator A",
                NombreNormalizado = "AUDIT_CI_ADMIN_A",
                EmpresaId = tenantA.Id,
                Ambito = AmbitoAutorizacion.Empresa,
                EsAdministrador = true,
                Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "Audit CI Administrator B",
                NombreNormalizado = "AUDIT_CI_ADMIN_B",
                EmpresaId = tenantB.Id,
                Ambito = AmbitoAutorizacion.Empresa,
                EsAdministrador = true,
                Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();

            var username = "auditci_" + Guid.NewGuid().ToString("N")[..12];
            var actor = new Usuario
            {
                NombreUsuario = username,
                NombreCompleto = "Synthetic audit actor",
                PasswordHash = "unusable-ci-only",
                RolId = roleA.Id,
                Activo = true
            };
            var permission = new Permiso
            {
                Codigo = "C0303_MYSQL_AUDITORIA_VER",
                Nombre = "C0303 disposable audit read",
                Modulo = ModuloSistema.Auditoria,
                Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Empresa,
                Activo = true
            };
            db.Usuarios.Add(actor);
            db.Permisos.Add(permission);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(actor.Id, tenantA.Id, roleA.Id),
                new UsuarioEmpresa(actor.Id, tenantB.Id, roleB.Id));
            db.RolPermisos.Add(new RolPermiso
            {
                RolId = roleA.Id,
                PermisoId = permission.Id
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
            current.SetupGet(x => x.EstaAutenticado).Returns(true);
            current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
            current.SetupGet(x => x.NombreUsuario).Returns(username);
            var scope = new UsuarioScopeService(db, current.Object);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.MapInboundClaims = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "c0303-mysql-ci",
                        ValidateAudience = true,
                        ValidAudience = "c0303-mysql-ci",
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = Signing,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
            await using var provider = services.BuildServiceProvider();
            var jwt = new JwtSecurityToken(
                issuer: "c0303-mysql-ci", audience: "c0303-mysql-ci",
                claims: [
                    new Claim("id", actor.Id.ToString()),
                    new Claim("nombreUsuario", username),
                    new Claim("sub", username)
                ],
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(8),
                signingCredentials: new SigningCredentials(Signing, SecurityAlgorithms.HmacSha256));
            await using var trustedRequestScope = provider.CreateAsyncScope();
            var http = new DefaultHttpContext { RequestServices = trustedRequestScope.ServiceProvider };
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("id", actor.Id.ToString())], "synthetic-not-a-grant"));
            http.Request.Headers.Authorization =
                "Bearer " + new JwtSecurityTokenHandler().WriteToken(jwt);
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenantA.Id;
            http.Request.RouteValues["empresaId"] = tenantA.Id.ToString();

            var accessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
            accessor.SetupGet(x => x.HttpContext).Returns(http);
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SecurityAudit:EnableReadOnlyAuthorizationProbe"] = "true"
                }).Build();
            var reader = new C0303VerifiedAuditTenantReaders(
                accessor.Object, current.Object, scope, db, config);

            // The real MySQL membership/grant join must attest a *read-only*
            // candidate for tenant A; it must never certify a durable audit sink.
            var positive = await reader.ReadCurrentAsync(default);
            Assert.NotNull(positive);
            Assert.Equal(tenantA.Id, positive.TenantId);
            Assert.False(positive.TransactionalSinkVerified);
            Assert.NotNull(await reader.ReadCurrentAsync(
                actor.Id, tenantA.Id, Rules.Action.AuditRead, default));

            // Forged and expired bearer tokens must fail even when a caller
            // manually presents an authenticated principal and valid SQL grants.
            // This tests JwtBearer cryptography; it is not hosted HTTP proof.
            async Task AssertUntrustedBearerRejectedAsync(string bearerToken)
            {
                await using var untrustedRequestScope = provider.CreateAsyncScope();
                var attempted = new DefaultHttpContext { RequestServices = untrustedRequestScope.ServiceProvider };
                attempted.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("id", actor.Id.ToString())], "synthetic-not-a-grant"));
                attempted.Request.Headers.Authorization = "Bearer " + bearerToken;
                attempted.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
                    tenantA.Id;
                attempted.Request.RouteValues["empresaId"] = tenantA.Id.ToString();

                var forgedAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
                forgedAccessor.SetupGet(x => x.HttpContext).Returns(attempted);
                var attemptedReader = new C0303VerifiedAuditTenantReaders(
                    forgedAccessor.Object, current.Object, scope, db, config);
                Assert.False((await attempted.AuthenticateAsync(
                    JwtBearerDefaults.AuthenticationScheme)).Succeeded);
                Assert.Null(await attemptedReader.ReadCurrentAsync(default));
            }

            var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                "solqaryn-c0303-m0303-forged-jwt-ci-key-rejected-2026!"));
            var forgedToken = new JwtSecurityToken(
                issuer: "c0303-mysql-ci", audience: "c0303-mysql-ci",
                claims: [
                    new Claim("id", actor.Id.ToString()),
                    new Claim("nombreUsuario", username),
                    new Claim("sub", username)
                ],
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(8),
                signingCredentials: new SigningCredentials(
                    wrongKey, SecurityAlgorithms.HmacSha256));
            await AssertUntrustedBearerRejectedAsync(
                new JwtSecurityTokenHandler().WriteToken(forgedToken));

            var expiredToken = new JwtSecurityToken(
                issuer: "c0303-mysql-ci", audience: "c0303-mysql-ci",
                claims: [
                    new Claim("id", actor.Id.ToString()),
                    new Claim("nombreUsuario", username),
                    new Claim("sub", username)
                ],
                notBefore: DateTime.UtcNow.AddMinutes(-15),
                expires: DateTime.UtcNow.AddMinutes(-5),
                signingCredentials: new SigningCredentials(
                    Signing, SecurityAlgorithms.HmacSha256));
            await AssertUntrustedBearerRejectedAsync(
                new JwtSecurityTokenHandler().WriteToken(expiredToken));


            // Real ASP.NET Core TestServer HTTP pipeline + JwtBearer +
            // server-owned tenant marker + ephemeral Oracle EF/MySQL 8.4.
            // It maps the SAME Development opt-in route as Program.cs.
            // No business audit event producer or durable sink is installed.
            using (var host = await new HostBuilder().ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddLogging();
                    s.AddAuthorization();
                    s.AddHttpContextAccessor();
                    s.AddSingleton<IConfiguration>(config);
                    s.AddSingleton(db);
                    s.AddSingleton<ICurrentUserService>(current.Object);
                    s.AddSingleton<IUsuarioScopeService>(scope);
                    s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(o =>
                        {
                            o.MapInboundClaims = false;
                            o.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidIssuer = "c0303-mysql-ci",
                                ValidateAudience = true,
                                ValidAudience = "c0303-mysql-ci",
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = Signing,
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero
                            };
                        });
                    s.AddScoped<C0303VerifiedAuditTenantReaders>();
                    s.AddScoped<IC0303VerifiedAuditEventReader>(
                        sp => sp.GetRequiredService<C0303VerifiedAuditTenantReaders>());
                    s.AddScoped<IC0303VerifiedAuditResourceReader>(
                        sp => sp.GetRequiredService<C0303VerifiedAuditTenantReaders>());
                    s.AddScoped<C0303SecurityAuditApplicationService>();
                    s.AddScoped<C0303TransactionalAuditPersistenceAdapter>();
                    s.AddScoped<IAuditoriaService>(sp => new AuditoriaService(
                        new AuditoriaRepository(db), current.Object,
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<ILogger<AuditoriaService>>()));
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    // Exercise the SAME endpoint mapping used in Program.cs,
                    // no test middleware injecting a privileged tenant marker.
                    app.UseEndpoints(C0303AuditReadOnlyProbeEndpoint.Map);
                })).StartAsync())
            {
                using var client = host.GetTestClient();
                async Task<HttpStatusCode> HttpAuditAsync(int routeTenant, string bearerJwt)
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Get, "/__dev/audit-authorization/" + routeTenant);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerJwt);
                    using var response = await client.SendAsync(request);
                    return response.StatusCode;
                }

                async Task<HttpStatusCode> HttpAuditRecordAsync(
                    int routeTenant, string bearerJwt, int? selector = null)
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Post, "/__dev/audit-authorization/" +
                        routeTenant + "/record");
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerJwt);
                    if (selector.HasValue)
                        request.Headers.TryAddWithoutValidation(
                            "X-Empresa-Id", selector.Value.ToString());
                    using var response = await client.SendAsync(request);
                    return response.StatusCode;
                }

                var signed = new JwtSecurityTokenHandler().WriteToken(jwt);
                Assert.Equal(HttpStatusCode.NoContent,
                    await HttpAuditAsync(tenantA.Id, signed));
                // GET must remain read-only; POST is the only ledger writer.
                Assert.Empty(await db.RegistrosAuditoria.AsNoTracking()
                    .Where(x => x.Entidad == "Empresa" &&
                        x.Descripcion == "C03-03.M03.03: acceso de auditoría validado")
                    .ToListAsync());
                Assert.Equal(HttpStatusCode.NoContent,
                    await HttpAuditRecordAsync(tenantA.Id, signed));
                db.ChangeTracker.Clear();
                var recorded = await db.RegistrosAuditoria.AsNoTracking()
                    .Where(x => x.Entidad == "Empresa" &&
                        x.Descripcion == "C03-03.M03.03: acceso de auditoría validado")
                    .ToListAsync();
                var singleRecord = Assert.Single(recorded);
                Assert.Equal(actor.Id, singleRecord.UsuarioId);
                Assert.Equal(tenantA.Id, singleRecord.EmpresaId);
                Assert.Equal(tenantA.Id, singleRecord.ReferenciaId);
                Assert.Equal(ModuloSistema.Auditoria, singleRecord.Modulo);
                Assert.Equal(AccionPermiso.Ver, singleRecord.Accion);
                Assert.Equal("Exito", singleRecord.Resultado);
                Assert.Equal(HttpStatusCode.Forbidden,
                    await HttpAuditRecordAsync(tenantB.Id, signed));
                Assert.Equal(HttpStatusCode.Unauthorized,
                    await HttpAuditRecordAsync(tenantA.Id,
                        new JwtSecurityTokenHandler().WriteToken(forgedToken)));
                Assert.Equal(HttpStatusCode.Unauthorized,
                    await HttpAuditRecordAsync(tenantA.Id,
                        new JwtSecurityTokenHandler().WriteToken(expiredToken)));
                Assert.Equal(HttpStatusCode.Forbidden,
                    await HttpAuditRecordAsync(tenantA.Id, signed, tenantB.Id));
                Assert.Single(await db.RegistrosAuditoria.AsNoTracking()
                    .Where(x => x.Entidad == "Empresa" &&
                        x.Descripcion == "C03-03.M03.03: acceso de auditoría validado")
                    .ToListAsync());
                // R2 real MySQL 8.4: the shared audit repository must enforce
                // tenant-bound reads even if EF's global filter is absent.
                // B is seeded into the disposable database by the test harness,
                // not by an authenticated business operation.
                db.RegistrosAuditoria.Add(new RegistroAuditoria
                {
                    UsuarioId = actor.Id,
                    NombreUsuario = username,
                    EmpresaId = tenantB.Id,
                    Modulo = ModuloSistema.Auditoria,
                    Accion = AccionPermiso.Ver,
                    Entidad = "Empresa",
                    ReferenciaId = tenantB.Id,
                    Descripcion = "C0303_TENANT_B_PRIVATE_SENTINEL"
                });
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                var auditReadHttp = new DefaultHttpContext();
                auditReadHttp.Request.Method = HttpMethods.Get;
                auditReadHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
                    tenantA.Id;
                var readAccessor = new HttpContextAccessor
                {
                    HttpContext = auditReadHttp
                };
                await using var auditReadDb = new AppDbContext(options, null, readAccessor);
                var auditRepository = new AuditoriaRepository(auditReadDb);
                var filter = new Solqaryn.Application.DTOs.AuditoriaFiltroDto
                {
                    Page = 1,
                    PageSize = 50
                };
                var (tenantARecords, tenantATotal) =
                    await auditRepository.GetFilteredAsync(filter);
                Assert.True(tenantATotal > 0);
                Assert.All(tenantARecords,
                    row => Assert.Equal(tenantA.Id, row.EmpresaId));
                Assert.DoesNotContain(tenantARecords,
                    row => row.Descripcion == "C0303_TENANT_B_PRIVATE_SENTINEL");

                auditReadHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
                    tenantB.Id;
                var (tenantBRecords, tenantBTotal) =
                    await auditRepository.GetFilteredAsync(filter);
                Assert.True(tenantBTotal > 0);
                Assert.All(tenantBRecords,
                    row => Assert.Equal(tenantB.Id, row.EmpresaId));
                Assert.Contains(tenantBRecords,
                    row => row.Descripcion == "C0303_TENANT_B_PRIVATE_SENTINEL");

                auditReadHttp.Items.Remove(
                    VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
                await Assert.ThrowsAsync<
                    Solqaryn.Application.Exceptions.ForbiddenAccessException>(
                    () => auditRepository.GetFilteredAsync(filter));

                Assert.Equal(HttpStatusCode.Forbidden,
                    await HttpAuditAsync(tenantB.Id, signed));
                Assert.Equal(HttpStatusCode.Unauthorized,
                    await HttpAuditAsync(tenantA.Id,
                        new JwtSecurityTokenHandler().WriteToken(forgedToken)));
                Assert.Equal(HttpStatusCode.Unauthorized,
                    await HttpAuditAsync(tenantA.Id,
                        new JwtSecurityTokenHandler().WriteToken(expiredToken)));
                // Selector spoofing is never a tenant authority.
                using var wrongSelector = new HttpRequestMessage(
                    HttpMethod.Get, "/__dev/audit-authorization/" + tenantA.Id);
                wrongSelector.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer", signed);
                wrongSelector.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", tenantB.Id.ToString());
                using var selectorResponse = await client.SendAsync(wrongSelector);
                Assert.Equal(HttpStatusCode.Forbidden, selectorResponse.StatusCode);
                // Fail-closed 403 must never be cached for cross-tenant probes.
                Assert.True(selectorResponse.Headers.CacheControl?.NoStore == true);

                using var foreignRecord = new HttpRequestMessage(
                    HttpMethod.Post, "/__dev/audit-authorization/" +
                    tenantB.Id + "/record");
                foreignRecord.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", signed);
                using var foreignResponse = await client.SendAsync(foreignRecord);
                Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
                Assert.True(foreignResponse.Headers.CacheControl?.NoStore == true);

            }

            // V27: the production /auditoria MVC route must pass the actual
            // RequierePermiso filter, current SQL membership, tenant-bound
            // repository and the P0 middleware. A direct controller action or
            // a Development probe alone is NOT an end-to-end HTTP assertion.
            var mvcConfig = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SecurityAudit:EnableRealAuditReadPilot"] = "true"
                }).Build();
            using (var mvcHost = await new HostBuilder()
                .UseEnvironment(Environments.Development)
                .ConfigureWebHost(web => web.UseTestServer()
                    .ConfigureServices(s =>
                    {
                        s.AddLogging();
                        s.AddRouting();
                        s.AddControllers().AddApplicationPart(typeof(AuditoriaController).Assembly);
                        s.AddHttpContextAccessor();
                        s.AddSingleton<IConfiguration>(mvcConfig);
                        // The production P0 middleware declares both public-store
                        // provider DI parameters even on unrelated private routes.
                        // Test-only no-op providers satisfy DI; /auditoria never
                        // calls either provider and tenant auth remains real SQL.
                        s.AddSingleton<IPublicStoreHostTenantResolver>(
                            Mock.Of<IPublicStoreHostTenantResolver>());
                        s.AddSingleton<IPublicStoreProofReplayStore>(
                            Mock.Of<IPublicStoreProofReplayStore>());
                        s.AddScoped(_ => new AppDbContext(options, null,
                            _.GetRequiredService<IHttpContextAccessor>()));
                        s.AddScoped<ICurrentUserService, CurrentUserService>();
                        s.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                        s.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
                        s.AddScoped<IRolPermisoRepository, RolPermisoRepository>();
                        s.AddScoped<IRolRepository, RolRepository>();
                        s.AddScoped<IPermisoRepository, PermisoRepository>();
                        s.AddScoped<IPermisoService, PermisoService>();
                        s.AddScoped<IAuditoriaService, AuditoriaService>();
                        s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(o =>
                            {
                                o.MapInboundClaims = false;
                                o.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidIssuer = "c0303-mysql-ci",
                                    ValidateAudience = true,
                                    ValidAudience = "c0303-mysql-ci",
                                    ValidateIssuerSigningKey = true,
                                    IssuerSigningKey = Signing,
                                    ValidateLifetime = true,
                                    ClockSkew = TimeSpan.Zero
                                };
                            });
                        s.AddAuthorization();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseMiddleware<Solqaryn.API.Middleware.UnscopedCommercialTenantGuardMiddleware>();
                        app.UseEndpoints(e => e.MapControllers());
                    })).StartAsync())
            {
                using var mvcClient = mvcHost.GetTestClient();
                var adminJwt = new JwtSecurityToken(
                    issuer: "c0303-mysql-ci", audience: "c0303-mysql-ci",
                    claims: [
                        new Claim("id", actor.Id.ToString()),
                        new Claim("nombreUsuario", username),
                        new Claim("sub", username),
                        new Claim("esAdministrador", "true")
                    ],
                    notBefore: DateTime.UtcNow.AddMinutes(-1),
                    expires: DateTime.UtcNow.AddMinutes(8),
                    signingCredentials: new SigningCredentials(
                        Signing, SecurityAlgorithms.HmacSha256));
                var realJwt = new JwtSecurityTokenHandler().WriteToken(adminJwt);

                async Task<(HttpStatusCode Code, string Body, string Cache)> GetAuditAsync(
                    string bearer, int? selector = null, string? rawSelector = null)
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Get, "/auditoria?Page=1&PageSize=25");
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer", bearer);
                    if (rawSelector is not null)
                        request.Headers.TryAddWithoutValidation(
                            "X-Empresa-Id", rawSelector);
                    else if (selector.HasValue)
                        request.Headers.TryAddWithoutValidation(
                            "X-Empresa-Id", selector.Value.ToString());
                    using var response = await mvcClient.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    return (response.StatusCode, body,
                        response.Headers.CacheControl?.ToString() ?? string.Empty);
                }

                // Requests rejected by the P0 multi-company quarantine do not
                // reach the MVC permission filter. Its explicit contract is 503
                // with an opaque isolation code, never protected audit content.
                async Task AssertQuarantinedAsync(
                    string bearer, int? selector = null, string? rawSelector = null)
                {
                    var denied = await GetAuditAsync(bearer, selector, rawSelector);
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, denied.Code);
                    Assert.Contains("TENANT_DATA_ISOLATION_REQUIRED", denied.Body,
                        StringComparison.Ordinal);
                    Assert.DoesNotContain("C0303_TENANT_B_PRIVATE_SENTINEL",
                        denied.Body, StringComparison.Ordinal);
                    Assert.DoesNotContain("C03-03.M03.03: acceso de auditoría validado",
                        denied.Body, StringComparison.Ordinal);
                    Assert.Contains("no-store", denied.Cache,
                        StringComparison.OrdinalIgnoreCase);
                }

                var authorized = await GetAuditAsync(realJwt, tenantA.Id);
                Assert.Equal(HttpStatusCode.OK, authorized.Code);
                Assert.Contains("no-store", authorized.Cache, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("C0303_TENANT_B_PRIVATE_SENTINEL",
                    authorized.Body, StringComparison.Ordinal);
                Assert.Contains("C03-03.M03.03: acceso de auditoría validado",
                    authorized.Body, StringComparison.Ordinal);

                await AssertQuarantinedAsync(realJwt, tenantB.Id);
                await AssertQuarantinedAsync(realJwt, null, "invalid");
                await AssertQuarantinedAsync(realJwt);
                Assert.Equal(HttpStatusCode.Unauthorized,
                    (await GetAuditAsync(new JwtSecurityTokenHandler().WriteToken(
                        forgedToken), tenantA.Id)).Code);
                Assert.Equal(HttpStatusCode.Unauthorized,
                    (await GetAuditAsync(new JwtSecurityTokenHandler().WriteToken(
                        expiredToken), tenantA.Id)).Code);

                // A real committed SQL grant revocation must invalidate the
                // still valid signed JWT, without changing its claims or cache.
                var liveGrant = await db.RolPermisos.IgnoreQueryFilters()
                    .SingleAsync(r => r.RolId == roleA.Id &&
                                      r.PermisoId == permission.Id);
                db.RolPermisos.Remove(liveGrant);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                await AssertQuarantinedAsync(realJwt, tenantA.Id);
                db.RolPermisos.Add(new RolPermiso
                {
                    RolId = roleA.Id, PermisoId = permission.Id
                });
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                Assert.Equal(HttpStatusCode.OK,
                    (await GetAuditAsync(realJwt, tenantA.Id)).Code);
            }

            // Route spoofing, membership B and critical changes cannot borrow A.
            http.Request.RouteValues["empresaId"] = tenantB.Id.ToString();
            Assert.Null(await reader.ReadCurrentAsync(default));
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenantB.Id;
            Assert.Null(await reader.ReadCurrentAsync(default)); // B has no grant
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenantA.Id;
            http.Request.RouteValues["empresaId"] = tenantA.Id.ToString();
            Assert.Null(await reader.ReadCurrentAsync(
                actor.Id, tenantB.Id, Rules.Action.AuditRead, default));
            Assert.Null(await reader.ReadCurrentAsync(
                actor.Id, tenantA.Id, Rules.Action.CriticalChange, default));

            // Physical transaction rejects the grant while revoked and reverts
            // the change without leaving a stale denial/allowance in SQL.
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                var persisted = await db.Permisos.IgnoreQueryFilters()
                    .SingleAsync(p => p.Id == permission.Id);
                persisted.Activo = false;
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                Assert.Null(await reader.ReadCurrentAsync(default));
                await tx.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            Assert.NotNull(await reader.ReadCurrentAsync(default));

            // V26: production MVC read must re-read the CURRENT admin role
            // from Oracle EF/MySQL 8.4. A stale JWT/admin hint is not a grant.
            // Direct action call; full HTTP MVC filter is a separate gate.
            current.SetupGet(x => x.EsAdministrador).Returns(true);
            var auditOutput = new Mock<IAuditoriaService>(MockBehavior.Strict);
            auditOutput.Setup(x => x.GetFilteredAsync(
                    It.IsAny<AuditoriaFiltroDto>()))
                .ReturnsAsync(new PagedResult<RegistroAuditoriaDto>
                {
                    Page = 1, PageSize = 10, TotalCount = 0
                });
            var productionAudit = new AuditoriaController(
                auditOutput.Object, current.Object, scope)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = http
                }
            };
            var productionFilter = new AuditoriaFiltroDto
            {
                Page = 1, PageSize = 10
            };
            Assert.IsType<OkObjectResult>(
                await productionAudit.GetFiltered(productionFilter));
            auditOutput.Verify(x => x.GetFilteredAsync(
                It.IsAny<AuditoriaFiltroDto>()), Times.Once);

            var downgraded = await db.Roles.IgnoreQueryFilters()
                .SingleAsync(r => r.Id == roleA.Id);
            downgraded.EsAdministrador = false;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.IsType<ForbidResult>(
                await productionAudit.GetFiltered(productionFilter));
            auditOutput.Verify(x => x.GetFilteredAsync(
                It.IsAny<AuditoriaFiltroDto>()), Times.Once);

            var restored = await db.Roles.IgnoreQueryFilters()
                .SingleAsync(r => r.Id == roleA.Id);
            restored.EsAdministrador = true;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.IsType<OkObjectResult>(
                await productionAudit.GetFiltered(productionFilter));
            auditOutput.Verify(x => x.GetFilteredAsync(
                It.IsAny<AuditoriaFiltroDto>()), Times.Exactly(2));

            // Committed revocation is observed even with an otherwise valid JWT.
            var membership = await db.UsuarioEmpresas.IgnoreQueryFilters()
                .SingleAsync(m => m.UsuarioId == actor.Id &&
                                  m.EmpresaId == tenantA.Id);
            membership.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Null(await reader.ReadCurrentAsync(default));
            Assert.Null(await reader.ReadCurrentAsync(
                actor.Id, tenantA.Id, Rules.Action.AuditRead, default));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
