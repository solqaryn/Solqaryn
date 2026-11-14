using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-006.A: la lista de selección se obtiene de membresías reales del actor
/// autenticado. No es un catálogo global, ni un grant por JWT o por nombre.
/// </summary>
public sealed class P1006ASelectorEmpresarialTests
{
    [Fact]
    public async Task SoloListaMembresiaVigentePropiaYNoEnumeraOtrosTenants()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1-006-a-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("Empresa autorizada");
        var empresaB = new Empresa("Empresa con membresia revocada");
        var empresaC = new Empresa("Empresa de otra cuenta");
        var empresaD = new Empresa("Empresa con rol plataforma");
        var empresaE = new Empresa("Empresa inactiva");
        empresaE.Desactivar();
        db.Set<Empresa>().AddRange(empresaA, empresaB, empresaC, empresaD, empresaE);

        var rolEmpresarial = new Rol
        {
            Nombre = "Operador empresa",
            NombreNormalizado = "P1006_EMPRESA",
            Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = true,
            Activo = true
        };
        var rolGlobal = new Rol
        {
            Nombre = "Administrador plataforma",
            NombreNormalizado = "P1006_GLOBAL",
            Ambito = AmbitoAutorizacion.Plataforma,
            EsSistema = true,
            EsAdministrador = true,
            Activo = true
        };
        db.Roles.AddRange(rolEmpresarial, rolGlobal);
        await db.SaveChangesAsync();

        var actor = new Usuario
        {
            NombreUsuario = "p1006-actor-a",
            NombreCompleto = "Actor A",
            RolId = rolEmpresarial.Id,
            PasswordHash = "fixture-no-secreto",
            Activo = true
        };
        var ajeno = new Usuario
        {
            NombreUsuario = "p1006-actor-b",
            NombreCompleto = "Actor B",
            RolId = rolEmpresarial.Id,
            PasswordHash = "fixture-no-secreto",
            Activo = true
        };
        db.Usuarios.AddRange(actor, ajeno);
        await db.SaveChangesAsync();

        var revocada = new UsuarioEmpresa(actor.Id, empresaB.Id, rolEmpresarial.Id);
        revocada.Desactivar();
        db.Set<UsuarioEmpresa>().AddRange(
            new UsuarioEmpresa(actor.Id, empresaA.Id, rolEmpresarial.Id),
            revocada,
            new UsuarioEmpresa(ajeno.Id, empresaC.Id, rolEmpresarial.Id),
            new UsuarioEmpresa(actor.Id, empresaD.Id, rolGlobal.Id),
            new UsuarioEmpresa(actor.Id, empresaE.Id, rolEmpresarial.Id));
        await db.SaveChangesAsync();

        var actual = new Mock<ICurrentUserService>();
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        actual.SetupGet(x => x.UsuarioId).Returns(actor.Id);
        var servicio = new UsuarioScopeService(db, actual.Object);

        var empresas = await servicio.ListarEmpresasSeleccionablesAsync();
        var unica = Assert.Single(empresas);
        Assert.Equal(empresaA.Id, unica.EmpresaId);
        Assert.Equal(empresaA.Nombre, unica.Nombre);
        Assert.Null(await servicio.ObtenerActualAsync(empresaB.Id));
        Assert.Null(await servicio.ObtenerActualAsync(empresaC.Id));
        Assert.Null(await servicio.ObtenerActualAsync(empresaD.Id));
        Assert.Null(await servicio.ObtenerActualAsync(empresaE.Id));

        var noAutenticado = new Mock<ICurrentUserService>();
        noAutenticado.SetupGet(x => x.UsuarioId).Returns(actor.Id);
        var sinSesion = new UsuarioScopeService(db, noAutenticado.Object);
        Assert.Empty(await sinSesion.ListarEmpresasSeleccionablesAsync());
    }
}
