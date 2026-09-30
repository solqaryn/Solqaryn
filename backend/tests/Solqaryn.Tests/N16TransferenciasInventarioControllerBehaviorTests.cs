using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciasInventarioControllerBehaviorTests
{
    [Fact]
    public async Task Create_RetornaCreatedAtActionConRecursoCreado()
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var movimientos = new Mock<ITransferenciaInventarioMovimientoService>();
        var dto = new CreateTransferenciaInventarioDto
        {
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            Detalles = { new TransferenciaInventarioDetalleInputDto { ProductoVarianteId = 10, CantidadSolicitada = 3 } }
        };
        var creado = new TransferenciaInventarioDto { Id = 77, Numero = "TR-77", AlmacenOrigenId = 1, AlmacenDestinoId = 2 };
        service.Setup(x => x.CreateAsync(dto)).ReturnsAsync(creado);
        var controller = new TransferenciasInventarioController(service.Object, movimientos.Object);

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TransferenciasInventarioController.GetById), created.ActionName);
        Assert.Equal(77, created.RouteValues!["id"]);
        service.Verify(x => x.CreateAsync(dto), Times.Once);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("solicitar")]
    public async Task AccionesSobreIdInexistente_RetornanNotFound(string accion)
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var movimientos = new Mock<ITransferenciaInventarioMovimientoService>();
        service.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((TransferenciaInventarioDto?)null);
        service.Setup(x => x.SolicitarAsync(999)).ReturnsAsync((TransferenciaInventarioDto?)null);
        var controller = new TransferenciasInventarioController(service.Object, movimientos.Object);

        IActionResult result = accion == "get"
            ? await controller.GetById(999)
            : await controller.Solicitar(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Recibir_DelegaDiscrepanciasAlCasoDeUsoYRetornaOk()
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var movimientos = new Mock<ITransferenciaInventarioMovimientoService>();
        var dto = new RecibirTransferenciaInventarioDto
        {
            Detalles =
            {
                new RecibirTransferenciaInventarioDetalleDto
                {
                    DetalleId = 5,
                    CantidadRecibida = 7,
                    CantidadFaltante = 1,
                    CantidadDanada = 1,
                    CantidadSobrante = 0
                }
            }
        };
        var recibida = new TransferenciaInventarioDto { Id = 21, Estado = "Recibida" };
        movimientos.Setup(x => x.RecibirAsync(21, dto)).ReturnsAsync(recibida);
        var controller = new TransferenciasInventarioController(service.Object, movimientos.Object);

        var result = await controller.Recibir(21, dto);

        Assert.IsType<OkObjectResult>(result);
        movimientos.Verify(x => x.RecibirAsync(21, dto), Times.Once);
    }

    [Fact]
    public async Task Despachar_IdInexistente_RetornaNotFound()
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var movimientos = new Mock<ITransferenciaInventarioMovimientoService>();
        var dto = new DespacharTransferenciaInventarioDto();
        movimientos.Setup(x => x.DespacharAsync(404, dto)).ReturnsAsync((TransferenciaInventarioDto?)null);
        var controller = new TransferenciasInventarioController(service.Object, movimientos.Object);

        var result = await controller.Despachar(404, dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Despachar_ReglaDeNegocio_PropagaExcepcionParaProblemDetailsGlobal()
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var movimientos = new Mock<ITransferenciaInventarioMovimientoService>();
        var dto = new DespacharTransferenciaInventarioDto();
        movimientos
            .Setup(x => x.DespacharAsync(31, dto))
            .ThrowsAsync(new BusinessRuleException("Solo una transferencia aprobada puede despacharse."));
        var controller = new TransferenciasInventarioController(service.Object, movimientos.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => controller.Despachar(31, dto));

        Assert.Contains("aprobada", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Recibir_ReintentoYaRecibido_RetornaMismoRecursoDentroDelEnvelopeApi()
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var movimientos = new Mock<ITransferenciaInventarioMovimientoService>();
        var dto = new RecibirTransferenciaInventarioDto();
        var yaRecibida = new TransferenciaInventarioDto { Id = 32, Estado = "Recibida" };
        movimientos.Setup(x => x.RecibirAsync(32, dto)).ReturnsAsync(yaRecibida);
        var controller = new TransferenciasInventarioController(service.Object, movimientos.Object);

        var result = await controller.Recibir(32, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<TransferenciaInventarioDto>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.Same(yaRecibida, envelope.Data);
        movimientos.Verify(x => x.RecibirAsync(32, dto), Times.Once);
    }
}
