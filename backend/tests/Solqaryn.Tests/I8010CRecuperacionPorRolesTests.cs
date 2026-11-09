using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-010.C: delimita el contrato negativo de recuperación administrativa de
/// contraseña por rol dentro del tenant verificado. No certifica envío SMTP,
/// flujo de recuperación anónimo, MySQL ni ejecución HTTP/JWT end-to-end.
/// </summary>
public sealed class I8010CRecuperacionPorRolesTests
{
    private const int EmpresaA = 11;
    private const int EmpresaB = 22;
    private const int UsuarioId = 87;

    [Fact]
    public async Task Reset_sin_permiso_rechaza_header_falsificado_antes_de_modificar_usuario()
    {
        var (controller, usuarios, permisos) = CrearController();
        permisos.Setup(x => x.VerificarPermisoAsync(
                EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena))
            .ThrowsAsync(new ForbiddenAccessException("Sin permiso de restablecimiento."));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.Update(UsuarioId, new UpdateUsuarioDto
            {
                RolId = 5,
                Rol = "Operador",
                NuevaPassword = "clave-sintetica-no-real"
            }));

        usuarios.Verify(x => x.GetByIdAsync(UsuarioId), Times.Once);
        usuarios.VerifyNoOtherCalls();
        permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaB, It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()), Times.Never);
    }

    [Fact]
    public async Task Cambio_de_rol_no_autorizado_impide_que_el_reset_eleve_privilegios()
    {
        var (controller, usuarios, permisos) = CrearController();
        permisos.Setup(x => x.VerificarPermisoAsync(
                EmpresaA, ModuloSistema.Usuarios, AccionPermiso.AsignarRol))
            .ThrowsAsync(new ForbiddenAccessException("Sin permiso de asignar rol."));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.Update(UsuarioId, new UpdateUsuarioDto
            {
                RolId = 6,
                Rol = "Administrador",
                NuevaPassword = "otra-clave-sintetica"
            }));

        usuarios.Verify(x => x.GetByIdAsync(UsuarioId), Times.Once);
        usuarios.VerifyNoOtherCalls();
        permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Never);
    }

    private static (
        UsuariosController Controller,
        Mock<IUsuarioService> Usuarios,
        Mock<IPermisoService> Permisos) CrearController()
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(UsuarioId)).ReturnsAsync(
            new UsuarioDetalleDto { Id = UsuarioId, RolId = 5, Rol = "Operador", Activo = true });

        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = EmpresaA;
        http.Request.Headers["X-Empresa-Id"] = EmpresaB.ToString();

        var controller = new UsuariosController(
            usuarios.Object,
            permisos.Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        return (controller, usuarios, permisos);
    }
}
