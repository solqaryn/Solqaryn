using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19SerieAuditPrivacyTests
{
    [Fact]
    public async Task Crear_serie_audita_sin_persistir_numero_serie_en_payload()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(false, true, false);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByNumeroAsync("SN-PRIVADO-001", false)).ReturnsAsync((SerieInventario?)null);
        repo.Setup(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>())).ReturnsAsync(true);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-serie-audit");
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

        await service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = " sn-privado-001 "
        });

        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            It.IsAny<int?>(),
            "SerieInventario",
            null,
            It.Is<object?>(payload => payload != null && payload.GetType().GetProperty("NumeroSerie") == null),
            null,
            "Exito",
            null), Times.Once);
    }
}
