using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Contabilidad;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class PeriodoContablePaginationFilterTests
{
    private readonly Mock<IPeriodoContableRepository> _repositoryMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly PeriodoContableService _service;

    public PeriodoContablePaginationFilterTests()
    {
        _repositoryMock = new Mock<IPeriodoContableRepository>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _service = new PeriodoContableService(_repositoryMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task GetPagedAsync_CallsRepository_WithCorrectFilter_AndReturnsMappedResult()
    {
        var filter = new PeriodoContableQueryDto
        {
            Page = 2,
            PageSize = 10,
            FechaDesde = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaHasta = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            Estado = EstadoPeriodoContable.Abierto
        };

        var dbPeriodo = new PeriodoContable(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc));
        typeof(PeriodoContable).GetProperty("Id")?.SetValue(dbPeriodo, 1);

        var pagedResult = new PagedResult<PeriodoContable>
        {
            Items = new List<PeriodoContable> { dbPeriodo },
            Page = 2,
            PageSize = 10,
            TotalCount = 25
        };

        _repositoryMock.Setup(r => r.GetPagedAsync(filter, default)).ReturnsAsync(pagedResult);

        var result = await _service.GetPagedAsync(filter);

        _repositoryMock.Verify(r => r.GetPagedAsync(filter, default), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items.First().Id);
        Assert.Equal(dbPeriodo.FechaInicio, result.Items.First().FechaInicio);
        Assert.Equal(dbPeriodo.FechaFin, result.Items.First().FechaFin);
        Assert.Equal(dbPeriodo.Estado, result.Items.First().Estado);
        Assert.Equal(dbPeriodo.CerradoEnUtc, result.Items.First().CerradoEnUtc);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsEmptyResult_WhenNoMatch()
    {
        var filter = new PeriodoContableQueryDto
        {
            Page = 1,
            PageSize = 10,
            Estado = EstadoPeriodoContable.Cerrado
        };

        var pagedResult = new PagedResult<PeriodoContable>
        {
            Items = new List<PeriodoContable>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        _repositoryMock.Setup(r => r.GetPagedAsync(filter, default)).ReturnsAsync(pagedResult);

        var result = await _service.GetPagedAsync(filter);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
