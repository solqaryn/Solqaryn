using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioExistenciaServiceTests
{
    [Fact]
    public async Task AplicarDespacho_DeduceStockFisicoBajoClaveBloqueada()
    {
        var clave = new InventarioExistenciaClave(91, 10, 101);
        var existencia = CrearExistencia(91, 10, 101, fisico: 12, reservado: 2, transito: 0);
        var demanda = new InventarioDemandaExistencia(44, 91, 10, 101, 5);
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
            new[] { demanda });

        var transiciones = await service.AplicarDespachoAsync(lockSet);

        var transicion = Assert.Single(transiciones);
        Assert.Equal(12, transicion.StockAnterior);
        Assert.Equal(7, transicion.StockNuevo);
        Assert.Equal(5, transicion.Cantidad);
        Assert.Equal(clave, fake.UltimaClave);
        Assert.Equal(12, fake.UltimoEsperado);
        Assert.Equal(7, fake.UltimoNuevo);
    }

    [Fact]
    public async Task BloquearParaDespacho_BloqueaOrigenYDestinoEnUnSoloConjuntoOrdenable()
    {
        var transferencia = CrearTransferenciaDespachada();
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);

        await service.BloquearParaDespachoAsync(transferencia);

        Assert.False(fake.UltimaEsDeduccion);
        Assert.Equal(2, fake.UltimasDemandas.Count);
        Assert.Contains(fake.UltimasDemandas, d => d.AlmacenId == 10 && d.UbicacionAlmacenId == 101 && d.Cantidad == 5);
        Assert.Contains(fake.UltimasDemandas, d => d.AlmacenId == 20 && d.UbicacionAlmacenId == 202 && d.Cantidad == 5);
    }

    [Fact]
    public async Task AplicarDespachoCompleto_DeduceOrigenYAumentaTransitoDestino()
    {
        var transferencia = CrearTransferenciaDespachada();
        var origenClave = new InventarioExistenciaClave(91, 10, 101);
        var destinoClave = new InventarioExistenciaClave(91, 20, 202);
        var origen = CrearExistencia(91, 10, 101, fisico: 12, reservado: 2, transito: 0);
        var destino = CrearExistencia(91, 20, 202, fisico: 3, reservado: 0, transito: 1);
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante>
            {
                [origenClave] = origen,
                [destinoClave] = destino
            },
            TransferenciaInventarioExistenciaContext.ConstruirDemandasBloqueoDespacho(transferencia));

        await service.AplicarDespachoCompletoAsync(lockSet, transferencia);

        Assert.Contains(fake.Ajustes, a =>
            a.Clave == origenClave && a.FisicoActual == 12 && a.FisicoNuevo == 7 && a.TransitoActual == 0 && a.TransitoNuevo == 0);
        Assert.Contains(fake.Ajustes, a =>
            a.Clave == destinoClave && a.FisicoActual == 3 && a.FisicoNuevo == 3 && a.TransitoActual == 1 && a.TransitoNuevo == 6);
    }

    [Fact]
    public async Task AplicarRecepcion_SoloMaterializaRecibidoYCierraTodoElTransitoDespachado()
    {
        var transferencia = CrearTransferenciaDespachada();
        transferencia.Detalles.Single().RegistrarRecepcion(recibida: 4, faltante: 1, danada: 0, sobrante: 2);
        var destinoClave = new InventarioExistenciaClave(91, 20, 202);
        var destino = CrearExistencia(91, 20, 202, fisico: 3, reservado: 0, transito: 5);
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [destinoClave] = destino },
            TransferenciaInventarioExistenciaContext.ConstruirDemandasCierreTransitoDestino(transferencia));

        await service.AplicarRecepcionAsync(lockSet, transferencia);

        var ajuste = Assert.Single(fake.Ajustes);
        Assert.Equal(destinoClave, ajuste.Clave);
        Assert.Equal(3, ajuste.FisicoActual);
        Assert.Equal(7, ajuste.FisicoNuevo);
        Assert.Equal(5, ajuste.TransitoActual);
        Assert.Equal(0, ajuste.TransitoNuevo);
        // El sobrante 2 queda como discrepancia; no se incorpora silenciosamente al stock.
    }

    private static TransferenciaInventario CrearTransferenciaDespachada()
    {
        var variante = new ProductoVariante { Id = 91, ProductoId = 44, Activo = true };
        var detalle = new TransferenciaInventarioDetalle
        {
            ProductoVarianteId = 91,
            ProductoVariante = variante,
            UbicacionOrigenId = 101,
            UbicacionDestinoId = 202,
            CreadoPorUsuarioId = 7
        };
        detalle.EstablecerCantidadSolicitada(6);
        detalle.AprobarCantidad(5);
        detalle.RegistrarDespacho(5);
        return new TransferenciaInventario
        {
            Numero = "TRF-N16-PHY",
            AlmacenOrigenId = 10,
            AlmacenDestinoId = 20,
            CreadoPorUsuarioId = 7,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };
    }

    private static ExistenciaVariante CrearExistencia(
        int varianteId,
        int almacenId,
        int? ubicacionId,
        int fisico,
        int reservado,
        int transito)
    {
        var existencia = new ExistenciaVariante
        {
            ProductoVarianteId = varianteId,
            AlmacenId = almacenId,
            UbicacionAlmacenId = ubicacionId
        };
        existencia.EstablecerStocks(fisico, reservado, transito, 0, null);
        return existencia;
    }

    private sealed class FakeConcurrency : IExistenciaVarianteConcurrencyService
    {
        public bool UltimaEsDeduccion { get; private set; }
        public IReadOnlyList<InventarioDemandaExistencia> UltimasDemandas { get; private set; } = Array.Empty<InventarioDemandaExistencia>();
        public InventarioExistenciaClave UltimaClave { get; private set; }
        public int UltimoEsperado { get; private set; }
        public int UltimoNuevo { get; private set; }
        public List<Ajuste> Ajustes { get; } = new();

        public Task<InventarioExistenciaLockSet> BloquearYValidarExistenciasAsync(IEnumerable<InventarioDemandaExistencia> demandas, bool esDeduccion = true)
        {
            UltimaEsDeduccion = esDeduccion;
            UltimasDemandas = demandas.ToList();
            return Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(), UltimasDemandas));
        }

        public Task<InventarioExistenciaLockSet> BloquearExistenciasParaReversionAsync(IEnumerable<InventarioDemandaExistencia> demandas) =>
            Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(), demandas.ToList()));

        public Task AjustarStockFisicoPesimistaAsync(InventarioExistenciaClave clave, int cantidadActualEsperada, int cantidadNueva)
        {
            UltimaClave = clave;
            UltimoEsperado = cantidadActualEsperada;
            UltimoNuevo = cantidadNueva;
            return Task.CompletedTask;
        }

        public Task AjustarStocksPesimistaAsync(
            InventarioExistenciaClave clave,
            int stockFisicoActualEsperado,
            int stockFisicoNuevo,
            int stockTransitoActualEsperado,
            int stockTransitoNuevo)
        {
            Ajustes.Add(new Ajuste(
                clave,
                stockFisicoActualEsperado,
                stockFisicoNuevo,
                stockTransitoActualEsperado,
                stockTransitoNuevo));
            return Task.CompletedTask;
        }
    }

    private sealed record Ajuste(
        InventarioExistenciaClave Clave,
        int FisicoActual,
        int FisicoNuevo,
        int TransitoActual,
        int TransitoNuevo);
}
