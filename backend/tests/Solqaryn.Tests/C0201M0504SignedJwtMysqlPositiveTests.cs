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
/// C02-01.M05.04: signed JWT + HTTP + Oracle EF/MySQL 8.4 positive tenant\n/// membership proof, with adversarial cases. The last preference fixture\n/// is NOT live per-user preference persistence or a completed UI redirect.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0201M0504SignedJwtMysqlPositiveTests
{
    private const string Issuer = "c0201-m0504-ci-issuer";
    private const string Audience = "c0201-m0504-ci-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0201-m0504-ephemeral-signed-token-key-2026-never-production"));

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
    public async Task SignedJwtPositiveTenantSelectionAndPreferenceRequireBackendReissue()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var source = new MySqlConnectionStringBuilder(raw);
        if (source.Server is not ("127.0.0.1" or "localhost") ||
            source.Port != 3306 || source.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0201_m0504_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated,
                m => m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var a = new Empresa("C0201 M0504 Tenant A");
            var b = new Empresa("C0201 M0504 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0201 M0504 role A", NombreNormalizado = "M0504_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0201 M0504 role B", NombreNormalizado = "M0504_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var user = new Usuario
            {
                NombreUsuario = "m0504_" + Guid.NewGuid().ToString("N")[..12],
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
            var signed = Sign(user.Id);
            var parts = signed.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join(".", parts));
            using (var forgedJwt = await client.GetAsync("/tenant-context/" + a.Id))
                Assert.Equal(HttpStatusCode.Unauthorized, forgedJwt.StatusCode);

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
            // The signed-JWT backend returns both active memberships from the real
            // Oracle EF/MySQL 8.4 store, never from a spoofed browser header.
            using (var validList = await client.GetAsync("/tenant-context/empresas"))
            {
                Assert.Equal(HttpStatusCode.OK, validList.StatusCode);
                using var json = JsonDocument.Parse(await validList.Content.ReadAsStringAsync());
                var valid = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
                Assert.Equal(2, valid.Length);
                Assert.Contains(valid, tenant => tenant.GetProperty("empresaId").GetInt32() == a.Id);
                Assert.Contains(valid, tenant => tenant.GetProperty("empresaId").GetInt32() == b.Id);
            }
            using (var validB = await client.GetAsync("/tenant-context/" + b.Id))
            {
                Assert.Equal(HttpStatusCode.OK, validB.StatusCode);
                using var json = JsonDocument.Parse(await validB.Content.ReadAsStringAsync());
                Assert.Equal(b.Id, json.RootElement.GetProperty("data").GetProperty("empresaId").GetInt32());
                Assert.Equal(roleB.Id, json.RootElement.GetProperty("data").GetProperty("rolId").GetInt32());
            }
            // Positive LAST-VALID decision only: preference provenance is a
            // contract fixture, not production preference SQL or a JWT reissue.
            var preferred = new V17IntelligentTenantLoginAuthorization.TrustedSnapshot(
                user.Id, true, true, true, false, false, null,
                b.Id, user.Id, true, false,
                [
                    new(user.Id, a.Id, "Tenant A", true, true, true, true, false),
                    new(user.Id, b.Id, "Tenant B", true, true, true, true, false)
                ]);
            var preferredDecision = await new V17IntelligentTenantLoginAuthorization(
                new FixedAuthorityReader(preferred)).ResolveAsync(new());
            Assert.Equal("PREFERENCE_REISSUE_REQUIRED", preferredDecision.Code);
            Assert.Equal(b.Id, preferredDecision.ProposedTenantId);
            Assert.True(preferredDecision.RequiresAuthenticatedBackendReissue);
            Assert.False(preferredDecision.IssuesJwt);
            Assert.False(preferredDecision.AuthorizesResourceAccess);

            // Another signed actor with zero persisted memberships cannot borrow A.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Sign(user.Id + 100000));
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
                Assert.DoesNotContain("C0201 M0504 Tenant B",
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
            // After B is revoked in real MySQL, only A remains eligible.
            // A fresh tenant snapshot must require backend reissue, not install
            // a client-side context or bypass navigation guards.
            var single = new V17IntelligentTenantLoginAuthorization.TrustedSnapshot(
                user.Id, true, true, true, false, false, null,
                null, null, false, false,
                [new(user.Id, a.Id, "Tenant A", true, true, true, true, false)]);
            var singleDecision = await new V17IntelligentTenantLoginAuthorization(
                new FixedAuthorityReader(single)).ResolveAsync(new());
            Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", singleDecision.Code);
            Assert.Equal(a.Id, singleDecision.ProposedTenantId);
            Assert.Empty(singleDecision.Choices);
            Assert.True(singleDecision.RequiresAuthenticatedBackendReissue);
            Assert.False(singleDecision.InstallsTenantContext);
            Assert.False(singleDecision.AuthorizesResourceAccess);

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

    private static string Sign(int id)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims:
            [
                new Claim("id", id.ToString()),
                new Claim("rolId", "999999"),
                new Claim("esAdministrador", "true")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(Key,
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
