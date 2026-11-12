using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-002.A: recibo adicional de no enumeracion entre servicio real de
/// autenticacion y controlador publico. No crea un flujo de reseteo:
/// el endpoint de token de un solo uso y SMTP requieren evidencia separada.
/// </summary>
public sealed class I8002ARecuperacionEmpresaContratoNegativoTests
{
    [Fact]
    public async Task LoginInvalido_UsuarioExistenteOInexistente_TieneRechazoIndistinguibleSinToken()
    {
        const string existente = "i8002a-usuario-sintetico-empresa-a";
        const string inexistente = "i8002a-identidad-ficticia-no-registrada";
        const string claveIncorrecta = "i8002a-clave-invalida-de-prueba";
        const string empresaFalsificada = "987654";

        var usuario = new Usuario
        {
            Id = 502,
            NombreUsuario = existente,
            NombreCompleto = "Cuenta sintetica de test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave-diferente-sintetica"),
            RolId = 502,
            Activo = true
        };

        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(r => r.GetByNombreUsuarioAsync(existente))
            .ReturnsAsync(usuario);
        repositorio.Setup(r => r.GetByNombreUsuarioAsync(inexistente))
            .ReturnsAsync((Usuario?)null);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(
            repositorio.Object,
            Mock.Of<IRolRepository>(),
            jwt.Object);

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(a => a.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.ConsultarHistorial,
                "Intento fallido de inicio de sesion.",
                null,
                "Sesion",
                null,
                null,
                null,
                "Rechazado",
                "Credenciales invalidas"))
            .Returns(Task.CompletedTask);

        string? respuestaAnterior = null;
        foreach (var nombre in new[] { existente, inexistente })
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["X-Empresa-Id"] = empresaFalsificada;
            var controlador = new AuthController(servicio, auditoria.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = http }
            };

            var resultado = await controlador.Login(new LoginRequestDto
            {
                NombreUsuario = nombre,
                Password = claveIncorrecta
            });

            var rechazo = Assert.IsType<UnauthorizedObjectResult>(resultado);
            Assert.Equal(StatusCodes.Status401Unauthorized, rechazo.StatusCode);
            var cuerpo = Assert.IsType<ApiResponse<object>>(rechazo.Value);
            Assert.False(cuerpo.Success);
            Assert.Null(cuerpo.Data);
            Assert.Empty(cuerpo.Errors);
            Assert.Equal("Usuario o contrasena incorrectos.", cuerpo.Message);

            var respuesta = JsonSerializer.Serialize(cuerpo);
            Assert.DoesNotContain(nombre, respuesta, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(empresaFalsificada, respuesta, StringComparison.Ordinal);
            Assert.DoesNotContain(claveIncorrecta, respuesta, StringComparison.Ordinal);
            Assert.DoesNotContain("Token", respuesta, StringComparison.OrdinalIgnoreCase);
            if (respuestaAnterior is not null)
                Assert.Equal(respuestaAnterior, respuesta);
            respuestaAnterior = respuesta;
        }

        repositorio.Verify(r => r.GetByNombreUsuarioAsync(existente), Times.Once);
        repositorio.Verify(r => r.GetByNombreUsuarioAsync(inexistente), Times.Once);
        repositorio.VerifyNoOtherCalls();
        jwt.VerifyNoOtherCalls();
        auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.ConsultarHistorial,
            "Intento fallido de inicio de sesion.",
            null,
            "Sesion",
            null,
            null,
            null,
            "Rechazado",
            "Credenciales invalidas"), Times.Exactly(2));
        auditoria.VerifyNoOtherCalls();
    }
}
