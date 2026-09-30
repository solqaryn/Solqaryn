using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N49GPeriodoContableConcurrencyRegressionTests
{
    private readonly Mock<IPeriodoContableRepository> _repositoryMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly PeriodoContableService _service;

    public N49GPeriodoContableConcurrencyRegressionTests()
    {
        _repositoryMock = new Mock<IPeriodoContableRepository>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _service = new PeriodoContableService(_repositoryMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task CerrarAsync_WhenAlreadyClosed_ThrowsConflictException_AndPreservesOriginalState()
    {
        // Arrange
        var periodo = new PeriodoContable(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc));

        // Force state to closed using reflection to simulate already closed period
        var originalCerradoEnUtc = new DateTime(2023, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        typeof(PeriodoContable).GetProperty("Estado")?.SetValue(periodo, EstadoPeriodoContable.Cerrado);
        typeof(PeriodoContable).GetProperty("CerradoEnUtc")?.SetValue(periodo, originalCerradoEnUtc);

        _repositoryMock.Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodo);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.CerrarAsync(1));
        Assert.Equal("El período contable ya está cerrado.", ex.Message);

        Assert.Equal(EstadoPeriodoContable.Cerrado, periodo.Estado);
        Assert.Equal(originalCerradoEnUtc, periodo.CerradoEnUtc);

        _repositoryMock.Verify(r => r.Update(It.IsAny<PeriodoContable>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenConcurrentInsertViolatesConstraint_ThrowsDbUpdateException_AndDoesNotAudit()
    {
        // Arrange
        var dto = new CrearPeriodoContableDto
        {
            FechaInicio = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaFin = new DateTime(2023, 2, 28, 23, 59, 59, DateTimeKind.Utc)
        };

        _repositoryMock.Setup(r => r.HasOverlappingPeriodAsync(dto.FechaInicio, dto.FechaFin, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Concurrent overlap detected"));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.CreateAsync(dto));

        _auditoriaMock.Verify(a => a.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CerrarAsync_WhenConcurrentUpdateModifiesPeriod_ThrowsDbUpdateConcurrencyException_AndDoesNotAudit()
    {
        // Arrange
        var periodo = new PeriodoContable(new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 3, 31, 23, 59, 59, DateTimeKind.Utc));

        _repositoryMock.Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodo);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency token mismatch"));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _service.CerrarAsync(1));

        _auditoriaMock.Verify(a => a.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}
