using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N23RecepcionCompraExistenciaMaterializadorTests
{
    [Fact]
    public async Task Aplicar_incrementa_solo_cantidad_aceptada_sobre_stock_fisico()
    {
        var clave = new InventarioExistenciaClave(21, 3, 9);
        var fake = new FakeExistencias(clave, productoId: 7, stockFisico: 10, stockReservado: 2);
        var sut = new RecepcionCompraExistenciaMaterializador(fake);
        var detalle = CrearDetalle(productoId: 7, varianteId: 21, almacenId: 3, ubicacionId: 9,
            recibida: 6, danada: 1, sobrante: 2);

        var resultado = await sut.AplicarAsync(new[] { detalle });

        var transicion = Assert.Single(resultado);
        Assert.Equal(3, transicion.CantidadAceptada);
        Assert.Equal(10, transicion.StockAnterior);
        Assert.Equal(13, transicion.StockNuevo);
        Assert.Equal(13, fake.Existencia.StockFisico);
        Assert.Equal(2, fake.Existencia.StockReservado);
    }

    [Fact]
    public async Task Aplicar_rechaza_cantidad_fraccionaria_antes_de_tocar_existencias()
    {
        var clave = new InventarioExistenciaClave(21, 3, null);
        var fake = new FakeExistencias(clave, productoId: 7, stockFisico: 10, stockReservado: 0);
        var sut = new RecepcionCompraExistenciaMaterializador(fake);
        var detalle = CrearDetalle(productoId: 7, varianteId: 21, almacenId: 3, ubicacionId: null,
            recibida: 1.5m);

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.AplicarAsync(new[] { detalle }));
        Assert.Equal(0, fake.Bloqueos);
        Assert.Equal(0, fake.Ajustes);
    }

    [Fact]
    public async Task Aplicar_rechaza_stock_aceptado_sin_variante_autoritativa()
    {
        var clave = new InventarioExistenciaClave(21, 3, null);
        var fake = new FakeExistencias(clave, productoId: 7, stockFisico: 10, stockReservado: 0);
        var sut = new RecepcionCompraExistenciaMaterializador(fake);
        var detalle = CrearDetalle(productoId: 7, varianteId: null, almacenId: 3, ubicacionId: null,
            recibida: 2);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => sut.AplicarAsync(new[] { detalle }));

        Assert.Contains("variante", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fake.Bloqueos);
        Assert.Equal(0, fake.Ajustes);
    }

    [Fact]
    public async Task Revertir_falla_cerrado_si_reduccion_invade_stock_reservado()
    {
        var clave = new InventarioExistenciaClave(21, 3, null);
        var fake = new FakeExistencias(clave, productoId: 7, stockFisico: 5, stockReservado: 4);
        var sut = new RecepcionCompraExistenciaMaterializador(fake);
        var detalle = CrearDetalle(productoId: 7, varianteId: 21, almacenId: 3, ubicacionId: null,
            recibida: 2);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => sut.RevertirAsync(new[] { detalle }));

        Assert.Contains("reservado", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5, fake.Existencia.StockFisico);
        Assert.Equal(0, fake.Ajustes);
    }

    private static RecepcionCompraDetalle CrearDetalle(
        int productoId,
        int? varianteId,
        int almacenId,
        int? ubicacionId,
        decimal recibida,
        decimal danada = 0,
        decimal faltante = 0,
        decimal sobrante = 0)
    {
        var detalle = new RecepcionCompraDetalle
        {
            OrdenCompraDetalleId = 1,
            ProductoId = productoId,
            ProductoVarianteId = varianteId,
            AlmacenId = almacenId,
            UbicacionAlmacenId = ubicacionId,
            CostoUnitarioSnapshot = 10m
        };
        detalle.EstablecerCantidades(recibida, danada, faltante, sobrante);
        return detalle;
    }

    private sealed class FakeExistencias : IExistenciaVarianteConcurrencyService
    {
        private readonly InventarioExistenciaClave _clave;
        public ExistenciaVariante Existencia { get; }
        public int Bloqueos { get; private set; }
        public int Ajustes { get; private set; }

        public FakeExistencias(
            InventarioExistenciaClave clave,
            int productoId,
            int stockFisico,
            int stockReservado)
        {
            _clave = clave;
            Existencia = new ExistenciaVariante
            {
                ProductoVarianteId = clave.ProductoVarianteId,
                AlmacenId = clave.AlmacenId,
                UbicacionAlmacenId = clave.UbicacionAlmacenId,
                ProductoVariante = new ProductoVariante { ProductoId = productoId }
            };
            Existencia.EstablecerStocks(stockFisico, stockReservado, 0, 0, null);
        }

        public Task<InventarioExistenciaLockSet> BloquearYValidarExistenciasAsync(
            IEnumerable<InventarioDemandaExistencia> demandas,
            bool esDeduccion = true)
        {
            Bloqueos++;
            var lista = demandas.ToList();
            var total = lista.Sum(x => x.Cantidad);
            var consolidada = new InventarioDemandaExistencia(
                lista[0].ProductoId,
                _clave.ProductoVarianteId,
                _clave.AlmacenId,
                _clave.UbicacionAlmacenId,
                total);
            IReadOnlyDictionary<InventarioExistenciaClave, ExistenciaVariante> mapa =
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [_clave] = Existencia };
            return Task.FromResult(new InventarioExistenciaLockSet(mapa, new[] { consolidada }));
        }

        public Task<InventarioExistenciaLockSet> BloquearExistenciasParaReversionAsync(
            IEnumerable<InventarioDemandaExistencia> demandas) =>
            BloquearYValidarExistenciasAsync(demandas, esDeduccion: false);

        public Task AjustarStockFisicoPesimistaAsync(
            InventarioExistenciaClave clave,
            int cantidadActualEsperada,
            int cantidadNueva)
        {
            Assert.Equal(_clave, clave);
            Assert.Equal(Existencia.StockFisico, cantidadActualEsperada);
            Ajustes++;
            Existencia.EstablecerStocks(
                cantidadNueva,
                Existencia.StockReservado,
                Existencia.StockTransito,
                Existencia.StockMinimo,
                Existencia.StockMaximo);
            return Task.CompletedTask;
        }
    }
}
