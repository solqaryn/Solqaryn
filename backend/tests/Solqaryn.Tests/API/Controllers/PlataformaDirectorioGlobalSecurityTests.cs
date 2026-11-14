using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// Microfase 4: directorio global seguro con Empresa A inactiva inactiva.
/// No habilita onboarding, CRUD, información comercial ni roles empresariales.
/// </summary>
public sealed class PlataformaDirectorioGlobalSecurityTests
{
    [Fact]
    public void EndpointSoloExisteBajoControladorAutenticado()
    {
        Assert.NotNull(Attribute.GetCustomAttribute(
            typeof(PlataformaEmpresasController), typeof(AuthorizeAttribute)));
        var ruta = Attribute.GetCustomAttribute(
            typeof(PlataformaEmpresasController), typeof(RouteAttribute)) as RouteAttribute;
        Assert.Equal("plataforma/empresas", ruta?.Template);
    }

    [Fact]
    public async Task PlataformaConempresaInactivaInactivaLeeDirectorioSinActivarTenants()
    {
        var (db, controller, empresa, actorId) = await FixtureAsync(global: true);
        await using (db)
        {
            var respuesta = Assert.IsType<OkObjectResult>(
                await controller.Listar(1, 25, CancellationToken.None));
            var payload = System.Text.Json.JsonSerializer.Serialize(respuesta.Value);
            Assert.Contains("Empresa A inactiva", payload);
            Assert.Contains("\"activa\":false", payload);
            Assert.Contains("\"puedeCrear\":false", payload);
            Assert.False(empresa.Activa);
            var log = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
            Assert.Null(log.EmpresaId);
            Assert.Equal(actorId, log.UsuarioId);
            Assert.Equal(AmbitoAutorizacion.Plataforma, log.AmbitoAutoridad);
        }
    }

    [Fact]
    public async Task AdministradorEmpresarialNoConsultaDirectorioGlobal()
    {
        var (db, controller, _, _) = await FixtureAsync(global: false);
        await using (db)
        {
            Assert.IsType<ForbidResult>(
                await controller.Listar(1, 25, CancellationToken.None));
            Assert.Empty(await db.Set<RegistroAuditoria>().ToListAsync());
        }
    }

    [Fact]
    public async Task RechazaPaginacionNoAcotadaSinFiltrarDatos()
    {
        var (db, controller, _, _) = await FixtureAsync(global: true);
        await using (db)
        {
            Assert.IsType<BadRequestObjectResult>(
                await controller.Listar(0, 25, CancellationToken.None));
            Assert.IsType<BadRequestObjectResult>(
                await controller.Listar(1, 51, CancellationToken.None));
            Assert.Empty(await db.Set<RegistroAuditoria>().ToListAsync());
        }
    }

    [Fact]
    public async Task MiddlewareNoAmpliaRutasGlobalesNiComerciales()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-directory-middleware-" + Guid.NewGuid().ToString("N")).Options);
        var empresaInactiva = new Empresa("Empresa A inactiva");
        var empresaB = new Empresa("Empresa B");
        empresaInactiva.Desactivar();
        db.Set<Empresa>().AddRange(empresaInactiva, empresaB);
        await db.SaveChangesAsync();
        var calls = 0;
        var guard = new UnscopedCommercialTenantGuardMiddleware(ctx =>
        {
            calls++;
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        async Task<int> Status(string verb, string ruta)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Method = verb;
            ctx.Request.Path = ruta;
            ctx.Response.Body = new MemoryStream();
            await guard.InvokeAsync(ctx, db);
            return ctx.Response.StatusCode;
        }

        Assert.Equal(204, await Status("GET", "/plataforma/empresas"));
        Assert.Equal(503, await Status("POST", "/plataforma/empresas"));
        Assert.Equal(503, await Status("GET", "/plataforma/empresas/1"));
        Assert.Equal(503, await Status("GET", "/plataforma/usuarios"));
        Assert.Equal(503, await Status("GET", "/clientes"));
        Assert.Equal(503, await Status("POST", "/empresas"));
        Assert.Equal(1, calls);
    }

    private static async Task<(AppDbContext Db, PlataformaEmpresasController Controller,
        Empresa Empresa, int ActorId)> FixtureAsync(bool global)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-directory-" + Guid.NewGuid().ToString("N")).Options);
        var platform = new Rol { Nombre = "Global", NombreNormalizado = "P4_GLOBAL",
            EsSistema = true, EsAdministrador = true, Activo = true,
            Ambito = AmbitoAutorizacion.Plataforma };
        var enterprise = new Rol { Nombre = "Enterprise", NombreNormalizado = "P4_ENTERPRISE",
            EsSistema = true, EsAdministrador = true, Activo = true,
            Ambito = AmbitoAutorizacion.Empresa };
        db.Roles.AddRange(platform, enterprise);
        await db.SaveChangesAsync();
        var user = new Usuario { NombreUsuario = global ? "global" : "soloempresa",
            NombreCompleto = "Operador", RolId = enterprise.Id,
            PasswordHash = "not-credential", Activo = true };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        if (global)
            db.Set<UsuarioRolPlataforma>().Add(new UsuarioRolPlataforma(user.Id, platform.Id));
        var empresaInactiva = new Empresa("Empresa A inactiva");
        empresaInactiva.Desactivar();
        db.Set<Empresa>().Add(empresaInactiva);
        await db.SaveChangesAsync();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(user.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(user.NombreUsuario);
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> {
                ["MultiEmpresa:CommercialIsolationCertified"] = "false",
                ["MultiEmpresa:PlatformOnboardingEnabled"] = "false"
            }).Build();
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "TestJWT"));
        var controller = new PlataformaEmpresasController(db, current.Object, config,
            new PlataformaScopeService(db, current.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        return (db, controller, empresaInactiva, user.Id);
    }
}
