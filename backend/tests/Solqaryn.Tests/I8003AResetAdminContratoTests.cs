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
/// I8-003.A: contrato de reset administrativo ya expuesto por PUT /usuarios/{id}.
/// Prueba controlador y colaboradores aislados; NO existe aquí una solicitud
/// de enlace de recuperación por correo. No acredita middleware HTTP/JWT,
/// entrega de correo, token de un solo uso ni MySQL.
/// </summary>
public sealed class I8003AResetAdminContratoTests
{
    private const int EmpresaA = 11;
    private const int EmpresaB = 22;
    private const int UsuarioA = 47;

    private static UpdateUsuarioDto Solicitud() => new()
    {
        NombreUsuario = "usuario_empresa_a",
        NombreCompleto = "Usuario A sintetico",
        Rol = "Vendedor",
        RolId = 7,
        NuevaPassword = "Clave-Sintetica-2026-Solo-Test!"
    };

    private static Mock<IUsuarioService> Usuarios()
    {
        var mock = new Mock<IUsuarioService>(MockBehavior.Strict);
        mock.Setup(x => x.GetByIdAsync(UsuarioA)).ReturnsAsync(new UsuarioDetalleDto
        {
            Id = UsuarioA,
            NombreUsuario = "usuario_empresa_a",
            NombreCompleto = "Usuario A sintetico",
            Rol = "Vendedor",
            RolId = 7
        });
        return mock;
    }

    private static UsuariosController CrearController(
        Mock<IUsuarioService> usuarios,
        Mock<IPermisoService> permisos,
        Mock<IUsuarioAdministracionEmpresaService> administracion)
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = EmpresaA;
        // No es autoridad: un solicitante puede falsificar esta cabecera.
        http.Request.Headers["X-Empresa-Id"] = EmpresaB.ToString();
        http.RequestServices = new ServiceCollection()
            .AddSingleton<IUsuarioAdministracionEmpresaService>(administracion.Object)
            .BuildServiceProvider();
        return new UsuariosController(
            usuarios.Object, permisos.Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task ResetAdminConEmpresaVerificada_UsaSoloEmpresaA_YNoElevaRol()
    {
        var usuarios = Usuarios();
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(
                EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena))
            .Returns(Task.CompletedTask);
        var administracion = new Mock<IUsuarioAdministracionEmpresaService>(MockBehavior.Strict);
        administracion.Setup(x => x.ActualizarAsync(
                UsuarioA, It.Is<UpdateUsuarioDto>(d =>
                    d.NuevaPassword == "Clave-Sintetica-2026-Solo-Test!" &&
                    d.RolId == 7),
                EmpresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto
            {
                Id = UsuarioA,
                NombreUsuario = "usuario_empresa_a",
                Rol = "Vendedor",
                RolId = 7
            });

        var response = await CrearController(usuarios, permisos, administracion)
            .Update(UsuarioA, Solicitud());

        Assert.IsType<OkObjectResult>(response);
        permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena),
            Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.AsignarRol),
            Times.Never);
        administracion.Verify(x => x.ActualizarAsync(
            UsuarioA, It.IsAny<UpdateUsuarioDto>(), EmpresaA,
            It.IsAny<CancellationToken>()), Times.Once);
        usuarios.Verify(x => x.GetByIdAsync(UsuarioA), Times.Once);
        permisos.VerifyNoOtherCalls();
        administracion.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetAdminSinPermiso_SeDetieneAntesDeModificarUsuario()
    {
        var usuarios = Usuarios();
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(
                EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena))
            .ThrowsAsync(new ForbiddenAccessException("Reset denegado"));
        var administracion = new Mock<IUsuarioAdministracionEmpresaService>(MockBehavior.Strict);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => CrearController(usuarios, permisos, administracion)
                .Update(UsuarioA, Solicitud()));

        administracion.VerifyNoOtherCalls();
        permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena),
            Times.Once);
        permisos.VerifyNoOtherCalls();
        usuarios.Verify(x => x.GetByIdAsync(UsuarioA), Times.Once);
    }
}
