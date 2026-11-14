using Solqaryn.Application.Bancos;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Enums.Bancos;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class CuentaBancariaServiceTests
{
    private readonly Mock<ICuentaBancariaRepository> _mockRepo;
    private readonly Mock<IAuditoriaService> _mockAuditoria;
    private readonly CuentaBancariaService _service;

    public CuentaBancariaServiceTests()
    {
        _mockRepo = new Mock<ICuentaBancariaRepository>();
        _mockAuditoria = new Mock<IAuditoriaService>();
        _service = new CuentaBancariaService(_mockRepo.Object, _mockAuditoria.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenExists()
    {
        var cuenta = new CuentaBancaria(1, "Test", "123", "HNL", 10m);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cuenta);
        var result = await _service.GetByIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal("Test", result.Nombre);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((CuentaBancaria?)null);
        var result = await _service.GetByIdAsync(1);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_CreatesAndAudits()
    {
        var dto = new CreateCuentaBancariaDto { BancoId = 1, Nombre = "N", NumeroCuenta = "123", Moneda = "HNL", SaldoInicial = 10m };
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<CuentaBancaria>())).Returns(Task.CompletedTask);
        _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var result = await _service.AddAsync(dto);
        Assert.NotNull(result);
        Assert.Equal("N", result.Nombre);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<CuentaBancaria>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockAuditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Finanzas, AccionPermiso.Crear,
            It.Is<string>(s => s.Contains("N")), It.IsAny<int?>(), "CuentaBancaria",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), "Exito", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ActivarAsync_ActivatesSavesAndAudits()
    {
        var cuenta = new CuentaBancaria(1, "Test", "123", "HNL", 10m);
        cuenta.Desactivar();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cuenta);
        await _service.ActivarAsync(1);
        Assert.Equal(EstadoCuentaBancaria.Activa, cuenta.Estado);
        _mockRepo.Verify(r => r.Update(cuenta), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockAuditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Finanzas, AccionPermiso.Activar,
            It.IsAny<string>(), It.IsAny<int?>(), "CuentaBancaria",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), "Exito", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ActivarAsync_ThrowsInvalidOperationException_WhenNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((CuentaBancaria?)null);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ActivarAsync(1));
        Assert.Equal("No se encontró la cuenta con Id 1.", exception.Message);
        _mockAuditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedDto()
    {
        var cuentas = new List<CuentaBancaria>
        {
            new CuentaBancaria(1, "Test 1", "123", "HNL", 10m),
            new CuentaBancaria(2, "Test 2", "456", "HNL", 20m)
        };
        var page = new CuentaBancariaPage<CuentaBancaria>(cuentas, 1, 10, 2);
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CuentaBancariaQueryFilter>())).ReturnsAsync(page);
        var result = await _service.GetAllAsync(new CuentaBancariaQueryFilter());
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Test 1", result.Items[0].Nombre);
        Assert.Equal("Test 2", result.Items[1].Nombre);
    }

    [Fact]
    public async Task GetActivasAsync_ReturnsListOfDto()
    {
        var cuentas = new List<CuentaBancaria> { new CuentaBancaria(1, "Test 1", "123", "HNL", 10m) };
        _mockRepo.Setup(r => r.GetActivasAsync()).ReturnsAsync(cuentas);
        var result = await _service.GetActivasAsync();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Test 1", result[0].Nombre);
    }

    [Fact]
    public async Task DesactivarAsync_DeactivatesSavesAndAudits()
    {
        var cuenta = new CuentaBancaria(1, "Test", "123", "HNL", 10m);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cuenta);
        await _service.DesactivarAsync(1);
        Assert.Equal(EstadoCuentaBancaria.Inactiva, cuenta.Estado);
        _mockRepo.Verify(r => r.Update(cuenta), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockAuditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Finanzas, AccionPermiso.Desactivar,
            It.IsAny<string>(), It.IsAny<int?>(), "CuentaBancaria",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), "Exito", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DesactivarAsync_ThrowsInvalidOperationException_WhenNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((CuentaBancaria?)null);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DesactivarAsync(1));
        Assert.Equal("No se encontró la cuenta con Id 1.", exception.Message);
        _mockAuditoria.VerifyNoOtherCalls();
    }
}
