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

namespace Solqaryn.Tests;

/// <summary>
/// P1-010.B: JWT real en TestServer + controlador y UsuarioScopeService reales.
/// La persistencia de esta suite es EF InMemory aislado; NO representa un
/// despliegue ni una prueba MySQL/HTTP remota del entorno DEV.
/// </summary>
public sealed class P1010BMultiEmpresaHttpJwtCertificationTests
{
    private const string Issuer = "solqaryn-p1010b-local-test";
    private const string Audience = "solqaryn-p1010b-local-test";
    private static readonly SymmetricSecurityKey TestKey = new(
        Encoding.UTF8.GetBytes("p1010b-local-test-signing-key-never-for-production-2026"));

    [Fact]
    public async Task JwtConRolAdminDeA_NuncaElevaPermisosDeB()
    {
        using var escenario = await CrearEscenarioAsync();
        using var cliente = escenario.Host.GetTestClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CrearJwtPrueba(escenario.UsuarioId, escenario.RolAdminAId));

        using var listado = await cliente.GetAsync("/tenant-context/empresas");
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        using (var json = JsonDocument.Parse(await listado.Content.ReadAsStringAsync()))
        {
            var empresas = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
            Assert.Equal(2, empresas.Length);
            Assert.Contains(empresas, x => x.GetProperty("empresaId").GetInt32() == escenario.EmpresaAId);
            Assert.Contains(empresas, x => x.GetProperty("empresaId").GetInt32() == escenario.EmpresaBId);
            Assert.DoesNotContain(empresas, x => x.GetProperty("empresaId").GetInt32() == escenario.EmpresaAjenaId);
        }

        using var respuestaA = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaAId}");
        Assert.Equal(HttpStatusCode.OK, respuestaA.StatusCode);
        using (var json = JsonDocument.Parse(await respuestaA.Content.ReadAsStringAsync()))
        {
            var data = json.RootElement.GetProperty("data");
            Assert.Equal(escenario.RolAdminAId, data.GetProperty("rolId").GetInt32());
            Assert.True(data.GetProperty("esAdministrador").GetBoolean());
        }

        using var respuestaB = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaBId}");
        Assert.Equal(HttpStatusCode.OK, respuestaB.StatusCode);
        using (var json = JsonDocument.Parse(await respuestaB.Content.ReadAsStringAsync()))
        {
            var data = json.RootElement.GetProperty("data");
            Assert.Equal(escenario.RolConsultaBId, data.GetProperty("rolId").GetInt32());
            Assert.False(data.GetProperty("esAdministrador").GetBoolean());
        }

        using var ajena = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaAjenaId}");
        Assert.Equal(HttpStatusCode.Forbidden, ajena.StatusCode);
    }

    [Fact]
    public async Task RevocacionYReactivacionDeB_SeAplicanEnCadaPeticionJwt()
    {
        using var escenario = await CrearEscenarioAsync();
        using var cliente = escenario.Host.GetTestClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CrearJwtPrueba(escenario.UsuarioId, escenario.RolAdminAId));

        await using (var scope = escenario.Host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membresiaB = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == escenario.UsuarioId && m.EmpresaId == escenario.EmpresaBId);
            membresiaB.Desactivar();
            await db.SaveChangesAsync();
        }

        using (var revocada = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaBId}"))
            Assert.Equal(HttpStatusCode.Forbidden, revocada.StatusCode);
        using (var aSigueVigente = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaAId}"))
            Assert.Equal(HttpStatusCode.OK, aSigueVigente.StatusCode);

        await using (var scope = escenario.Host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membresiaB = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == escenario.UsuarioId && m.EmpresaId == escenario.EmpresaBId);
            membresiaB.Activar();
            await db.SaveChangesAsync();
        }

        using var reactivada = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaBId}");
        Assert.Equal(HttpStatusCode.OK, reactivada.StatusCode);
        using var json = JsonDocument.Parse(await reactivada.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("data").GetProperty("esAdministrador").GetBoolean());
    }

    [Fact]
    public async Task RolPrivadoDeAAplicadoAB_SeRechazaSinRevocarA()
    {
        using var escenario = await CrearEscenarioAsync();
        using var cliente = escenario.Host.GetTestClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CrearJwtPrueba(escenario.UsuarioId, escenario.RolAdminAId));

        await using (var scope = escenario.Host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membresiaB = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == escenario.UsuarioId && m.EmpresaId == escenario.EmpresaBId);
            membresiaB.CambiarRol(escenario.RolAdminAId);
            await db.SaveChangesAsync();
        }

        using var b = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaBId}");
        Assert.Equal(HttpStatusCode.Forbidden, b.StatusCode);
        using var a = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaAId}");
        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
    }

    [Fact]
    public async Task TokenNoAutenticadoYUsuarioBloqueado_FallanCerrados()
    {
        using var escenario = await CrearEscenarioAsync();
        using var cliente = escenario.Host.GetTestClient();
        using (var anonimo = await cliente.GetAsync("/tenant-context/empresas"))
            Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CrearJwtPrueba(escenario.UsuarioId, escenario.RolAdminAId));

        await using (var scope = escenario.Host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == escenario.UsuarioId);
            usuario.Bloqueado = true;
            await db.SaveChangesAsync();
        }

        using var listado = await cliente.GetAsync("/tenant-context/empresas");
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        using (var json = JsonDocument.Parse(await listado.Content.ReadAsStringAsync()))
            Assert.Empty(json.RootElement.GetProperty("data").EnumerateArray());

        using var contexto = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaAId}");
        Assert.Equal(HttpStatusCode.Forbidden, contexto.StatusCode);
    }

    /// <summary>
    /// P2-018.C: smoke HTTP real con JWT de prueba y dos empresas, sin acceder a DEV.
    /// La cabecera de cliente es no autoritativa frente a la membresia del servidor.
    /// </summary>
    [Fact]
    public async Task SmokeHttpAB_HeaderFalsificado_NoCambiaRolNiPermiteTenantAjeno()
    {
        using var escenario = await CrearEscenarioAsync();
        using var cliente = escenario.Host.GetTestClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CrearJwtPrueba(escenario.UsuarioId, escenario.RolAdminAId));

        // Cabecera B forzada, pero la ruta A conserva su rol verdadero.
        cliente.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Empresa-Id", escenario.EmpresaBId.ToString());
        using (var a = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaAId}"))
        {
            Assert.Equal(HttpStatusCode.OK, a.StatusCode);
            using var json = JsonDocument.Parse(await a.Content.ReadAsStringAsync());
            var datos = json.RootElement.GetProperty("data");
            Assert.Equal(escenario.RolAdminAId, datos.GetProperty("rolId").GetInt32());
            Assert.True(datos.GetProperty("esAdministrador").GetBoolean());
        }

        // La misma sesion pide B: ni la cabecera A ni el rol admin de A elevan B.
        cliente.DefaultRequestHeaders.Remove("X-Empresa-Id");
        cliente.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Empresa-Id", escenario.EmpresaAId.ToString());
        using (var b = await cliente.GetAsync($"/tenant-context/{escenario.EmpresaBId}"))
        {
            Assert.Equal(HttpStatusCode.OK, b.StatusCode);
            using var json = JsonDocument.Parse(await b.Content.ReadAsStringAsync());
            var datos = json.RootElement.GetProperty("data");
            Assert.Equal(escenario.RolConsultaBId, datos.GetProperty("rolId").GetInt32());
            Assert.False(datos.GetProperty("esAdministrador").GetBoolean());
        }

        // Una tercera empresa sin membresia siempre se deniega.
        using var ajena = await cliente.GetAsync(
            $"/tenant-context/{escenario.EmpresaAjenaId}");
        Assert.Equal(HttpStatusCode.Forbidden, ajena.StatusCode);
    }

    private static string CrearJwtPrueba(int usuarioId, int rolAdminAId)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim("id", usuarioId.ToString()),
                new Claim("rolId", rolAdminAId.ToString()),
                new Claim("esAdministrador", "true")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(TestKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record Escenario(
        IHost Host,
        int UsuarioId,
        int EmpresaAId,
        int EmpresaBId,
        int EmpresaAjenaId,
        int RolAdminAId,
        int RolConsultaBId) : IDisposable
    {
        public void Dispose() => Host.Dispose();
    }

    private static async Task<Escenario> CrearEscenarioAsync()
    {
        var baseName = "p1010b-" + Guid.NewGuid().ToString("N");
        var host = await new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(baseName));
                    services.AddScoped<ICurrentUserService, CurrentUserService>();
                    services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
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
                    services.AddControllers().AddApplicationPart(typeof(TenantContextController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var empresaA = new Empresa("P1010B - Empresa A");
        var empresaB = new Empresa("P1010B - Empresa B");
        var ajena = new Empresa("P1010B - Empresa ajena");
        db.Set<Empresa>().AddRange(empresaA, empresaB, ajena);
        await db.SaveChangesAsync();

        var adminA = new Rol
        {
            Nombre = "Admin A de prueba",
            NombreNormalizado = "P1010B_ADMIN_A",
            EmpresaId = empresaA.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = true,
            Activo = true
        };
        var consultaB = new Rol
        {
            Nombre = "Consulta B de prueba",
            NombreNormalizado = "P1010B_CONSULTA_B",
            EmpresaId = empresaB.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = false,
            Activo = true
        };
        db.Roles.AddRange(adminA, consultaB);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p1010b-prueba",
            NombreCompleto = "Prueba P1-010.B",
            PasswordHash = "hash-ficticio-para-test",
            RolId = adminA.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.UsuarioEmpresas.AddRange(
            new UsuarioEmpresa(usuario.Id, empresaA.Id, adminA.Id),
            new UsuarioEmpresa(usuario.Id, empresaB.Id, consultaB.Id));
        await db.SaveChangesAsync();
        return new Escenario(
            host, usuario.Id, empresaA.Id, empresaB.Id, ajena.Id, adminA.Id, consultaB.Id);
    }
}
