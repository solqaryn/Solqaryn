using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-009.A: bloqueo manual auditable sin difundir datos libres que podrian
/// contener URL de reset o tokens hacia RegistroAuditoria.
/// Casos sintéticos; no acredita alertas externas, HTTP, MySQL ni JWT emitidos.
/// </summary>
public sealed class I8009ACierreAuditoriaBloqueoSinSecretoTests
{
    [Theory]
    [InlineData("Ver https://test.invalid/reset?token=I8009A-MARCADOR-FICTICIO", "I8009A-MARCADOR-FICTICIO")]
    [InlineData("Incidente Bearer I8009A-MARCADOR-PRUEBA", "I8009A-MARCADOR-PRUEBA")]
    public async Task BloqueoConMotivoLibre_NoPropagaTokenNiEnlaceALaAuditoria(
        string motivo, string marcador)
    {
        const int usuarioId = 9009;
        var usuario = new Usuario
        {
            Id = usuarioId,
            NombreUsuario = "i8009a-prueba",
            NombreCompleto = "Usuario de prueba",
            PasswordHash = "HASH-SINTETICO",
            Activo = true,
            Bloqueado = false,
            RolEntidad = null!
        };

        var usuarios = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuarioId)).ReturnsAsync(usuario);
        usuarios.Setup(x => x.Update(usuario));
        usuarios.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var rol = new Mock<IRolRepository>(MockBehavior.Strict);
        var empresa = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        var actual = new Mock<ICurrentUserService>(MockBehavior.Strict);
        actual.SetupGet(x => x.UsuarioId).Returns(100);

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var servicio = new UsuarioService(
            usuarios.Object, rol.Object, empresa.Object, auditoria.Object, actual.Object);

        var resultado = await servicio.BloquearAsync(usuarioId, motivo);

        Assert.True(resultado.Bloqueado);
        Assert.True(usuario.Bloqueado);
        Assert.Equal(motivo, usuario.MotivoBloqueo);
        Assert.NotNull(usuario.FechaBloqueo);
        Assert.Equal(100, usuario.BloqueadoPorUsuarioId);

        usuarios.Verify(x => x.Update(usuario), Times.Once);
        usuarios.Verify(x => x.SaveChangesAsync(), Times.Once);

        var llamada = Assert.Single(auditoria.Invocations);
        Assert.Equal(ModuloSistema.Usuarios, llamada.Arguments[0]);
        Assert.Equal(AccionPermiso.Desactivar, llamada.Arguments[1]);
        Assert.Equal(usuarioId, llamada.Arguments[3]);
        Assert.Equal("Usuario", llamada.Arguments[4]);
        Assert.Equal("Exito", llamada.Arguments[8]);

        var log = string.Join("|", llamada.Arguments.Select(a => a?.ToString() ?? ""));
        Assert.DoesNotContain(marcador, log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", log, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bloqueo registrado; consultar motivo en la ficha del usuario.",
            llamada.Arguments[7]);
    }
}
