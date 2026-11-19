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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C15-05.M05.02: TestServer real + JWT firmado de prueba + MVC controller/filter,
/// middleware de excepciones productivo y DTO validado en HTTP.
/// Permisos y servicio de datos son fixtures: NO certifica grants/MySQL/Render/PROD.
/// </summary>
public sealed class C1505M0502AuditHttpSecurityTests
{
    private const string Issuer = "solqaryn-c1505-audit-http-test";
    private const string Audience = "solqaryn-c1505-audit-http-test";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("v20-c1505-javier30-test-jwt-key-only-never-for-production-2026"));

    private static Mock<IAuditoriaService> Reader()
    {
        var service = new Mock<IAuditoriaService>(MockBehavior.Strict);
        service.Setup(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()))
            .ReturnsAsync(new PagedResult<RegistroAuditoriaDto>
            {
                Page = 1, PageSize = 50, TotalCount = 0
            });
        return service;
    }

    private static async Task<IHost> StartAsync(Mock<IAuditoriaService> reader)
    {
        var accessor = new HttpContextAccessor();
        var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
        current.SetupGet(x => x.EsAdministrador).Returns(() =>
            accessor.HttpContext?.User.HasClaim("audit_admin", "true") == true);

        var permission = new Mock<IPermisoService>(MockBehavior.Strict);
        permission.Setup(x => x.VerificarPermisoAsync(
                It.IsAny<int>(), ModuloSistema.Auditoria, AccionPermiso.Ver))
            .Returns((int empresaId, ModuloSistema _, AccionPermiso _) =>
            {
                var selected = accessor.HttpContext?.User.FindFirst("tenant_fixture")?.Value;
                if (selected != empresaId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    throw new ForbiddenAccessException("Tenant fixture sin permiso.");
                return Task.CompletedTask;
            });

        return await new HostBuilder().ConfigureWebHost(web =>
            web.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IHttpContextAccessor>(accessor);
                    services.AddScoped(_ => reader.Object);
                    services.AddScoped(_ => current.Object);
                    services.AddScoped(_ => permission.Object);
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options => options.TokenValidationParameters =
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
                    services.AddControllers()
                        .AddApplicationPart(typeof(AuditoriaController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                })).StartAsync();
    }

    private static string Token(int tenantId, bool admin)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "audit-test-actor"),
                new Claim("tenant_fixture",
                    tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim("audit_admin", admin ? "true" : "false")
            },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<HttpResponseMessage> GetAsync(
        HttpClient client, string? bearer, string query = "", int selectedTenant = 21)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auditoria" + query);
        request.Headers.Add("X-Empresa-Id",
            selectedTenant.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task SinJwtSeRechaza401SinConsultarAuditoria()
    {
        var reader = Reader();
        using var host = await StartAsync(reader);
        using var client = host.GetTestClient();
        using var response = await GetAsync(client, null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        reader.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task JwtNoAdministradorSeRechaza403SinConsultarAuditoria()
    {
        var reader = Reader();
        using var host = await StartAsync(reader);
        using var client = host.GetTestClient();
        using var response = await GetAsync(client, Token(21, false));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        reader.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task JwtEmpresaBNoPuedeUsarSelectorA()
    {
        var reader = Reader();
        using var host = await StartAsync(reader);
        using var client = host.GetTestClient();
        using var response = await GetAsync(client, Token(22, true), selectedTenant: 21);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        reader.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task JwtAdministradorTenantPropioRecibe200YRespuestaTipada()
    {
        var reader = Reader();
        using var host = await StartAsync(reader);
        using var client = host.GetTestClient();
        using var response = await GetAsync(client, Token(21, true), "?Page=1&PageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":true", await response.Content.ReadAsStringAsync());
        reader.Verify(x => x.GetFilteredAsync(
            It.Is<AuditoriaFiltroDto>(p => p.Page == 1 && p.PageSize == 50)), Times.Once);
    }

    [Fact]
    public async Task ErrorDeServicioSale500SaneadoSinExponerSentinel()
    {
        const string sentinel = "INTERNAL_TEST_SENTINEL_C1505_PRIVATE";
        var reader = Reader();
        reader.Setup(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()))
            .ThrowsAsync(new InvalidOperationException(sentinel));
        using var host = await StartAsync(reader);
        using var client = host.GetTestClient();
        using var response = await GetAsync(client, Token(21, true));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(sentinel, body, StringComparison.Ordinal);
        Assert.Contains("Ocurrió un error interno", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?PageSize=101")]
    [InlineData("?PageSize=2147483647")]
    [InlineData("?Page=0")]
    [InlineData("?Page=-1")]
    [InlineData("?Page=100001")]
    public async Task PaginacionFueraDeRangoDa400SinConsultarDatos(string query)
    {
        var reader = Reader();
        using var host = await StartAsync(reader);
        using var client = host.GetTestClient();
        using var response = await GetAsync(client, Token(21, true), query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        reader.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task BordePagina100PasaSinSobrepasarLimite()
    {
        var reader = Reader();
        using var host = await StartAsync(reader);
        using var client = host.GetTestClient();
        using var response = await GetAsync(client, Token(21, true), "?Page=100000&PageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        reader.Verify(x => x.GetFilteredAsync(
            It.Is<AuditoriaFiltroDto>(p => p.Page == 100000 && p.PageSize == 100)), Times.Once);
    }
}
