using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioServiceTests
{
    [Fact]
    public async Task GetById_ConteoCiegoEnBorrador_NoExponeStockEsperado()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var conteo = CrearConteoCiegoBorrador();
        repository.Setup(x => x.GetByIdAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

        var dto = await service.GetByIdAsync(18);

        Assert.NotNull(dto);
        Assert.True(dto!.EsCiego);
        Assert.Equal(EstadoConteoInventario.Borrador, dto.Estado);
        Assert.Single(dto.Detalles);
        Assert.Null(dto.Detalles[0].StockEsperado);
    }

    [Fact]
    public async Task GetById_ConteoCiegoEnProceso_NoExponeReferenciaNiDiferenciaInferible()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var conteo = CrearConteoCiegoEnProceso();
        conteo.Detalles.Single().RegistrarConteo(6, 7, DateTime.UtcNow);
        repository.Setup(x => x.GetByIdAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

        var dto = await service.GetByIdAsync(18);

        Assert.NotNull(dto);
        Assert.True(dto!.EsCiego);
        Assert.Equal(EstadoConteoInventario.EnProceso, dto.Estado);
        Assert.Single(dto.Detalles);
        Assert.Equal(6, dto.Detalles[0].CantidadContada.GetValueOrDefault());
        Assert.Null(dto.Detalles[0].StockEsperado);
        Assert.Null(dto.Detalles[0].Diferencia);
        Assert.Equal(0, dto.CantidadConDiferencia);
        Assert.Equal(0, dto.DiferenciaNeta);
    }

    [Fact]
    public async Task GetById_ConteoCiegoCerrado_RevelaReferenciaYDiferencia()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var conteo = CrearConteoCiegoEnProceso();
        conteo.Detalles.Single().RegistrarConteo(6, 7, DateTime.UtcNow);
        conteo.Cerrar(7, DateTime.UtcNow);
        repository.Setup(x => x.GetByIdAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

        var dto = await service.GetByIdAsync(18);

        Assert.NotNull(dto);
        Assert.True(dto!.EsCiego);
        Assert.Equal(EstadoConteoInventario.Cerrado, dto.Estado);
        Assert.Single(dto.Detalles);
        Assert.Equal(14, dto.Detalles[0].StockEsperado.GetValueOrDefault());
        Assert.Equal(6, dto.Detalles[0].CantidadContada.GetValueOrDefault());
        Assert.Equal(-8, dto.Detalles[0].Diferencia.GetValueOrDefault());
        Assert.Equal(1, dto.CantidadConDiferencia);
        Assert.Equal(-8, dto.DiferenciaNeta);
    }

    [Fact]
    public async Task GetPaged_ConteoCiegoEnProceso_NoExponeReferenciaNiDiferencia()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var conteo = CrearConteoCiegoEnProceso();
        conteo.Detalles.Single().RegistrarConteo(6, 7, DateTime.UtcNow);
        repository.Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .ReturnsAsync((new List<ConteoInventario> { conteo }, 1));
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

        var pagina = await service.GetPagedAsync(new ConteoInventarioQueryDto());

        var dto = Assert.Single(pagina.Items);
        var detalle = Assert.Single(dto.Detalles);
        Assert.Equal(EstadoConteoInventario.EnProceso, dto.Estado);
        Assert.Equal(6, detalle.CantidadContada.GetValueOrDefault());
        Assert.Null(detalle.StockEsperado);
        Assert.Null(detalle.Diferencia);
        Assert.Equal(0, dto.CantidadConDiferencia);
        Assert.Equal(0, dto.DiferenciaNeta);
    }

    [Fact]
    public async Task GetPaged_NormalizaLimitesAntesDeConsultarRepositorio()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .ReturnsAsync((new List<ConteoInventario>(), 0));
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);
        var query = new ConteoInventarioQueryDto { Page = 0, PageSize = 999 };

        var page = await service.GetPagedAsync(query);

        Assert.Equal(1, query.Page);
        Assert.Equal(100, query.PageSize);
        Assert.Equal(1, page.Page);
        Assert.Equal(100, page.PageSize);
        repository.Verify(x => x.GetPagedAsync(It.Is<ConteoInventarioQueryDto>(q => q.Page == 1 && q.PageSize == 100)), Times.Once);
    }

    [Fact]
    public async Task CapturarDetalle_MismaCantidad_EsIdempotenteSinPersistir()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        var unitOfWork = CrearUnitOfWork();
        var conteo = CrearConteoCiegoEnProceso();
        var detalle = conteo.Detalles.Single();
        detalle.RegistrarConteo(6, 7, DateTime.UtcNow.AddMinutes(-1));
        repository.Setup(x => x.GetByIdForUpdateAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

        var resultado = await service.CapturarDetalleAsync(
            18,
            detalle.Id,
            new CapturarConteoInventarioDetalleDto { CantidadContada = 6 });

        Assert.NotNull(resultado);
        Assert.Equal(6, resultado!.Detalles.Single().CantidadContada.GetValueOrDefault());
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Iniciar_ReintentoEnProceso_EsIdempotenteSinPersistir()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        var unitOfWork = CrearUnitOfWork();
        var conteo = CrearConteoCiegoEnProceso();
        repository.Setup(x => x.GetByIdForUpdateAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

        var resultado = await service.IniciarAsync(18);

        Assert.NotNull(resultado);
        Assert.Equal(EstadoConteoInventario.EnProceso, resultado!.Estado);
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

    private static ConteoInventario CrearConteoCiegoBorrador()
    {
        var detalle = new ConteoInventarioDetalle
        {
            Id = 4,
            ProductoVarianteId = 9,
            AlmacenId = 3
        };
        detalle.MaterializarSnapshot(14);
        return new ConteoInventario
        {
            Id = 18,
            Numero = "CNT-CIEGO-18",
            Tipo = TipoConteoInventario.Ciego,
            AlmacenId = 3,
            EsCiego = true,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };
    }

    private static ConteoInventario CrearConteoCiegoEnProceso()
    {
        var conteo = CrearConteoCiegoBorrador();
        conteo.Iniciar(7, DateTime.UtcNow);
        return conteo;
    }
}