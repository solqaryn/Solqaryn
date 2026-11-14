using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-006.D: un cambio voluntario solo procede desde una cuenta vigente
/// y nunca sirve para reactivar una cuenta o reciclar su contrasena.
/// </summary>
public class I8006DCambioPasswordCuentaVigenteTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task I8006D_CambiarPasswordAsync_CuentaNoVigente_NoModificaHashNiPersiste(
        bool activo, bool bloqueado, bool eliminado)
    {
        const string actual = "ActualSegura#123";
        var usuario = new Usuario
        {
            Id = 25,
            NombreUsuario = "usuario25",
            NombreCompleto = "Usuario de prueba",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(actual),
            Activo = activo,
            Bloqueado = bloqueado,
            Eliminado = eliminado,
            FechaCreacion = DateTime.UtcNow.AddDays(-7)
        };
        var hashAnterior = usuario.PasswordHash;
        var usuarioRepo = new Mock<IUsuarioRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var auditoria = new Mock<IAuditoriaService>();
        var imagenStorage = new Mock<IPerfilImagenStorageService>();
        currentUser.Setup(x => x.UsuarioId).Returns(25);
        usuarioRepo.Setup(x => x.GetByIdAsync(25)).ReturnsAsync(usuario);
        var service = new PerfilService(usuarioRepo.Object, currentUser.Object,
            auditoria.Object, imagenStorage.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CambiarPasswordAsync(new CambiarPasswordPropiaDto
            {
                PasswordActual = actual,
                PasswordNueva = "NuevaSegura#456"
            }));

        Assert.Equal(hashAnterior, usuario.PasswordHash);
        Assert.Null(usuario.FechaActualizacion);
        usuarioRepo.Verify(x => x.GetByIdAsync(25), Times.Once);
        usuarioRepo.Verify(x => x.GetByIdAsync(It.Is<int>(id => id != 25)), Times.Never);
        usuarioRepo.Verify(x => x.Update(It.IsAny<Usuario>()), Times.Never);
        usuarioRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task I8006D_CambiarPasswordAsync_NoPermiteReutilizarLaClaveActual()
    {
        const string actual = "ActualSegura#123";
        var usuario = new Usuario
        {
            Id = 25,
            NombreUsuario = "usuario25",
            NombreCompleto = "Usuario de prueba",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(actual),
            Activo = true,
            Bloqueado = false,
            Eliminado = false,
            FechaCreacion = DateTime.UtcNow.AddDays(-7)
        };
        var hashAnterior = usuario.PasswordHash;
        var usuarioRepo = new Mock<IUsuarioRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var auditoria = new Mock<IAuditoriaService>();
        var imagenStorage = new Mock<IPerfilImagenStorageService>();
        currentUser.Setup(x => x.UsuarioId).Returns(25);
        usuarioRepo.Setup(x => x.GetByIdAsync(25)).ReturnsAsync(usuario);
        var service = new PerfilService(usuarioRepo.Object, currentUser.Object,
            auditoria.Object, imagenStorage.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CambiarPasswordAsync(new CambiarPasswordPropiaDto
            {
                PasswordActual = actual,
                PasswordNueva = actual
            }));

        Assert.Equal(hashAnterior, usuario.PasswordHash);
        usuarioRepo.Verify(x => x.Update(It.IsAny<Usuario>()), Times.Never);
        usuarioRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.VerifyNoOtherCalls();
    }
}
