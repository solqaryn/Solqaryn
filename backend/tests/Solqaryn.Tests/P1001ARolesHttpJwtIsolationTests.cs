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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// HTTP real sobre TestServer, JWT firmado solo para este ensayo y controlador
/// RolesController real. Se simula la concesion de permisos: NO demuestra
/// membresias fisicas ni indices de MySQL DEV, ni sustituye ese gate.
/// </summary>
public sealed class P1001ARolesHttpJwtIsolationTests
{
    private const string Issuer = "solqaryn-p1001a-local-tests";
    private const string Audience = "solqaryn-p1001a-local-tests";
    private static readonly SymmetricSecurityKey TestKey = new(
        Encoding.UTF8.GetBytes("p1001a-only-local-test-signing-key-no-production-credentials-2026"));

    [Fact]
    public async Task JwtDeA_PasaPorController_SinExponerRolDeB()
    {
        var accessor = new HttpContextAccessor();
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(p => p.VerificarPermisoAsync(
                It.IsAny<int>(), ModuloSistema.Roles, AccionPermiso.Ver))
            .Returns((int empresaId, ModuloSistema _, AccionPermiso _) =>
            {
                var claimEmpresa = accessor.HttpContext?.User.FindFirst("empresa_test")?.Value;
                if (claimEmpresa != empresaId.ToString(CultureInfo.InvariantCulture))
                    throw new ForbiddenAccessException("Tenant no autorizado en fixture HTTP.");
                return Task.CompletedTask;
            });

        var dbName = "p1001a-jwt-http-" + Guid.NewGuid().ToString("N");
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IHttpContextAccessor>(accessor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
                services.AddScoped<IRolRepository, RolRepository>();
                services.AddScoped<IRolService, RolService>();
                services.AddScoped<IAuditoriaService>(_ => Mock.Of<IAuditoriaService>());
                services.AddScoped<ICurrentUserService>(_ => Mock.Of<ICurrentUserService>());
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
                services.AddControllers().AddApplicationPart(typeof(RolesController).Assembly);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }));

        int empresaAId;
        int empresaBId;
        int rolAId;
        int rolBId;
        using (var scope = server.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            accessor.HttpContext = new DefaultHttpContext();
            var empresaA = new Empresa("P1001A HTTP EMPRESA A");
            var empresaB = new Empresa("P1001A HTTP EMPRESA B");
            db.Set<Empresa>().AddRange(empresaA, empresaB);
            await db.SaveChangesAsync();

            empresaAId = empresaA.Id;
            empresaBId = empresaB.Id;
            accessor.HttpContext.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaAId;
            var rolA = new Rol
            {
                Nombre = "P1001A_HTTP_ROL_PROPIO_A",
                NombreNormalizado = "p1001a_http_rol_propio_a",
                EmpresaId = empresaAId,
                Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false
            };
            db.Roles.Add(rolA);
            await db.SaveChangesAsync();
            rolAId = rolA.Id;

            accessor.HttpContext.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaBId;
            var rolB = new Rol
            {
                Nombre = "P1001A_HTTP_ROL_PRIVADO_B",
                NombreNormalizado = "p1001a_http_rol_privado_b",
                EmpresaId = empresaBId,
                Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false
            };
            db.Roles.Add(rolB);
            await db.SaveChangesAsync();
            rolBId = rolB.Id;
            accessor.HttpContext = null;
        }

        using var client = server.CreateClient();
        var tokenA = CrearJwtPrueba(empresaAId);
        var tokenB = CrearJwtPrueba(empresaBId);

        using var ajeno = await PedirRol(client, rolBId, empresaAId, tokenA);
        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);

        using var propioA = await PedirRol(client, rolAId, empresaAId, tokenA);
        Assert.Equal(HttpStatusCode.OK, propioA.StatusCode);

        using var propioB = await PedirRol(client, rolBId, empresaBId, tokenB);
        Assert.Equal(HttpStatusCode.OK, propioB.StatusCode);

        using var listado = await PedirRol(client, null, empresaAId, tokenA);
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        var cuerpo = await listado.Content.ReadAsStringAsync();
        Assert.Contains("P1001A_HTTP_ROL_PROPIO_A", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("P1001A_HTTP_ROL_PRIVADO_B", cuerpo, StringComparison.Ordinal);

        using var sinJwt = await PedirRol(client, rolAId, empresaAId, null);
        Assert.Equal(HttpStatusCode.Unauthorized, sinJwt.StatusCode);

        // Cada permiso autorizado indica que el filtro alcanzo la ruta MVC;
        // el 404 ajeno procede del controlador y repositorio EF reales.
        permisos.Verify(p => p.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Roles, AccionPermiso.Ver),
            Times.Exactly(4));
    }

    private static async Task<HttpResponseMessage> PedirRol(
        HttpClient client, int? rolId, int empresaId, string? jwt)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, rolId is int id ? $"/roles/{id}" : "/roles");
        request.Headers.Add("X-Empresa-Id", empresaId.ToString(CultureInfo.InvariantCulture));
        if (jwt is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return await client.SendAsync(request);
    }

    private static string CrearJwtPrueba(int empresaId)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "p1001a-test-user-" + empresaId),
                new Claim("empresa_test", empresaId.ToString(CultureInfo.InvariantCulture))
            },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(TestKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
