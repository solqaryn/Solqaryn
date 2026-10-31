using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-005.A: rol global o de B no se puede asignar desde la administracion A.
/// Pruebas sin credenciales ni MySQL real; CI y revision independiente pendientes.
/// </summary>
public sealed class P1005ATenantAdminUserRoleBoundaryTests
{
    [Theory]
    [InlineData(22, AmbitoAutorizacion.Empresa, false)]
    [InlineData(null, AmbitoAutorizacion.Plataforma, false)]
    [InlineData(null, AmbitoAutorizacion.Empresa, false)]
    public async Task Validador_rechaza_rol_no_asignable_a_tenant(
        int? owner, AmbitoAutorizacion ambito, bool esSistema)
    {
        var users = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        roles.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Rol
        {
            Id = 7, Nombre = "rol-externo", NombreNormalizado = "ROL_EXTERNO",
            EmpresaId = owner, Ambito = ambito,
            EsSistema = esSistema, Activo = true
        });
        var service = ConstruirServicio(users.Object, roles.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ValidarRolEmpresaAsync(7, 11));

        users.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(null, true)]
    public async Task Validador_acepta_rol_propio_o_plantilla_sistema(
        int? owner, bool esSistema)
    {
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        roles.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Rol
        {
            Id = 7, Nombre = "rol-legitimo", NombreNormalizado = "ROL_LEGITIMO",
            EmpresaId = owner, Ambito = AmbitoAutorizacion.Empresa,
            EsSistema = esSistema, Activo = true
        });
        var service = ConstruirServicio(
            new Mock<IUsuarioRepository>(MockBehavior.Strict).Object, roles.Object);

        await service.ValidarRolEmpresaAsync(7, 11);
    }

    [Fact]
    public async Task Validador_sin_empresa_autorizada_falla_antes_de_leer_rol()
    {
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        var service = ConstruirServicio(
            new Mock<IUsuarioRepository>(MockBehavior.Strict).Object, roles.Object);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.ValidarRolEmpresaAsync(7, 0));
        roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Controlador_no_crea_usuario_si_rol_falla_validacion_tenant()
    {
        var (controller, usuarios, _) = ControladorConRolRechazado();
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            controller.Create(new CreateUsuarioDto
            {
                NombreUsuario = "usuario-a", NombreCompleto = "Usuario A", RolId = 7
            }));

        usuarios.Verify(x => x.CreateAsync(It.IsAny<CreateUsuarioDto>()), Times.Never);
    }

    [Fact]
    public async Task Controlador_no_edita_usuario_si_rol_falla_validacion_tenant()
    {
        var (controller, usuarios, _) = ControladorConRolRechazado();
        usuarios.Setup(x => x.GetByIdAsync(30)).ReturnsAsync(
            new UsuarioDetalleDto { Id = 30, RolId = 6, Rol = "Propio" });
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            controller.Update(30, new UpdateUsuarioDto
            {
                NombreUsuario = "usuario-a", NombreCompleto = "Usuario A", RolId = 7
            }));

        usuarios.Verify(x => x.UpdateAsync(30, It.IsAny<UpdateUsuarioDto>()), Times.Never);
    }


    [Fact]
    public async Task Repositorio_no_considera_exclusiva_identidad_A_B_aun_con_B_inactiva()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p1005a-identity-ab-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var empresaA = new Empresa("Empresa A");
        var empresaB = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "Rol A", NombreNormalizado = "ROL_A",
            EmpresaId = empresaA.Id, Activo = true
        };
        var rolB = new Rol
        {
            Nombre = "Rol B", NombreNormalizado = "ROL_B",
            EmpresaId = empresaB.Id, Activo = true
        };
        db.Roles.AddRange(rolA, rolB);
        await db.SaveChangesAsync();
        var usuario = new Usuario
        {
            NombreUsuario = "usuario-ab", NombreCompleto = "Usuario AB",
            PasswordHash = "solo-pruebas", RolId = rolA.Id, Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id));
        await db.SaveChangesAsync();

        var repo = new UsuarioRepository(db);
        Assert.True(await repo.EsIdentidadExclusivaDeEmpresaAsync(usuario.Id, empresaA.Id));
        Assert.False(await repo.EsIdentidadExclusivaDeEmpresaAsync(usuario.Id, empresaB.Id));

        var membresiaB = new UsuarioEmpresa(usuario.Id, empresaB.Id, rolB.Id);
        db.UsuarioEmpresas.Add(membresiaB);
        await db.SaveChangesAsync();
        Assert.False(await repo.EsIdentidadExclusivaDeEmpresaAsync(usuario.Id, empresaA.Id));
        Assert.False(await repo.EsIdentidadExclusivaDeEmpresaAsync(usuario.Id, empresaB.Id));

        membresiaB.Desactivar();
        await db.SaveChangesAsync();
        Assert.False(await repo.EsIdentidadExclusivaDeEmpresaAsync(usuario.Id, empresaA.Id));
    }

    [Fact]
    public async Task Servicio_rechaza_edicion_global_compartida_sin_escribir_identidad()
    {
        var users = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        users.Setup(x => x.EsIdentidadExclusivaDeEmpresaAsync(30, 11)).ReturnsAsync(false);
        var service = ConstruirServicio(users.Object, Mock.Of<IRolRepository>());

        var error = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.UpdateDesdeEmpresaAsync(30, new UpdateUsuarioDto
            {
                NombreUsuario = "nuevo-nombre", NombreCompleto = "Nombre Nuevo",
                RolId = 7, NuevaPassword = "Password-no-real-pruebas"
            }, 11));
        Assert.Contains("identidad", error.Message, StringComparison.OrdinalIgnoreCase);
        users.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        users.Verify(x => x.Update(It.IsAny<Usuario>()), Times.Never);
        users.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Controlador_usa_edicion_empresa_y_no_operacion_global_directa()
    {
        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        users.Setup(x => x.GetByIdAsync(30)).ReturnsAsync(
            new UsuarioDetalleDto { Id = 30, RolId = 7, Rol = "Rol A" });
        users.Setup(x => x.ValidarRolEmpresaAsync(7, 11)).Returns(Task.CompletedTask);
        users.Setup(x => x.UpdateDesdeEmpresaAsync(30, It.IsAny<UpdateUsuarioDto>(), 11))
            .ThrowsAsync(new ForbiddenAccessException("Identidad compartida no editable."));
        var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 11;
        var controller = new UsuariosController(
            users.Object, permissions.Object, Mock.Of<ICurrentUserService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.Update(30, new UpdateUsuarioDto
            {
                NombreUsuario = "usuario-ab", NombreCompleto = "Usuario AB", RolId = 7
            }));
        users.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateUsuarioDto>()), Times.Never);
        permissions.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("estado")]
    [InlineData("bloquear")]
    [InlineData("desbloquear")]
    [InlineData("eliminar")]
    public async Task Mutaciones_globales_rechazan_identidad_compartida_A_B_antes_de_escribir(string ruta)
    {
        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        users.Setup(x => x.ExigirIdentidadExclusivaDeEmpresaAsync(30, 11))
            .ThrowsAsync(new ForbiddenAccessException("Identidad compartida no editable."));
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(
                11, ModuloSistema.Usuarios, It.IsAny<AccionPermiso>()))
            .Returns(Task.CompletedTask);
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "11";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 11;
        var controller = new UsuariosController(
            users.Object, permisos.Object, Mock.Of<ICurrentUserService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            switch (ruta)
            {
                case "estado":
                    await controller.UpdateEstado(30, new UpdateUsuarioEstadoDto { Activo = false });
                    break;
                case "bloquear":
                    await controller.Bloquear(30, new BloquearUsuarioDto { Motivo = "prueba" });
                    break;
                case "desbloquear":
                    await controller.Desbloquear(30);
                    break;
                case "eliminar":
                    await controller.Eliminar(30);
                    break;
            }
        });

        users.Verify(x => x.UpdateEstadoAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        users.Verify(x => x.BloquearAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        users.Verify(x => x.DesbloquearAsync(It.IsAny<int>()), Times.Never);
        users.Verify(x => x.EliminarAsync(It.IsAny<int>()), Times.Never);
        users.Verify(x => x.ExigirIdentidadExclusivaDeEmpresaAsync(30, 11), Times.Once);
    }

    private static UsuarioService ConstruirServicio(
        IUsuarioRepository users, IRolRepository roles) =>
        new(users, roles,
            Mock.Of<IEmpresaRepository>(), Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>());

    private static (UsuariosController Controller, Mock<IUsuarioService> Users,
        Mock<IPermisoService> Permissions) ControladorConRolRechazado()
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(x => x.ValidarRolEmpresaAsync(7, 11))
            .ThrowsAsync(new BusinessRuleException("Rol fuera del tenant autorizado."));
        var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
        permissions.Setup(x => x.VerificarPermisoAsync(
                11, ModuloSistema.Usuarios, AccionPermiso.AsignarRol))
            .Returns(Task.CompletedTask);

        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 11;
        var controller = new UsuariosController(
            usuarios.Object, permissions.Object, Mock.Of<ICurrentUserService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        return (controller, usuarios, permissions);
    }
}
