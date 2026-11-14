using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14AjusteInventarioServiceCutoverContractTests
{
    [Fact]
    public async Task BloquearParaConfirmacion_UsaClaveFisicaYSinSemanticaDeDeduccion()
    {
        var fake = new FakeExistenciaConcurrencyService();
        var sut = new AjusteInventarioExistenciaCutoverService(fake);
        var detalle = CrearDetalle(cantidadObjetivo: 7);

        await sut.BloquearParaConfirmacionAsync(new[] { detalle });

        Assert.False(fake.UltimoEsDeduccion);
        var demanda = Assert.Single(fake.UltimasDemandas!);
        Assert.Equal(11, demanda.ProductoId);
        Assert.Equal(101, demanda.ProductoVarianteId);
        Assert.Equal(7, demanda.AlmacenId);
        Assert.Equal(3, demanda.UbicacionAlmacenId);
    }

    [Fact]
    public async Task BloquearParaConfirmacion_MismaVarianteEnUbicacionesDistintas_PreservaDosClavesFisicas()
    {
        var fake = new FakeExistenciaConcurrencyService();
        var sut = new AjusteInventarioExistenciaCutoverService(fake);
        var detalleA = CrearDetalle(cantidadObjetivo: 7);
        var detalleB = CrearDetalle(cantidadObjetivo: 4);
        detalleB.UbicacionAlmacenId = 8;

        await sut.BloquearParaConfirmacionAsync(new[] { detalleA, detalleB });

        Assert.False(fake.UltimoEsDeduccion);
        Assert.NotNull(fake.UltimasDemandas);
        var demandas = fake.UltimasDemandas!;
        Assert.Equal(2, demandas.Count);
        Assert.Contains(demandas, d =>
            d.ProductoId == 11 &&
            d.ProductoVarianteId == 101 &&
            d.AlmacenId == 7 &&
            d.UbicacionAlmacenId == 3);
        Assert.Contains(demandas, d =>
            d.ProductoId == 11 &&
            d.ProductoVarianteId == 101 &&
            d.AlmacenId == 7 &&
            d.UbicacionAlmacenId == 8);
    }

    [Fact]
    public async Task AplicarObjetivoConfirmacion_AjustaStockFisicoConPrecondicionPesimista()
    {
        var fake = new FakeExistenciaConcurrencyService();
        var sut = new AjusteInventarioExistenciaCutoverService(fake);
        var detalle = CrearDetalle(cantidadObjetivo: 7);
        var existencia = CrearExistencia(stockFisico: 10, stockReservado: 2);
        var clave = new InventarioExistenciaClave(101, 7, 3);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia });

        var diferencia = await sut.AplicarObjetivoConfirmacionAsync(lockSet, detalle);

        Assert.Equal(-3, diferencia);
        Assert.Equal(clave, fake.UltimaClaveAjuste);
        Assert.Equal(10, fake.UltimoStockEsperado);
        Assert.Equal(7, fake.UltimoStockNuevo);
    }

    [Fact]
    public async Task AplicarReversion_UsaDiferenciaHistoricaSobreStockFisico()
    {
        var fake = new FakeExistenciaConcurrencyService();
        var sut = new AjusteInventarioExistenciaCutoverService(fake);
        var detalle = CrearDetalle(cantidadObjetivo: 15);
        detalle.MaterializarConfirmacion(cantidadAnterior: 10, costoUnitario: 1m);
        var existencia = CrearExistencia(stockFisico: 15, stockReservado: 4);
        var clave = new InventarioExistenciaClave(101, 7, 3);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia });

        var objetivo = await sut.AplicarReversionAsync(lockSet, detalle);

        Assert.Equal(10, objetivo);
        Assert.Equal(clave, fake.UltimaClaveAjuste);
        Assert.Equal(15, fake.UltimoStockEsperado);
        Assert.Equal(10, fake.UltimoStockNuevo);
    }

    private static AjusteInventarioDetalle CrearDetalle(int cantidadObjetivo) => new()
    {
        ProductoId = 11,
        ProductoVarianteId = 101,
        AlmacenId = 7,
        UbicacionAlmacenId = 3,
        CantidadObjetivo = cantidadObjetivo
    };

    private static ExistenciaVariante CrearExistencia(int stockFisico, int stockReservado)
    {
        var existencia = new ExistenciaVariante
        {
            ProductoVarianteId = 101,
            AlmacenId = 7,
            UbicacionAlmacenId = 3
        };
        existencia.EstablecerStocks(
            stockFisico,
            stockReservado,
            stockTransito: 0,
            stockMinimo: 0,
            stockMaximo: null);
        return existencia;
    }

    private sealed class FakeExistenciaConcurrencyService : IExistenciaVarianteConcurrencyService
    {
        public IReadOnlyList<InventarioDemandaExistencia>? UltimasDemandas { get; private set; }
        public bool UltimoEsDeduccion { get; private set; }
        public InventarioExistenciaClave? UltimaClaveAjuste { get; private set; }
        public int? UltimoStockEsperado { get; private set; }
        public int? UltimoStockNuevo { get; private set; }

        public Task<InventarioExistenciaLockSet> BloquearYValidarExistenciasAsync(
            IEnumerable<InventarioDemandaExistencia> demandas,
            bool esDeduccion = true)
        {
            UltimasDemandas = demandas.ToList();
            UltimoEsDeduccion = esDeduccion;
            return Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(),
                UltimasDemandas));
        }

        public Task<InventarioExistenciaLockSet> BloquearExistenciasParaReversionAsync(
            IEnumerable<InventarioDemandaExistencia> demandas)
        {
            UltimasDemandas = demandas.ToList();
            return Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(),
                UltimasDemandas));
        }

        public Task AjustarStockFisicoPesimistaAsync(
            InventarioExistenciaClave clave,
            int cantidadActualEsperada,
            int cantidadNueva)
        {
            UltimaClaveAjuste = clave;
            UltimoStockEsperado = cantidadActualEsperada;
            UltimoStockNuevo = cantidadNueva;
            return Task.CompletedTask;
        }
    }
}
