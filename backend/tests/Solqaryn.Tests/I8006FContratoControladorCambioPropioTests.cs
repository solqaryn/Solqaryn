using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-006.F: recibo de contrato controlador PUT /perfil/password sin ID externo.
/// Prueba dirigida con servicio simulado, NO prueba HTTP/JWT, rate-limit, DB ni
/// persistencia real; complementa I8-006.A en la capa de servicio.
/// </summary>
public sealed class I8006FContratoControladorCambioPropioTests
{
    [Fact]
    public async Task CambiarPassword_ContratoPropio_DesestimaIdDeRutaYDelegacionEsUnica()
    {
        var metodo = typeof(PerfilController).GetMethod(nameof(PerfilController.CambiarPassword));
        Assert.NotNull(metodo);
        Assert.NotNull(typeof(PerfilController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("perfil", typeof(PerfilController).GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal("password", metodo!.GetCustomAttribute<HttpPutAttribute>()?.Template);
        Assert.Single(metodo.GetParameters());
        Assert.Equal(typeof(CambiarPasswordPropiaDto), metodo.GetParameters()[0].ParameterType);
        Assert.Null(typeof(CambiarPasswordPropiaDto).GetProperty("UsuarioId"));

        var dto = new CambiarPasswordPropiaDto
        {
            PasswordActual = "ClaveSintetica#2026A",
            PasswordNueva = "ClaveSintetica#2026B"
        };
        var servicio = new Mock<IPerfilService>(MockBehavior.Strict);
        servicio.Setup(x => x.CambiarPasswordAsync(dto)).Returns(Task.CompletedTask);
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers["X-Usuario-Id"] = "999";
        contexto.Request.RouteValues["usuarioId"] = "999";
        var controlador = new PerfilController(servicio.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = contexto }
        };

        var resultado = await controlador.CambiarPassword(dto);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var respuesta = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.True(respuesta.Success);
        Assert.Equal("Contraseña actualizada correctamente.", respuesta.Message);
        servicio.Verify(x => x.CambiarPasswordAsync(dto), Times.Once);
        servicio.VerifyNoOtherCalls();
    }
}
