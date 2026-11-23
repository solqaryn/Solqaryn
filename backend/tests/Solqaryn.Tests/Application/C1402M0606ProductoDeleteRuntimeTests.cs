using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C14-02.M06.06: unit tests of production ProductoService.
/// Not a replacement for signed JWT/MySQL HTTP integration.
/// </summary>
public sealed class C1402M0606ProductoDeleteRuntimeTests
{
    [Fact]
    public async Task DeleteWithZeroStockSoftDeletesProductAndCommercialVariant()
    {
        var variant = new ProductoVariante
        {
            Id = 202, ProductoId = 101, EmpresaId = 7, Cantidad = 0,
            Activo = true, Eliminado = false
        };
        var product = new Producto
        {
            Id = 101, EmpresaId = 7, Nombre = "Producto de prueba",
            Marca = "", Modelo = "", Activo = true, Eliminado = false,
            Variantes = new List<ProductoVariante> { variant }
        };
        var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetByIdAsync(101)).ReturnsAsync(product);
        repo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        var actor = new Mock<ICurrentUserService>();
        actor.SetupGet(x => x.UsuarioId).Returns(77);
        actor.SetupGet(x => x.NombreUsuario).Returns("actor-verificado");
        var audit = new Mock<IAuditoriaService>(MockBehavior.Strict);
        audit.Setup(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Productos, AccionPermiso.EliminarLogico,
            It.IsAny<string>(), 101, "Producto", It.IsAny<object?>(),
            It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<string?>())).Returns(Task.CompletedTask);
        var service = new ProductoService(repo.Object,
            Mock.Of<ICategoriaRepository>(), Mock.Of<IImageStorageService>(),
            actor.Object, audit.Object);

        Assert.True(await service.DeleteAsync(101));
        Assert.True(product.Eliminado);
        Assert.False(product.Activo);
        Assert.True(variant.Eliminado);
        Assert.False(variant.Activo);
        Assert.Equal(77, variant.EliminadoPorUsuarioId);
        Assert.Equal(product.FechaEliminacion, variant.FechaEliminacion);
        Assert.Equal("actor-verificado", variant.ActualizadoPorNombreUsuario);
        repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        audit.VerifyAll();
    }

    [Fact]
    public async Task PositiveStockRejectsDeleteBeforeAnyDatabaseMutationOrAudit()
    {
        var variant = new ProductoVariante
        {
            Id = 204, ProductoId = 103, EmpresaId = 7, Cantidad = 3,
            Activo = true, Eliminado = false
        };
        var product = new Producto
        {
            Id = 103, EmpresaId = 7, Nombre = "Producto con existencias",
            Marca = "", Modelo = "", Activo = true, Eliminado = false,
            Variantes = new List<ProductoVariante> { variant }
        };
        var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetByIdAsync(103)).ReturnsAsync(product);
        var service = new ProductoService(repo.Object,
            Mock.Of<ICategoriaRepository>(), Mock.Of<IImageStorageService>(),
            Mock.Of<ICurrentUserService>(), Mock.Of<IAuditoriaService>());

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.DeleteAsync(103));
        Assert.False(product.Eliminado);
        Assert.True(product.Activo);
        Assert.False(variant.Eliminado);
        Assert.True(variant.Activo);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
