using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioCancelacionFisicaTests
{
    [Fact]
    public async Task CancelarEnTransito_RestauraOrigenYCierraTransitoDestino()
    {
        var transferencia = CrearTransferenciaDespachada();
        var origenClave = new InventarioExistenciaClave(91, 10, 101);
        var destinoClave = new InventarioExistenciaClave(91, 20, 202);
        var origen = CrearExistencia(91, 10, 101, fisico: 7, reservado: 2, transito: 0);
        var destino = CrearExistencia(91, 20, 202, fisico: 3, reservado: 0, transito: 5);
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante>
            {
                [origenClave] = origen,
                [destinoClave] = destino
            },
            TransferenciaInventarioExistenciaContext.ConstruirDemandasBloqueoDespacho(transferencia));

        var transiciones = await service.AplicarCancelacionEnTransitoAsync(lockSet, transferencia);

        Assert.Contains(fake.Ajustes, a =>
            a.Clave == origenClave && a.FisicoActual == 7 && a.FisicoNuevo == 12 && a.TransitoActual == 0 && a.TransitoNuevo == 0);
        Assert.Contains(fake.Ajustes, a =>
            a.Clave == destinoClave && a.FisicoActual == 3 && a.FisicoNuevo == 3 && a.TransitoActual == 5 && a.TransitoNuevo == 0);
        Assert.Contains(transiciones, t => t.Clave == origenClave && t.CantidadFisica == 5);
        Assert.Contains(transiciones, t => t.Clave == destinoClave && t.CantidadFisica == 0);
    }

    [Fact]
    public async Task CancelarEnTransito_FallaCerrado_SiTransitoDestinoYaNoAlcanza()
    {
        var transferencia = CrearTransferenciaDespachada();
        var origenClave = new InventarioExistenciaClave(91, 10, 101);
        var destinoClave = new InventarioExistenciaClave(91, 20, 202);
        var origen = CrearExistencia(91, 10, 101, fisico: 7, reservado: 0, transito: 0);
        var destino = CrearExistencia(91, 20, 202, fisico: 3, reservado: 0, transito: 4);
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante>
            {
                [origenClave] = origen,
                [destinoClave] = destino
            },
            TransferenciaInventarioExistenciaContext.ConstruirDemandasBloqueoDespacho(transferencia));

        var ex = await Assert.ThrowsAsync<Solqaryn.Application.Exceptions.BusinessRuleException>(
            () => service.AplicarCancelacionEnTransitoAsync(lockSet, transferencia));

        Assert.Contains("más tránsito", ex.Message, StringComparison.OrdinalIgnoreCase);
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
        detalle.EstablecerCantidadSolicitada(5);
        detalle.AprobarCantidad(5);
        detalle.RegistrarDespacho(5);
        return new TransferenciaInventario
        {
            Id = 31,
            Numero = "TRF-CANCEL-31",
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
        public List<Ajuste> Ajustes { get; } = new();

        public Task<InventarioExistenciaLockSet> BloquearYValidarExistenciasAsync(
            IEnumerable<InventarioDemandaExistencia> demandas,
            bool esDeduccion = true) =>
            Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(), demandas.ToList()));

        public Task<InventarioExistenciaLockSet> BloquearExistenciasParaReversionAsync(
            IEnumerable<InventarioDemandaExistencia> demandas) =>
            Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(), demandas.ToList()));

        public Task AjustarStockFisicoPesimistaAsync(
            InventarioExistenciaClave clave,
            int cantidadActualEsperada,
            int cantidadNueva) => Task.CompletedTask;

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
