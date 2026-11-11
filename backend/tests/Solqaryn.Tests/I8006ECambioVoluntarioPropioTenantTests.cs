using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-006.E: la contraseña propia se cambia exclusivamente para la identidad autenticada.
/// Los identificadores externos no forman parte del contrato del endpoint de perfil.
/// </summary>
public class I8006ECambioVoluntarioPropioTenantTests
{
    [Theory]
    [InlineData(25, 40)]
    [InlineData(40, 25)]
    public async Task I8006E_CambiarPassword_NoConsultaNiActualizaOtraCuenta(
        int usuarioAutenticadoId, int usuarioAjenoId)
    {
        const string passwordActual = "ActualSegura#123";
        const string passwordNueva = "NuevaSegura#456";

        var usuarioAutenticado = CrearUsuario(usuarioAutenticadoId, passwordActual);
        var usuarioAjeno = CrearUsuario(usuarioAjenoId, "AjenaSegura#789");
        var hashAjenoAntes = usuarioAjeno.PasswordHash;

        var repositorio = new Mock<IUsuarioRepository>();
        var identidad = new Mock<ICurrentUserService>();
        var auditoria = new Mock<IAuditoriaService>();
        var imagenes = new Mock<IPerfilImagenStorageService>();

        identidad.Setup(x => x.UsuarioId).Returns(usuarioAutenticadoId);
        repositorio.Setup(x => x.GetByIdAsync(usuarioAutenticadoId))
            .ReturnsAsync(usuarioAutenticado);
        repositorio.Setup(x => x.GetByIdAsync(usuarioAjenoId))
            .ReturnsAsync(usuarioAjeno);
        repositorio.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var servicio = new PerfilService(
            repositorio.Object,
            identidad.Object,
            auditoria.Object,
            imagenes.Object);

        await servicio.CambiarPasswordAsync(new CambiarPasswordPropiaDto
        {
            PasswordActual = passwordActual,
            PasswordNueva = passwordNueva
        });

        Assert.True(BCrypt.Net.BCrypt.Verify(passwordNueva, usuarioAutenticado.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(passwordActual, usuarioAutenticado.PasswordHash));
        Assert.Equal(hashAjenoAntes, usuarioAjeno.PasswordHash);
        Assert.Equal(usuarioAutenticadoId, usuarioAutenticado.ActualizadoPorUsuarioId);

        repositorio.Verify(x => x.GetByIdAsync(usuarioAutenticadoId), Times.Once);
        repositorio.Verify(x => x.GetByIdAsync(usuarioAjenoId), Times.Never);
        repositorio.Verify(x => x.Update(usuarioAutenticado), Times.Once);
        repositorio.Verify(x => x.Update(It.Is<Usuario>(u => u.Id == usuarioAjenoId)), Times.Never);
        repositorio.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public void I8006E_CambiarPassword_EndpointAutenticadoNoAceptaUsuarioIdDeRuta()
    {
        var controlador = typeof(PerfilController);
        var ruta = controlador.GetCustomAttribute<RouteAttribute>();
        var autorizacion = controlador.GetCustomAttribute<AuthorizeAttribute>();
        var metodo = controlador.GetMethod(nameof(PerfilController.CambiarPassword));
        var put = metodo?.GetCustomAttribute<HttpPutAttribute>();

        Assert.NotNull(autorizacion);
        Assert.Equal("perfil", ruta?.Template);
        Assert.Equal("password", put?.Template);
        Assert.NotNull(metodo);

        var parametro = Assert.Single(metodo!.GetParameters());
        Assert.Equal(typeof(CambiarPasswordPropiaDto), parametro.ParameterType);
        Assert.NotNull(parametro.GetCustomAttribute<FromBodyAttribute>());
        Assert.DoesNotContain(
            typeof(CambiarPasswordPropiaDto).GetProperties(),
            propiedad => propiedad.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
    }

    private static Usuario CrearUsuario(int id, string password) => new()
    {
        Id = id,
        NombreUsuario = $"usuario{id}",
        NombreCompleto = $"Usuario {id}",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Activo = true,
        Bloqueado = false,
        Eliminado = false,
        FechaCreacion = DateTime.UtcNow.AddDays(-2)
    };
}
