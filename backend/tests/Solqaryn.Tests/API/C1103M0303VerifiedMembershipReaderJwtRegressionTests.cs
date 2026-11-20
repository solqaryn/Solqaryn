using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C11-03.M03.03 regression for the actual JwtService claim shape:
/// sub=login name, nombreUsuario=login name, id=positive numeric user id.
/// This runs the real JwtBearer handler and read-only DEV adapter via HTTP.
/// Membership and grant dependencies are mocked here; it does not certify
/// AppDbContext/MySQL 8.4 end-to-end or any outbox write/dispatch.
/// </summary>
public sealed class C1103M0303VerifiedMembershipReaderJwtRegressionTests
{
    private const string Issuer = "solqaryn-c1103-reader-test";
    private const string Audience = "solqaryn-c1103-reader-client";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c1103-jwt-reader-2026-test-only-signing-key-42chars"));
    private static readonly SymmetricSecurityKey ForgedKey = new(
        Encoding.UTF8.GetBytes("c1103-jwt-reader-2026-test-only-forged-key-42chars"));

    private static string Token(string subject, string userName, int actor = 911, bool genuine = true)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim("nombreUsuario", userName),
                new Claim("id", actor.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                genuine ? Key : ForgedKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<(IHost Host, Mock<IUsuarioScopeService> Scope,
        Mock<IPermisoService> Permission)> StartAsync(bool granted = true)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(s => s.ObtenerActualAsync(811, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(911, 811, 17, "TenantRole", false));
        scope.Setup(s => s.ObtenerActualAsync(812, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var permission = new Mock<IPermisoService>(MockBehavior.Strict);
        permission.Setup(p => p.TienePermisoAsync(
                811, ModuloSistema.Configuracion, AccionPermiso.Crear))
            .ReturnsAsync(granted);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OutboxPlanning:EnableReadOnlyAuthorizationProbe"] = "true"
            }).Build();
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHttpContextAccessor();
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddScoped<ICurrentUserService, CurrentUserService>();
                    services.AddSingleton<IUsuarioScopeService>(scope.Object);
                    services.AddSingleton<IPermisoService>(permission.Object);
                    services.AddScoped<C1103OutboxVerifiedMembershipReader>();
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            // Mirror the default production inbound-sub mapping.
                            options.MapInboundClaims = true;
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidIssuer = Issuer,
                                ValidateAudience = true,
                                ValidAudience = Audience,
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero,
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = Key,
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
                    app.UseEndpoints(endpoints => endpoints
                        .MapGet("/__c1103_test/reader/{tenantId:int}", async http =>
                        {
                            var reader = http.RequestServices
                                .GetRequiredService<C1103OutboxVerifiedMembershipReader>();
                            var tenant = Convert.ToInt32(http.Request.RouteValues["tenantId"],
                                System.Globalization.CultureInfo.InvariantCulture);
                            var authority = await reader.ReadCurrentAsync(
                                911, tenant, http.RequestAborted);
                            http.Response.StatusCode = authority is null
                                ? StatusCodes.Status403Forbidden
                                : StatusCodes.Status200OK;
                        }).RequireAuthorization());
                }))
            .StartAsync();
        return (host, scope, permission);
    }

    private static async Task<HttpResponseMessage> RequestAsync(
        HttpClient client, int tenant, string jwt)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/__c1103_test/reader/" + tenant);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.Add("X-Empresa-Id", "99999");
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task RealSignedJwtUsernameSubjectAndNumericIdAllowVerifiedTenantA()
    {
        var (host, scope, permission) = await StartAsync();
        using (host)
        using (var client = host.GetTestClient())
        using (var response = await RequestAsync(client, 811,
                   Token("user.c1103", "user.c1103")))
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        scope.Verify(s => s.ObtenerActualAsync(811, It.IsAny<CancellationToken>()),
            Times.Once);
        permission.Verify(p => p.TienePermisoAsync(
            811, ModuloSistema.Configuracion, AccionPermiso.Crear), Times.Once);
    }

    [Fact]
    public async Task SignedButConflictingUsernameSubjectCannotReachDatabase()
    {
        var (host, scope, permission) = await StartAsync();
        using (host)
        using (var client = host.GetTestClient())
        using (var response = await RequestAsync(client, 811,
                   Token("another-user", "user.c1103")))
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        scope.VerifyNoOtherCalls();
        permission.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidJwtCannotBorrowAnotherTenantOrRevokedGrant()
    {
        var (host, scope, permission) = await StartAsync(granted: false);
        using (host)
        using (var client = host.GetTestClient())
        {
            using var otherTenant = await RequestAsync(client, 812,
                Token("user.c1103", "user.c1103"));
            Assert.Equal(HttpStatusCode.Forbidden, otherTenant.StatusCode);
            using var revoked = await RequestAsync(client, 811,
                Token("user.c1103", "user.c1103"));
            Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        }
        permission.Verify(p => p.TienePermisoAsync(
            811, ModuloSistema.Configuracion, AccionPermiso.Crear), Times.Once);
    }

    [Fact]
    public async Task ForgedSignatureNeverEntersMembershipReader()
    {
        var (host, scope, permission) = await StartAsync();
        using (host)
        using (var client = host.GetTestClient())
        using (var response = await RequestAsync(client, 811,
                   Token("user.c1103", "user.c1103", genuine: false)))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        scope.VerifyNoOtherCalls();
        permission.VerifyNoOtherCalls();
    }
}
