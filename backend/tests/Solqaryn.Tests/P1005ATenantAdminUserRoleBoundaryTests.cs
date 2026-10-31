using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
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
