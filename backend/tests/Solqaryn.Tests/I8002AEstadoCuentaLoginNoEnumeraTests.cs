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
/// I8-002.A: una clave valida de cuenta bloqueada/inactiva no debe enumerar
/// identidad, tenant ni motivo de bloqueo mediante la ruta publica de login.
/// No representa el flujo de solicitud/consumo del token de recuperacion.
/// </summary>
public sealed class I8002AEstadoCuentaLoginNoEnumeraTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task LoginCuentaNoVigente_RespondeIgualQueIdentidadInexistente(
        bool bloqueado, bool activo)
    {
        const string nombre = "i8002a-identidad-privada";
        const string desconocido = "i8002a-usuario-inexistente";
        const string clave = "clave-correcta-solo-en-test";
        const string marcador = "TOKEN_INTERNO_DE_PRUEBA";
        var usuario = new Usuario
        {
            Id = 9002,
            NombreUsuario = nombre,
            NombreCompleto = "Identidad de prueba",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(clave),
            Bloqueado = bloqueado,
            Activo = activo,
            MotivoBloqueo = "Enlace privado https://test.invalid/reset?token=" + marcador,
            RolId = 9002
        };

        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(x => x.GetByNombreUsuarioAsync(nombre)).ReturnsAsync(usuario);
        repositorio.Setup(x => x.GetByNombreUsuarioAsync(desconocido)).ReturnsAsync((Usuario?)null);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(x => x.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.ConsultarHistorial,
            "Intento fallido de inicio de sesion.",
            null, "Sesion", null, null, null, "Rechazado", "Credenciales invalidas"))
            .Returns(Task.CompletedTask);

        string? respuestaBase = null;
        foreach (var identidad in new[] { nombre, desconocido })
        {
            var controller = new AuthController(servicio, auditoria.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            var result = await controller.Login(new LoginRequestDto
            {
                NombreUsuario = identidad,
                Password = clave
            });
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
            var body = Assert.IsType<ApiResponse<object>>(unauthorized.Value);
            Assert.False(body.Success);
            Assert.Equal("Usuario o contrasena incorrectos.", body.Message);
            Assert.Null(body.Data);
            var serialized = JsonSerializer.Serialize(body);
            Assert.DoesNotContain(nombre, serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(marcador, serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("test.invalid", serialized, StringComparison.OrdinalIgnoreCase);
            if (respuestaBase is not null) Assert.Equal(respuestaBase, serialized);
            respuestaBase = serialized;
        }

        jwt.VerifyNoOtherCalls();
        repositorio.VerifyAll();
        repositorio.VerifyNoOtherCalls();
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Usuarios, AccionPermiso.ConsultarHistorial,
            "Intento fallido de inicio de sesion.",
            null, "Sesion", null, null, null, "Rechazado", "Credenciales invalidas"), Times.Exactly(2));
        auditoria.VerifyNoOtherCalls();
    }
}
