using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
/// Aceptación de la gestión global mínima: datos de identidad únicamente.
/// Empresas y roles A/B aislados, sin atribuir privilegios de plataforma.
/// </summary>
public sealed class PlataformaGestionMvpSecurityTests
{
    private sealed record Datos(
        AppDbContext Db, PlataformaGestionMvpController Controller,
        Rol RolEmpresa, Rol RolAdmin, Rol RolPlataforma,
        Usuario Actor, Usuario EmpresaAdmin,
        Empresa EmpresaA, Empresa EmpresaB,
        UsuarioRolPlataforma? Grant);

    private static async Task<Datos> PrepararAsync(bool global = true)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-mvp-" + Guid.NewGuid().ToString("N")).Options);
        var rEmpresa = new Rol { Nombre = "Empleado", NombreNormalizado = "EMPL_SISTEMA",
            EsSistema = true, EsAdministrador = false, Activo = true,
            Ambito = AmbitoAutorizacion.Empresa };
        var rAdmin = new Rol { Nombre = "Admin empresa", NombreNormalizado = "ADMIN_EMP",
            EsSistema = true, EsAdministrador = true, Activo = true,
            Ambito = AmbitoAutorizacion.Empresa };
        var rGlobal = new Rol { Nombre = "Admin SOLQARYN", NombreNormalizado = "GLOBAL_PLAT",
            EsSistema = true, EsAdministrador = true, Activo = true,
            Ambito = AmbitoAutorizacion.Plataforma };
        db.Roles.AddRange(rEmpresa, rAdmin, rGlobal);
        await db.SaveChangesAsync();

        var actor = new Usuario { NombreUsuario = "operadorglobal",
            NombreCompleto = "Operador global", PasswordHash = "no-password", RolId = rEmpresa.Id };
        var admin = new Usuario { NombreUsuario = "adminempresa",
            NombreCompleto = "Admin corporativo", PasswordHash = "no-password", RolId = rAdmin.Id };
        db.Usuarios.AddRange(actor, admin);
        await db.SaveChangesAsync();

        var companyA = new Empresa("Comercio A inactivo");
        companyA.Desactivar();
        var companyB = new Empresa("Comercio B");
        db.Set<Empresa>().AddRange(companyA, companyB);
        await db.SaveChangesAsync();
        var grant = global ? new UsuarioRolPlataforma(actor.Id, rGlobal.Id) : null;
        if (grant is not null)
            db.Set<UsuarioRolPlataforma>().Add(grant);
        db.Set<UsuarioEmpresa>().Add(new UsuarioEmpresa(admin.Id, companyB.Id, rAdmin.Id));
        await db.SaveChangesAsync();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(actor.NombreUsuario);
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString()) }, "JWT"));
        http.Request.Headers["X-Empresa-Id"] = companyB.Id.ToString();
        var controller = new PlataformaGestionMvpController(
            db, current.Object, new PlataformaScopeService(db, current.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        return new Datos(db, controller, rEmpresa, rAdmin, rGlobal,
            actor, admin, companyA, companyB, grant);
    }

    [Fact]
    public async Task AdministradorEmpresaNuncaActuaComoGlobal()
    {
        var t = await PrepararAsync(global: false);
        await using (t.Db)
        {
            Assert.IsType<ForbidResult>(await t.Controller.Roles(t.EmpresaB.Id));
            Assert.IsType<ForbidResult>(await t.Controller.CrearUsuario(new()
            {
                NombreUsuario = "nuevo1", NombreCompleto = "Nuevo usuario",
                Password = "PasswordSegura!42"
            }, CancellationToken.None));
            Assert.IsType<ForbidResult>(await t.Controller.Asignar(t.EmpresaA.Id, new()
            {
                UsuarioId = t.Actor.Id, RolId = t.RolAdmin.Id
            }, CancellationToken.None));
            Assert.IsType<ForbidResult>(await t.Controller.CambiarEstadoEmpresa(
                t.EmpresaA.Id, new() { Activa = true }, CancellationToken.None));
            Assert.Equal(2, await t.Db.Usuarios.CountAsync());
            Assert.False(t.EmpresaA.Activa);
            Assert.Single(await t.Db.Set<UsuarioEmpresa>().ToListAsync());
            Assert.Empty(await t.Db.Set<RegistroAuditoria>().ToListAsync());
        }
    }

    [Fact]
    public async Task EmpresaAInactivaPermiteAdministracionGlobalSinHabilitarComercio()
    {
        var t = await PrepararAsync();
        await using (t.Db)
        {
            var rolB = new Rol { Nombre = "Rol solo B", NombreNormalizado = "B_ONLY",
                Ambito = AmbitoAutorizacion.Empresa, EmpresaId = t.EmpresaB.Id,
                Activo = true };
            t.Db.Roles.Add(rolB);
            await t.Db.SaveChangesAsync();

            var roles = Assert.IsType<OkObjectResult>(
                await t.Controller.Roles(t.EmpresaA.Id));
            var rolesPayload = System.Text.Json.JsonSerializer.Serialize(roles.Value);
            Assert.Contains("Empleado", rolesPayload);
            Assert.DoesNotContain("Rol solo B", rolesPayload);
            var miembros = Assert.IsType<OkObjectResult>(
                await t.Controller.Miembros(t.EmpresaB.Id, CancellationToken.None));
            Assert.Contains("adminempresa",
                System.Text.Json.JsonSerializer.Serialize(miembros.Value));
            Assert.False(t.EmpresaA.Activa);
            Assert.Equal(2, await t.Db.Set<RegistroAuditoria>().CountAsync());
            Assert.All(await t.Db.Set<RegistroAuditoria>().ToArrayAsync(),
                a => Assert.Equal(AmbitoAutorizacion.Plataforma, a.AmbitoAutoridad));
        }
    }

    [Fact]
    public async Task NoSePuedeAsignarRolPersonalizadoDeBOPrivilegioGlobalEnA()
    {
        var t = await PrepararAsync();
        await using (t.Db)
        {
            var rolB = new Rol { Nombre = "Solo B", NombreNormalizado = "SOLO_B",
                EmpresaId = t.EmpresaB.Id, Ambito = AmbitoAutorizacion.Empresa,
                Activo = true };
            t.Db.Roles.Add(rolB);
            await t.Db.SaveChangesAsync();

            Assert.IsType<BadRequestObjectResult>(await t.Controller.Asignar(
                t.EmpresaA.Id, new() { UsuarioId = t.Actor.Id, RolId = rolB.Id },
                CancellationToken.None));
            Assert.IsType<BadRequestObjectResult>(await t.Controller.Asignar(
                t.EmpresaA.Id, new() { UsuarioId = t.Actor.Id, RolId = t.RolPlataforma.Id },
                CancellationToken.None));
            Assert.Empty(await t.Db.Set<UsuarioEmpresa>().Where(
                m => m.EmpresaId == t.EmpresaA.Id).ToListAsync());
        }
    }

    [Fact]
    public async Task AsignacionDeAInclusoInactivaNoFiltraDatosDeB()
    {
        var t = await PrepararAsync();
        await using (t.Db)
        {
            var r = Assert.IsType<OkObjectResult>(await t.Controller.Asignar(
                t.EmpresaA.Id,
                new() { UsuarioId = t.Actor.Id, RolId = t.RolAdmin.Id },
                CancellationToken.None));
            Assert.NotNull(r.Value);
            var m = Assert.Single(await t.Db.Set<UsuarioEmpresa>().Where(
                x => x.EmpresaId == t.EmpresaA.Id).ToListAsync());
            Assert.Equal(t.Actor.Id, m.UsuarioId);
            Assert.Equal(t.RolAdmin.Id, m.RolId);
            Assert.False(t.EmpresaA.Activa);
            Assert.Equal(1, await t.Db.Set<UsuarioEmpresa>()
                .CountAsync(x => x.EmpresaId == t.EmpresaB.Id));
            Assert.All(await t.Db.Set<RegistroAuditoria>().ToArrayAsync(),
                audit => Assert.Equal(t.EmpresaA.Id, audit.EmpresaId));
        }
    }

    [Fact]
    public async Task UltimoAdministradorNoSeRevocaNiDegrada()
    {
        var t = await PrepararAsync();
        await using (t.Db)
        {
            Assert.IsType<ConflictObjectResult>(
                await t.Controller.CambiarEstadoMembresia(t.EmpresaB.Id, t.EmpresaAdmin.Id,
                    new() { Activa = false }, CancellationToken.None));
            Assert.IsType<ConflictObjectResult>(
                await t.Controller.CambiarRol(t.EmpresaB.Id, t.EmpresaAdmin.Id,
                    new() { RolId = t.RolEmpresa.Id }, CancellationToken.None));
            var m = Assert.Single(await t.Db.Set<UsuarioEmpresa>()
                .Where(x => x.EmpresaId == t.EmpresaB.Id).ToListAsync());
            Assert.True(m.Activa);
            Assert.Equal(t.RolAdmin.Id, m.RolId);
        }
    }

    [Fact]
    public async Task RevocacionDeRolGlobalEsInmediata()
    {
        var t = await PrepararAsync();
        await using (t.Db)
        {
            Assert.NotNull(t.Grant);
            t.Grant!.Desactivar();
            await t.Db.SaveChangesAsync();
            Assert.IsType<ForbidResult>(
                await t.Controller.Miembros(t.EmpresaB.Id, CancellationToken.None));
            Assert.IsType<ForbidResult>(
                await t.Controller.CambiarRol(t.EmpresaB.Id, t.EmpresaAdmin.Id,
                    new() { RolId = t.RolEmpresa.Id }, CancellationToken.None));
            Assert.Empty(await t.Db.Set<RegistroAuditoria>().ToListAsync());
        }
    }

    [Fact]
    public async Task UsuarioNuevoNoRecibePrivilegiosGlobalesNiMembresiaImplicitamente()
    {
        var t = await PrepararAsync();
        await using (t.Db)
        {
            var result = Assert.IsType<CreatedResult>(
                await t.Controller.CrearUsuario(new() {
                    NombreUsuario = "nuevo-empleado", NombreCompleto = "Nuevo Operador",
                    Password = "ClaveSegura!42" }, CancellationToken.None));
            Assert.NotNull(result.Value);
            var nuevo = await t.Db.Usuarios.SingleAsync(u => u.NombreUsuario == "nuevo-empleado");
            Assert.Equal(t.RolEmpresa.Id, nuevo.RolId);
            Assert.False(nuevo.Activo); // Login bloqueado hasta primera membresía.
            Assert.True(BCrypt.Net.BCrypt.Verify("ClaveSegura!42", nuevo.PasswordHash));
            Assert.Empty(await t.Db.Set<UsuarioRolPlataforma>()
                .Where(x => x.UsuarioId == nuevo.Id).ToListAsync());
            Assert.Empty(await t.Db.Set<UsuarioEmpresa>()
                .Where(x => x.UsuarioId == nuevo.Id).ToListAsync());
            Assert.Single(await t.Db.Set<RegistroAuditoria>().ToListAsync());

            Assert.IsType<OkObjectResult>(await t.Controller.Asignar(
                t.EmpresaA.Id,
                new() { UsuarioId = nuevo.Id, RolId = t.RolEmpresa.Id },
                CancellationToken.None));
            Assert.True(nuevo.Activo);
            Assert.Single(await t.Db.Set<UsuarioEmpresa>()
                .Where(m => m.UsuarioId == nuevo.Id && m.EmpresaId == t.EmpresaA.Id)
                .ToListAsync());
        }
    }

    [Fact]
    public async Task EmpresaInactivaNoSeReactivaSinAdminYLaEdicionDeNombreEsAuditada()
    {
        var t = await PrepararAsync();
        await using (t.Db)
        {
            Assert.IsType<ConflictObjectResult>(
                await t.Controller.CambiarEstadoEmpresa(t.EmpresaA.Id,
                    new() { Activa = true }, CancellationToken.None));
            Assert.IsType<OkObjectResult>(
                await t.Controller.CambiarNombre(t.EmpresaA.Id,
                    new() { Nombre = "Comercio A renombrado" }, CancellationToken.None));
            Assert.False(t.EmpresaA.Activa);
            Assert.Equal("Comercio A renombrado", t.EmpresaA.Nombre);
            Assert.Equal(1, await t.Db.Set<RegistroAuditoria>().CountAsync());
        }
    }

    [Fact]
    public async Task MiddlewareSoloPermiteRutasExactasGlobalesAutenticadas()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-mvp-middleware-" + Guid.NewGuid().ToString("N")).Options);
        db.Set<Empresa>().AddRange(new Empresa("Comercio A"), new Empresa("Comercio B"));
        await db.SaveChangesAsync();
        int llamadas = 0;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(context => {
            llamadas++;
            context.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        async Task<int> Status(string verb, string path, bool autenticado = true)
        {
            var http = new DefaultHttpContext();
            http.Request.Method = verb;
            http.Request.Path = path;
            http.Response.Body = new MemoryStream();
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                autenticado ? new[] { new Claim(ClaimTypes.NameIdentifier, "42") } :
                Array.Empty<Claim>(), autenticado ? "JWT" : null));
            await middleware.InvokeAsync(http, db);
            return http.Response.StatusCode;
        }

        Assert.Equal(204, await Status("GET", "/plataforma/gestion/roles"));
        Assert.Equal(204, await Status("GET", "/plataforma/gestion/usuarios/buscar"));
        Assert.Equal(204, await Status("POST", "/plataforma/gestion/usuarios"));
        Assert.Equal(204, await Status("GET", "/plataforma/gestion/empresas/1/membresias"));
        Assert.Equal(204, await Status("POST", "/plataforma/gestion/empresas/2/membresias"));
        Assert.Equal(204, await Status("PUT", "/plataforma/gestion/empresas/1/membresias/2/rol"));
        Assert.Equal(204, await Status("PUT", "/plataforma/gestion/empresas/1/estado"));
        Assert.Equal(503, await Status("GET", "/plataforma/gestion/empresas/1/usuarios"));
        Assert.Equal(503, await Status("POST", "/plataforma/gestion/empresas/1/nombre"));
        Assert.Equal(503, await Status("PUT", "/plataforma/gestion/empresas/1/estado/mas"));
        Assert.Equal(503, await Status("GET", "/plataforma/gestion/usuarios"));
        Assert.Equal(503, await Status("GET", "/plataforma/gestion/roles", false));
        Assert.Equal(503, await Status("GET", "/clientes"));
        Assert.Equal(7, llamadas);
    }
}
