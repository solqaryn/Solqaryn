using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// C15-03.M05.02: real API controller/DTO contract with in-memory persistence.
/// Test ClaimsIdentity is not a signed JWT; this is not HTTP middleware or MySQL proof.
/// </summary>
public sealed class C1503M0502PlataformaSesionApiContractTests
{
    private static async Task<(AppDbContext Db, PlataformaSesionController Controller,
        UsuarioRolPlataforma? Grant)> Fixture(bool platformGrant = true, bool authenticated = true)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("c1503-m0502-" + Guid.NewGuid().ToString("N")).Options);
        var enterprise = new Rol
        {
            Nombre = "Empresa A", NombreNormalizado = "C1503_EMP_A",
            Ambito = AmbitoAutorizacion.Empresa, EsAdministrador = true, Activo = true
        };
        var platform = new Rol
        {
            Nombre = "Plataforma", NombreNormalizado = "C1503_PLATFORM",
            Ambito = AmbitoAutorizacion.Plataforma, EsAdministrador = true,
            EsSistema = true, Activo = true
        };
        db.Roles.AddRange(enterprise, platform);
        await db.SaveChangesAsync();
        var actor = new Usuario
        {
            NombreUsuario = "operador-a", NombreCompleto = "Operador A",
            RolId = enterprise.Id, PasswordHash = "opaque-test-hash",
            Activo = true
        };
        db.Usuarios.Add(actor);
        await db.SaveChangesAsync();
        UsuarioRolPlataforma? grant = null;
        if (platformGrant)
        {
            grant = new UsuarioRolPlataforma(actor.Id, platform.Id);
            db.Set<UsuarioRolPlataforma>().Add(grant);
            await db.SaveChangesAsync();
        }
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(authenticated);
        current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(actor.NombreUsuario);
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("id", actor.Id.ToString()),
            new Claim("nombreUsuario", actor.NombreUsuario)
        }, authenticated ? "JwtBearer" : null));
        ctx.Request.Headers["X-Empresa-Id"] = "900001";
        ctx.Request.Headers["X-Role"] = "SuperAdministrador";
        var controller = new PlataformaSesionController(
            new PlataformaScopeService(db, current.Object), current.Object, db)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx }
        };
        return (db, controller, grant);
    }

    [Fact]
    public void EndpointHasAuthenticatedGetAndNoStoreContract()
    {
        var type = typeof(PlataformaSesionController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("plataforma/sesion", type.GetCustomAttribute<RouteAttribute>()?.Template);
        var method = type.GetMethod(nameof(PlataformaSesionController.ObtenerActual))!;
        Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>());
        var cache = method.GetCustomAttribute<ResponseCacheAttribute>();
        Assert.NotNull(cache);
        Assert.True(cache!.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
    }

    [Fact]
    public async Task ValidPlatformGrantReturnsSafeDtoAndAuditsWithoutClientTenantSpoof()
    {
        var (db, controller, _) = await Fixture();
        await using (db)
        {
            var result = Assert.IsType<OkObjectResult>(
                await controller.ObtenerActual(CancellationToken.None));
            var payload = Assert.IsType<ApiResponse<PlataformaSesionDto>>(result.Value);
            Assert.Equal("operador-a", payload.Data?.NombreUsuario);
            Assert.Equal("Plataforma", payload.Data?.Ambito);
            Assert.True(payload.Data?.EsAdministrador);
            Assert.Contains("no-store",
                controller.HttpContext.Response.Headers.CacheControl.ToString());
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.DoesNotContain("opaque-test-hash", json);
            Assert.DoesNotContain("900001", json);
            Assert.DoesNotContain("SuperAdministrador", json);
            var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
            Assert.Null(audit.EmpresaId);
            Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
            Assert.Equal("Exito", audit.Resultado);
        }
    }

    [Fact]
    public async Task EnterpriseAdministratorAndSpoofedHeadersCannotGainPlatformAccess()
    {
        var (db, controller, _) = await Fixture(platformGrant: false);
        await using (db)
        {
            Assert.IsType<ForbidResult>(
                await controller.ObtenerActual(CancellationToken.None));
            Assert.Contains("no-store",
                controller.HttpContext.Response.Headers.CacheControl.ToString());
            var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
            Assert.Equal("Rechazado", audit.Resultado);
            Assert.Null(audit.EmpresaId);
            Assert.DoesNotContain("900001", audit.Descripcion);
        }
    }

    [Fact]
    public async Task RevocationChangesNextControllerResultAndIsAudited()
    {
        var (db, controller, grant) = await Fixture();
        await using (db)
        {
            Assert.IsType<OkObjectResult>(await controller.ObtenerActual(CancellationToken.None));
            Assert.NotNull(grant);
            grant!.Desactivar();
            await db.SaveChangesAsync();
            Assert.IsType<ForbidResult>(await controller.ObtenerActual(CancellationToken.None));
            var audit = await db.Set<RegistroAuditoria>().ToListAsync();
            Assert.Equal(2, audit.Count);
            Assert.Equal("Exito", audit[0].Resultado);
            Assert.Equal("Rechazado", audit[1].Resultado);
        }
    }

    [Fact]
    public async Task AnonymousIdentityIsDeniedWithoutReadingOrWritingAudit()
    {
        var (db, controller, _) = await Fixture(authenticated: false);
        await using (db)
        {
            Assert.IsType<ForbidResult>(await controller.ObtenerActual(CancellationToken.None));
            Assert.Empty(await db.Set<RegistroAuditoria>().ToListAsync());
            Assert.Contains("no-store",
                controller.HttpContext.Response.Headers.CacheControl.ToString());
        }
    }

    [Fact]
    public void WireDtoSerializesOnlyFourPublicPropertiesAndNoTenancyIdentity()
    {
        var dto = new PlataformaSesionDto("usuario", "Administrador", "Plataforma", true);
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;
        Assert.Equal(4, root.EnumerateObject().Count());
        Assert.Equal("usuario", root.GetProperty("nombreUsuario").GetString());
        Assert.Equal("Administrador", root.GetProperty("rolNombre").GetString());
        Assert.Equal("Plataforma", root.GetProperty("ambito").GetString());
        Assert.True(root.GetProperty("esAdministrador").GetBoolean());
        Assert.False(root.TryGetProperty("empresaId", out _));
        Assert.False(root.TryGetProperty("tenantId", out _));
        Assert.False(root.TryGetProperty("token", out _));
        Assert.False(root.TryGetProperty("passwordHash", out _));
    }
}
