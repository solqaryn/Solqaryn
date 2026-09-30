using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N410DEstadosFinancierosControllerTests
{
    [Fact]
    public async Task Generar_Valido_RetornaApiResponse()
    {
        var service = new Mock<IEstadoFinancieroService>();
        var filtro = new EstadoFinancieroFiltroDto { PeriodoContableId = 1 };
        service.Setup(x => x.GenerarAsync(TipoEstadoFinanciero.BalanceGeneral, filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstadoFinancieroDto { Nombre = "Balance General" });
        var controller = new EstadosFinancierosController(service.Object);

        var result = await controller.Generar((int)TipoEstadoFinanciero.BalanceGeneral, filtro, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<EstadoFinancieroDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Balance General", response.Data!.Nombre);
    }

    [Fact]
    public async Task Generar_TipoInvalido_RetornaBadRequest()
    {
        var controller = new EstadosFinancierosController(new Mock<IEstadoFinancieroService>().Object);
        var result = await controller.Generar(999, new EstadoFinancieroFiltroDto { PeriodoContableId = 1 }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
