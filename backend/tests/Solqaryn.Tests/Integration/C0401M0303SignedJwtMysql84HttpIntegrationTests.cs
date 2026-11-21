using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Security;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>Ruta solo dentro del assembly de pruebas, no API de produccion.</summary>
[ApiController]
[Authorize]
[Route("c0401-m0303-test/{empresaId:int}")]
public sealed class C0401M0303TestOnlyController(
    IC0401VerifiedPlanActorSource source) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int empresaId, CancellationToken ct)
    {
        var actor = await source.ReadCurrentAsync(ct);
        return actor is not null && actor.Scope.TenantEmpresaId == empresaId
            ? Ok(new { empresaId = actor.Scope.TenantEmpresaId, checkedOnly = true })
            : Forbid();
    }
}

[Trait("Category", "Integration")]
public sealed class C0401M0303SignedJwtMysql84HttpIntegrationTests
{
    private const string Issuer = "c0401-m0303-ci-issuer";
    private const string Audience = "c0401-m0303-ci-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0401-m0303-ephemeral-jwt-integration-key-only-2026"));

    private sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUserService
    {
        public bool EstaAutenticado => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
        public int? UsuarioId => int.TryParse(
            accessor.HttpContext?.User.FindFirst("id")?.Value, out var id) ? id : null;
        public string? NombreUsuario => null;
        public string? NombreCompleto => null;
        public int? RolId => 999999;
        public bool EsAdministrador => true;
    }

    [Fact]
    public async Task SignedJwtMysqlTenantABAndRoleRevocationFailClosed()
    {
        var raw = Environment.GetEnvironmentVariable("C0401_M0303_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0401_m0303_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, m => m.MigrationsAssembly(
                "Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            await using (var sql = db.Database.GetDbConnection().CreateCommand())
            {
                sql.CommandText = "SELECT VERSION()";
                var version = Convert.ToString(await sql.ExecuteScalarAsync());
                Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true);
            }
            await db.Database.CloseConnectionAsync();

            var a = new Empresa("C0401M0303 Tenant A");
            var b = new Empresa("C0401M0303 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0401M0303 role A", NombreNormalizado = "C0401_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0401M0303 role B", NombreNormalizado = "C0401_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var user = new Usuario
            {
                NombreUsuario = "c0401_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "C0401 isolated actor",
                PasswordHash = "not-a-real-password",
                RolId = roleA.Id, Activo = true
            };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.Add(new UsuarioEmpresa(user.Id, a.Id, roleA.Id));
            await db.SaveChangesAsync();
            var permission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p =>
                    p.Modulo == ModuloSistema.Configuracion &&
                    p.Accion == AccionPermiso.Ver &&
                    p.Ambito == AmbitoAutorizacion.Empresa);
            if (permission is null)
            {
                permission = new Permiso
                {
                    Codigo = "C0401M0303.PRODUCTOS.EDITAR",
                    Nombre = "C0401 isolated edit",
                    Modulo = ModuloSistema.Configuracion,
                    Accion = AccionPermiso.Ver,
                    Ambito = AmbitoAutorizacion.Empresa,
                    Activo = true
                };
                db.Permisos.Add(permission);
                await db.SaveChangesAsync();
            }
            Assert.True(permission.Activo && !permission.Eliminado);
            db.RolPermisos.Add(new RolPermiso
            {
                RolId = roleA.Id, PermisoId = permission.Id
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PlanCatalogAuthorization:EnableReadOnlyProbe"] = "true"
                }).Build();
            using var host = await new HostBuilder()
                .ConfigureWebHost(web => web.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IConfiguration>(config);
                        services.AddDbContext<AppDbContext>(o =>
                            o.UseMySQL(isolated, m => m.MigrationsAssembly(
                                "Solqaryn.Infrastructure.Migrations")));
                        services.AddHttpContextAccessor();
                        services.AddScoped<ICurrentUserService, HttpCurrentUser>();
                        services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                        services.AddScoped<IC0401VerifiedPlanActorSource,
                            C0401PlanVerifiedActorSource>();
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(o => o.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = Issuer,
                                    ValidAudience = Audience,
                                    IssuerSigningKey = Key,
                                    ClockSkew = TimeSpan.Zero
                                });
                        services.AddAuthorization();
                        services.AddControllers().AddApplicationPart(
                            typeof(C0401M0303TestOnlyController).Assembly);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(routes => routes.MapControllers());
                    }))
                .StartAsync();
            using var client = host.GetTestClient();

            using (var anonymous = await client.GetAsync(
                $"/c0401-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

            // Opt-in OFF: autenticar correctamente no permite consultar por error.
            var signed = Sign(user.Id);
            config["PlanCatalogAuthorization:EnableReadOnlyProbe"] = "false";
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            using (var off = await client.GetAsync($"/c0401-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, off.StatusCode);
            config["PlanCatalogAuthorization:EnableReadOnlyProbe"] = "true";
            var parts = signed.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join(".", parts));
            using (var forged = await client.GetAsync(
                $"/c0401-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Empresa-Id", b.Id.ToString());
            using (var allowed = await client.GetAsync(
                $"/c0401-m0303-test/{a.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Contains(a.Id.ToString(),
                    await allowed.Content.ReadAsStringAsync(),
                    StringComparison.Ordinal);
            }
            using (var foreign = await client.GetAsync(
                $"/c0401-m0303-test/{b.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);

            var grant = await db.RolPermisos.IgnoreQueryFilters()
                .SingleAsync(p => p.RolId == roleA.Id &&
                                  p.PermisoId == permission.Id);
            db.RolPermisos.Remove(grant);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var revoked = await client.GetAsync(
                $"/c0401-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);

            // El rol empresarial de sistema es una plantilla valida sin EmpresaId;
            // una plantilla eliminada logicamente nunca autoriza, aunque siga Activo.
            db.RolPermisos.Add(new RolPermiso
            {
                RolId = roleA.Id, PermisoId = permission.Id
            });
            var template = new Rol
            {
                Nombre = "C0401 valid system template",
                NombreNormalizado = "C0401_TEMPLATE",
                EmpresaId = null,
                Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = true,
                Activo = true
            };
            db.Roles.Add(template);
            await db.SaveChangesAsync();
            var membership = await db.UsuarioEmpresas
                .SingleAsync(m => m.UsuarioId == user.Id && m.EmpresaId == a.Id);
            membership.CambiarRol(template.Id);
            db.RolPermisos.Add(new RolPermiso
            {
                RolId = template.Id, PermisoId = permission.Id
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var systemTemplate = await client.GetAsync(
                $"/c0401-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.OK, systemTemplate.StatusCode);

            var softDeletedTemplate = await db.Roles.IgnoreQueryFilters()
                .SingleAsync(r => r.Id == template.Id);
            softDeletedTemplate.Eliminado = true;
            softDeletedTemplate.Activo = true; // adversarial inconsistent soft-delete
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var deletedRole = await client.GetAsync(
                $"/c0401-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, deletedRole.StatusCode);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static string Sign(int id)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims:
            [
                new Claim("id", id.ToString()),
                new Claim("rolId", "999999"),
                new Claim("esAdministrador", "true")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
