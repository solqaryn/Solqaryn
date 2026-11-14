using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioAjusteTests
{
    [Fact]
    public async Task GenerarAjuste_ConteoAprobado_CreaBorradorYVinculaDiferencias()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDiferencia();
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        ajustes.Setup(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()))
            .ReturnsAsync(new AjusteInventarioDto { Id = 55, NumeroAjuste = "AI-000055" });
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var result = await service.GenerarAjusteAsync(10);

        Assert.NotNull(result);
        Assert.Equal(55, result!.Id);
        Assert.Equal(55, conteo.Detalles.Single().AjusteInventarioId);
        ajustes.Verify(x => x.CreateAsync(It.Is<CreateAjusteInventarioDto>(dto =>
            dto.Detalles.Count == 1 &&
            dto.Detalles[0].ProductoId == 21 &&
            dto.Detalles[0].ProductoVarianteId == 9 &&
            dto.Detalles[0].AlmacenId == 3 &&
            dto.Detalles[0].CantidadObjetivo == 6)), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerarAjuste_Reintento_RetornaAjusteExistenteSinDuplicarlo()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDiferencia();
        conteo.Detalles.Single().VincularAjuste(55);
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        ajustes.Setup(x => x.GetByIdAsync(55)).ReturnsAsync(new AjusteInventarioDto { Id = 55, NumeroAjuste = "AI-000055" });
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var result = await service.GenerarAjusteAsync(10);

        Assert.NotNull(result);
        Assert.Equal(55, result!.Id);
        ajustes.Verify(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerarAjuste_VinculoExistenteSinAjusteDisponible_FallaCerradoSinDuplicar()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDiferencia();
        conteo.Detalles.Single().VincularAjuste(55);
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        ajustes.Setup(x => x.GetByIdAsync(55)).ReturnsAsync((AjusteInventarioDto?)null);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GenerarAjusteAsync(10));

        Assert.Contains("ya no está disponible", error.Message, StringComparison.OrdinalIgnoreCase);
        ajustes.Verify(x => x.GetByIdAsync(55), Times.Once);
        ajustes.Verify(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()), Times.Never);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerarAjuste_SinDiferencias_FallaCerradoSinCrearNiPersistir()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDiferencia(cantidadContada: 8);
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GenerarAjusteAsync(10));

        Assert.Contains("no contiene diferencias", error.Message, StringComparison.OrdinalIgnoreCase);
        ajustes.Verify(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()), Times.Never);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerarAjuste_VinculoParcialInconsistente_FallaCerradoSinDuplicarAjuste()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDosDiferencias();
        conteo.Detalles.First().VincularAjuste(55);
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GenerarAjusteAsync(10));

        Assert.Contains("vínculos de ajuste inconsistentes", error.Message, StringComparison.OrdinalIgnoreCase);
        ajustes.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        ajustes.Verify(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerarAjuste_VinculosADistintosAjustes_FallaCerradoSinConsultarNiCrear()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDosDiferencias();
        var detalles = conteo.Detalles.OrderBy(x => x.Id).ToList();
        detalles[0].VincularAjuste(55);
        detalles[1].VincularAjuste(56);
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GenerarAjusteAsync(10));

        Assert.Contains("vínculos de ajuste inconsistentes", error.Message, StringComparison.OrdinalIgnoreCase);
        ajustes.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        ajustes.Verify(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()), Times.Never);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static Mock<IUnitOfWork> CrearUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
        return unitOfWork;
    }

    private static ConteoInventario CrearAprobadoConDiferencia(int cantidadContada = 6)
    {
        var variante = new ProductoVariante { Id = 9, ProductoId = 21, Sku = "SKU-21-9" };
        var detalle = new ConteoInventarioDetalle
        {
            Id = 4,
            ProductoVarianteId = 9,
            ProductoVariante = variante,
            AlmacenId = 3
        };
        detalle.MaterializarSnapshot(8);
        var conteo = new ConteoInventario
        {
            Id = 10,
            Numero = "CNT-10",
            Tipo = TipoConteoInventario.General,
            AlmacenId = 3,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };
        conteo.Iniciar(7, DateTime.UtcNow.AddMinutes(-3));
        detalle.RegistrarConteo(cantidadContada, 7, DateTime.UtcNow.AddMinutes(-2));
        conteo.Cerrar(7, DateTime.UtcNow.AddMinutes(-1));
        conteo.Aprobar(7, DateTime.UtcNow);
        return conteo;
    }

    private static ConteoInventario CrearAprobadoConDosDiferencias()
    {
        var detalleA = new ConteoInventarioDetalle
        {
            Id = 4,
            ProductoVarianteId = 9,
            ProductoVariante = new ProductoVariante { Id = 9, ProductoId = 21, Sku = "SKU-21-9" },
            AlmacenId = 3
        };
        var detalleB = new ConteoInventarioDetalle
        {
            Id = 5,
            ProductoVarianteId = 10,
            ProductoVariante = new ProductoVariante { Id = 10, ProductoId = 22, Sku = "SKU-22-10" },
            AlmacenId = 3
        };
        detalleA.MaterializarSnapshot(8);
        detalleB.MaterializarSnapshot(12);
        var conteo = new ConteoInventario
        {
            Id = 10,
            Numero = "CNT-10",
            Tipo = TipoConteoInventario.General,
            AlmacenId = 3,
            Detalles = new List<ConteoInventarioDetalle> { detalleA, detalleB }
        };
        conteo.Iniciar(7, DateTime.UtcNow.AddMinutes(-3));
        detalleA.RegistrarConteo(6, 7, DateTime.UtcNow.AddMinutes(-2));
        detalleB.RegistrarConteo(13, 7, DateTime.UtcNow.AddMinutes(-2));
        conteo.Cerrar(7, DateTime.UtcNow.AddMinutes(-1));
        conteo.Aprobar(7, DateTime.UtcNow);
        return conteo;
    }
}
