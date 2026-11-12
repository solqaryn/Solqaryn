using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-009.E: el rechazo de login no copia un nombre de usuario arbitrario
/// (que puede incluir un enlace de reset o token) a la auditoria.
/// Fixtures sinteticos; no prueba alertas externas, tokens reales, HTTP 429,
/// JWT ya emitidos ni persistencia MySQL.
/// </summary>
public sealed class I8009ELoginAuditoriaSinSecretoTests
{
    [Theory]
    [InlineData("https://test.invalid/restablecer?token=I8009E-TOKEN-FICTICIO", "I8009E-TOKEN-FICTICIO")]
    [InlineData("Bearer-I8009E-SECRETO-FICTICIO", "I8009E-SECRETO-FICTICIO")]
    public async Task I8009E_LoginRechazado_NoRegistraTokenNiEnlaceControlado(
        string nombreControlado, string marcador)
    {
        const string claveFicticia = "I8009E-CLAVE-SINTETICA";
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((LoginResponseDto?)null);

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var controller = new AuthController(auth.Object, auditoria.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var respuesta = await controller.Login(new LoginRequestDto
        {
            NombreUsuario = nombreControlado,
            Password = claveFicticia
        });

        var rechazo = Assert.IsType<UnauthorizedObjectResult>(respuesta);
        Assert.Equal(StatusCodes.Status401Unauthorized, rechazo.StatusCode);
        var cuerpo = Assert.IsType<ApiResponse<object>>(rechazo.Value);
        Assert.False(cuerpo.Success);
        Assert.Null(cuerpo.Data);
        Assert.Equal("Usuario o contrasena incorrectos.", cuerpo.Message);
        Assert.DoesNotContain(marcador, JsonSerializer.Serialize(cuerpo),
            StringComparison.OrdinalIgnoreCase);

        var llamada = Assert.Single(auditoria.Invocations);
        Assert.Equal(ModuloSistema.Usuarios, llamada.Arguments[0]);
        Assert.Equal(AccionPermiso.ConsultarHistorial, llamada.Arguments[1]);
        Assert.Equal("Sesion", llamada.Arguments[4]);
        Assert.Equal("Rechazado", llamada.Arguments[8]);
        Assert.Equal("Credenciales invalidas", llamada.Arguments[9]);
        Assert.Equal("Intento fallido de inicio de sesion.", llamada.Arguments[2]);

        var registro = string.Join("|", llamada.Arguments.Select(x => x?.ToString() ?? ""));
        Assert.DoesNotContain(marcador, registro, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", registro, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(claveFicticia, registro, StringComparison.Ordinal);
        auth.Verify(x => x.LoginAsync(It.IsAny<LoginRequestDto>()), Times.Once);
    }
}
