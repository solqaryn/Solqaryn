using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-016.A: una sesión global válida no depende de tenant comercial,
/// y una membresía empresarial nunca concede privilegios de plataforma.
/// EF InMemory: no sustituye un test HTTP/JWT/MySQL DEV vivo.
/// </summary>
public sealed class P1016APlatformWithoutTenantTests
{
    [Fact]
    public async Task P1016A_SuperadminSinMembresiasEntraAunqueEmpresaPilotoEsteInactiva()
    {
        var f = await CrearAsync(plataforma: true, membresia: false, empresaActiva: false);
        await using var db = f.Db;

        Assert.NotNull(await f.Plataforma.ObtenerActualAsync());
        Assert.Null(await f.Comercial.ObtenerActualAsync(f.Empresa.Id));
        Assert.Null(await f.Comercial.ObtenerUnicoActualAsync());

        var controller = CrearControlador(f);
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = f.Empresa.Id.ToString();
        Assert.IsType<OkObjectResult>(await controller.ObtenerActual(CancellationToken.None));
        Assert.Equal("private, no-store, max-age=0",
            controller.Response.Headers.CacheControl.ToString());

        var auditoria = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Equal("SesionPlataforma", auditoria.Entidad);
        Assert.Equal("Exito", auditoria.Resultado);
        Assert.Null(auditoria.EmpresaId); // ni siquiera un header empresarial forjado.
        Assert.Equal(f.Usuario.Id, auditoria.UsuarioId);
        Assert.Empty(await db.Set<UsuarioEmpresa>().ToListAsync());
    }

    [Fact]
    public async Task P1016A_UsuarioSinMembresiaNiAsignacionGlobalRecibeForbid()
    {
        var f = await CrearAsync(plataforma: false, membresia: false, empresaActiva: false);
        await using var db = f.Db;

        Assert.Null(await f.Plataforma.ObtenerActualAsync());
        Assert.Null(await f.Comercial.ObtenerUnicoActualAsync());
        var controller = CrearControlador(f);
        Assert.IsType<ForbidResult>(await controller.ObtenerActual(CancellationToken.None));

        var auditoria = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Equal("Rechazado", auditoria.Resultado);
        Assert.Null(auditoria.EmpresaId);
        Assert.Null(auditoria.AmbitoAutoridad);
    }

    [Fact]
    public async Task P1016A_AdminEmpresarialNoHeredaAccesoGlobalPorRolNiPorHeader()
    {
        var f = await CrearAsync(plataforma: false, membresia: true, empresaActiva: true);
        await using var db = f.Db;

        Assert.NotNull(await f.Comercial.ObtenerActualAsync(f.Empresa.Id));
        Assert.Null(await f.Plataforma.ObtenerActualAsync());

        var controller = CrearControlador(f);
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = f.Empresa.Id.ToString();
        Assert.IsType<ForbidResult>(await controller.ObtenerActual(CancellationToken.None));

        var auditoria = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Equal("Rechazado", auditoria.Resultado);
        Assert.Null(auditoria.EmpresaId);
        Assert.False(await db.Set<UsuarioRolPlataforma>()
            .AnyAsync(x => x.UsuarioId == f.Usuario.Id));
    }

    [Fact]
    public async Task P1016A_UsuarioGlobalBloqueadoNoConservaSesionPorJwtAnterior()
    {
        var f = await CrearAsync(plataforma: true, membresia: false, empresaActiva: false);
        await using var db = f.Db;
        f.Usuario.Bloqueado = true;
        await db.SaveChangesAsync();

        Assert.Null(await f.Plataforma.ObtenerActualAsync());
        var controller = CrearControlador(f);
        Assert.IsType<ForbidResult>(await controller.ObtenerActual(CancellationToken.None));

        var auditoria = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Equal("Rechazado", auditoria.Resultado);
        Assert.Null(auditoria.EmpresaId);
    }

    [Fact]
    public async Task P1016A_IdentidadHttpNoAutenticadaSeRechazaAntesDeAuditar()
    {
        var f = await CrearAsync(plataforma: true, membresia: false, empresaActiva: false);
        await using var db = f.Db;

        var controller = CrearControlador(f, httpAutenticado: false);
        Assert.IsType<ForbidResult>(await controller.ObtenerActual(CancellationToken.None));
        Assert.Empty(await db.Set<RegistroAuditoria>().ToListAsync());
    }

    private static PlataformaSesionController CrearControlador(
        Escenario f, bool httpAutenticado = true)
    {
        var identity = httpAutenticado
            ? new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, f.Usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, f.Usuario.NombreUsuario)
            }, "fixture-jwt-validado")
            : new ClaimsIdentity();

        return new PlataformaSesionController(f.Plataforma, f.Current, f.Db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                    TraceIdentifier = "P1016A-tenantless-request"
                }
            }
        };
    }

    private static async Task<Escenario> CrearAsync(
        bool plataforma, bool membresia, bool empresaActiva)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p1016a-" + Guid.NewGuid().ToString("N")).Options);

        var empresa = new Empresa("Empresa Piloto Inactiva");
        if (!empresaActiva)
            empresa.Desactivar();
        db.Set<Empresa>().Add(empresa);

        var rolGlobal = new Rol
        {
            Nombre = "Administrador de plataforma",
            NombreNormalizado = "P1016A_GLOBAL",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsAdministrador = true,
            EsSistema = true,
            Activo = true
        };
        var rolEmpresa = new Rol
        {
            Nombre = "Administrador empresarial",
            NombreNormalizado = "P1016A_EMPRESARIAL",
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = null,
            EsAdministrador = true,
            EsSistema = true,
            Activo = true
        };
        db.Roles.AddRange(rolGlobal, rolEmpresa);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p1016a-operador",
            NombreCompleto = "Operador de escenario sin tenant",
            PasswordHash = "solo-fixture-sin-credenciales-reales",
            RolId = rolEmpresa.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        if (plataforma)
            db.Set<UsuarioRolPlataforma>().Add(
                new UsuarioRolPlataforma(usuario.Id, rolGlobal.Id));
        if (membresia)
            db.Set<UsuarioEmpresa>().Add(
                new UsuarioEmpresa(usuario.Id, empresa.Id, rolEmpresa.Id));
        await db.SaveChangesAsync();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(usuario.NombreUsuario);

        return new Escenario(
            db, empresa, usuario, new PlataformaScopeService(db, current.Object),
            new UsuarioScopeService(db, current.Object), current.Object);
    }

    private sealed record Escenario(
        AppDbContext Db,
        Empresa Empresa,
        Usuario Usuario,
        PlataformaScopeService Plataforma,
        UsuarioScopeService Comercial,
        ICurrentUserService Current);
}
