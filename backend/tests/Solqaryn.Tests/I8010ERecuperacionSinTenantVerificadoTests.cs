using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-010.E: prueba negativa de recuperacion administrativa por roles.
/// Un header de empresa o un RolId solicitado no sustituyen el tenant verificado
/// por el gate RBAC. No utiliza cuentas compartidas, SMTP ni MySQL reales.
/// </summary>
public sealed class I8010ERecuperacionSinTenantVerificadoTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetConHeaderFalsificadoSinMarcaTenant_RechazaAntesDePermisosYEscritura(
        bool solicitaElevacionDeRol)
    {
        const int usuarioId = 87;
        const int empresaFalsa = 22;
        const string claveSintetica = "I8010E-CLAVE-FICTICIA";

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuarioId)).ReturnsAsync(
            new UsuarioDetalleDto
            {
                Id = usuarioId, RolId = 5, Rol = "Operador", Activo = true
            });

        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var escritor = new Mock<IUsuarioAdministracionEmpresaService>(MockBehavior.Strict);
        using var servicios = new ServiceCollection()
            .AddSingleton<IUsuarioAdministracionEmpresaService>(escritor.Object)
            .BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = servicios };
        // No se establece VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey.
        // Una cabecera controlada por cliente no concede permiso empresarial.
        http.Request.Headers["X-Empresa-Id"] = empresaFalsa.ToString();

        var controller = new UsuariosController(
            usuarios.Object, permisos.Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        var cambio = new UpdateUsuarioDto
        {
            RolId = solicitaElevacionDeRol ? 6 : 5,
            Rol = solicitaElevacionDeRol ? "Administrador" : "Operador",
            NuevaPassword = claveSintetica
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.Update(usuarioId, cambio));

        // Ambos caminos (cambio de rol y reset sin cambio) deben exigir
        // la marca server-side antes de consultar permisos o persistir.
        usuarios.Verify(x => x.GetByIdAsync(usuarioId), Times.Once);
        usuarios.VerifyNoOtherCalls();
        permisos.VerifyNoOtherCalls();
        escritor.VerifyNoOtherCalls();
        Assert.False(http.Items.ContainsKey(
            Solqaryn.Application.Security.VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey));
    }
}
