using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-001.G: rechazo dirigido sobre el login real con un DTO de edicion
/// sintetico que aun NO ha pasado por la operacion administrativa autorizada.
/// Este caso no simula ni certifica recuperacion por correo, SMTP o MySQL.
/// </summary>
public sealed class I8001GNuevaPasswordNoRecuperaCredencialErpTests
{
    [Fact]
    public async Task NuevaPasswordEnDtoSinEdicionAutorizada_NoAutenticaNiEmiteJwt()
    {
        const string nombre = "i8001g-operador-erp-sintetico";
        const string claveVigente = "I8001G-Clave-Vigente-ERP-2026";
        const string claveNoAplicada = "I8001G-Solo-DTO-Sin-Guardar-2026";

        var rol = new Rol { Id = 991, Nombre = "Operador ERP", Activo = true };
        var usuario = new Usuario
        {
            Id = 991,
            NombreUsuario = nombre,
            NombreCompleto = "Cuenta de test sin cliente",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(claveVigente, workFactor: 4),
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true
        };
        var hashOriginal = usuario.PasswordHash;
        var peticionEdicion = new UpdateUsuarioDto { NuevaPassword = claveNoAplicada };
        var repositorio = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repositorio.Setup(r => r.GetByNombreUsuarioAsync(nombre))
            .ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        var servicio = new AuthService(
            repositorio.Object, Mock.Of<IRolRepository>(), jwt.Object);

        // Un DTO creado por un cliente NO modifica el hash persistido:
        // no se llama a UsuariosController.Update ni a un servicio de escritura.
        var resultado = await servicio.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = nombre,
            Password = peticionEdicion.NuevaPassword!
        });

        Assert.Null(resultado);
        Assert.True(BCrypt.Net.BCrypt.Verify(claveVigente, usuario.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(claveNoAplicada, usuario.PasswordHash));
        Assert.Equal(hashOriginal, usuario.PasswordHash);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
        repositorio.Verify(x => x.GetByNombreUsuarioAsync(nombre), Times.Once);
        repositorio.VerifyNoOtherCalls();
    }

    [Fact]
    public void EdicionDeClaveSoloExponeRutaAutenticadaConFiltroDePermisos()
    {
        var controlador = typeof(UsuariosController);
        var metodo = controlador.GetMethod(nameof(UsuariosController.Update));

        Assert.NotNull(controlador.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(metodo);
        Assert.NotNull(metodo!.GetCustomAttribute<HttpPutAttribute>());
        Assert.NotNull(metodo.GetCustomAttribute<RequierePermisoAttribute>());
        Assert.NotNull(typeof(UpdateUsuarioDto).GetProperty(
            nameof(UpdateUsuarioDto.NuevaPassword)));

        // Comprobar atributos no ejecuta middleware, RBAC ni un reset por email.
    }
}
