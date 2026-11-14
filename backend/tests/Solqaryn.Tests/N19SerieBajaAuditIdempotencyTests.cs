using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19SerieBajaAuditIdempotencyTests
{
    [Fact]
    public async Task Dar_de_baja_dos_veces_no_duplica_auditoria_ni_persistencia()
    {
        var serie = new SerieInventario { Id = 21, ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-IDEMP-001");

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByIdAsync(21, true)).ReturnsAsync(serie);
        repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var variantes = new Mock<IProductoVarianteRepository>();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-serie-baja-idempotente");

        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var service = new TrazabilidadInventarioService(
            repo.Object, variantes.Object, current.Object, unit.Object, auditoria.Object);

        var primera = await service.DarDeBajaSerieAsync(21);
        var segunda = await service.DarDeBajaSerieAsync(21);

        Assert.Equal(EstadoSerieInventario.Baja, primera.Estado);
        Assert.Equal(EstadoSerieInventario.Baja, segunda.Estado);
        repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Anular,
            It.IsAny<string>(),
            21,
            "SerieInventario",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            null,
            "Exito",
            null), Times.Once);
    }
}
