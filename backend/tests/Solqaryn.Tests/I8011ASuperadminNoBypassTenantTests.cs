using System;
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
/// I8-011.A: contrato parcial de seleccion empresarial en presencia de claims
/// sintéticos de superadministrador. NO demuestra middleware JWT, browser E2E,
/// persistencia MySQL ni identidad de plataforma real.
/// </summary>
public sealed class I8011ASuperadminNoBypassTenantTests
{
    private static UsuariosController Crear(
        Mock<IUsuarioService> usuarios, int? tenantVerificado, bool globalAdmin)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "900"),
            new Claim(ClaimTypes.Role, "Superadmin"),
            new Claim("ambito", "Plataforma"),
            new Claim("es_administrador_plataforma", globalAdmin ? "true" : "false")
        }, "synthetic-test"));
        http.Request.Headers["X-Empresa-Id"] = "22";
        if (tenantVerificado is int empresaId)
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
        return new UsuariosController(
            usuarios.Object,
            new Mock<IPermisoService>(MockBehavior.Strict).Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlataformaGlobalSinTenant_VerificaDenegacionAntesDeAsignarEmpresa(
        bool globalAdmin)
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var sut = Crear(usuarios, null, globalAdmin);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.AsignarEmpresa(87, new AsignarUsuarioEmpresaDto
            {
                EmpresaId = 22,
                RolId = 4
            }));
        usuarios.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PlataformaConTenantA_NoPuedeCambiarRolDeEmpresaB()
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var sut = Crear(usuarios, 11, true);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.CambiarRolEmpresa(87, 22, new CambiarRolUsuarioEmpresaDto
            {
                RolId = 4
            }));
        usuarios.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeleccionVerificadaA_PermiteSoloCambioA_SinExponerSecretosEnDto()
    {
        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        usuarios.Setup(u => u.CambiarRolEmpresaAsync(87, 11, 4))
            .ReturnsAsync(new UsuarioEmpresaDto
            {
                Id = 601,
                UsuarioId = 87,
                EmpresaId = 11,
                RolId = 4,
                Activa = true
            });
        var sut = Crear(usuarios, 11, true);
        var response = Assert.IsType<OkObjectResult>(await sut.CambiarRolEmpresa(
            87, 11, new CambiarRolUsuarioEmpresaDto { RolId = 4 }));
        var payload = Assert.IsType<ApiResponse<UsuarioEmpresaDto>>(response.Value);
        Assert.NotNull(payload.Data);
        Assert.Equal(11, payload.Data.EmpresaId);
        Assert.Equal(4, payload.Data.RolId);
        var serialized = JsonSerializer.Serialize(payload.Data);
        Assert.DoesNotContain("Password", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Token", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret", serialized, StringComparison.OrdinalIgnoreCase);
        usuarios.Verify(u => u.CambiarRolEmpresaAsync(87, 11, 4), Times.Once);
        usuarios.VerifyNoOtherCalls();
    }
}
