using System;
using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-001.C: comprueba el contrato vigente del login ERP con BCrypt.
/// No acredita recuperacion, rotacion, MFA, JWT real ni autorizacion tenant HTTP.
/// </summary>
public sealed class I8001CCredencialesErpTests
{
    [Fact]
    public async Task Login_rechaza_password_incorrecto_sin_emitir_token()
    {
        var usuario = UsuarioValido();
        var (sut, repository, jwt) = CrearServicio(usuario);
        var result = await sut.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = "  operador  ",
            Password = "contraseña-incorrecta"
        });

        Assert.Null(result);
        repository.Verify(x => x.GetByNombreUsuarioAsync("operador"), Times.Once);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task Login_acepta_password_bcrypt_correcto_solo_con_rol_activo()
    {
        var usuario = UsuarioValido();
        var (sut, _, jwt) = CrearServicio(usuario);
        var expires = DateTime.UtcNow.AddMinutes(15);
        jwt.Setup(x => x.GenerarToken(usuario)).Returns(("token-sintetico-no-firmado", expires));

        var result = await sut.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = "operador",
            Password = "clave-correcta-test"
        });

        Assert.NotNull(result);
        Assert.Equal("token-sintetico-no-firmado", result.Token);
        Assert.Equal(expires, result.ExpiraEn);
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Login_no_emite_token_para_cuenta_bloqueada_o_inactiva(
        bool bloqueada, bool inactiva)
    {
        var usuario = UsuarioValido();
        usuario.Bloqueado = bloqueada;
        usuario.Activo = !inactiva;
        var (sut, _, jwt) = CrearServicio(usuario);

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.LoginAsync(
            new LoginRequestDto { NombreUsuario = "operador", Password = "clave-correcta-test" }));
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task Login_no_emite_token_para_usuario_eliminado()
    {
        var usuario = UsuarioValido();
        usuario.Eliminado = true;
        var (sut, _, jwt) = CrearServicio(usuario);

        var result = await sut.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = "operador",
            Password = "clave-correcta-test"
        });
        Assert.Null(result);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    private static Usuario UsuarioValido() => new()
    {
        Id = 801,
        NombreUsuario = "operador",
        NombreCompleto = "Operador de prueba",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave-correcta-test"),
        RolId = 9,
        RolEntidad = new Rol { Id = 9, Nombre = "Operador", Activo = true },
        Activo = true
    };

    private static (AuthService sut, Mock<IUsuarioRepository> repository, Mock<IJwtService> jwt)
        CrearServicio(Usuario usuario)
    {
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(x => x.GetByNombreUsuarioAsync("operador")).ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var sut = new AuthService(repository.Object, Mock.Of<IRolRepository>(), jwt.Object);
        return (sut, repository, jwt);
    }
}
