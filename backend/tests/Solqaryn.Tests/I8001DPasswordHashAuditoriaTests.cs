using System;
using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-001.D: audita el contrato de credenciales ERP con hashes BCrypt vigentes.
/// No certifica datos de produccion, revocacion de JWT ya emitidos ni MySQL real.
/// </summary>
public sealed class I8001DPasswordHashAuditoriaTests
{
    [Fact]
    public void Hashes_BCrypt_del_mismo_secreto_no_comparten_salt_ni_guardan_texto_plano()
    {
        const string secreto = "Clave-ERP-Auditada-2026";
        var primero = BCrypt.Net.BCrypt.HashPassword(secreto, workFactor: 4);
        var segundo = BCrypt.Net.BCrypt.HashPassword(secreto, workFactor: 4);

        Assert.NotEqual(secreto, primero);
        Assert.NotEqual(secreto, segundo);
        Assert.NotEqual(primero, segundo);
        Assert.StartsWith("$2", primero);
        Assert.StartsWith("$2", segundo);
        Assert.True(BCrypt.Net.BCrypt.Verify(secreto, primero));
        Assert.True(BCrypt.Net.BCrypt.Verify(secreto, segundo));
        Assert.False(BCrypt.Net.BCrypt.Verify(secreto + "!", primero));
    }

    [Fact]
    public async Task Login_rechaza_secreto_anterior_tras_cambiar_hash_y_emite_un_solo_token_vigente()
    {
        const string anterior = "Clave-ERP-Anterior-2026";
        const string vigente = "Clave-ERP-Vigente-2026";
        var usuario = new Usuario
        {
            Id = 8101,
            NombreUsuario = "operador-erp",
            NombreCompleto = "Operador auditado",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(anterior, workFactor: 4),
            RolId = 9,
            RolEntidad = new Rol { Id = 9, Nombre = "Operador", Activo = true },
            Activo = true
        };
        var repositorio = new Mock<IUsuarioRepository>();
        repositorio.Setup(x => x.GetByNombreUsuarioAsync("operador-erp"))
            .ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(
            repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);

        var incorrecta = await servicio.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = "operador-erp",
            Password = anterior + " "
        });
        Assert.Null(incorrecta);

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vigente, workFactor: 4);
        Assert.NotEqual(vigente, usuario.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(vigente, usuario.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(anterior, usuario.PasswordHash));

        var yaNoVigente = await servicio.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = "operador-erp",
            Password = anterior
        });
        Assert.Null(yaNoVigente);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);

        var expira = DateTime.UtcNow.AddMinutes(15);
        jwt.Setup(x => x.GenerarToken(usuario))
            .Returns(("token-sintetico-no-firmado", expira));
        var correcta = await servicio.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = " operador-erp ",
            Password = vigente
        });

        Assert.NotNull(correcta);
        Assert.Equal("token-sintetico-no-firmado", correcta.Token);
        Assert.Equal(expira, correcta.ExpiraEn);
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
        repositorio.Verify(x => x.GetByNombreUsuarioAsync("operador-erp"), Times.Exactly(3));
    }
}
