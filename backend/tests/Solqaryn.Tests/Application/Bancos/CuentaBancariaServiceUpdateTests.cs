using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class CuentaBancariaServiceUpdateTests
{
    private readonly Mock<ICuentaBancariaRepository> _mockRepo;
    private readonly Mock<IAuditoriaService> _mockAuditoria;
    private readonly CuentaBancariaService _service;

    public CuentaBancariaServiceUpdateTests()
    {
        _mockRepo = new Mock<ICuentaBancariaRepository>();
        _mockAuditoria = new Mock<IAuditoriaService>();
        _service = new CuentaBancariaService(_mockRepo.Object, _mockAuditoria.Object);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNombre_AndAudits_WhenExists()
    {
        var cuenta = new CuentaBancaria(1, "Old Name", "123", "HNL", 10m);
        var originalId = cuenta.Id;
        var originalBancoId = cuenta.BancoId;
        var originalNumeroCuenta = cuenta.NumeroCuenta;
        var originalMoneda = cuenta.Moneda;
        var originalSaldoInicial = cuenta.SaldoInicial;
        var originalEstado = cuenta.Estado;

        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cuenta);

        await _service.UpdateAsync(1, new UpdateCuentaBancariaDto { Nombre = "New Name" });

        Assert.Equal("New Name", cuenta.Nombre);
        Assert.Equal(originalId, cuenta.Id);
        Assert.Equal(originalBancoId, cuenta.BancoId);
        Assert.Equal(originalNumeroCuenta, cuenta.NumeroCuenta);
        Assert.Equal(originalMoneda, cuenta.Moneda);
        Assert.Equal(originalSaldoInicial, cuenta.SaldoInicial);
        Assert.Equal(originalEstado, cuenta.Estado);
        _mockRepo.Verify(r => r.Update(cuenta), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockAuditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Finanzas, AccionPermiso.Editar,
            It.Is<string>(s => s.Contains("New Name")), It.IsAny<int?>(), "CuentaBancaria",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(), "Exito", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenNotExists_AndDoesNotAudit()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((CuentaBancaria?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(1, new UpdateCuentaBancariaDto { Nombre = "New Name" }));

        _mockRepo.Verify(r => r.Update(It.IsAny<CuentaBancaria>()), Times.Never);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _mockAuditoria.VerifyNoOtherCalls();
    }
}
