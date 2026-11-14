using System;
using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-009.C: la renovación de credenciales no puede emitir otro JWT
/// cuando el usuario ha sido bloqueado o desactivado después del acceso original.
/// Prueba de servicio aislada: no acredita firma JWT, middleware HTTP ni alertas externas.
/// </summary>
public sealed class I8009CRevocacionRenovacionTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Renovar_rechaza_usuario_bloqueado_o_inactivo_sin_emitir_jwt(
        bool bloqueado, bool activo)
    {
        const int usuarioId = 8009;
        var usuario = new Usuario
        {
            Id = usuarioId,
            NombreUsuario = "i8009c-usuario",
            NombreCompleto = "Cuenta de prueba revocada",
            PasswordHash = "fixture-sin-secreto",
            Activo = activo,
            Bloqueado = bloqueado
        };

        var usuarios = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuarioId)).ReturnsAsync(usuario);
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(usuarios.Object, roles.Object, jwt.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => servicio.RenovarAsync(usuarioId));

        usuarios.Verify(x => x.GetByIdAsync(usuarioId), Times.Once);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task Renovar_cuenta_vigente_con_rol_activo_emite_solo_un_jwt()
    {
        const int usuarioId = 8010;
        var rol = new Rol
        {
            Id = 42, Nombre = "Operador", NombreNormalizado = "OPERADOR_I8009C",
            Activo = true, Eliminado = false
        };
        var usuario = new Usuario
        {
            Id = usuarioId,
            NombreUsuario = "i8009c-activo",
            NombreCompleto = "Cuenta vigente",
            PasswordHash = "fixture-sin-secreto",
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true,
            Bloqueado = false
        };

        var usuarios = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuarioId)).ReturnsAsync(usuario);
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var expiracion = DateTime.UtcNow.AddMinutes(35);
        jwt.Setup(x => x.GenerarToken(usuario))
            .Returns(("token-prueba-unicamente", expiracion));
        var servicio = new AuthService(usuarios.Object, roles.Object, jwt.Object);

        var resultado = await servicio.RenovarAsync(usuarioId);

        Assert.NotNull(resultado);
        Assert.Equal("token-prueba-unicamente", resultado!.Token);
        Assert.Equal(expiracion, resultado.ExpiraEn);
        usuarios.Verify(x => x.GetByIdAsync(usuarioId), Times.Once);
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
    }
}
