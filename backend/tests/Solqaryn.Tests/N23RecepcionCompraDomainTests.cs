using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N23RecepcionCompraDomainTests
{
    [Fact]
    public void CantidadAceptada_ExcluyeDanadosYSobrantes()
    {
        var detalle = CrearDetalle();

        detalle.EstablecerCantidades(cantidadRecibida: 12m, cantidadDanada: 1m, cantidadFaltante: 0m, cantidadSobrante: 2m);

        Assert.Equal(9m, detalle.CantidadAceptada);
        Assert.Equal(12m, detalle.CantidadRecibida);
        Assert.Equal(1m, detalle.CantidadDanada);
        Assert.Equal(2m, detalle.CantidadSobrante);
    }

    [Fact]
    public void EstablecerCantidades_DanadoMasSobranteSuperaRecibido_FallaCerradoSinMutar()
    {
        var detalle = CrearDetalle();
        detalle.EstablecerCantidades(10m, 1m, 0m, 1m);

        Assert.Throws<InvalidOperationException>(() =>
            detalle.EstablecerCantidades(cantidadRecibida: 10m, cantidadDanada: 6m, cantidadFaltante: 0m, cantidadSobrante: 5m));

        Assert.Equal(10m, detalle.CantidadRecibida);
        Assert.Equal(1m, detalle.CantidadDanada);
        Assert.Equal(0m, detalle.CantidadFaltante);
        Assert.Equal(1m, detalle.CantidadSobrante);
        Assert.Equal(8m, detalle.CantidadAceptada);
    }

    [Fact]
    public void EstablecerCantidades_SoloFaltante_PreservaStockAceptadoEnCero()
    {
        var detalle = CrearDetalle();

        detalle.EstablecerCantidades(cantidadRecibida: 0m, cantidadDanada: 0m, cantidadFaltante: 4m, cantidadSobrante: 0m);

        Assert.True(detalle.TieneActividadFisica);
        Assert.Equal(0m, detalle.CantidadAceptada);
    }

    [Fact]
    public void EstablecerIdempotencia_FingerprintNoHexadecimal_FallaCerradoSinMutar()
    {
        var recepcion = CrearRecepcion();
        var fingerprintValido = new string('a', 64);
        recepcion.EstablecerIdempotencia("recepcion-1", fingerprintValido);

        var error = Assert.Throws<ArgumentException>(() =>
            recepcion.EstablecerIdempotencia("recepcion-1", new string('g', 64)));

        Assert.Contains("SHA-256", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("recepcion-1", recepcion.IdempotencyKey);
        Assert.Equal(fingerprintValido, recepcion.IdempotencyFingerprint);
    }

    [Fact]
    public void EstablecerIdempotencia_NormalizaFingerprintHexadecimal()
    {
        var recepcion = CrearRecepcion();
        var fingerprint = new string('A', 64);

        recepcion.EstablecerIdempotencia("  recepcion-1  ", fingerprint);

        Assert.Equal("recepcion-1", recepcion.IdempotencyKey);
        Assert.Equal(new string('a', 64), recepcion.IdempotencyFingerprint);
    }

    [Fact]
    public void Confirmar_ConDetalleInvalido_FallaCerradoYSinMutarAuditoria()
    {
        var recepcion = CrearRecepcion();
        var detalle = Assert.Single(recepcion.Detalles);
        detalle.AlmacenId = 0;

        var fechaAntes = recepcion.FechaRecepcionUtc;
        var usuarioAntes = recepcion.RecibidaPorUsuarioId;

        Assert.Throws<InvalidOperationException>(() => recepcion.Confirmar(7, "Usuario QA", DateTime.UtcNow));

        Assert.Equal(EstadoRecepcionCompra.Borrador, recepcion.Estado);
        Assert.Equal(fechaAntes, recepcion.FechaRecepcionUtc);
        Assert.Equal(usuarioAntes, recepcion.RecibidaPorUsuarioId);
    }

    [Fact]
    public void Confirmar_ConDetalleValido_MaterializaSoloEstadoDocumental()
    {
        var recepcion = CrearRecepcion();
        var fecha = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

        recepcion.Confirmar(7, "  Usuario QA  ", fecha);

        Assert.Equal(EstadoRecepcionCompra.Recibida, recepcion.Estado);
        Assert.Equal(fecha, recepcion.FechaRecepcionUtc);
        Assert.Equal(7, recepcion.RecibidaPorUsuarioId);
        Assert.Equal("Usuario QA", recepcion.RecibidaPorNombreSnapshot);
        Assert.Equal(4m, recepcion.CantidadAceptadaTotal);
    }

    [Fact]
    public void ValidarDocumento_ClaveFisicaDuplicada_FallaCerrado()
    {
        var recepcion = CrearRecepcion();
        var detalleDuplicado = CrearDetalle();
        detalleDuplicado.EstablecerCantidades(cantidadRecibida: 1m);
        recepcion.Detalles.Add(detalleDuplicado);

        var error = Assert.Throws<InvalidOperationException>(() => recepcion.ValidarDocumento());

        Assert.Contains("clave física", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoRecepcionCompra.Borrador, recepcion.Estado);
    }

    private static RecepcionCompra CrearRecepcion()
    {
        var detalle = CrearDetalle();
        detalle.EstablecerCantidades(cantidadRecibida: 5m, cantidadDanada: 1m, cantidadFaltante: 0m, cantidadSobrante: 0m);

        return new RecepcionCompra
        {
            NumeroRecepcion = "RC-0001",
            OrdenCompraId = 10,
            Detalles = new List<RecepcionCompraDetalle> { detalle }
        };
    }

    private static RecepcionCompraDetalle CrearDetalle()
    {
        return new RecepcionCompraDetalle
        {
            OrdenCompraDetalleId = 100,
            ProductoId = 200,
            ProductoVarianteId = 300,
            AlmacenId = 400,
            UbicacionAlmacenId = 500,
            CostoUnitarioSnapshot = 25m,
            ProductoSkuSnapshot = "SKU-001",
            ProductoNombreSnapshot = "Producto QA"
        };
    }
}