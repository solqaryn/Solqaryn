using Solqaryn.Application.Bancos;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class OperacionBancariaServiceConciliacionTests
{
    private readonly Mock<ICuentaBancariaRepository> _mockCuentaRepo = new();
    private readonly Mock<IMovimientoFinancieroRepository> _mockMovimientoRepo = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly OperacionBancariaService _service;

    public OperacionBancariaServiceConciliacionTests()
    {
        _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(func => func());
        _mockMovimientoRepo.Setup(r => r.GetByBancosIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new List<MovimientoFinanciero>());
        _service = new OperacionBancariaService(_mockCuentaRepo.Object, _mockMovimientoRepo.Object, _mockUnitOfWork.Object);
    }

    private static CuentaBancaria CreateActiveCuenta(string numero = "123", string moneda = "HNL") => new(1, "Test Bank", numero, moneda, 1000m);

    private static MovimientoFinanciero ExistingConciliacion(string key, decimal monto = 500m, string referencia = "REF-1") => new()
    {
        Tipo = TipoMovimientoFinanciero.Ajuste,
        Categoria = CategoriaMovimientoFinanciero.Ajuste,
        Concepto = $"Ajuste por Conciliación Bancaria - {referencia}",
        Monto = monto,
        Estado = EstadoMovimientoFinanciero.Pagado,
        EsAutomatico = false,
        ModuloOrigen = "Bancos",
        ReferenciaId = 1,
        CreadoPorUsuarioId = 99,
        Descripcion = $"IdempotencyKey: {key}"
    };

    [Fact]
    public async Task RegistrarConciliacion_CreatesAjuste_WhenValid()
    {
        var dto = new ConciliacionBancariaDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = "key-con-1" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta());
        await _service.RegistrarConciliacionAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Ajuste && m.Categoria == CategoriaMovimientoFinanciero.Ajuste && m.Monto == 500m && m.CreadoPorUsuarioId == 99 && m.Concepto == "Ajuste por Conciliación Bancaria - REF-1" && m.Descripcion!.Contains("key-con-1"))), Times.Once);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarConciliacion_ThrowsArgumentException_WhenCuentaNotFound()
    {
        var dto = new ConciliacionBancariaDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = "key-con-1" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((CuentaBancaria)null!);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegistrarConciliacionAsync(dto, 99));
    }

    [Fact]
    public async Task RegistrarConciliacion_Idempotency_SameKeyAndPayload_DoesNotDuplicate()
    {
        const string key = "idemp-con-same-1";
        var dto = new ConciliacionBancariaDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = key };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta());
        _mockMovimientoRepo.SetupSequence(r => r.GetByBancosIdempotencyKeyAsync(key, 99)).ReturnsAsync(new List<MovimientoFinanciero>()).ReturnsAsync(new List<MovimientoFinanciero> { ExistingConciliacion(key) });
        await _service.RegistrarConciliacionAsync(dto, 99);
        await _service.RegistrarConciliacionAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarConciliacion_Idempotency_SameKeyDifferentPayload_ThrowsConflictException()
    {
        const string key = "idemp-con-diff-1";
        var dto1 = new ConciliacionBancariaDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = key };
        var dto2 = new ConciliacionBancariaDto { CuentaId = 1, Monto = 600m, Referencia = "REF-2", IdempotencyKey = key };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta());
        _mockMovimientoRepo.SetupSequence(r => r.GetByBancosIdempotencyKeyAsync(key, 99)).ReturnsAsync(new List<MovimientoFinanciero>()).ReturnsAsync(new List<MovimientoFinanciero> { ExistingConciliacion(key) });
        await _service.RegistrarConciliacionAsync(dto1, 99);
        await Assert.ThrowsAsync<ConflictException>(() => _service.RegistrarConciliacionAsync(dto2, 99));
    }
}
