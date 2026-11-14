using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class AjusteInventarioServiceTests
{
    private const int VarianteId = 101;
    private const int AlmacenId = 7;

    [Fact]
    public async Task ConfirmarAsync_MaterializaSnapshots_AjustaExistencia_YRegistraOrigenTipado()
    {
        var fixture = new Fixture();
        var (producto, variante) = CrearProductoConVariante(cantidadLegacy: 5, costo: 2.50m);
        var existencia = CrearExistencia(stockFisico: 5);
        var ajuste = CrearBorrador(7, producto.Id, cantidadObjetivo: 8);
        fixture.ConfigurarAutoridad(producto, variante, existencia);

        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);
        fixture.Ajustes.Setup(x => x.GetByIdAsync(ajuste.Id)).ReturnsAsync(ajuste);

        MovimientoInventario? movimiento = null;
        OrigenMovimientoInventario? origen = null;
        fixture.Movimientos
            .Setup(x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((m, o) =>
            {
                movimiento = m;
                origen = o;
            })
            .Returns(Task.CompletedTask);

        var resultado = await fixture.Service.ConfirmarAsync(ajuste.Id);

        Assert.NotNull(resultado);
        Assert.Equal(EstadoAjusteInventario.Confirmado, ajuste.Estado);
        Assert.Equal(8, existencia.StockFisico);
        Assert.Equal(8, producto.Cantidad);
        Assert.Equal(8, variante.Cantidad);
        var detalle = Assert.Single(ajuste.Detalles);
        Assert.Equal(5, detalle.CantidadAnteriorSnapshot);
        Assert.Equal(8, detalle.CantidadNuevaSnapshot);
        Assert.Equal(3, detalle.DiferenciaSnapshot);
        Assert.Equal(2.50m, detalle.CostoUnitarioSnapshot);
        Assert.Equal(7.50m, detalle.ImpactoCostoSnapshot);
        Assert.Equal("Producto prueba", detalle.NombreSnapshot);
        Assert.NotNull(movimiento);
        Assert.Equal(TipoMovimientoInventario.Ajuste, movimiento!.Tipo);
        Assert.Equal(CausaMovimientoInventario.AjusteManual, movimiento.Causa);
        Assert.Equal(3, movimiento.Cantidad);
        Assert.Equal(5, movimiento.StockAnterior);
        Assert.Equal(8, movimiento.StockNuevo);
        Assert.Equal(AlmacenId, movimiento.AlmacenId);
        Assert.NotNull(origen);
        Assert.Equal(ajuste.Id, origen!.AjusteInventarioId);
        Assert.Null(origen.CompraId);
        Assert.Null(origen.VentaId);
        Assert.Null(origen.ConsumoInsumoId);
    }

    [Fact]
    public async Task AjustarStockCompatibilidadAsync_CreaYConfirmaEnUnaTransaccionFormal()
    {
        var fixture = new Fixture();
        var (producto, variante) = CrearProductoConVariante(cantidadLegacy: 5, costo: 3m);
        var existencia = CrearExistencia(stockFisico: 5);
        fixture.ConfigurarAutoridad(producto, variante, existencia);
        AjusteInventario? ajusteCreado = null;

        fixture.Ajustes
            .Setup(x => x.AddAsync(It.IsAny<AjusteInventario>()))
            .Callback<AjusteInventario>(ajuste =>
            {
                ajuste.Id = 77;
                var index = 1;
                foreach (var detalle in ajuste.Detalles)
                {
                    detalle.Id = 770 + index++;
                    detalle.AjusteInventarioId = ajuste.Id;
                }
                ajusteCreado = ajuste;
            })
            .Returns(Task.CompletedTask);

        OrigenMovimientoInventario? origen = null;
        fixture.Movimientos
            .Setup(x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((_, o) => origen = o)
            .Returns(Task.CompletedTask);

        var resultado = await fixture.Service.AjustarStockCompatibilidadAsync(
            producto.Id,
            VarianteId,
            new AjusteStockRequest
            {
                AlmacenId = AlmacenId,
                CantidadActualEsperada = 5,
                CantidadNueva = 8,
                Motivo = "Conteo físico"
            });

        Assert.Equal(5, resultado.CantidadAnterior);
        Assert.Equal(8, resultado.CantidadNueva);
        Assert.Equal(3, resultado.Diferencia);
        Assert.Equal(8, existencia.StockFisico);
        Assert.Equal(8, producto.Cantidad);
        Assert.Equal(8, variante.Cantidad);
        Assert.NotNull(ajusteCreado);
        Assert.Equal(EstadoAjusteInventario.Confirmado, ajusteCreado!.Estado);
        Assert.Equal("AI-000077", ajusteCreado.NumeroAjuste);
        Assert.Equal(AlmacenId, Assert.Single(ajusteCreado.Detalles).AlmacenId);
        Assert.Equal(77, origen!.AjusteInventarioId);
        fixture.UnitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()),
            Times.Once);
    }

    [Fact]
    public async Task AjustarStockCompatibilidadAsync_StockFisicoEsperadoDesactualizado_FallaAntesDeMutar()
    {
        var fixture = new Fixture();
        var (producto, variante) = CrearProductoConVariante(cantidadLegacy: 6, costo: 3m);
        var existencia = CrearExistencia(stockFisico: 6);
        fixture.ConfigurarAutoridad(producto, variante, existencia);
        AjusteInventario? ajusteCreado = null;

        fixture.Ajustes
            .Setup(x => x.AddAsync(It.IsAny<AjusteInventario>()))
            .Callback<AjusteInventario>(ajuste =>
            {
                ajuste.Id = 78;
                ajusteCreado = ajuste;
            })
            .Returns(Task.CompletedTask);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.AjustarStockCompatibilidadAsync(
                producto.Id,
                VarianteId,
                new AjusteStockRequest
                {
                    AlmacenId = AlmacenId,
                    CantidadActualEsperada = 5,
                    CantidadNueva = 8,
                    Motivo = "Conteo físico"
                }));

        Assert.Contains("Esperado: 5; actual: 6", ex.Message);
        Assert.Equal(6, existencia.StockFisico);
        Assert.Equal(6, producto.Cantidad);
        Assert.Equal(6, variante.Cantidad);
        Assert.NotNull(ajusteCreado);
        Assert.Equal(EstadoAjusteInventario.Borrador, ajusteCreado!.Estado);
        fixture.Movimientos.Verify(
            x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()),
            Times.Never);
        fixture.UnitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()),
            Times.Once);
    }

    [Fact]
    public async Task AnularAsync_AplicaMovimientoInversoSobreExistenciaActual_SinReescribirHistoria()
    {
        var fixture = new Fixture();
        var (producto, variante) = CrearProductoConVariante(cantidadLegacy: 10, costo: 2m);
        var existencia = CrearExistencia(stockFisico: 10);
        var ajuste = CrearBorrador(7, producto.Id, cantidadObjetivo: 8);
        var detalle = Assert.Single(ajuste.Detalles);
        detalle.MaterializarConfirmacion(cantidadAnterior: 5, costoUnitario: 2m);
        ajuste.Confirmar(99, "tester", DateTime.UtcNow.AddMinutes(-5));
        fixture.ConfigurarAutoridad(producto, variante, existencia);

        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);
        fixture.Ajustes.Setup(x => x.GetByIdAsync(ajuste.Id)).ReturnsAsync(ajuste);

        MovimientoInventario? movimiento = null;
        OrigenMovimientoInventario? origen = null;
        fixture.Movimientos
            .Setup(x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((m, o) =>
            {
                movimiento = m;
                origen = o;
            })
            .Returns(Task.CompletedTask);

        var resultado = await fixture.Service.AnularAsync(ajuste.Id, "Conteo corregido");

        Assert.NotNull(resultado);
        Assert.Equal(EstadoAjusteInventario.Anulado, ajuste.Estado);
        Assert.Equal(7, existencia.StockFisico);
        Assert.Equal(7, producto.Cantidad);
        Assert.Equal(7, variante.Cantidad);
        Assert.Equal(5, detalle.CantidadAnteriorSnapshot);
        Assert.Equal(8, detalle.CantidadNuevaSnapshot);
        Assert.NotNull(movimiento);
        Assert.Equal(TipoMovimientoInventario.Reversion, movimiento!.Tipo);
        Assert.Equal(3, movimiento.Cantidad);
        Assert.Equal(10, movimiento.StockAnterior);
        Assert.Equal(7, movimiento.StockNuevo);
        Assert.Equal(AlmacenId, movimiento.AlmacenId);
        Assert.Equal(ajuste.Id, origen!.AjusteInventarioId);
    }

    [Fact]
    public async Task ConfirmarAsync_SegundaConfirmacion_FallaCerradoSinNuevoMovimiento()
    {
        var fixture = new Fixture();
        var ajuste = CrearBorrador(7, productoId: 10, cantidadObjetivo: 8);
        var detalle = Assert.Single(ajuste.Detalles);
        detalle.MaterializarConfirmacion(cantidadAnterior: 5, costoUnitario: 2m);
        ajuste.Confirmar(99, "tester", DateTime.UtcNow);
        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => fixture.Service.ConfirmarAsync(ajuste.Id));

        Assert.Contains("Borrador", ex.Message);
        fixture.Movimientos.Verify(
            x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()),
            Times.Never);
    }

    [Fact]
    public async Task AnularAsync_SegundaAnulacion_FallaCerradoSinNuevoMovimiento()
    {
        var fixture = new Fixture();
        var ajuste = CrearBorrador(7, productoId: 10, cantidadObjetivo: 8);
        var detalle = Assert.Single(ajuste.Detalles);
        detalle.MaterializarConfirmacion(cantidadAnterior: 5, costoUnitario: 2m);
        ajuste.Confirmar(99, "tester", DateTime.UtcNow.AddMinutes(-2));
        ajuste.Anular(99, "tester", "Primera anulación", DateTime.UtcNow.AddMinutes(-1));
        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.AnularAsync(ajuste.Id, "Reintento duplicado"));

        Assert.Contains("confirmados", ex.Message, StringComparison.OrdinalIgnoreCase);
        fixture.Movimientos.Verify(
            x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()),
            Times.Never);
    }

    private static AjusteInventario CrearBorrador(int id, int productoId, int cantidadObjetivo)
    {
        var ajuste = new AjusteInventario
        {
            Id = id,
            NumeroAjuste = $"AI-{id:D6}",
            Motivo = "Conteo físico"
        };
        ajuste.Detalles.Add(new AjusteInventarioDetalle
        {
            Id = id * 10,
            AjusteInventarioId = id,
            ProductoId = productoId,
            ProductoVarianteId = VarianteId,
            AlmacenId = AlmacenId,
            CantidadObjetivo = cantidadObjetivo
        });
        return ajuste;
    }

    private static (Producto Producto, ProductoVariante Variante) CrearProductoConVariante(
        int cantidadLegacy,
        decimal costo)
    {
        var producto = new Producto
        {
            Id = 10,
            Nombre = "Producto prueba",
            Marca = "Marca",
            Modelo = "Modelo",
            Cantidad = cantidadLegacy,
            Costo = costo
        };
        var variante = new ProductoVariante
        {
            Id = VarianteId,
            ProductoId = producto.Id,
            Producto = producto,
            Sku = "SKU-N14",
            Cantidad = cantidadLegacy,
            Costo = costo,
            Activo = true
        };
        producto.Variantes.Add(variante);
        return (producto, variante);
    }

    private static ExistenciaVariante CrearExistencia(int stockFisico)
    {
        var existencia = new ExistenciaVariante
        {
            ProductoVarianteId = VarianteId,
            AlmacenId = AlmacenId
        };
        existencia.EstablecerStocks(
            stockFisico,
            stockReservado: 0,
            stockTransito: 0,
            stockMinimo: 0,
            stockMaximo: null);
        return existencia;
    }

    private sealed class Fixture
    {
        public Mock<IAjusteInventarioRepository> Ajustes { get; } = new();
        public Mock<IProductoRepository> Productos { get; } = new();
        public Mock<IProductoVarianteRepository> Variantes { get; } = new();
        public Mock<IMovimientoInventarioRepository> Movimientos { get; } = new();
        public Mock<IInventarioConcurrencyService> Concurrency { get; } = new();
        public Mock<IExistenciaVarianteConcurrencyService> Existencias { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<ICurrentUserService> CurrentUser { get; } = new();
        public Mock<IAuditoriaService> Auditoria { get; } = new();

        public Fixture()
        {
            UnitOfWork
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(operation => operation());
            Ajustes.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
            CurrentUser.SetupGet(x => x.UsuarioId).Returns(99);
            CurrentUser.SetupGet(x => x.NombreUsuario).Returns("tester");
            Auditoria
                .Setup(x => x.RegistrarAsync(
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
            Auditoria
                .Setup(x => x.RegistrarEstrictoAsync(
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

            Service = new AjusteInventarioService(
                Ajustes.Object,
                Productos.Object,
                Variantes.Object,
                Movimientos.Object,
                Concurrency.Object,
                UnitOfWork.Object,
                CurrentUser.Object,
                Auditoria.Object,
                Existencias.Object);
        }

        public AjusteInventarioService Service { get; }

        public void ConfigurarAutoridad(
            Producto producto,
            ProductoVariante variante,
            ExistenciaVariante existencia)
        {
            Productos.Setup(x => x.GetByIdAsync(producto.Id)).ReturnsAsync(producto);
            Concurrency
                .Setup(x => x.BloquearInventarioParaReversionAsync(It.IsAny<IEnumerable<InventarioDemanda>>()))
                .ReturnsAsync(new InventarioLockSet(
                    new Dictionary<int, Producto> { [producto.Id] = producto },
                    new Dictionary<int, ProductoVariante> { [variante.Id] = variante }));

            var clave = new InventarioExistenciaClave(variante.Id, AlmacenId, null);
            InventarioExistenciaLockSet CrearLockSet() => new(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia });

            Existencias
                .Setup(x => x.BloquearYValidarExistenciasAsync(
                    It.IsAny<IEnumerable<InventarioDemandaExistencia>>(),
                    false))
                .ReturnsAsync(() => CrearLockSet());
            Existencias
                .Setup(x => x.BloquearExistenciasParaReversionAsync(
                    It.IsAny<IEnumerable<InventarioDemandaExistencia>>()))
                .ReturnsAsync(() => CrearLockSet());
            Existencias
                .Setup(x => x.AjustarStockFisicoPesimistaAsync(
                    It.IsAny<InventarioExistenciaClave>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Callback<InventarioExistenciaClave, int, int>((solicitada, esperado, nuevo) =>
                {
                    Assert.Equal(clave, solicitada);
                    Assert.Equal(existencia.StockFisico, esperado);
                    existencia.EstablecerStocks(
                        nuevo,
                        existencia.StockReservado,
                        existencia.StockTransito,
                        existencia.StockMinimo,
                        existencia.StockMaximo);
                })
                .Returns(Task.CompletedTask);
        }
    }
}
