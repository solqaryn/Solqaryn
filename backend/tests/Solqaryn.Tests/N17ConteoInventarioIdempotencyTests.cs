using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteoInventarioIdempotencyTests
{
    [Fact]
    public async Task Iniciar_ReintentoEnProceso_NoReescribeDocumento()
    {
        var (service, repository, _, _) = CrearServicio();
        var conteo = CrearEnProceso();
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);

        var result = await service.IniciarAsync(10);

        Assert.NotNull(result);
        Assert.Equal(EstadoConteoInventario.EnProceso, result!.Estado);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Capturar_MismaCantidad_NoReescribeTimestampNiPersistencia()
    {
        var (service, repository, _, _) = CrearServicio();
        var conteo = CrearEnProceso();
        var detalle = conteo.Detalles.Single();
        detalle.Id = 4;
        detalle.RegistrarConteo(6, 7, new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));
        var fechaOriginal = detalle.FechaConteo;
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);

        var result = await service.CapturarDetalleAsync(10, 4, new CapturarConteoInventarioDetalleDto { CantidadContada = 6 });

        Assert.NotNull(result);
        Assert.Equal(fechaOriginal, detalle.FechaConteo);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static (ConteoInventarioService Service, Mock<IConteoInventarioRepository> Repository, Mock<IExistenciaVarianteRepository> Existencias, Mock<ICurrentUserService> User) CrearServicio()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var user = new Mock<ICurrentUserService>();
        user.SetupGet(x => x.UsuarioId).Returns(7);
        user.SetupGet(x => x.NombreUsuario).Returns("qa-n17");
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
        return (new ConteoInventarioService(repository.Object, existencias.Object, user.Object, unitOfWork.Object), repository, existencias, user);
    }

    private static ConteoInventario CrearEnProceso()
    {
        var detalle = new ConteoInventarioDetalle { ProductoVarianteId = 9, AlmacenId = 3 };
        detalle.MaterializarSnapshot(8);
        var conteo = new ConteoInventario
        {
            Id = 10,
            Numero = "CNT-IDEMP-10",
            Tipo = TipoConteoInventario.General,
            AlmacenId = 3,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };
        conteo.Iniciar(7, new DateTime(2026, 8, 16, 11, 0, 0, DateTimeKind.Utc));
        return conteo;
    }
}
