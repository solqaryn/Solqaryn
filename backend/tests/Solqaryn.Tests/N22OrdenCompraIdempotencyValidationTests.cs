using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N22OrdenCompraIdempotencyValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("clave con espacios")]
    [InlineData("clave/unsafe")]
    public async Task Create_rechaza_clave_idempotencia_invalida_antes_de_tocar_dependencias(string key)
    {
        var fixture = CrearFixtureEstricto();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.CreateAsync(new CreateOrdenCompraDto(), key));

        fixture.Repository.VerifyNoOtherCalls();
        fixture.Proveedores.VerifyNoOtherCalls();
        fixture.Productos.VerifyNoOtherCalls();
        fixture.Solicitudes.VerifyNoOtherCalls();
        fixture.CurrentUser.VerifyNoOtherCalls();
        fixture.UnitOfWork.VerifyNoOtherCalls();
        fixture.Auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_rechaza_clave_idempotencia_mayor_a_128_caracteres_antes_de_tocar_dependencias()
    {
        var fixture = CrearFixtureEstricto();
        var key = new string('a', 129);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.CreateAsync(new CreateOrdenCompraDto(), key));

        Assert.Contains("128", ex.Message, StringComparison.Ordinal);
        fixture.Repository.VerifyNoOtherCalls();
        fixture.UnitOfWork.VerifyNoOtherCalls();
        fixture.Auditoria.VerifyNoOtherCalls();
    }

    private static Fixture CrearFixtureEstricto()
    {
        var repository = new Mock<IOrdenCompraRepository>(MockBehavior.Strict);
        var proveedores = new Mock<IProveedorRepository>(MockBehavior.Strict);
        var productos = new Mock<IProductoRepository>(MockBehavior.Strict);
        var solicitudes = new Mock<ISolicitudCompraRepository>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var service = new OrdenCompraService(
            repository.Object,
            proveedores.Object,
            productos.Object,
            solicitudes.Object,
            currentUser.Object,
            unitOfWork.Object,
            auditoria.Object);

        return new Fixture(service, repository, proveedores, productos, solicitudes, currentUser, unitOfWork, auditoria);
    }

    private sealed record Fixture(
        OrdenCompraService Service,
        Mock<IOrdenCompraRepository> Repository,
        Mock<IProveedorRepository> Proveedores,
        Mock<IProductoRepository> Productos,
        Mock<ISolicitudCompraRepository> Solicitudes,
        Mock<ICurrentUserService> CurrentUser,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IAuditoriaService> Auditoria);
}
