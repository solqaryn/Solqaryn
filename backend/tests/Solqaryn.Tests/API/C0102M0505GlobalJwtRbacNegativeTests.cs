using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C01-02.M05.05: genuine signed-JWT HTTP pipeline and the existing
/// server-side global-platform role reader. The database in this isolated
/// contract test is EF InMemory; it is NOT proof of production MySQL or
/// of a future global QA catalog route.
/// </summary>
public sealed class C0102M0505GlobalJwtRbacNegativeTests
{
    private const string Issuer = "c0102-m0505-ephemeral-issuer";
    private const string Audience = "c0102-m0505-private-test-client";
    private static readonly SymmetricSecurityKey GoodKey = new(
        Encoding.UTF8.GetBytes("solqaryn-c0102-m0505-ephemeral-jwt-test-only-key"));
    private static readonly SymmetricSecurityKey WrongKey = new(
        Encoding.UTF8.GetBytes("solqaryn-c0102-m0505-unauthorized-signature-key"));

    private sealed record Fixture(
        int GlobalId, string GlobalName, int EnterpriseId,
        string EnterpriseName, int TenantA, int TenantB,
        int PlatformRoleId);

    private static async Task<IHost> StartAsync()
    {
        var databaseName = "c0102-m0505-" + Guid.NewGuid().ToString("N");
        var host = new HostBuilder().ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddRouting();
                services.AddHttpContextAccessor();
                services.AddControllers()
                    .AddApplicationPart(typeof(PlataformaGestionMvpController).Assembly);
                services.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase(databaseName));
                services.AddScoped<ICurrentUserService, CurrentUserService>();
                services.AddScoped<IPlataformaScopeService, PlataformaScopeService>();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = Issuer,
                            ValidateAudience = true,
                            ValidAudience = Audience,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = GoodKey,
                            RequireSignedTokens = true,
                            RequireExpirationTime = true
                        };
                    });
                services.AddAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).Build();
        await host.StartAsync();
        return host;
    }

    private static async Task<Fixture> SeedAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantA = new Empresa("V22 tenant A private catalog");
        var tenantB = new Empresa("V22 tenant B private catalog");
        db.Set<Empresa>().AddRange(tenantA, tenantB);
        await db.SaveChangesAsync();

        var enterpriseRole = new Rol
        {
            Nombre = "Enterprise administrator", NombreNormalizado = "V22_ENTERPRISE",
            EsSistema = true, EsAdministrador = true, Activo = true,
            Ambito = AmbitoAutorizacion.Empresa
        };
        var platformRole = new Rol
        {
            Nombre = "Platform administrator", NombreNormalizado = "V22_PLATFORM",
            EsSistema = true, EsAdministrador = true, Activo = true,
            Ambito = AmbitoAutorizacion.Plataforma
        };
        var tenantARole = new Rol
        {
            Nombre = "CAP_A_ONLY", NombreNormalizado = "V22_CAP_A",
            EsSistema = false, EsAdministrador = false, Activo = true,
            Ambito = AmbitoAutorizacion.Empresa, EmpresaId = tenantA.Id
        };
        var tenantBRole = new Rol
        {
            Nombre = "CAP_B_ONLY", NombreNormalizado = "V22_CAP_B",
            EsSistema = false, EsAdministrador = false, Activo = true,
            Ambito = AmbitoAutorizacion.Empresa, EmpresaId = tenantB.Id
        };
        db.Roles.AddRange(enterpriseRole, platformRole, tenantARole, tenantBRole);
        await db.SaveChangesAsync();

        const string platformName = "v22_platform_operator";
        const string enterpriseName = "v22_enterprise_administrator";
        var global = new Usuario
        {
            NombreUsuario = platformName, NombreCompleto = "Isolated platform operator",
            PasswordHash = "test-only-not-login", RolId = enterpriseRole.Id
        };
        var business = new Usuario
        {
            NombreUsuario = enterpriseName, NombreCompleto = "Isolated enterprise operator",
            PasswordHash = "test-only-not-login", RolId = enterpriseRole.Id
        };
        db.Usuarios.AddRange(global, business);
        await db.SaveChangesAsync();

        db.Set<UsuarioEmpresa>().Add(
            new UsuarioEmpresa(business.Id, tenantB.Id, enterpriseRole.Id));
        db.Set<UsuarioRolPlataforma>().Add(
            new UsuarioRolPlataforma(global.Id, platformRole.Id));
        await db.SaveChangesAsync();
        return new Fixture(global.Id, platformName, business.Id,
            enterpriseName, tenantA.Id, tenantB.Id, platformRole.Id);
    }

    private static string Token(int userId, string userName, int claimedRoleId,
        bool forged = false, bool expired = false)
    {
        var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: new[]
            {
                new Claim("id", userId.ToString()),
                new Claim("nombreUsuario", userName),
                // Deliberately hostile JWT hints: the backend MUST ignore
                // these for the platform authority decision.
                new Claim("rolId", claimedRoleId.ToString()),
                new Claim("esAdministrador", "true")
            },
            notBefore: now.AddMinutes(-10),
            expires: expired ? now.AddMinutes(-2) : now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                forged ? WrongKey : GoodKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<HttpResponseMessage> GetRolesAsync(
        HttpClient client, int tenant, string? jwt)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/plataforma/gestion/roles?empresaId=" + tenant);
        if (jwt is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.Add("X-Empresa-Id", tenant.ToString());
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task AnonymousEnterpriseAndForgedJwtCannotReadGlobalPrivateCatalog()
    {
        using var host = await StartAsync();
        var f = await SeedAsync(host);
        using var client = host.GetTestClient();

        using (var anonymous = await GetRolesAsync(client, f.TenantA, null))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            Assert.DoesNotContain("CAP_A_ONLY", await anonymous.Content.ReadAsStringAsync());
        }
        using (var enterprise = await GetRolesAsync(client, f.TenantA,
            Token(f.EnterpriseId, f.EnterpriseName, f.PlatformRoleId)))
        {
            Assert.Equal(HttpStatusCode.Forbidden, enterprise.StatusCode);
            Assert.DoesNotContain("CAP_A_ONLY", await enterprise.Content.ReadAsStringAsync());
        }
        using (var forged = await GetRolesAsync(client, f.TenantA,
            Token(f.GlobalId, f.GlobalName, f.PlatformRoleId, forged: true)))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            Assert.DoesNotContain("CAP_A_ONLY", await forged.Content.ReadAsStringAsync());
        }

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.Set<RegistroAuditoria>().CountAsync());
    }

    [Fact]
    public async Task OnlyLiveServerSidePlatformGrantCanReadTenantScopedCatalog()
    {
        using var host = await StartAsync();
        var f = await SeedAsync(host);
        using var client = host.GetTestClient();

        using (var expired = await GetRolesAsync(client, f.TenantA,
            Token(f.GlobalId, f.GlobalName, f.PlatformRoleId, expired: true)))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        }

        var token = Token(f.GlobalId, f.GlobalName, f.PlatformRoleId);
        using (var accepted = await GetRolesAsync(client, f.TenantA, token))
        {
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            var json = await accepted.Content.ReadAsStringAsync();
            Assert.Contains("CAP_A_ONLY", json);
            Assert.DoesNotContain("CAP_B_ONLY", json);
            Assert.DoesNotContain("Platform administrator", json);
            Assert.NotNull(accepted.Headers.CacheControl);
            Assert.True(accepted.Headers.CacheControl.Private);
            Assert.True(accepted.Headers.CacheControl.NoStore);
        }

        // Revocation is resolved from server-side data for every request:
        // the SAME valid signed JWT cannot keep platform privileges.
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var grant = await db.Set<UsuarioRolPlataforma>()
                .SingleAsync(x => x.UsuarioId == f.GlobalId);
            grant.Desactivar();
            await db.SaveChangesAsync();
        }
        using var revoked = await GetRolesAsync(client, f.TenantA, token);
        Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        Assert.DoesNotContain("CAP_A_ONLY", await revoked.Content.ReadAsStringAsync());

        using var auditScope = host.Services.CreateScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(await auditDb.Set<RegistroAuditoria>().ToArrayAsync());
    }
}
