using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class ConciliacionBancariaServiceAuditTests
{
    [Fact]
    public async Task ImportarEstadoCuentaAsync_RegistraAuditoriaImportar_SinExponerPayload()
    {
        var fixture = new Fixture();
        fixture.ConciliacionRepo
            .Setup(r => r.GetActivaByCuentaAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConciliacionBancaria?)null);

        var request = new ImportarEstadoCuentaRequestDto
        {
            CuentaBancariaId = 7,
            Movimientos = Array.Empty<MovimientoEstadoCuentaDto>()
        };

        await fixture.Service.ImportarEstadoCuentaAsync(request, 31);

        fixture.Auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Importar,
            It.Is<string>(s => s.Contains("Importación", StringComparison.OrdinalIgnoreCase)),
            7,
            "ConciliacionBancaria",
            null,
            It.IsAny<object>(),
            null,
            "Fallo",
            It.Is<string>(s => s.Contains("incidencia", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Fact]
    public async Task ConciliarMovimientosAsync_UsaMismoPermisoCrearQueElController()
    {
        var fixture = new Fixture();
        fixture.ConciliacionRepo
            .Setup(r => r.GetActivaByCuentaAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConciliacionBancaria?)null);

        var request = new ConciliarMovimientosRequestDto
        {
            CuentaBancariaId = 9,
            Matches = Array.Empty<MatchConciliacionDto>()
        };

        await fixture.Service.ConciliarMovimientosAsync(request, 31);

        fixture.Auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            9,
            "ConciliacionBancaria",
            null,
            It.IsAny<object>(),
            null,
            "Fallo",
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CerrarPeriodoAsync_Exitoso_RegistraAuditoriaEstrictoCerrar()
    {
        var fixture = new Fixture();
        var conciliacion = new ConciliacionBancaria(
            11,
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 31),
            0,
            0,
            "AUDIT-CLOSE");
        conciliacion.MarcarComoEnProceso();

        fixture.ConciliacionRepo
            .Setup(r => r.GetActivaByCuentaAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conciliacion);

        var response = await fixture.Service.CerrarPeriodoAsync(new CerrarPeriodoConciliacionRequestDto
        {
            CuentaBancariaId = 11,
            Mes = 8,
            Anio = 2026,
            SaldoFinalEstadoCuenta = 0
        }, 31);

        Assert.True(response.Exitoso);
        fixture.Auditoria.Verify(a => a.RegistrarEstrictoAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Cerrar,
            It.IsAny<string>(),
            conciliacion.Id,
            "ConciliacionBancaria",
            null,
            It.IsAny<object>(),
            null,
            "Exito",
            null), Times.Once);
    }

    private sealed class Fixture
    {
        public Mock<IConciliacionBancariaRepository> ConciliacionRepo { get; } = new();
        public Mock<IMovimientoFinancieroRepository> MovimientoRepo { get; } = new();
        public Mock<IOperacionBancariaService> OperacionService { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<IAuditoriaService> Auditoria { get; } = new();
        public ConciliacionBancariaService Service { get; }

        public Fixture()
        {
            UnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            Service = new ConciliacionBancariaService(
                ConciliacionRepo.Object,
                MovimientoRepo.Object,
                OperacionService.Object,
                UnitOfWork.Object,
                Auditoria.Object);
        }
    }
}
