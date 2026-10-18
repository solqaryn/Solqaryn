using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class UsuarioTenantRoleOwnershipTests
{
    [Fact]
    public async Task Rbac_Descarta_Membresia_Con_Rol_Otra_Empresa_O_Plataforma()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-owner-rbac-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(dbOptions);
        var a = new Empresa("Scope A");
        var b = new Empresa("Scope B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var roleB = new Rol
        {
            Nombre = "ROL B", NombreNormalizado = "ROL_B",
            EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        var platform = new Rol
        {
            Nombre = "ROL PLATAFORMA", NombreNormalizado = "ROL_PLATAFORMA",
            EmpresaId = null, Ambito = AmbitoAutorizacion.Plataforma,
            EsAdministrador = true, Activo = true
        };
        var plantilla = new Rol
        {
            Nombre = "ROL PLANTILLA", NombreNormalizado = "ROL_PLANTILLA",
            EmpresaId = null, Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = true, Activo = true
        };
        db.Roles.AddRange(roleB, platform, plantilla);
        await db.SaveChangesAsync();
        var usuario = new Usuario
        {
            NombreUsuario = "p4-owner", NombreCompleto = "P4 Owner",
            PasswordHash = "solo-pruebas", RolId = roleB.Id, Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        var membership = new UsuarioEmpresa(usuario.Id, a.Id, roleB.Id);
        var ownMembership = new UsuarioEmpresa(usuario.Id, b.Id, roleB.Id);
        db.UsuarioEmpresas.AddRange(membership, ownMembership);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        var scope = new UsuarioScopeService(db, current.Object);

        // A NO puede adquirir las facultades del rol personalizado de B.
        Assert.Null(await scope.ObtenerActualAsync(a.Id));
        Assert.Equal(roleB.Id, (await scope.ObtenerActualAsync(b.Id))?.RolId);

        var almacenada = await db.UsuarioEmpresas.SingleAsync(x =>
            x.UsuarioId == usuario.Id && x.EmpresaId == a.Id);
        almacenada.CambiarRol(platform.Id);
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(a.Id));

        // Un rol personalizado legacy sin owner jamás adquiere autoridad A.
        var huerfano = new Rol {
            Nombre = "SIN OWNER", NombreNormalizado = "SIN_OWNER",
            Ambito = AmbitoAutorizacion.Empresa, EsSistema = false, Activo = true
        };
        db.Roles.Add(huerfano);
        await db.SaveChangesAsync();
        almacenada.CambiarRol(huerfano.Id);
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(a.Id));

        almacenada.CambiarRol(plantilla.Id);
        await db.SaveChangesAsync();
        Assert.Equal(plantilla.Id, (await scope.ObtenerActualAsync(a.Id))?.RolId);
        Assert.Equal(roleB.Id, (await scope.ObtenerActualAsync(b.Id))?.RolId);
    }

    [Theory]
    [InlineData(22, AmbitoAutorizacion.Empresa)]
    [InlineData(null, AmbitoAutorizacion.Plataforma)]
    [InlineData(null, AmbitoAutorizacion.Empresa)]
    public async Task UsuarioService_NoAsigna_Roles_Ajenos_En_Membresias(
        int? roleOwner, AmbitoAutorizacion scope)
    {
        var (sut, usuarios) = ConstruirServicio(new Rol
        {
            Id = 4, Nombre = "Rol inseguro", NombreNormalizado = "ROL_INSEGURO",
            EmpresaId = roleOwner, Ambito = scope, Activo = true
        });
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.AsignarEmpresaAsync(30, new() { EmpresaId = 11, RolId = 4 }));
        Assert.Contains("rol empresarial", error.Message, StringComparison.OrdinalIgnoreCase);
        usuarios.Verify(x => x.AddEmpresaAsync(It.IsAny<UsuarioEmpresa>()), Times.Never);
        usuarios.Verify(x => x.SaveChangesAsync(), Times.Never);

        error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CambiarRolEmpresaAsync(30, 11, 4));
        Assert.Contains("rol empresarial", error.Message, StringComparison.OrdinalIgnoreCase);
        usuarios.Verify(x => x.UpdateEmpresa(It.IsAny<UsuarioEmpresa>()), Times.Never);
    }

    [Fact]
    public async Task UsuarioService_NoReactiva_Membresia_Con_Rol_Cruzado()
    {
        var rol = new Rol
        {
            Id = 4, Nombre = "Rol de B", NombreNormalizado = "ROL_B",
            EmpresaId = 22, Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        var (sut, usuarios) = ConstruirServicio(rol);
        var membre = new UsuarioEmpresa(30, 11, 4);
        membre.Desactivar();
        usuarios.Setup(x => x.GetEmpresaAsync(30, 11)).ReturnsAsync(membre);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CambiarEstadoEmpresaAsync(30, 11, true));
        Assert.False(membre.Activa);
        usuarios.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UsuarioService_Conserva_Plantilla_Empresarial_Legitima()
    {
        var rol = new Rol
        {
            Id = 4, Nombre = "Plantilla Empresa",
            NombreNormalizado = "PLANTILLA_EMPRESA",
            EmpresaId = null, Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = true, Activo = true
        };
        var (sut, usuarios) = ConstruirServicio(rol);
        usuarios.Setup(x => x.GetEmpresaAsync(30, 11))
            .ReturnsAsync((UsuarioEmpresa?)null);
        usuarios.Setup(x => x.AddEmpresaAsync(It.IsAny<UsuarioEmpresa>()))
            .Returns(Task.CompletedTask);
        usuarios.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var asignada = await sut.AsignarEmpresaAsync(30, new()
        {
            EmpresaId = 11, RolId = 4
        });
        Assert.Equal(11, asignada.EmpresaId);
        Assert.Equal(4, asignada.RolId);
        usuarios.Verify(x => x.AddEmpresaAsync(It.IsAny<UsuarioEmpresa>()), Times.Once);
    }

    private static (UsuarioService Service, Mock<IUsuarioRepository> Users)
        ConstruirServicio(Rol rol)
    {
        var users = new Mock<IUsuarioRepository>();
        users.Setup(x => x.GetByIdAsync(30)).ReturnsAsync(new Usuario
        {
            Id = 30, NombreUsuario = "usuario30", NombreCompleto = "Usuario 30",
            Activo = true, Bloqueado = false
        });
        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(rol);
        var companies = new Mock<IEmpresaRepository>();
        companies.Setup(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empresa("A") { Id = 11 });
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        return (new UsuarioService(users.Object, roles.Object, companies.Object,
            Mock.Of<IAuditoriaService>(), current.Object), users);
    }
}
