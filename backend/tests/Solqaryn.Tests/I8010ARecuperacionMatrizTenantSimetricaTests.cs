using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-010.A: evidencia dirigida y sintetica de simetria entre empresas A/B
/// en el reset administrativo. El tenant debe provenir de la marca verificada
/// por el servidor, nunca de X-Empresa-Id enviado por el solicitante.
/// No acredita la matriz completa de actores, SMTP/TLS ni HTTP/JWT/MySQL.
/// </summary>
public sealed class I8010ARecuperacionMatrizTenantSimetricaTests
{
    private const int UsuarioId = 87;
    private const int RolId = 5;
    private const string ClaveFicticia = "I8010A-CLAVE-SINTETICA-NO-REAL";

    [Theory]
    [InlineData(11, 22)]
    [InlineData(22, 11)]
    public async Task ResetAutorizado_UsaSoloElTenantVerificado_NoElHeaderAdversarial(
        int empresaVerificada, int empresaSuplantada)
    {
        var usuarios = CrearUsuarioActual();
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(
                empresaVerificada, ModuloSistema.Usuarios,
                AccionPermiso.RestablecerContrasena))
            .Returns(Task.CompletedTask);

        var escritor = new Mock<IUsuarioAdministracionEmpresaService>(MockBehavior.Strict);
        escritor.Setup(x => x.ActualizarAsync(
                UsuarioId,
                It.Is<UpdateUsuarioDto>(d =>
                    d.RolId == RolId && d.NuevaPassword == ClaveFicticia),
                empresaVerificada,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto
            {
                Id = UsuarioId, RolId = RolId, Rol = "Operador", Activo = true
            });

        using var servicios = new ServiceCollection()
            .AddSingleton<IUsuarioAdministracionEmpresaService>(escritor.Object)
            .BuildServiceProvider();

        var controller = CrearControlador(
            usuarios.Object, permisos.Object, servicios,
            empresaVerificada, empresaSuplantada);

        var resultado = await controller.Update(UsuarioId, NuevoCambio());
        Assert.IsType<OkObjectResult>(resultado);

        usuarios.Verify(x => x.GetByIdAsync(UsuarioId), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaVerificada, ModuloSistema.Usuarios,
            AccionPermiso.RestablecerContrasena), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios,
            AccionPermiso.AsignarRol), Times.Never);
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaSuplantada, It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>()), Times.Never);
        escritor.Verify(x => x.ActualizarAsync(
            UsuarioId, It.IsAny<UpdateUsuarioDto>(),
            empresaVerificada, It.IsAny<CancellationToken>()), Times.Once);
        escritor.Verify(x => x.ActualizarAsync(
            It.IsAny<int>(), It.IsAny<UpdateUsuarioDto>(),
            empresaSuplantada, It.IsAny<CancellationToken>()), Times.Never);
        usuarios.VerifyNoOtherCalls();
        permisos.VerifyNoOtherCalls();
        escritor.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(11, 22)]
    [InlineData(22, 11)]
    public async Task ResetDenegadoEnTenantVerificado_NoEscribeAunqueHeaderIndiqueOtro(
        int empresaVerificada, int empresaSuplantada)
    {
        var usuarios = CrearUsuarioActual();
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(
                empresaVerificada, ModuloSistema.Usuarios,
                AccionPermiso.RestablecerContrasena))
            .ThrowsAsync(new ForbiddenAccessException(
                "Permiso sintetico denegado para reset."));

        var escritor = new Mock<IUsuarioAdministracionEmpresaService>(MockBehavior.Strict);
        using var servicios = new ServiceCollection()
            .AddSingleton<IUsuarioAdministracionEmpresaService>(escritor.Object)
            .BuildServiceProvider();
        var controller = CrearControlador(
            usuarios.Object, permisos.Object, servicios,
            empresaVerificada, empresaSuplantada);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.Update(UsuarioId, NuevoCambio()));

        usuarios.Verify(x => x.GetByIdAsync(UsuarioId), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaVerificada, ModuloSistema.Usuarios,
            AccionPermiso.RestablecerContrasena), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaSuplantada, It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>()), Times.Never);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios,
            AccionPermiso.AsignarRol), Times.Never);
        escritor.VerifyNoOtherCalls();
        usuarios.VerifyNoOtherCalls();
        permisos.VerifyNoOtherCalls();
    }

    private static Mock<IUsuarioService> CrearUsuarioActual()
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(UsuarioId))
            .ReturnsAsync(new UsuarioDetalleDto
            {
                Id = UsuarioId, RolId = RolId,
                Rol = "Operador", Activo = true
            });
        return usuarios;
    }

    private static UsuariosController CrearControlador(
        IUsuarioService usuarios,
        IPermisoService permisos,
        IServiceProvider servicios,
        int empresaVerificada,
        int empresaSuplantada)
    {
        var http = new DefaultHttpContext { RequestServices = servicios };
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
            empresaVerificada;
        http.Request.Headers["X-Empresa-Id"] =
            empresaSuplantada.ToString();
        return new UsuariosController(
            usuarios, permisos,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static UpdateUsuarioDto NuevoCambio() => new()
    {
        RolId = RolId,
        Rol = "Operador",
        NuevaPassword = ClaveFicticia
    };
}
