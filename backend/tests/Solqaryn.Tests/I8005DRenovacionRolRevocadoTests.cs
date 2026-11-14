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
/// I8-005.D: prueba causal de no reemision de un token al revocar el rol
/// relacional entre dos renovaciones. No invalida retrospectivamente un JWT
/// ya emitido ni demuestra resistencia HTTP ante fuerza bruta distribuida.
/// </summary>
public sealed class I8005DRenovacionRolRevocadoTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task I8005D_Renovar_no_emite_otro_token_si_el_rol_fue_revocado(
        bool desactivado, bool eliminado)
    {
        var rol = new Rol { Id = 905, Nombre = "Operador", Activo = true };
        var usuario = new Usuario
        {
            Id = 805,
            NombreUsuario = "operador-i8005d",
            NombreCompleto = "Operador de prueba",
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true
        };

        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(x => x.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);

        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var expiration = DateTime.UtcNow.AddMinutes(10);
        jwt.Setup(x => x.GenerarToken(usuario))
            .Returns(("token-sintetico-no-firmado", expiration));

        var sut = new AuthService(
            repository.Object, Mock.Of<IRolRepository>(), jwt.Object);

        var anterior = await sut.RenovarAsync(usuario.Id);
        Assert.NotNull(anterior);
        Assert.Equal("token-sintetico-no-firmado", anterior!.Token);

        rol.Activo = !desactivado;
        rol.Eliminado = eliminado;

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => sut.RenovarAsync(usuario.Id));

        repository.Verify(x => x.GetByIdAsync(usuario.Id), Times.Exactly(2));
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
    }
}
