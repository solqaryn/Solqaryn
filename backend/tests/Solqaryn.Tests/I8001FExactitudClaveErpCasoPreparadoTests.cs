using System;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-001.F: fixture medible para auditar contrasenas ERP existentes.
/// La verificacion de la clave debe ser exacta, sin recortar espacios
/// aunque el nombre de usuario si se normalice. Datos sinteticos solamente.
/// No demuestra hash de cuentas reales, HTTP/JWT firmado ni MySQL.
/// </summary>
public sealed class I8001FExactitudClaveErpCasoPreparadoTests
{
    private const string Nombre = "operador-caso-preparado";
    private const string Secreto = " I8001F-Clave-ERP-Sintetica-2026 ";

    [Theory]
    [InlineData(" I8001F-Clave-ERP-Sintetica-2026 ", true)]
    [InlineData("I8001F-Clave-ERP-Sintetica-2026", false)]
    [InlineData(" I8001F-Clave-ERP-Sintetica-2026", false)]
    [InlineData("I8001F-Clave-ERP-Sintetica-2026 ", false)]
    public async Task Login_usa_clave_exacta_sin_normalizar_espacios(
        string claveIngresada, bool debeAutenticar)
    {
        var rol = new Rol
        {
            Id = 901,
            Nombre = "Operador ERP sintetico",
            Activo = true,
            Eliminado = false
        };
        var usuario = new Usuario
        {
            Id = 8106,
            NombreUsuario = Nombre,
            NombreCompleto = "Cuenta sintetica I8-001.F",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Secreto, workFactor: 4),
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true,
            Eliminado = false
        };
        var hashOriginal = usuario.PasswordHash;
        var repo = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetByNombreUsuarioAsync(Nombre)).ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var vence = DateTime.UtcNow.AddMinutes(5);
        if (debeAutenticar)
        {
            jwt.Setup(x => x.GenerarToken(usuario))
                .Returns(("I8001F-TOKEN-SINTETICO-NO-FIRMADO", vence));
        }
        var sut = new AuthService(repo.Object, Mock.Of<IRolRepository>(), jwt.Object);

        var respuesta = await sut.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = "  " + Nombre + "  ",
            Password = claveIngresada
        });

        if (debeAutenticar)
        {
            Assert.NotNull(respuesta);
            Assert.Equal("I8001F-TOKEN-SINTETICO-NO-FIRMADO", respuesta.Token);
            Assert.Equal(vence, respuesta.ExpiraEn);
            var json = JsonSerializer.Serialize(respuesta);
            Assert.DoesNotContain(Secreto, json, StringComparison.Ordinal);
            Assert.DoesNotContain(hashOriginal, json, StringComparison.Ordinal);
            jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
        }
        else
        {
            Assert.Null(respuesta);
            jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
        }

        Assert.Equal(hashOriginal, usuario.PasswordHash);
        repo.Verify(x => x.GetByNombreUsuarioAsync(Nombre), Times.Once);
    }
}
