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

public class OperacionBancariaServiceTests
{
    private readonly Mock<ICuentaBancariaRepository> _mockCuentaRepo = new();
    private readonly Mock<IMovimientoFinancieroRepository> _mockMovimientoRepo = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly OperacionBancariaService _service;

    public OperacionBancariaServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(func => func());
        _mockMovimientoRepo
            .Setup(r => r.GetByBancosIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<MovimientoFinanciero>());
        _service = new OperacionBancariaService(_mockCuentaRepo.Object, _mockMovimientoRepo.Object, _mockUnitOfWork.Object);
    }

    private static CuentaBancaria CreateActiveCuenta(string numero = "123", string moneda = "HNL") =>
        new(1, "Test Bank", numero, moneda, 1000m);

    private static MovimientoFinanciero ExistingDeposito(string key, decimal monto = 500m, string referencia = "REF-1") =>
        new()
        {
            Tipo = TipoMovimientoFinanciero.Ingreso,
            Categoria = CategoriaMovimientoFinanciero.Otro,
            Concepto = $"Depósito Bancario - {referencia}",
            Monto = monto,
            Estado = EstadoMovimientoFinanciero.Pagado,
            EsAutomatico = false,
            ModuloOrigen = "Bancos",
            ReferenciaId = 1,
            CreadoPorUsuarioId = 99,
            Descripcion = $"IdempotencyKey: {key}"
        };

    [Fact]
    public async Task RegistrarDeposito_CreatesIngreso_WhenValid()
    {
        var dto = new DepositoBancarioDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = "key1" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta());
        await _service.RegistrarDepositoAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Ingreso && m.Monto == 500m && m.CreadoPorUsuarioId == 99 && m.Descripcion!.Contains("key1"))), Times.Once);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarRetiro_CreatesEgreso_WhenValid()
    {
        var dto = new RetiroBancarioDto { CuentaId = 1, Monto = 200m, Referencia = "REF-2", IdempotencyKey = "key2" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta());
        await _service.RegistrarRetiroAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Egreso && m.Monto == 200m && m.CreadoPorUsuarioId == 99 && m.Descripcion!.Contains("key2"))), Times.Once);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarTransferencia_CreatesIngresoAndEgreso_WhenValid()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 300m, Referencia = "REF-3", IdempotencyKey = "key3" };
        var origen = CreateActiveCuenta("123");
        var destino = CreateActiveCuenta("456");
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(destino, 2);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(destino);
        await _service.RegistrarTransferenciaAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Egreso && m.Monto == 300m && m.Descripcion!.Contains("key3-Egreso"))), Times.Once);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Ingreso && m.Monto == 300m && m.Descripcion!.Contains("key3-Ingreso"))), Times.Once);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarDeposito_ThrowsArgumentException_WhenCuentaNotFound()
    {
        var dto = new DepositoBancarioDto { CuentaId = 999, Monto = 500m, Referencia = "REF-1", IdempotencyKey = "key1" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CuentaBancaria?)null);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegistrarDepositoAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarRetiro_ThrowsArgumentException_WhenCuentaNotFound()
    {
        var dto = new RetiroBancarioDto { CuentaId = 999, Monto = 200m, Referencia = "REF-2", IdempotencyKey = "key2" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CuentaBancaria?)null);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegistrarRetiroAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferencia_ThrowsArgumentException_WhenCuentaOrigenNotFound()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 999, CuentaDestinoId = 2, Monto = 300m, Referencia = "REF-3", IdempotencyKey = "key3" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CuentaBancaria?)null);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegistrarTransferenciaAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferencia_ThrowsArgumentException_WhenCuentaDestinoNotFound()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 999, Monto = 300m, Referencia = "REF-3", IdempotencyKey = "key3" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta("123"));
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CuentaBancaria?)null);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegistrarTransferenciaAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarDeposito_ThrowsArgumentOutOfRangeException_WhenMontoIsZeroOrNegative()
    {
        var dto = new DepositoBancarioDto { CuentaId = 1, Monto = 0m, Referencia = "REF-1", IdempotencyKey = "key1" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta("123"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.RegistrarDepositoAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarDeposito_ThrowsInvalidOperationException_WhenCuentaInactive()
    {
        var dto = new DepositoBancarioDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = "key1" };
        var origen = CreateActiveCuenta("123");
        origen.Desactivar();
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarDepositoAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferencia_ThrowsInvalidOperationException_WhenCuentaDestinoInactive()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 300m, Referencia = "REF-3", IdempotencyKey = "key3" };
        var origen = CreateActiveCuenta("123");
        var destino = CreateActiveCuenta("456");
        destino.Desactivar();
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(destino, 2);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(destino);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarTransferenciaAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferencia_ThrowsInvalidOperationException_WhenOrigenAndDestinoAreSame()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 1, Monto = 300m, Referencia = "REF-3", IdempotencyKey = "key3" };
        var origen = CreateActiveCuenta("123");
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarTransferenciaAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferencia_ThrowsInvalidOperationException_WhenMonedasDiffer()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 300m, Referencia = "REF-3", IdempotencyKey = "key3" };
        var origen = CreateActiveCuenta("123", "HNL");
        var destino = CreateActiveCuenta("456", "USD");
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(destino, 2);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(destino);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarTransferenciaAsync(dto, 99));
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarDeposito_Idempotency_SameKeyAndPayload_DoesNotDuplicate()
    {
        const string key = "idemp-same-1";
        var dto = new DepositoBancarioDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = key };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta("123"));
        _mockMovimientoRepo
            .SetupSequence(r => r.GetByBancosIdempotencyKeyAsync(key, 99))
            .ReturnsAsync(new List<MovimientoFinanciero>())
            .ReturnsAsync(new List<MovimientoFinanciero> { ExistingDeposito(key) });

        await _service.RegistrarDepositoAsync(dto, 99);
        await _service.RegistrarDepositoAsync(dto, 99);

        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Once);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarDeposito_Idempotency_SameKeyDifferentPayload_ThrowsConflictException()
    {
        const string key = "idemp-diff-1";
        var dto1 = new DepositoBancarioDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = key };
        var dto2 = new DepositoBancarioDto { CuentaId = 1, Monto = 600m, Referencia = "REF-2", IdempotencyKey = key };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta("123"));
        _mockMovimientoRepo
            .SetupSequence(r => r.GetByBancosIdempotencyKeyAsync(key, 99))
            .ReturnsAsync(new List<MovimientoFinanciero>())
            .ReturnsAsync(new List<MovimientoFinanciero> { ExistingDeposito(key) });

        await _service.RegistrarDepositoAsync(dto1, 99);
        await Assert.ThrowsAsync<ConflictException>(() => _service.RegistrarDepositoAsync(dto2, 99));

        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Once);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarTransferencia_Idempotency_SameKeyAndPayload_DoesNotDuplicate()
    {
        const string key = "idemp-trans-same-1";
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 300m, Referencia = "REF-3", IdempotencyKey = key };
        var origen = CreateActiveCuenta("123");
        var destino = CreateActiveCuenta("456");
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(destino, 2);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(destino);

        var existingMovimientos = new List<MovimientoFinanciero>
        {
            new MovimientoFinanciero { Tipo = TipoMovimientoFinanciero.Egreso, Categoria = CategoriaMovimientoFinanciero.Otro, Concepto = "Transferencia a cuenta 456 - REF-3", Monto = 300m, ReferenciaId = 1, CreadoPorUsuarioId = 99, Descripcion = $"IdempotencyKey: {key}-Egreso" },
            new MovimientoFinanciero { Tipo = TipoMovimientoFinanciero.Ingreso, Categoria = CategoriaMovimientoFinanciero.Otro, Concepto = "Transferencia de cuenta 123 - REF-3", Monto = 300m, ReferenciaId = 2, CreadoPorUsuarioId = 99, Descripcion = $"IdempotencyKey: {key}-Ingreso" }
        };

        _mockMovimientoRepo
            .SetupSequence(r => r.GetByBancosIdempotencyKeyAsync(key, 99))
            .ReturnsAsync(new List<MovimientoFinanciero>())
            .ReturnsAsync(existingMovimientos);

        await _service.RegistrarTransferenciaAsync(dto, 99);
        await _service.RegistrarTransferenciaAsync(dto, 99);

        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Exactly(2));
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarTransferencia_Idempotency_SameKeyDifferentPayload_ThrowsConflictException()
    {
        const string key = "idemp-trans-diff-1";
        var dto1 = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 300m, Referencia = "REF-3", IdempotencyKey = key };
        var dto2 = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 400m, Referencia = "REF-4", IdempotencyKey = key };
        var origen = CreateActiveCuenta("123");
        var destino = CreateActiveCuenta("456");
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(destino, 2);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(destino);

        var existingMovimientos = new List<MovimientoFinanciero>
        {
            new MovimientoFinanciero { Tipo = TipoMovimientoFinanciero.Egreso, Categoria = CategoriaMovimientoFinanciero.Otro, Concepto = "Transferencia a cuenta 456 - REF-3", Monto = 300m, ReferenciaId = 1, CreadoPorUsuarioId = 99, Descripcion = $"IdempotencyKey: {key}-Egreso" },
            new MovimientoFinanciero { Tipo = TipoMovimientoFinanciero.Ingreso, Categoria = CategoriaMovimientoFinanciero.Otro, Concepto = "Transferencia de cuenta 123 - REF-3", Monto = 300m, ReferenciaId = 2, CreadoPorUsuarioId = 99, Descripcion = $"IdempotencyKey: {key}-Ingreso" }
        };

        _mockMovimientoRepo
            .SetupSequence(r => r.GetByBancosIdempotencyKeyAsync(key, 99))
            .ReturnsAsync(new List<MovimientoFinanciero>())
            .ReturnsAsync(existingMovimientos);

        await _service.RegistrarTransferenciaAsync(dto1, 99);
        await Assert.ThrowsAsync<ConflictException>(() => _service.RegistrarTransferenciaAsync(dto2, 99));

        _mockMovimientoRepo.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Exactly(2));
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
