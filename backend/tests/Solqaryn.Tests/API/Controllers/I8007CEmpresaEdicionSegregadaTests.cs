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

/// <summary>
/// I8-007.C: limita la edicion empresarial a la identidad tenant ya verificada.
/// Prueba unitaria del controlador, no certificacion JWT/HTTP ni MySQL.
/// </summary>
public sealed class I8007CEmpresaEdicionSegregadaTests
{
    private static EmpresasController Controller(
        Mock<IEmpresaService> service,
        int empresaAutorizada,
        int empresaEnHeader)
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaAutorizada;
        http.Request.Headers["X-Empresa-Id"] = empresaEnHeader.ToString();
        return new EmpresasController(service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task I8007C_EdicionDeB_RechazaHeaderFalsificadoAntesDeInvocarServicio()
    {
        var service = new Mock<IEmpresaService>(MockBehavior.Strict);
        var controller = Controller(service, empresaAutorizada: 11, empresaEnHeader: 22);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controller.Update(22, new UpdateEmpresaDto { Nombre = "B adulterada" },
                CancellationToken.None));

        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task I8007C_EdicionDeA_ConservaTenantVerificadoAunqueHeaderSoliciteB()
    {
        var service = new Mock<IEmpresaService>(MockBehavior.Strict);
        service.Setup(x => x.UpdateAsync(
                11,
                It.Is<UpdateEmpresaDto>(dto => dto.Nombre == "Empresa A actualizada"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmpresaDto
            {
                Id = 11,
                Nombre = "Empresa A actualizada",
                Activa = true
            });

        var controller = Controller(service, empresaAutorizada: 11, empresaEnHeader: 22);
        var response = Assert.IsType<OkObjectResult>(
            await controller.Update(
                11,
                new UpdateEmpresaDto { Nombre = "Empresa A actualizada" },
                CancellationToken.None));
        var data = Assert.IsType<ApiResponse<EmpresaDto>>(response.Value).Data;

        Assert.NotNull(data);
        Assert.Equal(11, data.Id);
        Assert.Equal("Empresa A actualizada", data.Nombre);
        service.Verify(x => x.UpdateAsync(
            11,
            It.IsAny<UpdateEmpresaDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
        service.VerifyNoOtherCalls();
    }
}
