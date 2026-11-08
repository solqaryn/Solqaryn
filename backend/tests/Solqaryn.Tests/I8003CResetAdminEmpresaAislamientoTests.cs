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
/// I8-003.C: delimita el reset administrado ya expuesto por PUT /usuarios/{id}.
/// Prueba directa del controlador; no certifica middleware HTTP/JWT, MySQL,
/// envio de correo ni un flujo nuevo de recuperacion.
/// </summary>
public sealed class I8003CResetAdminEmpresaAislamientoTests
{
    private const int TargetUserId = 47;
    private const int VerifiedEmpresaA = 11;
    private const int SpoofedEmpresaB = 22;

    private static UpdateUsuarioDto CambioConReset() => new()
    {
        NombreUsuario = "usuario_empresa_a",
        NombreCompleto = "Usuario de prueba A",
        Rol = "Vendedor",
        RolId = 7,
        NuevaPassword = "Secreto-prueba-nuevo-2026!"
    };

    private static Mock<IUsuarioService> CrearUsuarios()
    {
        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        users.Setup(x => x.GetByIdAsync(TargetUserId))
            .ReturnsAsync(new UsuarioDetalleDto
            {
                Id = TargetUserId,
                NombreUsuario = "usuario_empresa_a",
                NombreCompleto = "Usuario de prueba A",
                Rol = "Vendedor",
                RolId = 7
            });
        return users;
    }

    [Fact]
    public async Task ResetDeUsuarioA_DenegadoSinPermisoAunqueCabeceraAfirmeEmpresaB()
    {
        var users = CrearUsuarios();
        var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
        permissions.Setup(x => x.VerificarPermisoAsync(
                VerifiedEmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena))
            .ThrowsAsync(new ForbiddenAccessException("Permiso de reset denegado."));

        var controller = new UsuariosController(
            users.Object, permissions.Object, new Mock<ICurrentUserService>().Object);
        var context = new DefaultHttpContext();
        context.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = VerifiedEmpresaA;
        context.Request.Headers["X-Empresa-Id"] = SpoofedEmpresaB.ToString();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => controller.Update(TargetUserId, CambioConReset()));

        permissions.Verify(x => x.VerificarPermisoAsync(
            VerifiedEmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena),
            Times.Once);
        permissions.VerifyNoOtherCalls();
        users.Verify(x => x.GetByIdAsync(TargetUserId), Times.Once);
        users.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateUsuarioDto>()), Times.Never);
    }

    [Fact]
    public async Task ResetConSoloCabeceraSinMarcaServidor_NoLlegaAlPermisoNiAlCambio()
    {
        var users = CrearUsuarios();
        var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
        var controller = new UsuariosController(
            users.Object, permissions.Object, new Mock<ICurrentUserService>().Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Empresa-Id"] = SpoofedEmpresaB.ToString();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => controller.Update(TargetUserId, CambioConReset()));

        permissions.VerifyNoOtherCalls();
        users.Verify(x => x.GetByIdAsync(TargetUserId), Times.Once);
        users.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateUsuarioDto>()), Times.Never);
    }
}
