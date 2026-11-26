using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ProductoEdicionTenantTests
{
    private static IUnitOfWork Transaction()
    {
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        return uow.Object;
    }

    [Fact]
    public async Task Update_ProductoNoVisibleEnTenant_Devuelve404SinConsultarVariantesNiEscribir()
    {
        // El repositorio filtrado devuelve null tanto para un id ajeno como
        // para uno inexistente o eliminado; no distinguir estos casos.
        const int id = 91;
        var producto = new Mock<IProductoService>(MockBehavior.Strict);
        producto.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((ProductoDto?)null);
        var variantes = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var controller = new ProductosController(producto.Object, variantes.Object,
            Transaction(), Mock.Of<IImageStorageService>(), Mock.Of<IAuditoriaService>());
        var dto = new UpdateProductoDto { Nombre = "Intento ajeno", Costo = 5, Precio = 10 };

        Assert.IsType<NotFoundObjectResult>(await controller.Update(id, dto));

        producto.Verify(x => x.GetByIdAsync(id), Times.Once);
        producto.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateProductoDto>()), Times.Never);
        producto.VerifyNoOtherCalls();
        variantes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ProductoVisible_VerificaPropiedadAntesDeVariantesYConservaFlujoTransaccional()
    {
        const int id = 11;
        var dto = new UpdateProductoDto { Nombre = "Producto editado", Costo = 5, Precio = 10 };
        var actual = new ProductoDto { Id = id, Nombre = "Producto original" };
        var editado = new ProductoDto { Id = id, Nombre = dto.Nombre };
        var tecnica = new ProductoVarianteDto { Id = 21, ProductoId = id, EsTecnica = true };
        var producto = new Mock<IProductoService>(MockBehavior.Strict);
        var variantes = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var sequence = new MockSequence();
        producto.InSequence(sequence).Setup(x => x.GetByIdAsync(id)).ReturnsAsync(actual);
        variantes.InSequence(sequence).Setup(x => x.GetByProductoIdAsync(id, true))
            .ReturnsAsync(new List<ProductoVarianteDto> { tecnica });
        producto.InSequence(sequence).Setup(x => x.UpdateAsync(id, dto)).ReturnsAsync(editado);
        variantes.InSequence(sequence).Setup(x => x.SincronizarTecnicaAsync(id, It.IsAny<ProductoVarianteFormularioDto>()))
            .ReturnsAsync(tecnica);
        variantes.InSequence(sequence).Setup(x => x.SincronizarTecnicaConProductoAsync(id)).Returns(Task.CompletedTask);
        producto.InSequence(sequence).Setup(x => x.GetByIdAsync(id)).ReturnsAsync(editado);
        var controller = new ProductosController(producto.Object, variantes.Object,
            Transaction(), Mock.Of<IImageStorageService>(), Mock.Of<IAuditoriaService>());

        Assert.IsType<OkObjectResult>(await controller.Update(id, dto));

        producto.Verify(x => x.GetByIdAsync(id), Times.Exactly(2));
        producto.Verify(x => x.UpdateAsync(id, dto), Times.Once);
        variantes.Verify(x => x.GetByProductoIdAsync(id, true), Times.Once);
        variantes.Verify(x => x.SincronizarTecnicaAsync(id, It.IsAny<ProductoVarianteFormularioDto>()), Times.Once);
        variantes.Verify(x => x.SincronizarTecnicaConProductoAsync(id), Times.Once);
        producto.VerifyNoOtherCalls();
        variantes.VerifyNoOtherCalls();
    }
}
