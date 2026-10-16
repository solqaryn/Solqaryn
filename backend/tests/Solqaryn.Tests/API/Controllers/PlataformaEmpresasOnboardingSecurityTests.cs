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

public sealed class PlataformaEmpresasOnboardingSecurityTests
{
    [Fact]
    public void EndpointDePlataformaExigeJwt()
    {
        Assert.NotNull(Attribute.GetCustomAttribute(
            typeof(PlataformaEmpresasController), typeof(AuthorizeAttribute)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task AltaRechazadaPorCualquierGateApagado(
        bool onboarding, bool aislamiento)
    {
        var fixture = await PrepararAsync(onboarding, aislamiento, true);
        await using var db = fixture.Db;
        var result = await fixture.Controller.AltaControlada(new()
        {
            Nombre = "B",
            AdministradorUsuarioId = fixture.AdministradorId
        }, CancellationToken.None);
        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Empty(await db.Set<Empresa>().ToListAsync());
        Assert.Empty(await db.Set<UsuarioEmpresa>().ToListAsync());
        Assert.Empty(await db.Set<RegistroAuditoria>().ToListAsync());
    }

    [Fact]
    public async Task AdministradorEmpresarialSinRolDePlataformaNoPuedeCrearEmpresa()
    {
        var fixture = await PrepararAsync(true, true, false);
        await using var db = fixture.Db;
        var result = await fixture.Controller.AltaControlada(new()
        {
            Nombre = "B",
            AdministradorUsuarioId = fixture.AdministradorId
        }, CancellationToken.None);
        Assert.IsType<ForbidResult>(result);
        Assert.Empty(await db.Set<Empresa>().ToListAsync());
        Assert.Empty(await db.Set<UsuarioEmpresa>().ToListAsync());
    }

    [Fact]
    public async Task LaIdentidadDelJwtSinSesionActualVerificadaNoConcedeAlta()
    {
        var fixture = await PrepararAsync(true, true, true, currentUserValid: false);
        await using var db = fixture.Db;
        var result = await fixture.Controller.AltaControlada(new()
        {
            Nombre = "B",
            AdministradorUsuarioId = fixture.AdministradorId
        }, CancellationToken.None);
        Assert.IsType<ForbidResult>(result);
        Assert.Empty(await db.Set<Empresa>().ToListAsync());
    }

    [Fact]
    public async Task PlataformaCreaEmpresaYMembresiaAtomicaSinConcederAutoridadPlataformaAlTenant()
    {
        var fixture = await PrepararAsync(true, true, true);
        await using var db = fixture.Db;
        db.Set<Empresa>().Add(new Empresa("A histórica"));
        await db.SaveChangesAsync();

        var result = await fixture.Controller.AltaControlada(new()
        {
            Nombre = "  Empresa B  ",
            AdministradorUsuarioId = fixture.AdministradorId
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(created.Value);

        var companies = await db.Set<Empresa>()
            .IgnoreQueryFilters().OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, companies.Count);
        var nueva = Assert.Single(companies.Where(x => x.Nombre == "Empresa B"));
        Assert.True(nueva.Activa);
        Assert.Equal(fixture.ActorId, nueva.CreadoPorUsuarioId);

        var member = Assert.Single(await db.Set<UsuarioEmpresa>().ToListAsync());
        Assert.True(member.Activa);
        Assert.Equal(nueva.Id, member.EmpresaId);
        Assert.Equal(fixture.AdministradorId, member.UsuarioId);
        var role = await db.Roles.SingleAsync(x => x.Id == member.RolId);
        Assert.Equal(AmbitoAutorizacion.Empresa, role.Ambito);
        Assert.True(role.EsAdministrador);
        Assert.Null(role.EmpresaId);

        var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Equal(nueva.Id, audit.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
        Assert.Equal(OrigenAutorizacion.RolPlataforma, audit.OrigenAutorizacion);
        Assert.Equal(fixture.ActorId, audit.UsuarioId);
        Assert.Equal(AccionPermiso.Crear, audit.Accion);
    }

    [Fact]
    public async Task NoCreaEmpresaConAdministradorInactivoNiRepiteNombre()
    {
        var fixture = await PrepararAsync(true, true, true);
        await using var db = fixture.Db;
        var admin = await db.Usuarios.SingleAsync(x => x.Id == fixture.AdministradorId);
        admin.Activo = false;
        await db.SaveChangesAsync();

        var result = await fixture.Controller.AltaControlada(new()
        {
            Nombre = "B", AdministradorUsuarioId = fixture.AdministradorId
        }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Set<Empresa>().ToListAsync());

        admin.Activo = true;
        db.Set<Empresa>().Add(new Empresa("B"));
        await db.SaveChangesAsync();
        result = await fixture.Controller.AltaControlada(new()
        {
            Nombre = "B", AdministradorUsuarioId = fixture.AdministradorId
        }, CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(await db.Set<Empresa>().ToListAsync());
        Assert.Empty(await db.Set<UsuarioEmpresa>().ToListAsync());
    }

    [Fact]
    public async Task AltaAtomicaActivaSoloIdentidadPendienteCreadaPorOperadorGlobal()
    {
        var fixture = await PrepararAsync(true, true, true);
        await using var db = fixture.Db;
        var usuario = await db.Usuarios.SingleAsync(u => u.Id == fixture.AdministradorId);
        usuario.Activo = false;
        usuario.CreadoPorUsuarioId = fixture.ActorId;
        await db.SaveChangesAsync();

        var respuesta = await fixture.Controller.AltaControlada(new()
        {
            Nombre = "Empresa nueva mínima",
            AdministradorUsuarioId = fixture.AdministradorId
        }, CancellationToken.None);

        Assert.IsType<CreatedResult>(respuesta);
        Assert.True(usuario.Activo);
        var empresa = Assert.Single(await db.Set<Empresa>().ToListAsync());
        var membresia = Assert.Single(await db.Set<UsuarioEmpresa>().ToListAsync());
        Assert.Equal(empresa.Id, membresia.EmpresaId);
        Assert.Equal(fixture.AdministradorId, membresia.UsuarioId);
        Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
    }

    [Fact]
    public async Task MiddlewareSoloDejaPasarRutaDePlataformaExactaConDobleGateYJwt()
    {
        var fixture = await PrepararAsync(true, true, true);
        await using var db = fixture.Db;
        db.Set<Empresa>().AddRange(new Empresa("A"), new Empresa("B"));
        await db.SaveChangesAsync();

        var calls = 0;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx =>
        {
            calls++;
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        async Task<int> RequestAsync(string method, string path,
            bool auth, bool onboarding, bool certified)
        {
            var settings = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PlatformOnboardingEnabled"] = onboarding.ToString(),
                    ["MultiEmpresa:CommercialIsolationCertified"] = certified.ToString()
                }).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(settings).BuildServiceProvider();
            var ctx = new DefaultHttpContext { RequestServices = services };
            ctx.Request.Method = method;
            ctx.Request.Path = path;
            ctx.Response.Body = new MemoryStream();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                auth ? new[] { new Claim(ClaimTypes.NameIdentifier, "7") } :
                       Array.Empty<Claim>(), auth ? "TestJwt" : null));
            await middleware.InvokeAsync(ctx, db);
            return ctx.Response.StatusCode;
        }

        Assert.Equal(503, await RequestAsync("POST",
            "/plataforma/empresas/alta-controlada", true, false, true));
        Assert.Equal(503, await RequestAsync("POST",
            "/plataforma/empresas/alta-controlada", true, true, false));
        Assert.Equal(503, await RequestAsync("POST",
            "/plataforma/empresas/alta-controlada", false, true, true));
        Assert.Equal(503, await RequestAsync("POST",
            "/empresas", true, true, true));
        Assert.Equal(503, await RequestAsync("POST",
            "/plataforma/empresas/alta-controlada/extra", true, true, true));
        Assert.Equal(503, await RequestAsync("GET",
            "/plataforma/empresas/alta-controlada", true, true, true));
        Assert.Equal(204, await RequestAsync("POST",
            "/plataforma/empresas/alta-controlada", true, true, true));
        Assert.Equal(1, calls);
    }

    private static async Task<Fixture> PrepararAsync(
        bool onboarding, bool aislamiento, bool platformMembership,
        bool currentUserValid = true)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("platform-onboarding-" + Guid.NewGuid().ToString("N"))
            .Options);
        var rolePlatform = new Rol
        {
            Nombre = "PLATFORM_ADMIN_TEST",
            NombreNormalizado = "PLATFORM_ADMIN_TEST",
            Ambito = AmbitoAutorizacion.Plataforma,
            EsAdministrador = true, EsSistema = true, Activo = true
        };
        var roleEmpresa = new Rol
        {
            Nombre = "ADMIN_EMPRESA_TEST",
            NombreNormalizado = "ADMIN_EMPRESA_TEST",
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = true, EsSistema = true, Activo = true
        };
        db.Roles.AddRange(rolePlatform, roleEmpresa);
        await db.SaveChangesAsync();
        var actor = new Usuario
        {
            NombreUsuario = "platform-test", NombreCompleto = "Operador Plataforma",
            RolId = rolePlatform.Id, PasswordHash = "test-not-a-real-credential",
            Activo = true
        };
        var administrator = new Usuario
        {
            NombreUsuario = "empresa-admin-test", NombreCompleto = "Operador Empresa",
            RolId = roleEmpresa.Id, PasswordHash = "test-not-a-real-credential",
            Activo = true
        };
        db.Usuarios.AddRange(actor, administrator);
        await db.SaveChangesAsync();
        if (platformMembership)
        {
            db.Set<UsuarioRolPlataforma>().Add(
                new UsuarioRolPlataforma(actor.Id, rolePlatform.Id));
            await db.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["MultiEmpresa:PlatformOnboardingEnabled"] = onboarding.ToString(),
                ["MultiEmpresa:CommercialIsolationCertified"] = aislamiento.ToString()
            }).Build();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(currentUserValid);
        current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(actor.NombreUsuario);
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString()) },
            "TestJwt"));
        var controller = new PlataformaEmpresasController(db, current.Object, config,
            new PlataformaScopeService(db, current.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        return new Fixture(db, controller, actor.Id, administrator.Id);
    }

    private sealed record Fixture(
        AppDbContext Db, PlataformaEmpresasController Controller,
        int ActorId, int AdministradorId);
}
