using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
/// I8-010.F: fixture dirigida para certificar recuperación administrativa por roles.
/// Condición medible: aun con tenant server-side verificado, negar el permiso
/// AsignarRol o RestablecerContrasena debe impedir cualquier persistencia.
/// Esta prueba es sintética; no acredita correos, JWT ni MySQL reales.
/// </summary>
public sealed class I8010FPermisoRevocadoImpideResetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenantVerificadoConPermisoDenegado_NoActualizaClaveNiRol(
        bool intentaElevarRol)
    {
        const int usuarioId = 87;
        const int empresaAutorizada = 11;
        const int empresaFalsificada = 22;
        const string claveSintetica = "I8010F-CLAVE-FICTICIA-DE-PRUEBA";

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuarioId)).ReturnsAsync(
            new UsuarioDetalleDto
            {
                Id = usuarioId,
                RolId = 5,
                Rol = "Operador",
                Activo = true
            });

        var accionDenegada = intentaElevarRol
            ? AccionPermiso.AsignarRol
            : AccionPermiso.RestablecerContrasena;
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(
                empresaAutorizada, ModuloSistema.Usuarios, accionDenegada))
            .ThrowsAsync(new ForbiddenAccessException(
                "Permiso sintetico denegado para este rol."));

        var escritor = new Mock<IUsuarioAdministracionEmpresaService>(
            MockBehavior.Strict);
        using var provider = new ServiceCollection()
            .AddSingleton<IUsuarioAdministracionEmpresaService>(escritor.Object)
            .BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = provider };
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
            empresaAutorizada;
        http.Request.Headers["X-Empresa-Id"] = empresaFalsificada.ToString();

        var controlador = new UsuariosController(
            usuarios.Object, permisos.Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        var cambio = new UpdateUsuarioDto
        {
            RolId = intentaElevarRol ? 6 : 5,
            Rol = intentaElevarRol ? "Administrador" : "Operador",
            NuevaPassword = claveSintetica
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controlador.Update(usuarioId, cambio));

        // Evidencia esperada: la identidad se consulta una vez, el permiso del
        // tenant realmente marcado se valida y la escritura nunca se alcanza.
        usuarios.Verify(x => x.GetByIdAsync(usuarioId), Times.Once);
        usuarios.VerifyNoOtherCalls();
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaAutorizada, ModuloSistema.Usuarios, accionDenegada),
            Times.Once);
        permisos.VerifyNoOtherCalls();
        escritor.VerifyNoOtherCalls();
        Assert.Equal(
            empresaAutorizada,
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey]);
    }
}
