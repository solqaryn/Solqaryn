using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class PerfilServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<IPerfilImagenStorageService> _imagenStorage = new();
    private readonly PerfilService _service;

    public PerfilServiceTests()
    {
        _currentUser.Setup(c => c.UsuarioId).Returns(25);
        _usuarioRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        _service = new PerfilService(
            _usuarioRepository.Object,
            _currentUser.Object,
            _auditoria.Object,
            _imagenStorage.Object);
    }

    [Fact]
    public async Task GetPerfilAsync_Usa_Exclusivamente_El_Usuario_Autenticado()
    {
        var usuario = CrearUsuario();
        _usuarioRepository.Setup(r => r.GetByIdAsync(25)).ReturnsAsync(usuario);

        var perfil = await _service.GetPerfilAsync();

        Assert.Equal(25, perfil.Id);
        Assert.Equal("javier", perfil.NombreUsuario);
        _usuarioRepository.Verify(r => r.GetByIdAsync(25), Times.Once);
        _usuarioRepository.Verify(r => r.GetByIdAsync(It.Is<int>(id => id != 25)), Times.Never);
    }

    [Fact]
    public async Task ActualizarPerfilAsync_Rechaza_Nombre_De_Usuario_Duplicado()
    {
        var usuarioActual = CrearUsuario();
        _usuarioRepository.Setup(r => r.GetByIdAsync(25)).ReturnsAsync(usuarioActual);
        _usuarioRepository.Setup(r => r.GetByNombreUsuarioAsync("vendedor2"))
            .ReturnsAsync(new Usuario { Id = 40, NombreUsuario = "vendedor2", NombreCompleto = "Otro usuario" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ActualizarPerfilAsync(new ActualizarPerfilDto
        {
            NombreUsuario = "vendedor2",
            NombreCompleto = "Javier Mejía"
        }));

        _usuarioRepository.Verify(r => r.Update(It.IsAny<Usuario>()), Times.Never);
        _usuarioRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ActualizarPerfilAsync_Normaliza_Datos_Y_Actualiza_El_Usuario_Actual()
    {
        var usuario = CrearUsuario();
        _usuarioRepository.Setup(r => r.GetByIdAsync(25)).ReturnsAsync(usuario);
        _usuarioRepository.Setup(r => r.GetByNombreUsuarioAsync("javier.mejia")).ReturnsAsync((Usuario?)null);

        var perfil = await _service.ActualizarPerfilAsync(new ActualizarPerfilDto
        {
            NombreUsuario = "  javier.mejia  ",
            NombreCompleto = "  Javier   Mejía  "
        });

        Assert.Equal("javier.mejia", perfil.NombreUsuario);
        Assert.Equal("Javier Mejía", perfil.NombreCompleto);
        Assert.Equal(25, usuario.ActualizadoPorUsuarioId);
        Assert.NotNull(usuario.FechaActualizacion);
        _usuarioRepository.Verify(r => r.Update(usuario), Times.Once);
    }

    [Fact]
    public async Task CambiarPasswordAsync_Rechaza_Contrasena_Debil()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CambiarPasswordAsync(new CambiarPasswordPropiaDto
        {
            PasswordActual = "ActualSegura#123",
            PasswordNueva = "debil"
        }));

        _usuarioRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _usuarioRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CambiarPasswordAsync_Valida_Actual_Y_Guarda_Hash_Nuevo()
    {
        var usuario = CrearUsuario();
        const string actual = "ActualSegura#123";
        const string nueva = "NuevaSegura#456";
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(actual);
        _usuarioRepository.Setup(r => r.GetByIdAsync(25)).ReturnsAsync(usuario);

        await _service.CambiarPasswordAsync(new CambiarPasswordPropiaDto
        {
            PasswordActual = actual,
            PasswordNueva = nueva
        });

        Assert.True(BCrypt.Net.BCrypt.Verify(nueva, usuario.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(actual, usuario.PasswordHash));
        Assert.Equal(25, usuario.ActualizadoPorUsuarioId);
        _usuarioRepository.Verify(r => r.Update(usuario), Times.Once);
        _usuarioRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    private static Usuario CrearUsuario() => new()
    {
        Id = 25,
        NombreUsuario = "javier",
        NombreCompleto = "Javier Mejía",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("ActualSegura#123"),
        Activo = true,
        Bloqueado = false,
        Eliminado = false,
        FechaCreacion = DateTime.UtcNow.AddDays(-10)
    };
}
