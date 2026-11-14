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
/// I8-001.E: prueba dirigida de autenticacion BCrypt con rol relacional no valido.
/// Una contrasena correcta nunca reemplaza el control de rol; no prueba JWT firmado,
/// autenticacion HTTP, datos productivos ni revocacion de sesiones emitidas.
/// </summary>
public sealed class I8001EHashCorrectoRolInvalidoTests
{
    [Theory]
    [InlineData(0, true, false, false)]
    [InlineData(9, false, false, false)]
    [InlineData(9, true, true, false)]
    [InlineData(9, true, false, true)]
    public async Task Login_con_hash_bcrypt_correcto_no_emite_jwt_si_rol_invalido(
        int rolId, bool existeRol, bool rolInactivo, bool rolEliminado)
    {
        const string password = "Clave-ERP-Auditoria-Rol-2026";
        var usuario = new Usuario
        {
            Id = 8105,
            NombreUsuario = "auditor-erp",
            NombreCompleto = "Cuenta de prueba sintetica",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
            RolId = rolId,
            RolEntidad = existeRol
                ? new Rol
                {
                    Id = 9,
                    Nombre = "Operador",
                    Activo = !rolInactivo,
                    Eliminado = rolEliminado
                }
                : null!,
            Activo = true
        };
        Assert.True(BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash));

        var repositorio = new Mock<IUsuarioRepository>();
        repositorio.Setup(x => x.GetByNombreUsuarioAsync("auditor-erp"))
            .ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var sut = new AuthService(
            repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.LoginAsync(
            new LoginRequestDto
            {
                NombreUsuario = " auditor-erp ",
                Password = password
            }));
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);

        // Control positivo sobre el MISMO hash: solo restaurar un rol relacional
        // vigente permite emitir un token sintetico; no se altera la clave.
        var hashAntes = usuario.PasswordHash;
        usuario.RolId = 9;
        usuario.RolEntidad = new Rol
        {
            Id = 9,
            Nombre = "Operador",
            Activo = true,
            Eliminado = false
        };
        var expira = DateTime.UtcNow.AddMinutes(15);
        jwt.Setup(x => x.GenerarToken(usuario))
            .Returns(("token-sintetico-no-firmado", expira));
        var resultado = await sut.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = "auditor-erp",
            Password = password
        });

        Assert.NotNull(resultado);
        Assert.Equal(hashAntes, usuario.PasswordHash);
        Assert.Equal("token-sintetico-no-firmado", resultado.Token);
        Assert.Equal(expira, resultado.ExpiraEn);
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
        repositorio.Verify(
            x => x.GetByNombreUsuarioAsync("auditor-erp"),
            Times.Exactly(2));
    }
}
