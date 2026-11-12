using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-006.H: regresion positiva de la rotacion de contraseña PROPIA.
/// Comprueba que la credencial nueva reemplaza a la anterior y que un
/// intento de reutilizar la clave antigua no revierte ni persiste cambios.
/// Sólo repositorios sintéticos: NO es prueba HTTP, JWT ni MySQL real.
/// </summary>
public sealed class I8006HRotacionConsecutivaPasswordPropiaTests
{
    [Fact]
    public async Task RotarClavePropia_DosVeces_RechazaClaveAnteriorSinNuevaEscritura()
    {
        const string inicial = "ActualSegura#123";
        const string segunda = "NuevaSegura#456";
        const string tercera = "SiguienteSegura#789";
        var usuario = new Usuario
        {
            Id = 25,
            NombreUsuario = "i8006h-propio",
            NombreCompleto = "Usuario sintetico I8-006.H",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(inicial),
            Activo = true,
            Bloqueado = false,
            Eliminado = false
        };
        var hashInicial = usuario.PasswordHash;
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(r => r.GetByIdAsync(25)).ReturnsAsync(usuario);
        repositorio.Setup(r => r.Update(usuario));
        repositorio.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(i => i.UsuarioId).Returns(25);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(a => a.RegistrarAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var servicio = new PerfilService(
            repositorio.Object, identidad.Object, auditoria.Object,
            new Mock<IPerfilImagenStorageService>(MockBehavior.Strict).Object);

        await servicio.CambiarPasswordAsync(new CambiarPasswordPropiaDto
        {
            PasswordActual = inicial,
            PasswordNueva = segunda
        });

        var hashSegundo = usuario.PasswordHash;
        Assert.NotEqual(hashInicial, hashSegundo);
        Assert.True(BCrypt.Net.BCrypt.Verify(segunda, hashSegundo));
        Assert.False(BCrypt.Net.BCrypt.Verify(inicial, hashSegundo));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servicio.CambiarPasswordAsync(new CambiarPasswordPropiaDto
            {
                PasswordActual = inicial,
                PasswordNueva = tercera
            }));

        Assert.Equal(hashSegundo, usuario.PasswordHash);
        repositorio.Verify(r => r.SaveChangesAsync(), Times.Once);

        await servicio.CambiarPasswordAsync(new CambiarPasswordPropiaDto
        {
            PasswordActual = segunda,
            PasswordNueva = tercera
        });

        Assert.True(BCrypt.Net.BCrypt.Verify(tercera, usuario.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(segunda, usuario.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(inicial, usuario.PasswordHash));
        Assert.Equal(25, usuario.ActualizadoPorUsuarioId);
        Assert.NotNull(usuario.FechaActualizacion);

        repositorio.Verify(r => r.GetByIdAsync(25), Times.Exactly(3));
        repositorio.Verify(r => r.GetByIdAsync(It.Is<int>(id => id != 25)), Times.Never);
        repositorio.Verify(r => r.Update(usuario), Times.Exactly(2));
        repositorio.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        repositorio.VerifyNoOtherCalls();

        Assert.Equal(2, auditoria.Invocations.Count);
        foreach (var llamada in auditoria.Invocations)
        {
            Assert.Equal(ModuloSistema.Usuarios, llamada.Arguments[0]);
            Assert.Equal(AccionPermiso.RestablecerContrasena, llamada.Arguments[1]);
            Assert.Equal(25, llamada.Arguments[3]);
            var texto = string.Join("|", llamada.Arguments.Select(a => a?.ToString() ?? ""));
            Assert.DoesNotContain(inicial, texto, StringComparison.Ordinal);
            Assert.DoesNotContain(segunda, texto, StringComparison.Ordinal);
            Assert.DoesNotContain(tercera, texto, StringComparison.Ordinal);
            Assert.DoesNotContain(hashInicial, texto, StringComparison.Ordinal);
            Assert.DoesNotContain(hashSegundo, texto, StringComparison.Ordinal);
            Assert.DoesNotContain(usuario.PasswordHash, texto, StringComparison.Ordinal);
        }
    }
}
