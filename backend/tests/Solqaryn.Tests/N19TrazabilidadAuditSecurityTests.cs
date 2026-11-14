using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19TrazabilidadAuditSecurityTests
{
    [Fact]
    public async Task Crear_lote_registra_auditoria_estricta_sin_exponer_codigo_trazable()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, false, true, 30);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByCodigoAsync(11, "LOTE-AUD", false)).ReturnsAsync((LoteInventario?)null);
        repo.Setup(x => x.TryAddLoteAsync(It.IsAny<LoteInventario>())).ReturnsAsync(true);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var current = CurrentUser();
        var unit = UnitOfWork();
        var auditoria = AuditoriaExitosa();
        var service = new TrazabilidadInventarioService(repo.Object, variantes.Object, current.Object, unit.Object, auditoria.Object);

        await service.CrearLoteAsync(new CrearLoteInventarioRequest
        {
            ProductoVarianteId = 11,
            Codigo = " lote-aud ",
            FechaFabricacion = new DateTime(2026, 8, 1),
            FechaVencimiento = new DateTime(2027, 8, 1)
        });

        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Crear,
            It.Is<string>(d => d.Contains("Lote", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<int?>(), "LoteInventario", null,
            It.Is<object?>(payload => NoExponePropiedad(payload, "Codigo")),
            null, "Exito", null), Times.Once);
    }

    [Fact]
    public async Task Crear_lote_propaga_fallo_de_auditoria_estricta_dentro_de_transaccion()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, false, false);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByCodigoAsync(11, "LOTE-FAIL", false)).ReturnsAsync((LoteInventario?)null);
        repo.Setup(x => x.TryAddLoteAsync(It.IsAny<LoteInventario>())).ReturnsAsync(true);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var current = CurrentUser();
        var unit = UnitOfWork();
        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("audit-store-down"));
        var service = new TrazabilidadInventarioService(repo.Object, variantes.Object, current.Object, unit.Object, auditoria.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CrearLoteAsync(new CrearLoteInventarioRequest
        {
            ProductoVarianteId = 11,
            Codigo = "LOTE-FAIL"
        }));

        Assert.Equal("audit-store-down", ex.Message);
        unit.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
    }

    [Fact]
    public async Task Desactivar_lote_audita_anulacion_y_repeticion_idempotente_no_duplica_evento()
    {
        var lote = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("LOTE-SENSIBLE", null, null, false);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, true)).ReturnsAsync(lote);
        repo.Setup(x => x.TieneSeriesActivasEnLoteAsync(8)).ReturnsAsync(false);
        repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        var auditoria = AuditoriaExitosa();
        var service = new TrazabilidadInventarioService(
            repo.Object, new Mock<IProductoVarianteRepository>().Object,
            CurrentUser().Object, UnitOfWork().Object, auditoria.Object);

        var primero = await service.DesactivarLoteAsync(8);
        var segundo = await service.DesactivarLoteAsync(8);

        Assert.False(primero.Activo);
        Assert.False(segundo.Activo);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Anular,
            It.IsAny<string>(),
            8,
            "LoteInventario",
            null,
            It.Is<object?>(payload => NoExponePropiedad(payload, "Codigo")),
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task Baja_idempotente_no_duplica_auditoria()
    {
        var serie = new SerieInventario { Id = 5, ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-SENSIBLE");
        serie.DarDeBaja();
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByIdAsync(5, true)).ReturnsAsync(serie);
        var auditoria = new Mock<IAuditoriaService>();
        var service = new TrazabilidadInventarioService(
            repo.Object, new Mock<IProductoVarianteRepository>().Object,
            CurrentUser().Object, UnitOfWork().Object, auditoria.Object);

        var resultado = await service.DarDeBajaSerieAsync(5);

        Assert.Equal(EstadoSerieInventario.Baja, resultado.Estado);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    private static Mock<ICurrentUserService> CurrentUser()
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-audit-test");
        return current;
    }

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        return unit;
    }

    private static Mock<IAuditoriaService> AuditoriaExitosa()
    {
        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return auditoria;
    }

    private static bool NoExponePropiedad(object? payload, string nombre) =>
        payload is not null && payload.GetType().GetProperty(nombre) is null;
}
