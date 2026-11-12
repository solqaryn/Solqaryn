using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-001.A: cierre de auditoria de contrasenas ERP actuales.
/// Reutiliza las pruebas C/D/E y añade contraste de rechazo BCrypt,
/// rol relacional y contratos de entrada. No prueba correo de recuperacion,
/// middleware HTTP ni tokens firmados; emplea solo datos sinteticos.
/// </summary>
public sealed class I8001ACierreAuditoriaCredencialesTests
{
    private const string ClaveValida = "I8001A-Password-Sintetica-Vigente-2026";
    private const string UsuarioPrueba = "auditoria-erp-a";

    [Fact]
    public async Task ClaveIncorrectaNoEmiteJwtNiRevelaHash()
    {
        var usuario = CrearUsuarioConRolVigente();
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(x => x.GetByNombreUsuarioAsync(UsuarioPrueba))
            .ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(
            repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);

        var resultado = await servicio.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = " " + UsuarioPrueba + " ",
            Password = "I8001A-Clave-Equivocada"
        });

        Assert.Null(resultado);
        Assert.True(BCrypt.Net.BCrypt.Verify(ClaveValida, usuario.PasswordHash));
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
        repositorio.Verify(x => x.GetByNombreUsuarioAsync(UsuarioPrueba), Times.Once);
    }

    [Fact]
    public async Task HashCorrectoConRolRevocadoNoEmiteJwt()
    {
        var usuario = CrearUsuarioConRolVigente();
        usuario.RolEntidad.Activo = false;
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(x => x.GetByNombreUsuarioAsync(UsuarioPrueba))
            .ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(
            repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.LoginAsync(
            new LoginRequestDto { NombreUsuario = UsuarioPrueba, Password = ClaveValida }));
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task HashYRolValidosEmitenSoloTokenSinteticoSinPassword()
    {
        var usuario = CrearUsuarioConRolVigente();
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(x => x.GetByNombreUsuarioAsync(UsuarioPrueba))
            .ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var expira = DateTime.UtcNow.AddMinutes(10);
        jwt.Setup(x => x.GenerarToken(usuario))
            .Returns(("I8001A-TOKEN-NO-FIRMADO", expira));
        var servicio = new AuthService(
            repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);

        var respuesta = await servicio.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = UsuarioPrueba,
            Password = ClaveValida
        });

        Assert.NotNull(respuesta);
        Assert.Equal("I8001A-TOKEN-NO-FIRMADO", respuesta.Token);
        Assert.Equal(expira, respuesta.ExpiraEn);
        Assert.DoesNotContain(ClaveValida, JsonSerializer.Serialize(respuesta));
        Assert.DoesNotContain(usuario.PasswordHash, JsonSerializer.Serialize(respuesta));
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
    }

    [Fact]
    public void EntradaDeRestablecimientoNoEquivaleARecuperacionPorCorreo()
    {
        Assert.NotNull(typeof(UsuariosController)
            .GetCustomAttribute<AuthorizeAttribute>());
        var edicion = typeof(UsuariosController).GetMethod(nameof(UsuariosController.Update));
        Assert.NotNull(edicion);
        Assert.NotNull(edicion.GetCustomAttribute<HttpPutAttribute>());
        Assert.NotNull(edicion.GetCustomAttribute<RequierePermisoAttribute>());
        Assert.NotNull(typeof(UpdateUsuarioDto).GetProperty(nameof(UpdateUsuarioDto.NuevaPassword)));

        var login = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        Assert.NotNull(login);
        Assert.NotNull(login.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("AuthLogin",
            login.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        // Solo contratos y metadatos: NO acredita reset de correo, HTTP ni JWT.
    }

    private static Usuario CrearUsuarioConRolVigente()
    {
        var rol = new Rol
        {
            Id = 801,
            Nombre = "Operador ERP",
            Activo = true,
            Eliminado = false
        };
        return new Usuario
        {
            Id = 8001,
            NombreUsuario = UsuarioPrueba,
            NombreCompleto = "Cuenta sintetica de auditoria",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ClaveValida, workFactor: 4),
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true
        };
    }
}
