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
/// I8-009.D: una cuenta vigente no puede renovar JWT con rol relacional
/// revocado, eliminado, sin enlace válido o sin entidad de rol.
/// Prueba de AuthService aislada: no acredita HTTP, alertas ni revocación
/// retroactiva de JWT ya emitidos.
/// </summary>
public sealed class I8009DRevocacionRolRenovacionTests
{
    [Theory]
    [InlineData(false, false, 42, true)]  // Rol inactivo.
    [InlineData(true, true, 42, true)]    // Rol eliminado.
    [InlineData(true, false, 0, true)]    // FK RolId inválida.
    [InlineData(true, false, 42, false)] // Entidad relacional ausente.
    public async Task Renovar_no_emite_jwt_si_el_rol_ha_perdido_vigencia(
        bool rolActivo, bool rolEliminado, int rolId, bool tieneRolEntidad)
    {
        const int usuarioId = 80094;
        var rol = new Rol
        {
            Id = 42,
            Nombre = "Operador de prueba",
            NombreNormalizado = "OPERADOR_I8009D",
            Activo = rolActivo,
            Eliminado = rolEliminado
        };
        var usuario = new Usuario
        {
            Id = usuarioId,
            NombreUsuario = "i8009d-cuenta",
            NombreCompleto = "Cuenta sintética",
            PasswordHash = "fixture-sin-clave-real",
            Activo = true,
            Bloqueado = false,
            RolId = rolId,
            RolEntidad = tieneRolEntidad ? rol : null!
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
    public async Task Renovar_no_emite_jwt_para_usuario_eliminado()
    {
        const int usuarioId = 80095;
        var rol = new Rol
        {
            Id = 42,
            Nombre = "Operador de prueba",
            NombreNormalizado = "OPERADOR_I8009D",
            Activo = true,
            Eliminado = false
        };
        var usuario = new Usuario
        {
            Id = usuarioId,
            NombreUsuario = "i8009d-eliminado",
            NombreCompleto = "Cuenta sintética eliminada",
            PasswordHash = "fixture-sin-clave-real",
            Activo = true,
            Bloqueado = false,
            Eliminado = true,
            RolId = rol.Id,
            RolEntidad = rol
        };
        var usuarios = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuarioId)).ReturnsAsync(usuario);
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(usuarios.Object, roles.Object, jwt.Object);

        Assert.Null(await servicio.RenovarAsync(usuarioId));
        usuarios.Verify(x => x.GetByIdAsync(usuarioId), Times.Once);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }
}
