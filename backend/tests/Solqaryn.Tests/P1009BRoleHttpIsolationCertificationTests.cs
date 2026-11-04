using System.Globalization;
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
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-009.B: certificacion HTTP negativa con JWT de prueba, controlador, filtro
/// de permisos, servicio y middleware reales. Repositorio simulado: no demuestra
/// indices fisicos ni membresias en MySQL DEV y no altera datos productivos.
/// </summary>
public sealed class P1009BRoleHttpIsolationCertificationTests
{
    private const string Issuer = "solqaryn-p1009b-test";
    private const string Audience = "solqaryn-p1009b-test";
    private static readonly SymmetricSecurityKey TestKey = new(
        Encoding.UTF8.GetBytes("p1009b-local-only-test-jwt-key-no-prod-secrets-2026"));

    [Theory]
    [InlineData(22, AmbitoAutorizacion.Empresa, false)]
    [InlineData(null, AmbitoAutorizacion.Plataforma, true)]
    [InlineData(null, AmbitoAutorizacion.Empresa, false)]
    public async Task P1009B_DuplicarRolAjenoGlobalOHuerfano_Responde400SinEscrituras(
        int? empresaPropietaria, AmbitoAutorizacion ambito, bool esSistema)
    {
        const int empresaAutorizada = 11;
        var accessor = new HttpContextAccessor();
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(p => p.VerificarPermisoAsync(
                It.IsAny<int>(), ModuloSistema.Roles, AccionPermiso.Duplicar))
            .Returns((int empresaId, ModuloSistema modulo, AccionPermiso accion) =>
            {
                var claim = accessor.HttpContext?.User.FindFirst("empresa_test")?.Value;
                if (empresaId != empresaAutorizada ||
                    claim != empresaAutorizada.ToString(CultureInfo.InvariantCulture))
                    throw new ForbiddenAccessException("Empresa no autorizada en el fixture.");
                return Task.CompletedTask;
            });

        var rolNoAsignable = new Rol
        {
            Id = 42,
            Nombre = "Rol externo de prueba",
            NombreNormalizado = "rol_externo_de_prueba",
            EmpresaId = empresaPropietaria,
            Ambito = ambito,
            EsSistema = esSistema,
            Activo = true
        };
        Assert.False(rolNoAsignable.EsAsignableAEmpresa(empresaAutorizada));

        var repositorio = new Mock<IRolRepository>(MockBehavior.Strict);
        repositorio.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(rolNoAsignable);

        using var host = await new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IHttpContextAccessor>(accessor);
                    services.AddScoped<IRolService>(_ => new RolService(
                        repositorio.Object, Mock.Of<IAuditoriaService>(),
                        Mock.Of<ICurrentUserService>()));
                    services.AddScoped<IPermisoService>(_ => permisos.Object);
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = Issuer,
                                ValidAudience = Audience,
                                IssuerSigningKey = TestKey,
                                ClockSkew = TimeSpan.Zero
                            };
                        });
                    services.AddAuthorization();
                    services.AddControllers()
                        .AddApplicationPart(typeof(RolesController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();

        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/roles/42/duplicar")
        {
            Content = new StringContent(
                "{\"nuevoNombre\":\"Copia que debe rechazarse\"}",
                Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Empresa-Id", empresaAutorizada.ToString(CultureInfo.InvariantCulture));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", CrearJwtPrueba(empresaAutorizada));

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // Una regla de negocio rechazada debe llegar a HTTP 4xx, nunca 5xx
        // ni 201. El texto prueba que no fue solo un error de model binding.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("No se puede duplicar", body, StringComparison.OrdinalIgnoreCase);
        repositorio.Verify(r => r.GetByIdAsync(42), Times.Once);
        repositorio.Verify(r => r.AddAsync(It.IsAny<Rol>()), Times.Never);
        repositorio.Verify(r => r.SaveChangesAsync(), Times.Never);
        permisos.Verify(p => p.VerificarPermisoAsync(
            empresaAutorizada, ModuloSistema.Roles, AccionPermiso.Duplicar), Times.Once);
    }

    private static string CrearJwtPrueba(int empresaId)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "p1009b-user-" + empresaId),
                new Claim("empresa_test", empresaId.ToString(CultureInfo.InvariantCulture))
            },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(TestKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
