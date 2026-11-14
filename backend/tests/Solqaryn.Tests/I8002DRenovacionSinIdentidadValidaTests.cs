using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-002.D: limita la reutilizacion del endpoint protegido de renovacion
/// como via de recuperacion sin identidad valida. Es una prueba de controlador,
/// no una simulacion de middleware HTTP, JWT o aislamiento MySQL.
/// </summary>
public sealed class I8002DRenovacionSinIdentidadValidaTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("no-numerico")]
    [InlineData("2147483648")]
    public async Task Renovar_rechaza_id_faltante_o_invalido_sin_llamar_al_servicio(string? id)
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "42") };
        if (id is not null)
            claims.Add(new Claim("id", id));

        var controller = CrearController(auth, claims, "999");

        var response = await controller.Renovar();

        Assert.IsType<UnauthorizedObjectResult>(response);
        auth.Verify(s => s.RenovarAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Renovar_rechaza_sesion_invalida_aunque_el_id_sea_numerico()
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(s => s.RenovarAsync(42)).ReturnsAsync((LoginResponseDto?)null);
        var controller = CrearController(auth, new[] { new Claim("id", "42") }, "1");

        var response = await controller.Renovar();

        Assert.IsType<UnauthorizedObjectResult>(response);
        auth.Verify(s => s.RenovarAsync(42), Times.Once);
    }

    private static AuthController CrearController(
        Mock<IAuthService> auth, IEnumerable<Claim> claims, string headerEmpresa)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "regresion-i8-002-d"))
        };
        http.Request.Headers["X-Empresa-Id"] = headerEmpresa;

        return new AuthController(auth.Object, Mock.Of<IAuditoriaService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }
}
