using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Contabilidad;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.Tests;

public sealed class N49GPeriodoContableApiServiceRegressionTests
{
    [Fact]
    public async Task Controller_GetById_WhenMissing_ReturnsNotFoundFailResponse()
    {
        var service = new Mock<IPeriodoContableService>();
        service.Setup(x => x.GetByIdAsync(404)).ReturnsAsync((PeriodoContableDto?)null);
        var controller = new PeriodosContablesController(service.Object);

        var result = await controller.GetById(404);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(payload.Success);
        Assert.Equal("Período contable no encontrado.", payload.Message);
    }

    [Fact]
    public async Task Controller_Create_WhenServiceSucceeds_ReturnsCreatedAtGetById()
    {
        var input = new CrearPeriodoContableDto
        {
            FechaInicio = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaFin = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc)
        };
        var createdDto = new PeriodoContableDto
        {
            Id = 17,
            FechaInicio = input.FechaInicio,
            FechaFin = input.FechaFin
        };
        var service = new Mock<IPeriodoContableService>();
        service.Setup(x => x.CreateAsync(input)).ReturnsAsync(createdDto);
        var controller = new PeriodosContablesController(service.Object);

        var result = await controller.Create(input);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PeriodosContablesController.GetById), created.ActionName);
        Assert.Equal(17, created.RouteValues!["id"]);
        var payload = Assert.IsType<ApiResponse<PeriodoContableDto>>(created.Value);
        Assert.True(payload.Success);
        Assert.Equal(17, payload.Data!.Id);
    }

    [Fact]
    public async Task Service_Create_WhenPeriodOverlaps_FailsBeforeWriteOrAudit()
    {
        var repository = new Mock<IPeriodoContableRepository>();
        var auditoria = new Mock<IAuditoriaService>();
        var service = new PeriodoContableService(repository.Object, auditoria.Object);
        var input = new CrearPeriodoContableDto
        {
            FechaInicio = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaFin = new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        repository
            .Setup(x => x.HasOverlappingPeriodAsync(
                input.FechaInicio,
                input.FechaFin,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(input));

        Assert.Equal("El período contable se superpone con un período existente.", error.Message);
        repository.Verify(x => x.AddAsync(It.IsAny<PeriodoContable>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Service_GetById_WhenRepositoryReturnsNull_ReturnsNull()
    {
        var repository = new Mock<IPeriodoContableRepository>();
        var auditoria = new Mock<IAuditoriaService>();
        repository
            .Setup(x => x.GetByIdAsync(404, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PeriodoContable?)null);
        var service = new PeriodoContableService(repository.Object, auditoria.Object);

        var result = await service.GetByIdAsync(404);

        Assert.Null(result);
    }
}
