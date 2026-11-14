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
/// I8-007.D: la activacion/desactivacion empresarial no debe confiar en
/// X-Empresa-Id sin un tenant autorizado. Complementa el PUT de I8-007.C.
/// Prueba unitaria de controlador; no equivale a HTTP/JWT ni MySQL reales.
/// </summary>
public sealed class I8007DEmpresaEstadoSegregadoTests
{
    private static EmpresasController CrearControlador(
        Mock<IEmpresaService> servicio, int? empresaAutorizada, int empresaHeader)
    {
        var http = new DefaultHttpContext();
        if (empresaAutorizada.HasValue)
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
                empresaAutorizada.Value;
        http.Request.Headers["X-Empresa-Id"] = empresaHeader.ToString();

        return new EmpresasController(servicio.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task I8007D_CambioEstadoB_ConTenantA_RechazaHeaderBAntesDeServicio(bool activar)
    {
        var servicio = new Mock<IEmpresaService>(MockBehavior.Strict);
        var controller = CrearControlador(servicio, empresaAutorizada: 11, empresaHeader: 22);

        await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            if (activar)
                await controller.Activar(22, CancellationToken.None);
            else
                await controller.Desactivar(22, CancellationToken.None);
        });

        servicio.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task I8007D_SinMarcaTenant_RechazaAunqueHeaderCoincida(bool activar)
    {
        var servicio = new Mock<IEmpresaService>(MockBehavior.Strict);
        var controller = CrearControlador(servicio, empresaAutorizada: null, empresaHeader: 22);

        await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            if (activar)
                await controller.Activar(22, CancellationToken.None);
            else
                await controller.Desactivar(22, CancellationToken.None);
        });

        servicio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task I8007D_DesactivarA_PreservaTenantVerificadoAnteHeaderFalsificado()
    {
        var servicio = new Mock<IEmpresaService>(MockBehavior.Strict);
        servicio.Setup(x => x.CambiarEstadoAsync(
                11, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmpresaDto { Id = 11, Nombre = "Empresa A", Activa = false });

        var controller = CrearControlador(servicio, empresaAutorizada: 11, empresaHeader: 22);
        var response = Assert.IsType<OkObjectResult>(
            await controller.Desactivar(11, CancellationToken.None));
        var data = Assert.IsType<ApiResponse<EmpresaDto>>(response.Value).Data;

        Assert.NotNull(data);
        Assert.Equal(11, data.Id);
        Assert.False(data.Activa);
        servicio.Verify(x => x.CambiarEstadoAsync(
            11, false, It.IsAny<CancellationToken>()), Times.Once);
        servicio.VerifyNoOtherCalls();
    }
}
