using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Moq;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N49FPeriodoContableAuditCoverageTests
{
    private readonly Mock<IPeriodoContableRepository> _repositoryMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly PeriodoContableService _service;

    public N49FPeriodoContableAuditCoverageTests()
    {
        _repositoryMock = new Mock<IPeriodoContableRepository>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _service = new PeriodoContableService(_repositoryMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Registra_Auditoria_Con_Datos_Correctos()
    {
        var dto = new CrearPeriodoContableDto
        {
            FechaInicio = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow.AddDays(30)
        };

        _repositoryMock.Setup(r => r.HasOverlappingPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PeriodoContable>(), It.IsAny<CancellationToken>()))
            .Callback<PeriodoContable, CancellationToken>((p, c) =>
            {
                typeof(PeriodoContable).GetProperty("Id")?.SetValue(p, 1);
            })
            .Returns(Task.CompletedTask);

        _auditoriaMock.Setup(a => a.RegistrarAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        await _service.CreateAsync(dto);

        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Crear,
            "Crear período contable",
            1,
            "PeriodoContable",
            null,
            It.Is<object>(v => VerifyValoresNuevosCreate(v, dto.FechaInicio, dto.FechaFin)),
            null,
            "Exito",
            null), Times.Once);
    }

    private static bool VerifyValoresNuevosCreate(object? obj, DateTime expectedInicio, DateTime expectedFin)
    {
        if (obj == null) return false;
        var type = obj.GetType();
        var inicio = (DateTime?)type.GetProperty("FechaInicio")?.GetValue(obj);
        var fin = (DateTime?)type.GetProperty("FechaFin")?.GetValue(obj);
        var estado = (EstadoPeriodoContable?)type.GetProperty("Estado")?.GetValue(obj);

        return inicio == expectedInicio && fin == expectedFin && estado == EstadoPeriodoContable.Abierto;
    }

    [Fact]
    public async Task CerrarAsync_Registra_Auditoria_Con_Datos_Correctos()
    {
        var periodo = new PeriodoContable(DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        typeof(PeriodoContable).GetProperty("Id")?.SetValue(periodo, 1);

        _repositoryMock.Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodo);

        _auditoriaMock.Setup(a => a.RegistrarAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        await _service.CerrarAsync(1);

        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Cerrar,
            "Cerrar período contable",
            1,
            "PeriodoContable",
            It.Is<object>(v => VerifyValoresAnterioresCerrar(v)),
            It.Is<object>(v => VerifyValoresNuevosCerrar(v)),
            null,
            "Exito",
            null), Times.Once);
    }

    private static bool VerifyValoresAnterioresCerrar(object? obj)
    {
        if (obj == null) return false;
        var type = obj.GetType();
        var estado = (EstadoPeriodoContable?)type.GetProperty("Estado")?.GetValue(obj);
        var cerrado = (DateTime?)type.GetProperty("CerradoEnUtc")?.GetValue(obj);

        return estado == EstadoPeriodoContable.Abierto && cerrado == null;
    }

    private static bool VerifyValoresNuevosCerrar(object? obj)
    {
        if (obj == null) return false;
        var type = obj.GetType();
        var estado = (EstadoPeriodoContable?)type.GetProperty("Estado")?.GetValue(obj);
        var cerrado = (DateTime?)type.GetProperty("CerradoEnUtc")?.GetValue(obj);

        return estado == EstadoPeriodoContable.Cerrado && cerrado != null;
    }
}
