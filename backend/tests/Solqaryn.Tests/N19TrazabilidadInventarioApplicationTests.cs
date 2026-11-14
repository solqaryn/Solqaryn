using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19TrazabilidadInventarioApplicationTests
{
    [Fact]
    public async Task Configurar_rechaza_habilitar_dimension_nueva_si_hay_stock_existente()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.TieneStockFisicoAsync(11)).ReturnsAsync(true);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConfigurarAsync(11, new ConfigurarTrazabilidadVarianteRequest
        {
            ControlaLote = true
        }));

        Assert.Contains("adopción/apertura", ex.Message);
        variantes.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Configurar_misma_politica_es_idempotente_y_no_revalida_stock_ni_persiste()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, true, 45);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var resultado = await service.ConfigurarAsync(11, new ConfigurarTrazabilidadVarianteRequest
        {
            ControlaLote = true,
            ControlaNumeroSerie = true,
            ControlaFechaVencimiento = true,
            DiasAlertaVencimiento = 45
        });

        Assert.True(resultado.ControlaLote);
        Assert.True(resultado.ControlaNumeroSerie);
        Assert.True(resultado.ControlaFechaVencimiento);
        Assert.Equal(45, resultado.DiasAlertaVencimiento);
        repo.Verify(x => x.TieneStockFisicoAsync(It.IsAny<int>()), Times.Never);
        repo.Verify(x => x.TieneLotesActivosAsync(It.IsAny<int>()), Times.Never);
        repo.Verify(x => x.TieneSeriesActivasAsync(It.IsAny<int>()), Times.Never);
        variantes.Verify(x => x.Update(It.IsAny<ProductoVariante>()), Times.Never);
        variantes.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Crear_lote_es_idempotente_por_variante_y_codigo_si_payload_es_equivalente()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, false, true, 30);
        var existente = new LoteInventario { Id = 7, ProductoVarianteId = 11 };
        existente.ConfigurarIdentidad("lote-001", new DateTime(2026, 8, 1), new DateTime(2027, 8, 1), true);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByCodigoAsync(11, "LOTE-001", false)).ReturnsAsync(existente);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var resultado = await service.CrearLoteAsync(new CrearLoteInventarioRequest
        {
            ProductoVarianteId = 11,
            Codigo = " lote-001 ",
            FechaFabricacion = new DateTime(2026, 8, 1),
            FechaVencimiento = new DateTime(2027, 8, 1)
        });

        Assert.Equal(7, resultado.Id);
        repo.Verify(x => x.TryAddLoteAsync(It.IsAny<LoteInventario>()), Times.Never);
    }

    [Fact]
    public async Task Crear_lote_rechaza_misma_clave_idempotente_con_payload_diferente()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, false, true, 30);
        var existente = new LoteInventario { Id = 7, ProductoVarianteId = 11 };
        existente.ConfigurarIdentidad("LOTE-001", new DateTime(2026, 8, 1), new DateTime(2027, 8, 1), true);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByCodigoAsync(11, "LOTE-001", false)).ReturnsAsync(existente);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CrearLoteAsync(new CrearLoteInventarioRequest
        {
            ProductoVarianteId = 11,
            Codigo = "LOTE-001",
            FechaFabricacion = new DateTime(2026, 8, 2),
            FechaVencimiento = new DateTime(2027, 8, 1)
        }));

        Assert.Contains("datos diferentes", ex.Message, StringComparison.OrdinalIgnoreCase);
        repo.Verify(x => x.TryAddLoteAsync(It.IsAny<LoteInventario>()), Times.Never);
    }

    [Fact]
    public async Task Consultar_lotes_rechaza_rango_de_vencimiento_invertido_sin_tocar_repositorio()
    {
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        var service = CrearService(repo, new Mock<IProductoVarianteRepository>());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetLotesAsync(new LoteInventarioQueryDto
        {
            VenceDesde = new DateTime(2027, 8, 2),
            VenceHasta = new DateTime(2027, 8, 1)
        }));

        Assert.Contains("fecha inicial", ex.Message, StringComparison.OrdinalIgnoreCase);
        repo.Verify(x => x.GetLotesPagedAsync(It.IsAny<LoteInventarioQueryDto>()), Times.Never);
    }

    [Fact]
    public async Task Consultar_series_normaliza_paginacion_sin_deformar_total()
    {
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSeriesPagedAsync(It.IsAny<SerieInventarioQueryDto>()))
            .ReturnsAsync((Array.Empty<SerieInventario>(), 245));
        var service = CrearService(repo, new Mock<IProductoVarianteRepository>());

        var resultado = await service.GetSeriesAsync(new SerieInventarioQueryDto
        {
            Page = 0,
            PageSize = 999
        });

        Assert.Equal(1, resultado.Page);
        Assert.Equal(200, resultado.PageSize);
        Assert.Equal(245, resultado.TotalCount);
    }

    [Fact]
    public async Task Crear_serie_con_lote_opcional_respeta_contrato_B()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, false);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByNumeroAsync("SN-001", false)).ReturnsAsync((SerieInventario?)null);
        repo.Setup(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>())).ReturnsAsync(true);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var resultado = await service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = " sn-001 ",
            LoteInventarioId = null
        });

        Assert.Equal("SN-001", resultado.NumeroSerie);
        Assert.Null(resultado.LoteInventarioId);
        repo.Verify(x => x.TryAddSerieAsync(It.Is<SerieInventario>(s => s.ProductoVarianteId == 11 && s.LoteInventarioId == null)), Times.Once);
    }

    [Fact]
    public async Task Crear_serie_es_idempotente_si_numero_variante_y_lote_coinciden()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, false);
        var lote = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("L-11", null, null, false);
        var existente = new SerieInventario { Id = 21, ProductoVarianteId = 11 };
        existente.ConfigurarIdentidad("SN-IDEMP");
        existente.VincularLote(lote);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, false)).ReturnsAsync(lote);
        repo.Setup(x => x.GetSerieByNumeroAsync("SN-IDEMP", false)).ReturnsAsync(existente);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var resultado = await service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = " sn-idemp ",
            LoteInventarioId = 8
        });

        Assert.Equal(21, resultado.Id);
        Assert.Equal(8, resultado.LoteInventarioId);
        repo.Verify(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>()), Times.Never);
    }

    [Fact]
    public async Task Crear_serie_rechaza_numero_existente_con_variante_distinta()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(false, true, false);
        var existente = new SerieInventario { Id = 21, ProductoVarianteId = 12 };
        existente.ConfigurarIdentidad("SN-GLOBAL");

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByNumeroAsync("SN-GLOBAL", false)).ReturnsAsync(existente);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = "SN-GLOBAL"
        }));

        Assert.Contains("identidad logística diferente", ex.Message, StringComparison.OrdinalIgnoreCase);
        repo.Verify(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>()), Times.Never);
    }

    [Fact]
    public async Task Crear_serie_rechaza_lote_de_otra_variante()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, false);
        var loteAjeno = new LoteInventario { Id = 8, ProductoVarianteId = 12 };
        loteAjeno.ConfigurarIdentidad("L-12", null, null, false);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, false)).ReturnsAsync(loteAjeno);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = "SN-X",
            LoteInventarioId = 8
        }));

        repo.Verify(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>()), Times.Never);
    }

    [Fact]
    public async Task Crear_serie_rechaza_lote_inactivo_sin_persistir_serie()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, false);
        var lote = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("L-11", null, null, false);
        lote.Desactivar();

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, false)).ReturnsAsync(lote);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = "SN-INACTIVA",
            LoteInventarioId = 8
        }));

        Assert.Contains("inactivo", ex.Message, StringComparison.OrdinalIgnoreCase);
        repo.Verify(x => x.GetSerieByNumeroAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        repo.Verify(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>()), Times.Never);
    }

    [Fact]
    public async Task Desactivar_lote_rechaza_si_existen_series_activas()
    {
        var lote = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("L-11", null, null, false);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, true)).ReturnsAsync(lote);
        repo.Setup(x => x.TieneSeriesActivasEnLoteAsync(8)).ReturnsAsync(true);
        var service = CrearService(repo, new Mock<IProductoVarianteRepository>());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DesactivarLoteAsync(8));

        Assert.Contains("series activas", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(lote.Activo);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Baja_de_serie_es_idempotente()
    {
        var serie = new SerieInventario { Id = 5, ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-005");
        serie.DarDeBaja();
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByIdAsync(5, true)).ReturnsAsync(serie);
        var service = CrearService(repo, new Mock<IProductoVarianteRepository>());

        var resultado = await service.DarDeBajaSerieAsync(5);

        Assert.Equal(EstadoSerieInventario.Baja, resultado.Estado);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static TrazabilidadInventarioService CrearService(
        Mock<ITrazabilidadInventarioRepository> repo,
        Mock<IProductoVarianteRepository> variantes)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-test");
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return new TrazabilidadInventarioService(
            repo.Object,
            variantes.Object,
            current.Object,
            unit.Object,
            auditoria.Object);
    }
}
