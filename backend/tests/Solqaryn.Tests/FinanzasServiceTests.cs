using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Tests;

public class FinanzasServiceTests
{
    private readonly Mock<IMovimientoFinancieroRepository> _movRepoMock = new();
    private readonly Mock<IRevisionFinancieraRepository> _revisionRepoMock = new();
    private readonly Mock<IVentaRepository> _ventaRepoMock = new();
    private readonly Mock<ICompraRepository> _compraRepoMock = new();
    private readonly Mock<IProductoRepository> _productoRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IUsuarioScopeService> _usuarioScopeMock = new();
    private readonly FinanzasService _service;

    public FinanzasServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _currentUserMock.Setup(c => c.NombreCompleto).Returns("Administrador");
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Administrador", true));
        _movRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _revisionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        _service = new FinanzasService(
            _movRepoMock.Object,
            _revisionRepoMock.Object,
            _ventaRepoMock.Object,
            _compraRepoMock.Object,
            _productoRepoMock.Object,
            _currentUserMock.Object,
            _usuarioScopeMock.Object);
    }

    [Fact]
    public async Task RegistrarMovimientoManualAsync_Guarda_Usuario_Y_No_Es_Automatico()
    {
        MovimientoFinanciero? creado = null;
        _movRepoMock.Setup(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()))
            .Callback<MovimientoFinanciero>(m => creado = m)
            .Returns(Task.CompletedTask);

        await _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
        {
            Tipo = "Egreso", Categoria = "GastoOperativo", Concepto = "Pago de internet", Monto = 500
        });

        Assert.NotNull(creado);
        Assert.False(creado!.EsAutomatico);
        Assert.Equal(1, creado.CreadoPorUsuarioId);
        Assert.Equal("Manual", creado.ModuloOrigen);
    }

    [Fact]
    public async Task RegistrarMovimientoManualAsync_MetodoPagoUsaCatalogoRelacional()
    {
        var catalogo = new CatalogoMetodoPago { Id = 12, Codigo = "TRANSFERENCIA", Nombre = "Transferencia bancaria", Tipo = "Transferencia" };
        _movRepoMock.Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("Transferencia"))
            .ReturnsAsync(catalogo);

        MovimientoFinanciero? creado = null;
        _movRepoMock.Setup(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()))
            .Callback<MovimientoFinanciero>(m => creado = m)
            .Returns(Task.CompletedTask);

        var resultado = await _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
        {
            Tipo = "Egreso",
            Categoria = "GastoOperativo",
            Concepto = "Pago proveedor",
            Monto = 250m,
            MetodoPago = "Transferencia"
        });

        Assert.NotNull(creado);
        Assert.Equal(12, creado!.MetodoPagoId);
        Assert.Same(catalogo, creado.MetodoPagoCatalogo);
        Assert.Null(creado.MetodoPago);
        Assert.Equal("Transferencia bancaria", resultado.MetodoPago);
    }

    [Fact]
    public async Task RegistrarMovimientoManualAsync_MetodoPagoInexistente_FallaCerrado()
    {
        _movRepoMock.Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("Cripto"))
            .ReturnsAsync((CatalogoMetodoPago?)null);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
            {
                Tipo = "Egreso",
                Categoria = "GastoOperativo",
                Concepto = "Pago inválido",
                Monto = 100m,
                MetodoPago = "Cripto"
            }));

        Assert.Contains("no existe en el catálogo", error.Message, StringComparison.OrdinalIgnoreCase);
        _movRepoMock.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarMovimientoManualAsync_Rechaza_Tipo_Y_Categoria_Invalidos()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
        {
            Tipo = "Desconocido", Categoria = "GastoOperativo", Concepto = "Error", Monto = 100
        }));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
        {
            Tipo = "Egreso", Categoria = "Desconocida", Concepto = "Error", Monto = 100
        }));

        _movRepoMock.Verify(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarMovimientoManualAsync_GastoOperativo_Debe_Ser_Egreso()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
        {
            Tipo = "Ingreso", Categoria = "GastoOperativo", Concepto = "No válido", Monto = 100
        }));
    }

    [Fact]
    public async Task RegistrarMovimientoManualAsync_Rechaza_Categorias_Automaticas()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
        {
            Tipo = "Ingreso", Categoria = "Venta", Concepto = "Venta manual", Monto = 100
        }));
    }

    [Fact]
    public async Task GetResumenAsync_Separa_GastosOperativos_De_Otros_Egresos()
    {
        _movRepoMock.Setup(r => r.GetFilteredAsync(null, null)).ReturnsAsync(new List<MovimientoFinanciero>
        {
            new() { Tipo = TipoMovimientoFinanciero.Egreso, Categoria = CategoriaMovimientoFinanciero.GastoOperativo, Monto = 100m, EsAutomatico = false, Estado = EstadoMovimientoFinanciero.Pagado },
            new() { Tipo = TipoMovimientoFinanciero.Egreso, Categoria = CategoriaMovimientoFinanciero.Otro, Monto = 50m, EsAutomatico = false, Estado = EstadoMovimientoFinanciero.Pagado },
            new() { Tipo = TipoMovimientoFinanciero.Egreso, Categoria = CategoriaMovimientoFinanciero.GastoOperativo, Monto = 75m, EsAutomatico = true, Estado = EstadoMovimientoFinanciero.Pagado },
            new() { Tipo = TipoMovimientoFinanciero.Egreso, Categoria = CategoriaMovimientoFinanciero.GastoOperativo, Monto = 25m, EsAutomatico = false, Estado = EstadoMovimientoFinanciero.Anulado }
        });
        _ventaRepoMock.Setup(r => r.GetUtilidadBrutaTotalAsync((int?)null)).ReturnsAsync(500m);
        _ventaRepoMock.Setup(r => r.GetCuentasPorCobrarAsync((int?)null)).ReturnsAsync(0m);
        _ventaRepoMock.Setup(r => r.GetTotalDelMesAsync((int?)null)).ReturnsAsync(0);
        _ventaRepoMock.Setup(r => r.GetIngresosDelMesAsync((int?)null)).ReturnsAsync(0m);
        _compraRepoMock.Setup(r => r.GetCuentasPorPagarAsync((int?)null)).ReturnsAsync(0m);
        _compraRepoMock.Setup(r => r.GetTotalDelMesAsync((int?)null)).ReturnsAsync(0);
        _revisionRepoMock.Setup(r => r.GetUltimaAsync()).ReturnsAsync((RevisionFinanciera?)null);
        _productoRepoMock.Setup(r => r.GetValorTotalCostoPorTipoAsync(TipoInventario.MercaderiaVenta)).ReturnsAsync(800m);
        _productoRepoMock.Setup(r => r.GetValorTotalCostoPorTipoAsync(TipoInventario.InsumoAdministrativo)).ReturnsAsync(200m);
        _productoRepoMock.Setup(r => r.GetValorTotalPrecioPorTipoAsync(TipoInventario.MercaderiaVenta)).ReturnsAsync(1200m);

        var resultado = await _service.GetResumenAsync();

        Assert.Equal(100m, resultado.GastosOperativos);
        Assert.Equal(400m, resultado.UtilidadNeta);
        Assert.Equal(1000m, resultado.ValorInventarioCosto);
        Assert.Equal(800m, resultado.ValorInventarioCostoMercaderia);
        Assert.Equal(200m, resultado.ValorInventarioCostoInsumosAdministrativos);
        Assert.Equal(1200m, resultado.ValorPotencialVentaMercaderia);
        Assert.Equal(400m, resultado.UtilidadInventarioPotencial);
    }

    [Fact]
    public async Task AnularMovimientoAsync_Bloquea_Movimientos_Automaticos()
    {
        var movimiento = new MovimientoFinanciero { Id = 1, EsAutomatico = true, ModuloOrigen = "Venta" };
        _movRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(movimiento);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularMovimientoAsync(1, "motivo"));
    }

    [Fact]
    public async Task AnularMovimientoAsync_Permite_Anular_Manuales()
    {
        var movimiento = new MovimientoFinanciero { Id = 1, EsAutomatico = false, ModuloOrigen = "Manual" };
        _movRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(movimiento);

        var resultado = await _service.AnularMovimientoAsync(1, "Error de registro");

        Assert.Equal("Anulado", resultado!.Estado);
        Assert.Equal("Error de registro", movimiento.MotivoAnulacion);
    }

    [Fact]
    public async Task RegistrarRevisionAsync_Guarda_Usuario_Revisor_Administrador()
    {
        var resultado = await _service.RegistrarRevisionAsync(new CreateRevisionFinancieraDto
        {
            FechaDesde = DateTime.UtcNow.AddDays(-30),
            FechaHasta = DateTime.UtcNow,
            EstadoRevision = "Revisado",
            Observaciones = "Todo cuadra"
        });

        Assert.Equal("Administrador", resultado.RevisadoPorNombreUsuario);
        Assert.Equal("Revisado", resultado.EstadoRevision);
    }

    [Fact]
    public async Task RegistrarRevisionAsync_Usuario_No_Administrador_Es_Rechazado()
    {
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(2, 2, "Vendedor", false));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarRevisionAsync(new CreateRevisionFinancieraDto
        {
            FechaDesde = DateTime.UtcNow.AddDays(-30),
            FechaHasta = DateTime.UtcNow,
            EstadoRevision = "Revisado"
        }));

        _revisionRepoMock.Verify(r => r.AddAsync(It.IsAny<RevisionFinanciera>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarRevisionAsync_Fecha_Hasta_Menor_A_Desde_Lanza_Excepcion()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarRevisionAsync(new CreateRevisionFinancieraDto
        {
            FechaDesde = DateTime.UtcNow,
            FechaHasta = DateTime.UtcNow.AddDays(-10)
        }));
    }

    [Fact]
    public async Task GetResumenAsync_Sesion_Sin_Usuario_Dinamico_Falla_Cerrada()
    {
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync((UsuarioScopeActual?)null);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.GetResumenAsync());
        _movRepoMock.Verify(r => r.GetFilteredAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
    }
}
