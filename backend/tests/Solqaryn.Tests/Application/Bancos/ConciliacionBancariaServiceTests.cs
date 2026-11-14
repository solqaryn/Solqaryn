using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class ConciliacionBancariaServiceTests
{
    private readonly Mock<IConciliacionBancariaRepository> _conciliacionRepoMock;
    private readonly Mock<IMovimientoFinancieroRepository> _movimientoFinancieroRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOperacionBancariaService> _operacionBancariaServiceMock;
    private readonly ConciliacionBancariaService _service;

    public ConciliacionBancariaServiceTests()
    {
        _conciliacionRepoMock = new Mock<IConciliacionBancariaRepository>();
        _movimientoFinancieroRepoMock = new Mock<IMovimientoFinancieroRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _operacionBancariaServiceMock = new Mock<IOperacionBancariaService>();

        _unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(f => f());

        _service = new ConciliacionBancariaService(
            _conciliacionRepoMock.Object,
            _movimientoFinancieroRepoMock.Object,
            _operacionBancariaServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ImportarEstadoCuentaAsync_NoActiva_ReturnsError()
    {
        _conciliacionRepoMock.Setup(r => r.GetActivaByCuentaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConciliacionBancaria?)null);

        var request = new ImportarEstadoCuentaRequestDto
        {
            CuentaBancariaId = 1,
            Movimientos = new[]
            {
                new MovimientoEstadoCuentaDto
                {
                    IdentificadorExternoTransaccion = "EXT-1",
                    Monto = 100,
                    FechaOperacion = DateTime.Today
                }
            }
        };

        var response = await _service.ImportarEstadoCuentaAsync(request, 1);

        Assert.Equal(1, response.CuentaBancariaId);
        Assert.Equal(0, response.MovimientosImportados);
        Assert.Single(response.Errores);
        Assert.Contains("No hay una conciliación activa", response.Errores.First());
    }

    [Fact]
    public async Task ImportarEstadoCuentaAsync_IgnoresDuplicateMovimiento()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.Today, DateTime.Today, 0, 100, "Ref");
        conciliacion.AgregarMovimiento(new MovimientoEstadoCuenta("EXT-1", DateTime.Today, "Desc", "Ref", TipoMovimientoEstadoCuenta.Credito, 100));
        
        _conciliacionRepoMock.Setup(r => r.GetActivaByCuentaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conciliacion);

        var request = new ImportarEstadoCuentaRequestDto
        {
            CuentaBancariaId = 1,
            Movimientos = new[]
            {
                new MovimientoEstadoCuentaDto
                {
                    IdentificadorExternoTransaccion = "EXT-1",
                    Monto = 100,
                    FechaOperacion = DateTime.Today
                }
            }
        };

        var response = await _service.ImportarEstadoCuentaAsync(request, 1);

        Assert.Equal(1, response.MovimientosDuplicadosIgnorados);
        Assert.Equal(0, response.MovimientosImportados);
    }

    [Fact]
    public async Task CerrarPeriodoAsync_Valido_CompletaYRetornaExitoso()
    {
        var conciliacion = new ConciliacionBancaria(1, new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), 0, 0, "Ref");
        conciliacion.MarcarComoEnProceso();
        
        _conciliacionRepoMock.Setup(r => r.GetActivaByCuentaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conciliacion);

        var request = new CerrarPeriodoConciliacionRequestDto
        {
            CuentaBancariaId = 1,
            Mes = 8,
            Anio = 2026,
            SaldoFinalEstadoCuenta = 0
        };

        var response = await _service.CerrarPeriodoAsync(request, 1);

        Assert.True(response.Exitoso);
        Assert.Equal("Conciliación cerrada exitosamente.", response.Mensaje);
        Assert.Equal(EstadoConciliacionBancaria.Completada, conciliacion.Estado);
        _conciliacionRepoMock.Verify(r => r.Update(conciliacion), Times.Once);
    }
}
