using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N26DevolucionProveedorQaRegressionTests
{
    [Fact]
    public async Task Confirmar_repetido_no_persiste_ni_duplica_auditoria()
    {
        var devolucion = CrearDevolucionValida();
        devolucion.Confirmar(7, "qa", DateTime.UtcNow);

        var repository = new Mock<IDevolucionProveedorRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByIdForUpdateAsync(91)).ReturnsAsync(devolucion);

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var service = CrearService(repository.Object, auditoria.Object);

        var resultado = await service.ConfirmarAsync(91);

        Assert.Equal(devolucion.Id, resultado.Id);
        Assert.Equal(devolucion.Estado, resultado.Estado);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Anular_repetido_no_persiste_ni_duplica_auditoria()
    {
        var devolucion = CrearDevolucionValida();
        devolucion.Confirmar(7, "qa", DateTime.UtcNow.AddMinutes(-1));
        devolucion.Anular(7, "Motivo QA", DateTime.UtcNow);

        var repository = new Mock<IDevolucionProveedorRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByIdForUpdateAsync(91)).ReturnsAsync(devolucion);

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var service = CrearService(repository.Object, auditoria.Object);

        var resultado = await service.AnularAsync(91, new AnularDevolucionProveedorDto { Motivo = "Motivo repetido" });

        Assert.Equal(devolucion.Id, resultado.Id);
        Assert.Equal(devolucion.Estado, resultado.Estado);
        Assert.Equal("Motivo QA", devolucion.MotivoAnulacion);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.VerifyNoOtherCalls();
    }

    private static DevolucionProveedorService CrearService(
        IDevolucionProveedorRepository repository,
        IAuditoriaService auditoria)
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        return new DevolucionProveedorService(
            repository,
            Mock.Of<IRecepcionCompraRepository>(),
            Mock.Of<IFacturaProveedorRepository>(),
            currentUser.Object,
            unitOfWork.Object,
            auditoria);
    }

    private static DevolucionProveedor CrearDevolucionValida() => new()
    {
        Id = 91,
        NumeroDevolucion = "DVP-QA-000091",
        ProveedorId = 10,
        OrdenCompraId = 40,
        RecepcionCompraId = 20,
        FacturaProveedorId = 30,
        ProveedorNombreSnapshot = "Proveedor QA",
        Moneda = "HNL",
        Motivo = "QA regresión",
        Detalles = new List<DevolucionProveedorDetalle>
        {
            new()
            {
                RecepcionCompraDetalleId = 200,
                OrdenCompraDetalleId = 400,
                ProductoId = 500,
                ProductoVarianteId = 600,
                AlmacenId = 700,
                Cantidad = 1m,
                CostoUnitarioSnapshot = 10m,
                ImpuestoUnitarioSnapshot = 1.5m,
                ProductoNombreSnapshot = "Producto QA"
            }
        }
    };
}
