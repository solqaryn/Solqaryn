using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-011.G: un rol declarado de plataforma no sustituye el tenant verificado
/// por RBAC. Ejecuta UsuariosController.GetEmpresas con datos A/B sinteticos.
/// No prueba middleware, autenticacion JWT ni base de datos real.
/// </summary>
public sealed class I8011GSuperadminTenantNoBypassTests
{
    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("AdministradorPlataforma")]
    public async Task RolGlobalConSoloHeaderFalsificado_NoConsultaMembresias(
        string rolGlobal)
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var controller = CrearController(usuarios);
        var contexto = CrearContexto(rolGlobal, empresaHeader: 22);
        controller.ControllerContext = new ControllerContext { HttpContext = contexto };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => controller.GetEmpresas(701));

        usuarios.Verify(x => x.GetEmpresasAsync(It.IsAny<int>()), Times.Never);
        usuarios.VerifyNoOtherCalls();
        Assert.Null(VerifiedCommercialTenant.Resolve(contexto));
    }

    [Fact]
    public async Task TenantVerificadoA_DescartaMembresiaB_PeseAHeaderSuplantado()
    {
        const int usuarioId = 701;
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetEmpresasAsync(usuarioId))
            .ReturnsAsync(new List<UsuarioEmpresaDto>
            {
                new() { Id = 1, UsuarioId = usuarioId, EmpresaId = 11, RolId = 901, Activa = true },
                new() { Id = 2, UsuarioId = usuarioId, EmpresaId = 22, RolId = 902, Activa = true }
            });

        var controller = CrearController(usuarios);
        var contexto = CrearContexto("SuperAdmin", empresaHeader: 22);
        contexto.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 11;
        controller.ControllerContext = new ControllerContext { HttpContext = contexto };

        var respuesta = Assert.IsType<OkObjectResult>(
            await controller.GetEmpresas(usuarioId));
        var contrato = Assert.IsType<ApiResponse<List<UsuarioEmpresaDto>>>(
            respuesta.Value);
        Assert.True(contrato.Success);
        var unica = Assert.Single(contrato.Data!);
        Assert.Equal(usuarioId, unica.UsuarioId);
        Assert.Equal(11, unica.EmpresaId);
        Assert.Equal(11, VerifiedCommercialTenant.Resolve(contexto));
        usuarios.Verify(x => x.GetEmpresasAsync(usuarioId), Times.Once);
        usuarios.VerifyNoOtherCalls();

        var cuerpo = JsonSerializer.Serialize(respuesta.Value);
        Assert.DoesNotContain("PasswordHash", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResetToken", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RefreshToken", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarcaTenantInvalida_SeRechazaAntesDeConsultarMembresias()
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var controller = CrearController(usuarios);
        var contexto = CrearContexto("SuperAdmin", empresaHeader: 22);
        contexto.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 0;
        controller.ControllerContext = new ControllerContext { HttpContext = contexto };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => controller.GetEmpresas(701));
        usuarios.Verify(x => x.GetEmpresasAsync(It.IsAny<int>()), Times.Never);
    }

    private static UsuariosController CrearController(Mock<IUsuarioService> usuarios) =>
        new(usuarios.Object, Mock.Of<IPermisoService>(), Mock.Of<ICurrentUserService>());

    private static DefaultHttpContext CrearContexto(string rol, int empresaHeader)
    {
        var contexto = new DefaultHttpContext();
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "701"),
                new Claim(ClaimTypes.Role, rol)
            },
            authenticationType: "fixture-test"));
        contexto.Request.Headers["X-Empresa-Id"] = empresaHeader.ToString();
        return contexto;
    }
}
