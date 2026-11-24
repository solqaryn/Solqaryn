using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C02-01.M05.05: signed JWT + real HTTP + Oracle EF/MySQL 8.4 adversarial tenant
/// isolation. Test-only; no runtime endpoint, global-console grant or live
/// per-user preference persistence is introduced or certified.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0201M0505SignedJwtMysqlNegativeTests
{
    private const string Issuer = "c0201-m0505-ci-issuer";
    private const string Audience = "c0201-m0505-ci-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0201-m0505-ephemeral-signed-token-key-2026-never-production"));

    private sealed class VerifiedHttpCurrentUser(IHttpContextAccessor access) : ICurrentUserService
    {
        public bool EstaAutenticado =>
            access.HttpContext?.User.Identity?.IsAuthenticated == true;
        public int? UsuarioId => int.TryParse(
            access.HttpContext?.User.FindFirst("id")?.Value, out var id) ? id : null;
        public string? NombreUsuario => null;
        public string? NombreCompleto => null;
        public int? RolId => null; // Never trust this JWT claim as an authority.
        public bool EsAdministrador => false;
    }

    private sealed class FixedAuthorityReader(
        V17IntelligentTenantLoginAuthorization.TrustedSnapshot snapshot)
        : ITrustedTenantLoginAuthorityReader
    {
        public ValueTask<V17IntelligentTenantLoginAuthorization.TrustedSnapshot?>
            ReadCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<
                V17IntelligentTenantLoginAuthorization.TrustedSnapshot?>(snapshot);
        }
    }

    [Fact]
    public async Task ForgedHostJwtCrossTenantRevocationAndForeignPreferenceFailClosed()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var source = new MySqlConnectionStringBuilder(raw);
        if (source.Server is not ("127.0.0.1" or "localhost") ||
            source.Port != 3306 || source.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0201_m0505_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated,
                m => m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var a = new Empresa("C0201 M0505 Tenant A");
            var b = new Empresa("C0201 M0505 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0201 M0505 role A", NombreNormalizado = "M0505_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0201 M0505 role B", NombreNormalizado = "M0505_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var user = new Usuario
            {
                NombreUsuario = "m0505_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "Isolated integration actor",
                PasswordHash = "never-a-real-password", RolId = roleA.Id,
                Activo = true
            };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(user.Id, a.Id, roleA.Id),
                new UsuarioEmpresa(user.Id, b.Id, roleB.Id));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            using var host = await new HostBuilder()
                .ConfigureWebHost(web => web.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<AppDbContext>(o =>
                            o.UseMySQL(isolated, m =>
                                m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")));
                        services.AddHttpContextAccessor();
                        services.AddScoped<ICurrentUserService, VerifiedHttpCurrentUser>();
                        services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(o => o.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidateIssuer = true, ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = Issuer, ValidAudience = Audience,
                                    IssuerSigningKey = Key, ClockSkew = TimeSpan.Zero
                                });
                        services.AddAuthorization();
                        services.AddControllers().AddApplicationPart(
                            typeof(TenantContextController).Assembly);
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

            // Anonymous and a tampered signature never reach tenant membership.
            using (var anonymous = await client.GetAsync("/tenant-context/empresas"))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            var signed = Sign(user.Id, roleA.Id);
            var parts = signed.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join(".", parts));
            using (var forgedJwt = await client.GetAsync("/tenant-context/" + a.Id))
                Assert.Equal(HttpStatusCode.Unauthorized, forgedJwt.StatusCode);

            // A correctly signed token with an invalid base role must still
            // fail closed; the selected tenant's role is not the actor's base role.
            foreach (var invalidRoleId in new[] { 999999, roleB.Id })
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Sign(user.Id, invalidRoleId));
                using var denied = await client.GetAsync("/tenant-context/" + a.Id);
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                using var deniedList = await client.GetAsync("/tenant-context/empresas");
                Assert.Equal(HttpStatusCode.Forbidden, deniedList.StatusCode);
                Assert.DoesNotContain(a.Nombre, await denied.Content.ReadAsStringAsync(),
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(b.Nombre, await deniedList.Content.ReadAsStringAsync(),
                    StringComparison.OrdinalIgnoreCase);
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Empresa-Id",
                b.Id.ToString());
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Forwarded-Host", "other-tenant.attacker.invalid");
            using (var authorizedA = await client.GetAsync("/tenant-context/" + a.Id))
            {
                Assert.Equal(HttpStatusCode.OK, authorizedA.StatusCode);
                using var json = JsonDocument.Parse(
                    await authorizedA.Content.ReadAsStringAsync());
                var data = json.RootElement.GetProperty("data");
                Assert.Equal(a.Id, data.GetProperty("empresaId").GetInt32());
                Assert.Equal(roleA.Id, data.GetProperty("rolId").GetInt32());
                Assert.Equal(user.Id, data.GetProperty("usuarioId").GetInt32());
            }
            // Another signed actor with zero persisted memberships cannot borrow A.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Sign(user.Id + 100000, roleA.Id));
            using (var foreignActor = await client.GetAsync("/tenant-context/" + a.Id))
                Assert.Equal(HttpStatusCode.Forbidden, foreignActor.StatusCode);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            var memberB = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == user.Id && m.EmpresaId == b.Id);
            memberB.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var revokedB = await client.GetAsync("/tenant-context/" + b.Id))
            {
                Assert.Equal(HttpStatusCode.Forbidden, revokedB.StatusCode);
                Assert.DoesNotContain("C0201 M0505 Tenant B",
                    await revokedB.Content.ReadAsStringAsync(),
                    StringComparison.OrdinalIgnoreCase);
            }
            using (var list = await client.GetAsync("/tenant-context/empresas"))
            {
                Assert.Equal(HttpStatusCode.OK, list.StatusCode);
                using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
                var data = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
                Assert.Single(data);
                Assert.Equal(a.Id, data[0].GetProperty("empresaId").GetInt32());
            }
            var memberA = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == user.Id && m.EmpresaId == a.Id);
            memberA.CambiarRol(roleB.Id); // Role belonging to B cannot authorize A.
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var stolenRole = await client.GetAsync("/tenant-context/" + a.Id))
                Assert.Equal(HttpStatusCode.Forbidden, stolenRole.StatusCode);

            // The real Application preference policy rejects a different user's
            // preference. This is synthetic evidence, NOT live preference SQL.
            var snapshot = new V17IntelligentTenantLoginAuthorization.TrustedSnapshot(
                user.Id, true, true, true, false, false, null,
                b.Id, user.Id + 100000, true, false,
                [
                    new(user.Id, a.Id, "Tenant A", true, true, true, true, false),
                    new(user.Id, b.Id, "Tenant B", true, true, true, true, false)
                ]);
            var decision = await new V17IntelligentTenantLoginAuthorization(
                new FixedAuthorityReader(snapshot)).ResolveAsync(new());
            Assert.Equal("SELECT_VERIFIED_TENANT", decision.Code);
            Assert.True(decision.DiscardStalePreference);
            Assert.Null(decision.ProposedTenantId);
            Assert.False(decision.AuthorizesResourceAccess);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static string Sign(int id, int baseRoleId)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims:
            [
                new Claim("id", id.ToString()),
                new Claim("rolId", baseRoleId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim("esAdministrador", "true")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(Key,
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
