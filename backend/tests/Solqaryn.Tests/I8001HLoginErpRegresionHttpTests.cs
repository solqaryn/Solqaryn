using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-001.H: regresion positiva de clave ERP vigente por AuthService real
/// y AuthController.Login real, con dependencias sinteticas.
/// No sustituye middleware HTTP, JWT firmado, MySQL ni SMTP.
/// </summary>
public sealed class I8001HLoginErpRegresionHttpTests
{
    [Fact]
    public async Task HashYRolVigentes_DevuelvenLogin200ConAuditoriaSinExponerHash()
    {
        const string usuarioLogin = "i8001h-erp-operador";
        const string clave = "I8001H-Clave-ERP-Correcta-2026";
        const string tokenSintetico = "I8001H-TOKEN-SINTETICO-NO-FIRMADO";
        var rol = new Rol { Id = 8019, Nombre = "Operador ERP", Activo = true };
        var usuario = new Usuario
        {
            Id = 8019,
            NombreUsuario = usuarioLogin,
            NombreCompleto = "Operador sintetico",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(clave, workFactor: 4),
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true
        };
        var hashOriginal = usuario.PasswordHash;
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(x => x.GetByNombreUsuarioAsync(usuarioLogin))
            .ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var expiracion = new DateTime(2026, 11, 12, 18, 0, 0, DateTimeKind.Utc);
        jwt.Setup(x => x.GenerarToken(usuario))
            .Returns((tokenSintetico, expiracion));
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(x => x.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.ConsultarHistorial,
                "Inicio de sesion exitoso para '" + usuarioLogin + "'.",
                null, "Sesion", null, null, null, "Exito", null))
            .Returns(Task.CompletedTask);

        var servicio = new AuthService(
            repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);
        var controlador = new AuthController(servicio, auditoria.Object);

        // El usuario puede contener espacios externos; la clave no se altera.
        var action = await controlador.Login(new LoginRequestDto
        {
            NombreUsuario = " " + usuarioLogin + " ",
            Password = clave
        });

        var ok = Assert.IsType<OkObjectResult>(action);
        var resultado = Assert.IsType<ApiResponse<LoginResponseDto>>(ok.Value);
        Assert.True(resultado.Success);
        Assert.NotNull(resultado.Data);
        Assert.Equal(usuarioLogin, resultado.Data.NombreUsuario);
        Assert.Equal("Operador ERP", resultado.Data.Rol);
        Assert.Equal(tokenSintetico, resultado.Data.Token);
        Assert.Equal(expiracion, resultado.Data.ExpiraEn);
        Assert.Equal(hashOriginal, usuario.PasswordHash);
        var salida = JsonSerializer.Serialize(resultado);
        Assert.DoesNotContain(clave, salida, StringComparison.Ordinal);
        Assert.DoesNotContain(hashOriginal, salida, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", salida, StringComparison.Ordinal);

        repositorio.Verify(x => x.GetByNombreUsuarioAsync(usuarioLogin), Times.Once);
        jwt.Verify(x => x.GenerarToken(usuario), Times.Once);
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.ConsultarHistorial,
            "Inicio de sesion exitoso para '" + usuarioLogin + "'.",
            null, "Sesion", null, null, null, "Exito", null), Times.Once);
        repositorio.VerifyNoOtherCalls();
        jwt.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }
}
