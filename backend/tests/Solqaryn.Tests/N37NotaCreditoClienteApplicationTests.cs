using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N37NotaCreditoClienteApplicationTests
{
    [Fact]
    public async Task CreateAsync_PersisteYAuditaDentroDeTransaccion()
    {
        var repository = new Mock<INotaCreditoClienteRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<NotaCreditoCliente>()))
            .Callback<NotaCreditoCliente>(x => x.Id = 77)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var facturas = new Mock<IFacturaRepository>();
        facturas.Setup(x => x.GetByIdAsync(12)).ReturnsAsync(new Factura
        {
            Id = 12,
            VentaId = 34,
            Estado = EstadoFactura.Emitida,
            Moneda = "hnl",
            Total = 1000m
        });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(9);
        currentUser.SetupGet(x => x.NombreCompleto).Returns("QA Usuario");

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var service = new NotaCreditoClienteService(
            repository.Object, facturas.Object, currentUser.Object, unitOfWork.Object, auditoria.Object);

        var result = await service.CreateAsync(new CreateNotaCreditoClienteDto
        {
            FacturaId = 12,
            MontoCredito = 250.12555m,
            Motivo = "Ajuste comercial",
            Observaciones = "Referencia interna"
        });

        Assert.Equal(77, result.Id);
        Assert.Equal(12, result.FacturaId);
        Assert.Equal(34, result.VentaId);
        Assert.Equal("HNL", result.Moneda);
        Assert.Equal(250.1256m, result.MontoCredito);
        Assert.Equal(9, result.CreadoPorUsuarioId);
        repository.Verify(x => x.AddAsync(It.IsAny<NotaCreditoCliente>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            77,
            nameof(NotaCreditoCliente),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FacturaInexistente_FallaComoNotFoundSinPersistir()
    {
        var repository = new Mock<INotaCreditoClienteRepository>();
        var facturas = new Mock<IFacturaRepository>();
        facturas.Setup(x => x.GetByIdAsync(404)).ReturnsAsync((Factura?)null);
        var currentUser = UsuarioAutenticado();
        var unitOfWork = UnidadTransaccional();
        var auditoria = new Mock<IAuditoriaService>();
        var service = new NotaCreditoClienteService(repository.Object, facturas.Object, currentUser.Object, unitOfWork.Object, auditoria.Object);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.CreateAsync(new CreateNotaCreditoClienteDto
        {
            FacturaId = 404,
            MontoCredito = 10m,
            Motivo = "Factura ausente"
        }));

        repository.Verify(x => x.AddAsync(It.IsAny<NotaCreditoCliente>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CreditoMayorAlTotal_FallaComoReglaDeNegocio()
    {
        var repository = new Mock<INotaCreditoClienteRepository>();
        var facturas = new Mock<IFacturaRepository>();
        facturas.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new Factura
        {
            Id = 5,
            VentaId = 8,
            Estado = EstadoFactura.Emitida,
            Moneda = "HNL",
            Total = 100m
        });
        var currentUser = UsuarioAutenticado();
        var unitOfWork = UnidadTransaccional();
        var auditoria = new Mock<IAuditoriaService>();
        var service = new NotaCreditoClienteService(repository.Object, facturas.Object, currentUser.Object, unitOfWork.Object, auditoria.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateNotaCreditoClienteDto
        {
            FacturaId = 5,
            MontoCredito = 101m,
            Motivo = "Monto inválido"
        }));

        repository.Verify(x => x.AddAsync(It.IsAny<NotaCreditoCliente>()), Times.Never);
    }

    private static Mock<ICurrentUserService> UsuarioAutenticado()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(9);
        currentUser.SetupGet(x => x.NombreCompleto).Returns("QA Usuario");
        return currentUser;
    }

    private static Mock<IUnitOfWork> UnidadTransaccional()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        return unitOfWork;
    }
}
