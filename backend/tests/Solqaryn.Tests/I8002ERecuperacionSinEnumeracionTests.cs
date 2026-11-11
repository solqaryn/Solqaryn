using System;
using System.Text.Json;
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
/// I8-002.E: prueba negativa de no enumeracion en la respuesta publica
/// del unico flujo anonimo de autenticacion actualmente expuesto.
/// No simula recuperacion por correo ni prueba SMTP o tokens de reset.
/// </summary>
public sealed class I8002ERecuperacionSinEnumeracionTests
{
    [Fact]
    public async Task Login_fallido_no_expone_usuario_ni_tenant_aun_con_cabecera_falsificada()
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((LoginResponseDto?)null);

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(a => a.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.ConsultarHistorial,
                It.IsAny<string>(),
                null,
                "Sesion",
                null,
                null,
                null,
                "Rechazado",
                "Credenciales invalidas"))
            .Returns(Task.CompletedTask);

        string? respuestaAnterior = null;
        var casos = new[]
        {
            (Usuario: "usuario-simulado-empresa-a", EmpresaFalsificada: "91234"),
            (Usuario: "identidad-sintetica-inexistente", EmpresaFalsificada: "99876")
        };

        foreach (var caso in casos)
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["X-Empresa-Id"] = caso.EmpresaFalsificada;
            var controller = new AuthController(auth.Object, auditoria.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = http }
            };

            var resultado = await controller.Login(new LoginRequestDto
            {
                NombreUsuario = caso.Usuario,
                Password = "clave-sintetica-incorrecta"
            });

            var rechazo = Assert.IsType<UnauthorizedObjectResult>(resultado);
            var cuerpo = Assert.IsType<ApiResponse<object>>(rechazo.Value);
            Assert.False(cuerpo.Success);
            Assert.Null(cuerpo.Data);
            Assert.Empty(cuerpo.Errors);
            Assert.Equal("Usuario o contrasena incorrectos.", cuerpo.Message);

            var serializado = JsonSerializer.Serialize(cuerpo);
            Assert.DoesNotContain(caso.Usuario, serializado, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(caso.EmpresaFalsificada, serializado);
            Assert.DoesNotContain("clave-sintetica-incorrecta", serializado);
            if (respuestaAnterior is not null)
                Assert.Equal(respuestaAnterior, serializado);
            respuestaAnterior = serializado;
        }

        auth.Verify(s => s.LoginAsync(It.IsAny<LoginRequestDto>()), Times.Exactly(2));
        auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.ConsultarHistorial,
            It.IsAny<string>(),
            null,
            "Sesion",
            null,
            null,
            null,
            "Rechazado",
            "Credenciales invalidas"), Times.Exactly(2));
    }
}
