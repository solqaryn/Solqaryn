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

public sealed class EmpresasTenantBoundaryTests
{
    private static EmpresasController Controller(Mock<IEmpresaService> service, int empresaId)
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
        return new EmpresasController(service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task ListarEmpresas_ConPermisoTenantA_NoEnumeraIdentidadDeB()
    {
        var service = new Mock<IEmpresaService>(MockBehavior.Strict);
        service.Setup(x => x.ListAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmpresaDto>
            {
                new() { Id = 11, Nombre = "Solo A" },
                new() { Id = 22, Nombre = "Confidencial B" }
            });
        var sut = Controller(service, 11);
        var result = Assert.IsType<OkObjectResult>(
            await sut.List(null, CancellationToken.None));
        var data = Assert.IsType<ApiResponse<List<EmpresaDto>>>(result.Value).Data!;
        Assert.Single(data);
        Assert.Equal(11, data[0].Id);
        Assert.DoesNotContain(data, x => x.Id == 22);
        service.VerifyAll();
    }

    [Fact]
    public async Task EndpointsConEmpresaIdDeB_NoAccedenAServicioDesdeA()
    {
        var service = new Mock<IEmpresaService>(MockBehavior.Strict);
        var sut = Controller(service, 11);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.GetById(22, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.Update(22, new UpdateEmpresaDto { Nombre = "B" }, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.Activar(22, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.Desactivar(22, CancellationToken.None));
        service.VerifyNoOtherCalls();
    }
}
