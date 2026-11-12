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
/// I8-009.F / PREPARAR CASO: fixture y resultado esperado de login
/// con credencial correcta y estado de cuenta revocado. Complementa
/// I8-009.C/D (renovacion) y no acredita invalidacion de JWT previos,
/// alertas, middleware HTTP, base MySQL ni notificaciones externas.
/// </summary>
public sealed class I8009FLoginBloqueoSinNuevoJwtTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Login_con_clave_correcta_y_cuenta_no_vigente_no_emite_jwt(
        bool bloqueado, bool activo)
    {
        const string nombre = "i8009f-fixture-cuenta";
        const string claveSintetica = "i8009f-clave-prueba-no-real";
        var usuario = new Usuario
        {
            Id = 8096,
            NombreUsuario = nombre,
            NombreCompleto = "Cuenta sintética I8-009.F",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(claveSintetica),
            RolId = 42,
            Bloqueado = bloqueado,
            Activo = activo,
            Eliminado = false
        };

        var usuarios = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        usuarios.Setup(r => r.GetByNombreUsuarioAsync(nombre)).ReturnsAsync(usuario);
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(usuarios.Object, roles.Object, jwt.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => servicio.LoginAsync(new LoginRequestDto
            {
                NombreUsuario = nombre,
                Password = claveSintetica
            }));

        Assert.DoesNotContain(claveSintetica, error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(usuario.PasswordHash, error.Message, StringComparison.Ordinal);
        usuarios.Verify(r => r.GetByNombreUsuarioAsync(nombre), Times.Once);
        usuarios.VerifyNoOtherCalls();
        jwt.Verify(j => j.GenerarToken(It.IsAny<Usuario>()), Times.Never);
        jwt.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_cuenta_vigente_rol_activo_con_clave_correcta_emite_un_jwt_sintetico()
    {
        const string nombre = "i8009f-fixture-positivo";
        const string claveSintetica = "i8009f-clave-prueba-positiva";
        var rol = new Rol
        {
            Id = 42,
            Nombre = "Operador fixture",
            NombreNormalizado = "OPERADOR_I8009F",
            Activo = true,
            Eliminado = false
        };
        var usuario = new Usuario
        {
            Id = 8097,
            NombreUsuario = nombre,
            NombreCompleto = "Cuenta sintética activa",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(claveSintetica),
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true,
            Bloqueado = false,
            Eliminado = false
        };
        var usuarios = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        usuarios.Setup(r => r.GetByNombreUsuarioAsync(nombre)).ReturnsAsync(usuario);
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var expira = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        jwt.Setup(j => j.GenerarToken(usuario)).Returns(("jwt-sintetico-no-firmado", expira));
        var servicio = new AuthService(usuarios.Object, roles.Object, jwt.Object);

        var resultado = await servicio.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = nombre,
            Password = claveSintetica
        });

        Assert.NotNull(resultado);
        Assert.Equal("jwt-sintetico-no-firmado", resultado!.Token);
        Assert.Equal(expira, resultado.ExpiraEn);
        Assert.Equal(nombre, resultado.NombreUsuario);
        usuarios.Verify(r => r.GetByNombreUsuarioAsync(nombre), Times.Once);
        jwt.Verify(j => j.GenerarToken(usuario), Times.Once);
    }
}
