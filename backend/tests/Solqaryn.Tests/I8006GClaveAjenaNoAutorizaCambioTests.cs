using System;
using System.Text.Json;
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
/// I8-006.G: negativo dirigido de suplantacion de UsuarioId en cuerpo y clave ajena.
/// El servicio solo debe consultar la identidad autenticada y rechazar la clave
/// de otro usuario, sin persistencia ni auditoria de cambio exitoso.
/// No sustituye pruebas HTTP/JWT ni transacciones MySQL.
/// </summary>
public sealed class I8006GClaveAjenaNoAutorizaCambioTests
{
    [Theory]
    [InlineData(25, 40)]
    [InlineData(40, 25)]
    public async Task I8006G_IdAjenoInyectadoYPasswordAjena_NoActualizaCuenta(
        int propioId, int ajenoId)
    {
        const string clavePropia = "PropiaActual#123";
        const string claveAjena = "AjenaActual#456";
        const string claveNueva = "NuevaSegura#789";

        var propio = new Usuario
        {
            Id = propioId,
            NombreUsuario = "propio",
            NombreCompleto = "Usuario propio sintetico",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(clavePropia),
            Activo = true,
            Bloqueado = false,
            Eliminado = false
        };
        var ajeno = new Usuario
        {
            Id = ajenoId,
            NombreUsuario = "ajeno",
            NombreCompleto = "Usuario ajeno sintetico",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(claveAjena),
            Activo = true,
            Bloqueado = false,
            Eliminado = false
        };
        var hashPropioAntes = propio.PasswordHash;
        var hashAjenoAntes = ajeno.PasswordHash;

        // Simula un cuerpo hostil: UsuarioId no forma parte del DTO aceptado.
        var cuerpo = $"{{\"PasswordActual\":\"{claveAjena}\",\"PasswordNueva\":\"{claveNueva}\",\"UsuarioId\":{ajenoId}}}";
        var dto = JsonSerializer.Deserialize<CambiarPasswordPropiaDto>(cuerpo);
        Assert.NotNull(dto);
        Assert.DoesNotContain(typeof(CambiarPasswordPropiaDto).GetProperties(),
            p => p.Name.Equals("UsuarioId", StringComparison.OrdinalIgnoreCase));

        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(r => r.GetByIdAsync(propioId)).ReturnsAsync(propio);
        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(c => c.UsuarioId).Returns(propioId);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var imagenes = new Mock<IPerfilImagenStorageService>(MockBehavior.Strict);
        var servicio = new PerfilService(repositorio.Object, identidad.Object,
            auditoria.Object, imagenes.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => servicio.CambiarPasswordAsync(dto!));

        Assert.Equal(hashPropioAntes, propio.PasswordHash);
        Assert.Equal(hashAjenoAntes, ajeno.PasswordHash);
        repositorio.Verify(r => r.GetByIdAsync(propioId), Times.Once);
        repositorio.Verify(r => r.GetByIdAsync(ajenoId), Times.Never);
        repositorio.Verify(r => r.Update(It.IsAny<Usuario>()), Times.Never);
        repositorio.Verify(r => r.SaveChangesAsync(), Times.Never);
        auditoria.VerifyNoOtherCalls();
    }
}
