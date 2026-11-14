using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class CommercialPermissionsProjectionControllerTests
{
    [Fact]
    public async Task TresEmpresas_DevuelveSoloCapacidadesDePilotosHabilitadosTrasValidarMembresia()
    {
        await using var db = Database();
        var a = new Empresa("A");
        var b = new Empresa("B");
        var c = new Empresa("C");
        db.Set<Empresa>().AddRange(a, b, c);
        await db.SaveChangesAsync();

        var permissions = new Mock<IPermisoService>();
        permissions.Setup(x => x.GetMisPermisosAsync(a.Id)).ReturnsAsync(() => new MisPermisosDto
        {
            Rol = "Administrador", EsAdministrador = true,
            Permisos = new List<string> { "Productos:Ver", "Dashboard:Ver" }
        });
        var controller = Controller(db, permissions.Object,
            ("MultiEmpresa:PrivateReadPilotEnabled", "true"));

        var result = Assert.IsType<OkObjectResult>(
            await controller.GetMisPermisosEmpresa(a.Id));
        var body = Assert.IsType<ApiResponse<MisPermisosDto>>(result.Value);
        Assert.True(body.Data!.OperacionMultiEmpresaRestringida);
        Assert.Contains("Productos:Ver", body.Data.CapacidadesHabilitadas);
        Assert.DoesNotContain("Dashboard:Ver", body.Data.CapacidadesHabilitadas);
        Assert.Contains("productos", body.Data.RutasHabilitadas);
        Assert.Equal("private, no-store, max-age=0",
            controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task NoRevelaPermisosDeEmpresaSinMembresia()
    {
        await using var db = Database();
        var a = new Empresa("A");
        var b = new Empresa("B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var permissions = new Mock<IPermisoService>();
        permissions.Setup(x => x.GetMisPermisosAsync(b.Id)).ReturnsAsync(new MisPermisosDto());
        var controller = Controller(db, permissions.Object);
        Assert.IsType<ForbidResult>(await controller.GetMisPermisosEmpresa(b.Id));
    }

    [Fact]
    public async Task EmpresaSuspendidaNoExponeCapacidadesAunqueExistaRol()
    {
        await using var db = Database();
        var a = new Empresa("A");
        var b = new Empresa("B");
        b.Desactivar();
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var permissions = new Mock<IPermisoService>();
        permissions.Setup(x => x.GetMisPermisosAsync(b.Id)).ReturnsAsync(
            new MisPermisosDto { Rol = "Administrador", EsAdministrador = true });
        var controller = Controller(db, permissions.Object);
        Assert.IsType<ForbidResult>(await controller.GetMisPermisosEmpresa(b.Id));
    }

    private static AppDbContext Database() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("mf3-permissions-" + Guid.NewGuid().ToString("N"))
            .Options);

    private static PermisosController Controller(AppDbContext db, IPermisoService service,
        params (string Key, string Value)[] settings)
    {
        var catalog = new Mock<IPermisoCatalogoService>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();
        return new PermisosController(service, catalog.Object, db, config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
