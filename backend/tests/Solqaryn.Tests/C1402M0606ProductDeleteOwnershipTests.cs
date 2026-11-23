using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C14-02.M06.06 bounded API-controller regression. These tests exercise the
/// real product controller action, with mocked tenant-scoped services.
/// They are not proof of real SQL, JWT filters, or global BOLA/IDOR safety.
/// </summary>
public sealed class C1402M0606ProductDeleteOwnershipTests
{
    private const int ProductId = 42;

    private static Mock<IUnitOfWork> ExecutingTransaction()
    {
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> body) => body());
        return unit;
    }

    private static ProductosController Controller(
        Mock<IProductoService> product,
        Mock<IProductoVarianteService> variants,
        Mock<IUnitOfWork> unit) =>
        new(product.Object, variants.Object, unit.Object,
            new Mock<IImageStorageService>().Object,
            new Mock<IAuditoriaService>().Object);

    [Fact]
    public async Task Delete_MissingOrForeignTenantProduct_DoesNotTouchAnyVariantOrDelete()
    {
        var product = new Mock<IProductoService>(MockBehavior.Strict);
        var variants = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var unit = ExecutingTransaction();
        product.Setup(x => x.LockForDeleteAsync(ProductId))
            .ReturnsAsync(false);

        var result = await Controller(product, variants, unit).Delete(ProductId);

        Assert.IsType<NotFoundObjectResult>(result);
        product.Verify(x => x.LockForDeleteAsync(ProductId), Times.Once);
        product.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        product.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        variants.Verify(x => x.EliminarTecnicaConProductoAsync(It.IsAny<int>()), Times.Never);
        unit.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
    }

    [Fact]
    public async Task Delete_OwnTenantProduct_PerformsTechnicalCleanupBeforeProductDeletion()
    {
        var order = new List<string>();
        var product = new Mock<IProductoService>(MockBehavior.Strict);
        var variants = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var unit = ExecutingTransaction();
        product.Setup(x => x.LockForDeleteAsync(ProductId))
            .ReturnsAsync(true);
        product.Setup(x => x.GetByIdAsync(ProductId))
            .ReturnsAsync(new ProductoDto { Id = ProductId });
        variants.Setup(x => x.EliminarTecnicaConProductoAsync(ProductId))
            .Returns(() => { order.Add("cleanup"); return Task.CompletedTask; });
        product.Setup(x => x.DeleteAsync(ProductId))
            .ReturnsAsync(() => { order.Add("delete"); return true; });

        var result = await Controller(product, variants, unit).Delete(ProductId);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(new[] { "cleanup", "delete" }, order);
        product.Verify(x => x.GetByIdAsync(ProductId), Times.Once);
        variants.Verify(x => x.EliminarTecnicaConProductoAsync(ProductId), Times.Once);
        product.Verify(x => x.DeleteAsync(ProductId), Times.Once);
    }

    [Fact]
    public async Task Delete_TechnicalVariantCleanupFailure_NeverRunsProductDeletion()
    {
        var product = new Mock<IProductoService>(MockBehavior.Strict);
        var variants = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var unit = ExecutingTransaction();
        product.Setup(x => x.LockForDeleteAsync(ProductId))
            .ReturnsAsync(true);
        product.Setup(x => x.GetByIdAsync(ProductId))
            .ReturnsAsync(new ProductoDto { Id = ProductId });
        variants.Setup(x => x.EliminarTecnicaConProductoAsync(ProductId))
            .ThrowsAsync(new InvalidOperationException("isolated cleanup failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Controller(product, variants, unit).Delete(ProductId));

        product.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ProductMutationReturnsFalse_AbortsOuterTransactionAfterVariantCleanup()
    {
        var product = new Mock<IProductoService>(MockBehavior.Strict);
        var variants = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var cleanupCalled = false;
        var transactionCompleted = false;
        var transactionAborted = false;
        product.Setup(x => x.LockForDeleteAsync(ProductId))
            .ReturnsAsync(true);
        product.Setup(x => x.GetByIdAsync(ProductId))
            .ReturnsAsync(new ProductoDto { Id = ProductId });
        variants.Setup(x => x.EliminarTecnicaConProductoAsync(ProductId))
            .Returns(() => { cleanupCalled = true; return Task.CompletedTask; });
        product.Setup(x => x.DeleteAsync(ProductId)).ReturnsAsync(false);
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> operation) =>
            {
                try
                {
                    await operation();
                    transactionCompleted = true;
                }
                catch
                {
                    transactionAborted = true;
                    throw;
                }
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Controller(product, variants, unit).Delete(ProductId));
        Assert.Equal("PRODUCT_DELETE_ATOMICITY_ROLLBACK_REQUIRED", exception.Message);
        Assert.True(cleanupCalled);
        Assert.True(transactionAborted);
        Assert.False(transactionCompleted);
        product.Verify(x => x.GetByIdAsync(ProductId), Times.Once);
        product.Verify(x => x.DeleteAsync(ProductId), Times.Once);
        variants.Verify(x => x.EliminarTecnicaConProductoAsync(ProductId), Times.Once);
    }

    [Fact]
    public async Task Delete_LocksCurrentTenantProductBeforeOrdinaryReadOrVariantCleanup()
    {
        var order = new List<string>();
        var product = new Mock<IProductoService>(MockBehavior.Strict);
        var variants = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var unit = ExecutingTransaction();
        product.Setup(x => x.LockForDeleteAsync(ProductId)).ReturnsAsync(() =>
        {
            order.Add("for-update");
            return true;
        });
        product.Setup(x => x.GetByIdAsync(ProductId)).ReturnsAsync(() =>
        {
            order.Add("read-variants");
            return new ProductoDto { Id = ProductId };
        });
        variants.Setup(x => x.EliminarTecnicaConProductoAsync(ProductId))
            .Returns(() => { order.Add("cleanup"); return Task.CompletedTask; });
        product.Setup(x => x.DeleteAsync(ProductId))
            .ReturnsAsync(() => { order.Add("delete"); return true; });

        Assert.IsType<OkObjectResult>(
            await Controller(product, variants, unit).Delete(ProductId));
        Assert.Equal(new[] { "for-update", "read-variants", "cleanup", "delete" }, order);
    }
}
