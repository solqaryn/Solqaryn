using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
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

            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
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
                int? forgedTenant = null)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post,
                    $"/__dev/expedientes/almacenes/{caseId}/{suffix}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (forgedTenant.HasValue) req.Headers.Add("X-Empresa-Id",
                    forgedTenant.Value.ToString(CultureInfo.InvariantCulture));
                req.Content = JsonContent.Create(body);
                return await client.SendAsync(req);
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

            setup.RolPermisos.Remove(writerGrant);
            await setup.SaveChangesAsync();
            await Expect(HttpStatusCode.Forbidden, tokenA, 1, "versiones", Version(warehouseA, 2));
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
}
