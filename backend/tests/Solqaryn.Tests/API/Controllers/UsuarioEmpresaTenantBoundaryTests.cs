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

namespace Solqaryn.Tests.API.Controllers;

public sealed class UsuarioEmpresaTenantBoundaryTests
{
    private static UsuariosController CreateController(Mock<IUsuarioService> users, int empresaId)
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
        return new UsuariosController(
            users.Object,
            new Mock<IPermisoService>(MockBehavior.Strict).Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task Asignacion_empresa_B_desde_contexto_A_no_consulta_ni_modifica_servicio()
    {
        var service = new Mock<IUsuarioService>(MockBehavior.Strict);
        var sut = CreateController(service, 11);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.AsignarEmpresa(88, new AsignarUsuarioEmpresaDto { EmpresaId = 22, RolId = 4 }));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cambio_rol_empresa_B_desde_contexto_A_rechazado()
    {
        var service = new Mock<IUsuarioService>(MockBehavior.Strict);
        var sut = CreateController(service, 11);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.CambiarRolEmpresa(88, 22, new CambiarRolUsuarioEmpresaDto { RolId = 4 }));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Listado_membresias_no_divulga_empresas_distintas_al_tenant_RBAC()
    {
        var service = new Mock<IUsuarioService>(MockBehavior.Strict);
        service.Setup(x => x.GetEmpresasAsync(88)).ReturnsAsync(new List<UsuarioEmpresaDto>
        {
            new() { UsuarioId = 88, EmpresaId = 11, RolId = 4 },
            new() { UsuarioId = 88, EmpresaId = 22, RolId = 5 }
        });
        var sut = CreateController(service, 11);
        var response = Assert.IsType<OkObjectResult>(await sut.GetEmpresas(88));
        var payload = Assert.IsType<ApiResponse<List<UsuarioEmpresaDto>>>(response.Value);
        Assert.Single(payload.Data!);
        Assert.Equal(11, payload.Data![0].EmpresaId);
        service.Verify(x => x.GetEmpresasAsync(88), Times.Once);
    }
}
