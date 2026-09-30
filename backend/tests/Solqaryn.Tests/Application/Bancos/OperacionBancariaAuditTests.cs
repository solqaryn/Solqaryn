using Solqaryn.Application.Bancos;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class OperacionBancariaAuditTests
{
    [Fact]
    public async Task SuccessfulOperations_RegisterOneSanitizedAuditEventEach()
    {
        var cuentaRepo = new Mock<ICuentaBancariaRepository>();
        var movimientoRepo = new Mock<IMovimientoFinancieroRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var auditoria = new Mock<IAuditoriaService>();

        var origen = new CuentaBancaria(1, "Cuenta Origen", "1234567890", "HNL", 1000m);
        var destino = new CuentaBancaria(1, "Cuenta Destino", "0987654321", "HNL", 1000m);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(destino, 2);

        cuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        cuentaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(destino);
        movimientoRepo
            .Setup(r => r.GetByBancosIdempotencyKeyAsync(It.IsAny<string>(), 99))
            .ReturnsAsync(new List<Solqaryn.Domain.Entities.MovimientoFinanciero>());
        unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        var service = new OperacionBancariaService(
            cuentaRepo.Object,
            movimientoRepo.Object,
            unitOfWork.Object,
            auditoria.Object);

        await service.RegistrarDepositoAsync(
            new DepositoBancarioDto { CuentaId = 1, Monto = 100m, Referencia = "DEP", IdempotencyKey = "audit-dep" }, 99);
        await service.RegistrarRetiroAsync(
            new RetiroBancarioDto { CuentaId = 1, Monto = 50m, Referencia = "RET", IdempotencyKey = "audit-ret" }, 99);
        await service.RegistrarTransferenciaAsync(
            new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 25m, Referencia = "TRF", IdempotencyKey = "audit-trf" }, 99);
        await service.RegistrarComisionAsync(
            new ComisionBancariaDto { CuentaId = 1, Monto = 5m, Referencia = "COM", IdempotencyKey = "audit-com" }, 99);
        await service.RegistrarInteresAsync(
            new InteresBancarioDto { CuentaId = 1, Monto = 3m, Referencia = "INT", IdempotencyKey = "audit-int" }, 99);
        await service.RegistrarConciliacionAsync(
            new ConciliacionBancariaDto { CuentaId = 1, Monto = 2m, Referencia = "CON", IdempotencyKey = "audit-con" }, 99);

        auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Crear,
            It.Is<string>(d => !d.Contains("1234567890") && !d.Contains("0987654321")),
            It.IsAny<int?>(),
            "OperacionBancaria",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            "Exito",
            It.IsAny<string?>()), Times.Exactly(6));
    }
}
