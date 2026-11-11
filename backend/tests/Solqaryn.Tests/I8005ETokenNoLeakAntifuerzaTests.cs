using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-005.E: regresion delimitada del contrato de fallo de login y renovacion.
/// Los secretos sinteticos nunca deben aparecer en auditoria ni en respuestas
/// rechazadas. La ejecucion HTTP real del limiter 429, la invalidacion de JWT
/// ya firmados y el almacenamiento MySQL requieren pruebas independientes.
/// </summary>
public sealed class I8005ETokenNoLeakAntifuerzaTests
{
    [Fact]
    public async Task I8005E_LoginRechazado_RespuestaGenericaSinPasswordNiBearerEnAuditoria()
    {
        const string contrasena = "I8005E-Clave-Sintetica-No-Real";
        const string token = "I8005E-Token-Sintetico-No-Firmado";
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

        var http = new DefaultHttpContext();
        http.Request.Headers["Authorization"] = "Bearer " + token;
        var controller = new AuthController(auth.Object, auditoria.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        var mensajes = new List<string>();

        foreach (var nombre in new[] { "cuenta-existente", "cuenta-desconocida" })
        {
            var resultado = await controller.Login(new LoginRequestDto
            {
                NombreUsuario = nombre,
                Password = contrasena
            });
            var rechazo = Assert.IsType<UnauthorizedObjectResult>(resultado);
            Assert.Equal(StatusCodes.Status401Unauthorized, rechazo.StatusCode);
            var cuerpo = Assert.IsType<ApiResponse<object>>(rechazo.Value);
            Assert.False(cuerpo.Success);
            Assert.Null(cuerpo.Data);
            mensajes.Add(cuerpo.Message);
            Assert.False(JsonSerializer.Serialize(cuerpo).Contains(token, StringComparison.Ordinal));
            Assert.False(JsonSerializer.Serialize(cuerpo).Contains(contrasena, StringComparison.Ordinal));
        }

        Assert.All(mensajes, m => Assert.Equal("Usuario o contrasena incorrectos.", m));
        Assert.Equal(2, auditoria.Invocations.Count);
        var registro = string.Join("|", auditoria.Invocations
            .SelectMany(i => i.Arguments)
            .Select(a => a?.ToString() ?? string.Empty));
        Assert.False(registro.Contains(token, StringComparison.Ordinal));
        Assert.False(registro.Contains(contrasena, StringComparison.Ordinal));
        Assert.False(http.Response.Headers.ContainsKey("Location"));
        Assert.Equal("AuthLogin", typeof(AuthController)
            .GetMethod(nameof(AuthController.Login))!
            .GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        auth.Verify(x => x.LoginAsync(It.IsAny<LoginRequestDto>()), Times.Exactly(2));
    }

    [Fact]
    public async Task I8005E_RenovacionRevocada_NoRetornaBearerNiRedireccion()
    {
        const string token = "I8005E-Token-Rechazado-No-Firmado";
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(x => x.RenovarAsync(805))
            .ReturnsAsync((LoginResponseDto?)null);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var http = new DefaultHttpContext();
        http.Request.Headers["Authorization"] = "Bearer " + token;
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("id", "805") }, "prueba-sintetica"));

        var controller = new AuthController(auth.Object, auditoria.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        var resultado = await controller.Renovar();
        var rechazo = Assert.IsType<UnauthorizedObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status401Unauthorized, rechazo.StatusCode);
        var cuerpo = Assert.IsType<ApiResponse<object>>(rechazo.Value);
        Assert.False(cuerpo.Success);
        Assert.Null(cuerpo.Data);
        Assert.Equal("La sesión ya no es válida.", cuerpo.Message);
        Assert.False(JsonSerializer.Serialize(cuerpo).Contains(token, StringComparison.Ordinal));
        Assert.False(http.Response.Headers.ContainsKey("Location"));
        auth.Verify(x => x.RenovarAsync(805), Times.Once);
        auditoria.VerifyNoOtherCalls();
    }
}
