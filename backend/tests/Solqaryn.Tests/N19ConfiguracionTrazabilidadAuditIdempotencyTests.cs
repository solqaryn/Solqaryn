using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19ConfiguracionTrazabilidadAuditIdempotencyTests
{
    [Fact]
    public async Task Configuracion_identica_no_persiste_ni_duplica_auditoria()
    {
        var variante = new ProductoVariante { Id = 31, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, true, 15);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(31)).ReturnsAsync(variante);

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-config-idempotente");

        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        var auditoria = new Mock<IAuditoriaService>();
        var service = new TrazabilidadInventarioService(
            repo.Object, variantes.Object, current.Object, unit.Object, auditoria.Object);

        var resultado = await service.ConfigurarAsync(31, new ConfigurarTrazabilidadVarianteRequest
        {
            ControlaLote = true,
            ControlaNumeroSerie = true,
            ControlaFechaVencimiento = true,
            DiasAlertaVencimiento = 15
        });

        Assert.True(resultado.ControlaLote);
        Assert.True(resultado.ControlaNumeroSerie);
        Assert.True(resultado.ControlaFechaVencimiento);
        Assert.Equal(15, resultado.DiasAlertaVencimiento);
        variantes.Verify(x => x.Update(It.IsAny<ProductoVariante>()), Times.Never);
        variantes.Verify(x => x.SaveChangesAsync(), Times.Never);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
}
