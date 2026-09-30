using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N25ConciliacionControllerTests
{
    [Fact]
    public void Controller_ExigeAutenticacionYRutaCanonica()
    {
        var type = typeof(ConciliacionController);

        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
        Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));

        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("conciliacion", route.Template);
    }

    [Fact]
    public void EvaluarThreeWayMatch_ConservaVerboRutaYPermisoComprasVer()
    {
        var method = typeof(ConciliacionController).GetMethod(nameof(ConciliacionController.EvaluarThreeWayMatch));
        Assert.NotNull(method);

        var httpGet = Assert.Single(method.GetCustomAttributes(inherit: true).OfType<HttpGetAttribute>());
        Assert.Equal("ordenes-compra/{ordenCompraId:int}/three-way-match", httpGet.Template);

        var permiso = Assert.Single(method.CustomAttributes.Where(a => a.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public async Task EvaluarThreeWayMatch_ConOrdenValida_RetornaOkConApiResponse()
    {
        const int ordenCompraId = 100;
        var cancellationToken = CancellationToken.None;
        var expected = new ThreeWayMatchResultDto(
            ordenCompraId,
            ThreeWayMatchStatus.Aprobado,
            Array.Empty<ThreeWayMatchLineDiscrepancyDto>());

        var service = new Mock<IThreeWayMatchService>(MockBehavior.Strict);
        service.Setup(s => s.EvaluarAsync(ordenCompraId, cancellationToken)).ReturnsAsync(expected);
        var controller = new ConciliacionController(service.Object);

        var result = await controller.EvaluarThreeWayMatch(ordenCompraId, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ThreeWayMatchResultDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Same(expected, response.Data);
        Assert.Equal(string.Empty, response.Message);
        service.Verify(s => s.EvaluarAsync(ordenCompraId, cancellationToken), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EvaluarThreeWayMatch_OrdenInexistente_PropagaResourceNotFoundParaProblemDetailsGlobal()
    {
        const int ordenCompraId = 404;
        var service = new Mock<IThreeWayMatchService>(MockBehavior.Strict);
        service.Setup(s => s.EvaluarAsync(ordenCompraId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceNotFoundException($"No existe la orden de compra {ordenCompraId}."));
        var controller = new ConciliacionController(service.Object);

        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => controller.EvaluarThreeWayMatch(ordenCompraId, CancellationToken.None));

        Assert.Contains(ordenCompraId.ToString(), ex.Message);
        service.Verify(s => s.EvaluarAsync(ordenCompraId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
