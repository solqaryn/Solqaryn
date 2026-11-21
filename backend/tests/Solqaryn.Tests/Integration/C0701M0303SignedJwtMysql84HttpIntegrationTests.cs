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
[Route("c0701-m0303-test/{tenantId:int}")]
public sealed class C0701M0303TestOnlyController(
    IC0701VerifiedCatalogActorReader source) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int tenantId, CancellationToken ct)
    {
        var actor = await source.ReadCurrentAsync(ct);
        return actor is not null && actor.Scope.EmpresaId == tenantId
            ? Ok(new { empresaId = actor.Scope.EmpresaId, checkedOnly = true })
            : Forbid();
    }
}

[Trait("Category", "Integration")]
public sealed class C0701M0303SignedJwtMysql84HttpIntegrationTests
{
    private const string Issuer = "c0701-m0303-ci-issuer";
    private const string Audience = "c0701-m0303-ci-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0701-m0303-ephemeral-jwt-integration-key-only-2026"));

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
        var raw = Environment.GetEnvironmentVariable("C0701_M0303_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0701_m0303_" + Guid.NewGuid().ToString("N")
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

            var a = new Empresa("C0701M0303 Tenant A");
            var b = new Empresa("C0701M0303 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0701M0303 role A", NombreNormalizado = "C0701_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0701M0303 role B", NombreNormalizado = "C0701_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var user = new Usuario
            {
                NombreUsuario = "c0701_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "C0701 isolated actor",
                PasswordHash = "not-a-real-password",
                RolId = roleA.Id, Activo = true
            };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.Add(new UsuarioEmpresa(user.Id, a.Id, roleA.Id));
            await db.SaveChangesAsync();
            var permission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p =>
                    p.Modulo == ModuloSistema.Productos &&
                    p.Accion == AccionPermiso.Editar &&
                    p.Ambito == AmbitoAutorizacion.Empresa);
            if (permission is null)
            {
                permission = new Permiso
                {
                    Codigo = "C0701M0303.PRODUCTOS.EDITAR",
                    Nombre = "C0701 isolated edit",
                    Modulo = ModuloSistema.Productos,
                    Accion = AccionPermiso.Editar,
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
                    ["CatalogAuthorization:EnableReadOnlyProbe"] = "true"
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
                        services.AddScoped<IC0701VerifiedCatalogActorReader,
                            C0701CatalogVerifiedActorSource>();
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
                            typeof(C0701M0303TestOnlyController).Assembly);
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
                $"/c0701-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

            var signed = Sign(user.Id);
            var parts = signed.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join(".", parts));
            using (var forged = await client.GetAsync(
                $"/c0701-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Empresa-Id", b.Id.ToString());
            using (var allowed = await client.GetAsync(
                $"/c0701-m0303-test/{a.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Contains(a.Id.ToString(),
                    await allowed.Content.ReadAsStringAsync(),
                    StringComparison.Ordinal);
            }
            // Signed JWT still cannot use noncanonical numeric route aliases.
            using (var tenantAlias = await client.GetAsync(
                $"/c0701-m0303-test/0{a.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, tenantAlias.StatusCode);
            using (var foreign = await client.GetAsync(
                $"/c0701-m0303-test/{b.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);

            // Two signed "id" claims are ambiguous authority even when first ID is real.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Sign(user.Id, duplicateId: true));
            using (var duplicateClaims = await client.GetAsync(
                $"/c0701-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, duplicateClaims.StatusCode);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);

            var grant = await db.RolPermisos.IgnoreQueryFilters()
                .SingleAsync(p => p.RolId == roleA.Id &&
                                  p.PermisoId == permission.Id);
            db.RolPermisos.Remove(grant);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var revoked = await client.GetAsync(
                $"/c0701-m0303-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static string Sign(int id, bool duplicateId = false)
    {
        var claims = new List<Claim>
        {
            new("id", id.ToString()),
            new("rolId", "999999"),
            new("esAdministrador", "true")
        };
        if (duplicateId)
            claims.Add(new Claim("id", (id + 1).ToString()));
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
