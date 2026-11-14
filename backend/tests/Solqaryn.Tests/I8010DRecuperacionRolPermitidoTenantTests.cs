using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-010.D: certifica únicamente la ruta positiva de reset administrativo
/// con rol inalterado en el tenant autorizado. La cabecera cliente no concede
/// autoridad; no se certifican HTTP/JWT reales, SMTP ni persistencia MySQL.
/// </summary>
public sealed class I8010DRecuperacionRolPermitidoTenantTests
{
    [Fact]
    public async Task Reset_permitido_con_rol_inmutable_usa_empresa_verificada_y_no_header_falso()
    {
        const int empresaA = 11;
        const int empresaB = 22;
        const int usuarioId = 87;
        const string passwordSintetica = "clave-sintetica-no-real";

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuarioId)).ReturnsAsync(
            new UsuarioDetalleDto { Id = usuarioId, RolId = 5, Rol = "Operador", Activo = true });

        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(
                empresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena))
            .Returns(Task.CompletedTask);

        var escritor = new Mock<IUsuarioAdministracionEmpresaService>(MockBehavior.Strict);
        escritor.Setup(x => x.ActualizarAsync(
                usuarioId,
                It.Is<UpdateUsuarioDto>(d => d.RolId == 5 && d.NuevaPassword == passwordSintetica),
                empresaA,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto
            {
                Id = usuarioId, RolId = 5, Rol = "Operador", Activo = true
            });

        using var provider = new ServiceCollection()
            .AddSingleton<IUsuarioAdministracionEmpresaService>(escritor.Object)
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = provider };
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA;
        http.Request.Headers["X-Empresa-Id"] = empresaB.ToString();

        var controller = new UsuariosController(
            usuarios.Object,
            permisos.Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        var result = await controller.Update(usuarioId, new UpdateUsuarioDto
        {
            RolId = 5,
            Rol = "Operador",
            NuevaPassword = passwordSintetica
        });

        Assert.IsType<OkObjectResult>(result);
        usuarios.Verify(x => x.GetByIdAsync(usuarioId), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Never);
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaB, It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()), Times.Never);
        escritor.Verify(x => x.ActualizarAsync(
            usuarioId, It.IsAny<UpdateUsuarioDto>(), empresaA,
            It.IsAny<CancellationToken>()), Times.Once);
        escritor.Verify(x => x.ActualizarAsync(
            It.IsAny<int>(), It.IsAny<UpdateUsuarioDto>(), empresaB,
            It.IsAny<CancellationToken>()), Times.Never);
        escritor.VerifyNoOtherCalls();
    }
}
