using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C01-02.M05.02: HTTP/JSON contract of the existing permissions API, not a
/// new capability endpoint or production RBAC/SQL certification.
/// </summary>
public sealed class C0102M0502PermissionsHttpContractTests
{
    private const string Issuer = "solqaryn-v22-c0102-contract-test";
    private const string Audience = "solqaryn-v22-contract-client";
    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("solqaryn-v22-c0102-ephemeral-http-only-signing-key"));

    private static string Token(bool wrongSignature = false, bool expired = false)
    {
        var now = DateTime.UtcNow;
        var key = wrongSignature
            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes("solqaryn-v22-c0102-other-http-only-signing-key"))
            : SigningKey;
        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "301") },
            notBefore: now.AddMinutes(-5),
            expires: expired ? now.AddMinutes(-1) : now.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<IHost> HostAsync(IPermisoService permissions)
    {
        var databaseName = "c0102-m0502-" + Guid.NewGuid().ToString("N");
        var host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddRouting();
                services.AddControllers().AddApplicationPart(typeof(PermisosController).Assembly);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddSingleton(permissions);
                services.AddSingleton(new Mock<IPermisoCatalogoService>().Object);
                services.AddSingleton<IConfiguration>(
                    new ConfigurationBuilder().AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["MultiEmpresa:PrivateReadPilotEnabled"] = "false"
                        }).Build());
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true, ValidIssuer = Issuer,
                            ValidateAudience = true, ValidAudience = Audience,
                            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true, IssuerSigningKey = SigningKey,
                            RequireSignedTokens = true, RequireExpirationTime = true
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

    private static HttpRequestMessage Request(string url, string? bearer = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearer is not null)
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return message;
    }

    [Fact]
    public async Task RealHttpGetRequiresValidSignedJwtBeforeReadingPermissions()
    {
        var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
        permissions.Setup(x => x.GetMisPermisosAsync()).ReturnsAsync(
            new MisPermisosDto { Rol = "Lector", Permisos = new List<string> { "Productos:Ver" } });

        using var host = await HostAsync(permissions.Object);
        using var client = host.GetTestClient();
        foreach (string? bearer in new[] { null, "malformed.jwt", Token(wrongSignature: true), Token(expired: true) })
        {
            using var request = Request("/permisos/mis-permisos", bearer);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.DoesNotContain("Productos:Ver", await response.Content.ReadAsStringAsync());
        }
        permissions.Verify(x => x.GetMisPermisosAsync(), Times.Never);

        using var goodRequest = Request("/permisos/mis-permisos", Token());
        using var good = await client.SendAsync(goodRequest);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        using var body = JsonDocument.Parse(await good.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Lector", body.RootElement.GetProperty("data").GetProperty("rol").GetString());
        Assert.Contains("Productos:Ver",
            body.RootElement.GetProperty("data").GetProperty("permisos")
                .EnumerateArray().Select(x => x.GetString()));
        permissions.Verify(x => x.GetMisPermisosAsync(), Times.Once);
    }

    [Fact]
    public async Task ContractRejectsInvalidForeignOrSuspendedTenantWithoutLeakingRights()
    {
        var tenantA = new Empresa("C0102 tenant A");
        var tenantB = new Empresa("C0102 tenant B");
        tenantB.Desactivar();
        var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
        permissions.Setup(x => x.GetMisPermisosAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => id == tenantA.Id
                ? new MisPermisosDto
                {
                    Rol = "Administrador",
                    Permisos = new List<string> { "Productos:Ver" }
                }
                : new MisPermisosDto());

        using var host = await HostAsync(permissions.Object);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();
        }
        Assert.NotEqual(tenantA.Id, tenantB.Id);
        Assert.True(tenantA.Id > 0);

        using var client = host.GetTestClient();
        using (var invalid = Request("/permisos/mis-permisos/empresa/0", Token()))
        using (var response = await client.SendAsync(invalid))
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
        }
        foreach (var forbiddenId in new[] { tenantB.Id, tenantB.Id + 101 })
        {
            using var request = Request($"/permisos/mis-permisos/empresa/{forbiddenId}", Token());
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.DoesNotContain("Productos:Ver", await response.Content.ReadAsStringAsync());
        }
        using var ownRequest = Request($"/permisos/mis-permisos/empresa/{tenantA.Id}", Token());
        using var own = await client.SendAsync(ownRequest);
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        // HTTP header directive ordering is normalized by HttpClient and not an
        // authorization/security contract. Require all directives as parsed values.
        var cache = own.Headers.CacheControl;
        Assert.NotNull(cache);
        Assert.True(cache.Private);
        Assert.True(cache.NoStore);
        Assert.Equal(TimeSpan.Zero, cache.MaxAge);
        using var ownJson = JsonDocument.Parse(await own.Content.ReadAsStringAsync());
        Assert.True(ownJson.RootElement.GetProperty("success").GetBoolean());

        // Contract compatibility: the future capability API must not be
        // mistaken for a registered runtime endpoint.
        using var nonexistent = Request("/api/v1/capacidades/evaluar", Token());
        using var noEndpoint = await client.SendAsync(nonexistent);
        Assert.Equal(HttpStatusCode.NotFound, noEndpoint.StatusCode);
    }
}
