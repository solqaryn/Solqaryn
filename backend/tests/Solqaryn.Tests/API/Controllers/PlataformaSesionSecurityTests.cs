using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class PlataformaSesionSecurityTests
{
    [Fact]
    public void RutaDePlataformaExigeJWT()
    {
        Assert.NotNull(Attribute.GetCustomAttribute(
            typeof(PlataformaSesionController), typeof(AuthorizeAttribute)));
        var route = (RouteAttribute?)Attribute.GetCustomAttribute(
            typeof(PlataformaSesionController), typeof(RouteAttribute));
        Assert.Equal("plataforma/sesion", route?.Template);
    }

    [Fact]
    public async Task AdministradorGlobalSinEmpresaActivaEntraConAuditoria_RevocacionEsInmediata()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-session-" + Guid.NewGuid().ToString("N")).Options);
        var enterprise = new Rol
        {
            Nombre = "Admin empresa", NombreNormalizado = "P4_EMP",
            Ambito = AmbitoAutorizacion.Empresa, EsAdministrador = true, Activo = true
        };
        var platform = new Rol
        {
            Nombre = "Admin SOLQARYN", NombreNormalizado = "P4_PLAT",
            Ambito = AmbitoAutorizacion.Plataforma, EsAdministrador = true,
            EmpresaId = null, Activo = true, EsSistema = true
        };
        db.Roles.AddRange(enterprise, platform);
        await db.SaveChangesAsync();
        var javier = new Usuario
        {
            NombreUsuario = "javier", NombreCompleto = "Operador Plataforma",
            RolId = enterprise.Id, PasswordHash = "hash-no-real",
            Activo = true
        };
        var enterpriseAdmin = new Usuario
        {
            NombreUsuario = "soloempresa", NombreCompleto = "Admin empresa",
            RolId = enterprise.Id, PasswordHash = "hash-no-real",
            Activo = true
        };
        db.Usuarios.AddRange(javier, enterpriseAdmin);
        await db.SaveChangesAsync();
        var oldCompany = new Empresa("Empresa suspendida");
        oldCompany.Desactivar();
        db.Set<Empresa>().Add(oldCompany);
        await db.SaveChangesAsync();
        db.Set<UsuarioEmpresa>().Add(
            new UsuarioEmpresa(javier.Id, oldCompany.Id, enterprise.Id));
        var grant = new UsuarioRolPlataforma(javier.Id, platform.Id);
        db.Set<UsuarioRolPlataforma>().Add(grant);
        await db.SaveChangesAsync();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(javier.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(javier.NombreUsuario);
        var scope = new PlataformaScopeService(db, current.Object);
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("id", javier.Id.ToString()),
                    new Claim("nombreUsuario", javier.NombreUsuario) }, "JwtBearer"));
        http.Request.Headers["X-Empresa-Id"] = "99999"; // spoof: no se consulta.
        var controller = new PlataformaSesionController(scope, current.Object, db)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        var ok = Assert.IsType<OkObjectResult>(
            await controller.ObtenerActual(CancellationToken.None));
        var success = Assert.IsType<ApiResponse<PlataformaSesionDto>>(ok.Value);
        Assert.Equal("javier", success.Data?.NombreUsuario);
        Assert.Equal("Plataforma", success.Data?.Ambito);
        Assert.True(success.Data?.EsAdministrador);
        Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        var audit = await db.Set<RegistroAuditoria>().SingleAsync();
        Assert.Null(audit.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
        Assert.Equal(OrigenAutorizacion.RolPlataforma, audit.OrigenAutorizacion);
        Assert.Equal("Exito", audit.Resultado);
        Assert.False(oldCompany.Activa);

        grant.Desactivar();
        await db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(
            await controller.ObtenerActual(CancellationToken.None));
        Assert.Equal(2, await db.Set<RegistroAuditoria>().CountAsync());
        Assert.Equal("Rechazado", (await db.Set<RegistroAuditoria>()
            .OrderByDescending(x => x.Id).FirstAsync()).Resultado);

        grant.Activar();
        platform.Activo = false;
        await db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(
            await controller.ObtenerActual(CancellationToken.None));
        platform.Activo = true;
        javier.Bloqueado = true;
        await db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(
            await controller.ObtenerActual(CancellationToken.None));
        javier.Bloqueado = false;
        await db.SaveChangesAsync();

        // Un JWT empresarial no concede acceso global, aunque sea administrador.
        current.SetupGet(x => x.UsuarioId).Returns(enterpriseAdmin.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(enterpriseAdmin.NombreUsuario);
        Assert.Null(await scope.ObtenerActualAsync());
        Assert.True(db.Set<Empresa>().Local.All(x => !x.Activa));
    }

    [Fact]
    public async Task DobleRolGlobalIdentidadJWTObsoletaYNoAutenticadoDenegados()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-global-multi-" + Guid.NewGuid().ToString("N")).Options);
        var roleA = new Rol
        {
            Nombre = "Global A", NombreNormalizado = "GLOBAL_A",
            Ambito = AmbitoAutorizacion.Plataforma, EsAdministrador = true, Activo = true
        };
        var roleB = new Rol
        {
            Nombre = "Global B", NombreNormalizado = "GLOBAL_B",
            Ambito = AmbitoAutorizacion.Plataforma, EsAdministrador = true, Activo = true
        };
        db.Roles.AddRange(roleA, roleB);
        await db.SaveChangesAsync();
        var user = new Usuario
        {
            NombreUsuario = "javier", NombreCompleto = "Javier",
            RolId = roleA.Id, PasswordHash = "hash-no-real", Activo = true
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        db.Set<UsuarioRolPlataforma>().AddRange(
            new UsuarioRolPlataforma(user.Id, roleA.Id),
            new UsuarioRolPlataforma(user.Id, roleB.Id));
        await db.SaveChangesAsync();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(user.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(user.NombreUsuario);
        var scope = new PlataformaScopeService(db, current.Object);
        Assert.Null(await scope.ObtenerActualAsync()); // Ambigüedad: STOP.

        var otherGrant = await db.Set<UsuarioRolPlataforma>()
            .SingleAsync(x => x.RolId == roleB.Id);
        otherGrant.Desactivar();
        await db.SaveChangesAsync();
        Assert.NotNull(await scope.ObtenerActualAsync());
        current.SetupGet(x => x.NombreUsuario).Returns("usuario-antiguo-del-JWT");
        Assert.Null(await scope.ObtenerActualAsync());
        current.SetupGet(x => x.NombreUsuario).Returns(user.NombreUsuario);
        current.SetupGet(x => x.EstaAutenticado).Returns(false);
        Assert.Null(await scope.ObtenerActualAsync());
    }

    [Fact]
    public async Task MiddlewareNoAmpliaCuarentenaComercialAlHabilitarSesion()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-global-guard-" + Guid.NewGuid().ToString("N")).Options);
        var a = new Empresa("A");
        var b = new Empresa("B");
        a.Desactivar();
        b.Desactivar();
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        int called = 0;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx =>
        {
            called++;
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        async Task<int> Request(string method, string path)
        {
            var http = new DefaultHttpContext();
            http.Request.Method = method;
            http.Request.Path = path;
            http.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(http, db);
            return http.Response.StatusCode;
        }
        Assert.Equal(204, await Request("GET", "/plataforma/sesion"));
        Assert.Equal(503, await Request("POST", "/plataforma/sesion"));
        Assert.Equal(503, await Request("GET", "/plataforma/sesion/otro"));
        Assert.Equal(503, await Request("GET", "/plataforma/usuarios"));
        Assert.Equal(503, await Request("GET", "/clientes"));
        Assert.Equal(503, await Request("POST", "/empresas"));
        Assert.Equal(1, called);
    }
}
