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
/// I8-006.A: confirma cierre acotado del cambio voluntario de contraseña:
/// identidad propia, hash nuevo y auditoría sin credenciales; no verifica HTTP/JWT/MySQL.
/// Complementa las regresiones C (clave actual), D (cuenta vigente), E (cuenta ajena).
/// </summary>
public sealed class I8006ACambioPropioAuditoriaSinSecretosTests
{
    [Theory]
    [InlineData(25, 40)]
    [InlineData(40, 25)]
    public async Task I8006A_CambioPropio_NoActualizaCuentaAjenaNiAuditaCredenciales(
        int usuarioAutenticadoId, int usuarioAjenoId)
    {
        const string actual = "ActualSegura#123";
        const string nueva = "NuevaSegura#456";
        var usuario = new Usuario
        {
            Id = usuarioAutenticadoId,
            NombreUsuario = "usuario-propio",
            NombreCompleto = "Usuario sintetico",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(actual),
            Activo = true,
            Bloqueado = false,
            Eliminado = false,
            FechaCreacion = DateTime.UtcNow.AddDays(-3)
        };
        var ajeno = new Usuario
        {
            Id = usuarioAjenoId,
            NombreUsuario = "usuario-ajeno",
            NombreCompleto = "Otro usuario sintetico",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("AjenaSegura#789"),
            Activo = true
        };
        var hashAjeno = ajeno.PasswordHash;
        var hashAnterior = usuario.PasswordHash;
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(x => x.GetByIdAsync(usuarioAutenticadoId)).ReturnsAsync(usuario);
        repositorio.Setup(x => x.GetByIdAsync(usuarioAjenoId)).ReturnsAsync(ajeno);
        repositorio.Setup(x => x.Update(usuario));
        repositorio.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(x => x.UsuarioId).Returns(usuarioAutenticadoId);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var servicio = new PerfilService(repositorio.Object, identidad.Object,
            auditoria.Object, new Mock<IPerfilImagenStorageService>(MockBehavior.Strict).Object);

        await servicio.CambiarPasswordAsync(new CambiarPasswordPropiaDto
        {
            PasswordActual = actual,
            PasswordNueva = nueva
        });

        Assert.True(BCrypt.Net.BCrypt.Verify(nueva, usuario.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(actual, usuario.PasswordHash));
        Assert.Equal(hashAjeno, ajeno.PasswordHash);
        Assert.Equal(usuarioAutenticadoId, usuario.ActualizadoPorUsuarioId);
        repositorio.Verify(x => x.GetByIdAsync(usuarioAutenticadoId), Times.Once);
        repositorio.Verify(x => x.GetByIdAsync(usuarioAjenoId), Times.Never);
        repositorio.Verify(x => x.Update(usuario), Times.Once);
        repositorio.Verify(x => x.Update(It.Is<Usuario>(u => u.Id == usuarioAjenoId)), Times.Never);
        repositorio.Verify(x => x.SaveChangesAsync(), Times.Once);

        var invocacion = Assert.Single(auditoria.Invocations);
        Assert.Equal(ModuloSistema.Usuarios, invocacion.Arguments[0]);
        Assert.Equal(AccionPermiso.RestablecerContrasena, invocacion.Arguments[1]);
        Assert.Equal(usuarioAutenticadoId, invocacion.Arguments[3]);
        var registro = string.Join("|", invocacion.Arguments.Select(a => a?.ToString() ?? ""));
        Assert.DoesNotContain(actual, registro, StringComparison.Ordinal);
        Assert.DoesNotContain(nueva, registro, StringComparison.Ordinal);
        Assert.DoesNotContain(hashAnterior, registro, StringComparison.Ordinal);
        Assert.DoesNotContain(usuario.PasswordHash, registro, StringComparison.Ordinal);
    }

    [Fact]
    public async Task I8006A_SinIdentidadAutenticada_NoConsultaNiCambiaNingunaCuenta()
    {
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(x => x.UsuarioId).Returns((int?)null);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var servicio = new PerfilService(repositorio.Object, identidad.Object,
            auditoria.Object, new Mock<IPerfilImagenStorageService>(MockBehavior.Strict).Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servicio.CambiarPasswordAsync(new CambiarPasswordPropiaDto
            {
                PasswordActual = "ActualSegura#123",
                PasswordNueva = "NuevaSegura#456"
            }));

        repositorio.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        repositorio.Verify(x => x.Update(It.IsAny<Usuario>()), Times.Never);
        repositorio.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.VerifyNoOtherCalls();
    }
}
