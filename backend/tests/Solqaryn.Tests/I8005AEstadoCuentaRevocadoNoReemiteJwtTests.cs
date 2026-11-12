using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-005.A: regresion negativa de renovacion tras revocacion de cuenta.
/// Prueba AuthService real con usuario y rol sinteticos; nunca emite JWT real.
/// No certifica digest persistido, single-use, rotacion de refresh tokens,
/// limitacion HTTP efectiva ni MySQL/telemetria de produccion.
/// </summary>
public sealed class I8005AEstadoCuentaRevocadoNoReemiteJwtTests
{
    [Theory]
    [InlineData("bloqueada")]
    [InlineData("desactivada")]
    [InlineData("eliminada")]
    public async Task RevocacionPosterior_NoRenuevaNiPermiteLoginConClaveCorrecta(
        string estadoRevocado)
    {
        const string clave = "I8005A-Clave-Unica-Sintetica-No-Real";
        const string tokenSintetico = "I8005A-JWT-No-Firmado-Para-Test";
        var rol = new Rol { Id = 905, Nombre = "Operador", Activo = true };
        var usuario = new Usuario
        {
            Id = 805,
            NombreUsuario = "usuario-i8005a-sintetico",
            NombreCompleto = "Usuario prueba",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(clave),
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true,
            Bloqueado = false,
            Eliminado = false
        };

        var repository = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        repository.Setup(x => x.GetByNombreUsuarioAsync(usuario.NombreUsuario))
            .ReturnsAsync(usuario);

        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        jwt.Setup(x => x.GenerarToken(usuario))
            .Returns((tokenSintetico, DateTime.UtcNow.AddMinutes(10)));

        var sut = new AuthService(
            repository.Object, Mock.Of<IRolRepository>(), jwt.Object);

        // La renovacion inicial es valida; el estado cambia despues.
        var antes = await sut.RenovarAsync(usuario.Id);
        Assert.NotNull(antes);
        Assert.Equal(tokenSintetico, antes!.Token);

        switch (estadoRevocado)
        {
            case "bloqueada":
                usuario.Bloqueado = true;
                usuario.MotivoBloqueo = "Prueba de revocacion";
                break;
            case "desactivada":
                usuario.Activo = false;
                break;
            case "eliminada":
                usuario.Eliminado = true;
                break;
            default:
                throw new InvalidOperationException("Estado de prueba desconocido.");
        }

        var credenciales = new LoginRequestDto
        {
            NombreUsuario = usuario.NombreUsuario,
            Password = clave
        };

        if (usuario.Eliminado)
        {
            Assert.Null(await sut.RenovarAsync(usuario.Id));
            Assert.Null(await sut.LoginAsync(credenciales));
        }
        else
        {
            var renovacion = await Assert.ThrowsAsync<BusinessRuleException>(
                () => sut.RenovarAsync(usuario.Id));
            var login = await Assert.ThrowsAsync<BusinessRuleException>(
                () => sut.LoginAsync(credenciales));
            Assert.DoesNotContain(clave, renovacion.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(tokenSintetico, renovacion.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(clave, login.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(tokenSintetico, login.Message, StringComparison.Ordinal);
        }

        repository.Verify(x => x.GetByIdAsync(usuario.Id), Times.Exactly(2));
        repository.Verify(x => x.GetByNombreUsuarioAsync(usuario.NombreUsuario), Times.Once);
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
        jwt.VerifyNoOtherCalls();
    }

    [Fact]
    public void LoginMantieneLimiteDeIntentosDeclarado()
    {
        // Comprobacion de contrato de ruta, no prueba HTTP 429 ni limitador distribuido.
        var metodo = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        var limiter = Attribute.GetCustomAttribute(
            metodo!, typeof(EnableRateLimitingAttribute))
            as EnableRateLimitingAttribute;
        Assert.NotNull(limiter);
        Assert.Equal("AuthLogin", limiter!.PolicyName);
    }
}
