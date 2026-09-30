using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioBatchAtomicityRegressionTests
{
    [Fact]
    public async Task CapturarLote_LineaAjena_NoMutaLineasValidasNiPersisteParcialmente()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        var detalleValido = CrearDetalle(41, 501, 3, 12);
        var conteo = CrearConteo(18, 3, detalleValido);

        repository.Setup(x => x.GetByIdForUpdateAsync(18)).ReturnsAsync(conteo);
        var service = CrearService(repository, existencias, currentUser, unitOfWork);

        var lote = new CapturarConteoInventarioLoteDto
        {
            Lineas = new List<CapturaConteoInventarioLineaDto>
            {
                new() { DetalleId = 41, CantidadContada = 10 },
                new() { DetalleId = 999, CantidadContada = 5 }
            }
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CapturarLoteAsync(18, lote));

        Assert.Contains("no pertenecen al conteo", error.Message, StringComparison.OrdinalIgnoreCase);
        AssertSinCaptura(detalleValido);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CapturarLote_CantidadNegativaEnLineaPosterior_NoMutaLineaPreviaNiPersiste()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        var primera = CrearDetalle(41, 501, 3, 12);
        var segunda = CrearDetalle(42, 502, 3, 8);
        var conteo = CrearConteo(19, 3, primera, segunda);

        repository.Setup(x => x.GetByIdForUpdateAsync(19)).ReturnsAsync(conteo);
        var service = CrearService(repository, existencias, currentUser, unitOfWork);

        var lote = new CapturarConteoInventarioLoteDto
        {
            Lineas = new List<CapturaConteoInventarioLineaDto>
            {
                new() { DetalleId = 41, CantidadContada = 10 },
                new() { DetalleId = 42, CantidadContada = -1 }
            }
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CapturarLoteAsync(19, lote));

        Assert.Contains("negativas", error.Message, StringComparison.OrdinalIgnoreCase);
        AssertSinCaptura(primera);
        AssertSinCaptura(segunda);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static ConteoInventarioDetalle CrearDetalle(int id, int varianteId, int almacenId, int stockEsperado)
    {
        var detalle = new ConteoInventarioDetalle
        {
            Id = id,
            ProductoVarianteId = varianteId,
            AlmacenId = almacenId
        };
        detalle.MaterializarSnapshot(stockEsperado);
        return detalle;
    }

    private static ConteoInventario CrearConteo(int id, int almacenId, params ConteoInventarioDetalle[] detalles)
    {
        var conteo = new ConteoInventario
        {
            Id = id,
            Numero = $"CNT-ATOMICO-{id}",
            Tipo = TipoConteoInventario.General,
            AlmacenId = almacenId,
            Detalles = detalles.ToList()
        };
        conteo.Iniciar(7, DateTime.UtcNow);
        return conteo;
    }

    private static ConteoInventarioService CrearService(
        Mock<IConteoInventarioRepository> repository,
        Mock<IExistenciaVarianteRepository> existencias,
        Mock<ICurrentUserService> currentUser,
        Mock<IUnitOfWork> unitOfWork) =>
        new(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

    private static void AssertSinCaptura(ConteoInventarioDetalle detalle)
    {
        Assert.Null(detalle.CantidadContada);
        Assert.Null(detalle.FechaConteo);
        Assert.Null(detalle.ContadoPorUsuarioId);
    }
}
