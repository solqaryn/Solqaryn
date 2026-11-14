using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N22OrdenCompraControllerBehaviorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_sin_idempotency_key_falla_cerrado_y_no_invoca_servicio(string? key)
    {
        var service = new Mock<IOrdenCompraService>(MockBehavior.Strict);
        var controller = new OrdenesCompraController(service.Object);

        var result = await controller.Create(new CreateOrdenCompraDto(), key);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal("Idempotency-Key requerido", details.Title);
        Assert.Contains("Idempotency-Key", details.Detail, StringComparison.Ordinal);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_inexistente_responde_problem_details_404_sin_fail_open()
    {
        var service = new Mock<IOrdenCompraService>(MockBehavior.Strict);
        service.Setup(x => x.GetByIdAsync(404)).ReturnsAsync((OrdenCompraDto?)null);
        var controller = new OrdenesCompraController(service.Object);

        var result = await controller.GetById(404);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal("Orden de compra no encontrada", details.Title);
        Assert.Contains("identificador", details.Detail, StringComparison.OrdinalIgnoreCase);
        service.Verify(x => x.GetByIdAsync(404), Times.Once);
        service.VerifyNoOtherCalls();
    }
}
