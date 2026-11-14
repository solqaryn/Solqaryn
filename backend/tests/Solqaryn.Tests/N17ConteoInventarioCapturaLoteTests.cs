using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioCapturaLoteTests
{
    [Fact]
    public async Task CapturarLote_DosLineasValidas_PersisteUnaSolaVez()
    {
        var (service, repository, conteo) = CrearServicio();

        var resultado = await service.CapturarLoteAsync(18, new CapturarConteoInventarioLoteDto
        {
            Lineas = new List<CapturaConteoInventarioLineaDto>
            {
                new() { DetalleId = 4, CantidadContada = 6 },
                new() { DetalleId = 5, CantidadContada = 13 }
            }
        });

        Assert.NotNull(resultado);
        Assert.Equal(6, conteo.Detalles.Single(x => x.Id == 4).CantidadContada);
        Assert.Equal(13, conteo.Detalles.Single(x => x.Id == 5).CantidadContada);
        repository.Verify(x => x.Update(conteo), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CapturarLote_ReintentoMismosValores_EsIdempotenteSinPersistir()
    {
        var (service, repository, conteo) = CrearServicio();
        conteo.Detalles.Single(x => x.Id == 4).RegistrarConteo(6, 7, DateTime.UtcNow.AddMinutes(-1));
        conteo.Detalles.Single(x => x.Id == 5).RegistrarConteo(13, 7, DateTime.UtcNow.AddMinutes(-1));

        var resultado = await service.CapturarLoteAsync(18, new CapturarConteoInventarioLoteDto
        {
            Lineas = new List<CapturaConteoInventarioLineaDto>
            {
                new() { DetalleId = 5, CantidadContada = 13 },
                new() { DetalleId = 4, CantidadContada = 6 }
            }
        });

        Assert.NotNull(resultado);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CapturarLote_ContieneLineaAjena_FallaAntesDeMutarCualquierDetalle()
    {
        var (service, repository, conteo) = CrearServicio();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CapturarLoteAsync(18, new CapturarConteoInventarioLoteDto
        {
            Lineas = new List<CapturaConteoInventarioLineaDto>
            {
                new() { DetalleId = 4, CantidadContada = 6 },
                new() { DetalleId = 999, CantidadContada = 1 }
            }
        }));

        Assert.Contains("no pertenecen", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(conteo.Detalles.Single(x => x.Id == 4).CantidadContada);
        Assert.Null(conteo.Detalles.Single(x => x.Id == 5).CantidadContada);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CapturarLote_DetalleDuplicado_FallaCerradoAntesDeTomarElConteo()
    {
        var (service, repository, _) = CrearServicio();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CapturarLoteAsync(18, new CapturarConteoInventarioLoteDto
        {
            Lineas = new List<CapturaConteoInventarioLineaDto>
            {
                new() { DetalleId = 4, CantidadContada = 6 },
                new() { DetalleId = 4, CantidadContada = 7 }
            }
        }));

        Assert.Contains("duplicados", error.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(x => x.GetByIdForUpdateAsync(It.IsAny<int>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static (ConteoInventarioService Service, Mock<IConteoInventarioRepository> Repository, ConteoInventario Conteo) CrearServicio()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-n17");
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
        var conteo = CrearConteoEnProceso();
        repository.Setup(x => x.GetByIdForUpdateAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);
        return (service, repository, conteo);
    }

    private static ConteoInventario CrearConteoEnProceso()
    {
        var detalleA = new ConteoInventarioDetalle { Id = 4, ProductoVarianteId = 9, AlmacenId = 3 };
        var detalleB = new ConteoInventarioDetalle { Id = 5, ProductoVarianteId = 10, AlmacenId = 3 };
        detalleA.MaterializarSnapshot(8);
        detalleB.MaterializarSnapshot(12);
        var conteo = new ConteoInventario
        {
            Id = 18,
            Numero = "CNT-LOTE-18",
            Tipo = TipoConteoInventario.General,
            AlmacenId = 3,
            Detalles = new List<ConteoInventarioDetalle> { detalleA, detalleB }
        };
        conteo.Iniciar(7, DateTime.UtcNow.AddMinutes(-2));
        return conteo;
    }
}
