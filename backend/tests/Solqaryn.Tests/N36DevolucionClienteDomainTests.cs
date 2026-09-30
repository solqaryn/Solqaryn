using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N36DevolucionClienteDomainTests
{
    private static readonly DateTime FechaBaseUtc = new(2026, 8, 25, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CrearDesdeVenta_ConfirmadaPersistida_ConFacturaElegible_PreservaOrigenSinMutarlo()
    {
        var venta = CrearVentaConfirmada();
        var factura = CrearFactura(venta.Id, EstadoFactura.Emitida);

        var devolucion = DevolucionCliente.CrearDesdeVenta(venta, factura);

        Assert.Equal(venta.Id, devolucion.VentaId);
        Assert.Same(venta, devolucion.Venta);
        Assert.Equal(factura.Id, devolucion.FacturaId);
        Assert.Same(factura, devolucion.Factura);
        Assert.Equal(EstadoDevolucionCliente.Borrador, devolucion.Estado);
        Assert.True(devolucion.EsEditable);
        Assert.Equal(EstadoDocumento.Confirmada, venta.Estado);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
    }

    [Fact]
    public void CrearDesdeVenta_OrigenNoElegible_FallaCerrado()
    {
        Assert.Throws<ArgumentNullException>(() => DevolucionCliente.CrearDesdeVenta(null!));

        var noPersistida = CrearVentaConfirmada(id: 0);
        Assert.Throws<InvalidOperationException>(() => DevolucionCliente.CrearDesdeVenta(noPersistida));

        var borrador = CrearVentaConfirmada();
        borrador.Estado = EstadoDocumento.Borrador;
        Assert.Throws<InvalidOperationException>(() => DevolucionCliente.CrearDesdeVenta(borrador));

        var eliminada = CrearVentaConfirmada();
        eliminada.Eliminado = true;
        Assert.Throws<InvalidOperationException>(() => DevolucionCliente.CrearDesdeVenta(eliminada));
    }

    [Theory]
    [InlineData(EstadoFactura.Borrador)]
    [InlineData(EstadoFactura.Anulada)]
    [InlineData(EstadoFactura.Cancelada)]
    public void CrearDesdeVenta_FacturaNoElegible_FallaCerrado(EstadoFactura estado)
    {
        var venta = CrearVentaConfirmada();
        var factura = CrearFactura(venta.Id, estado);

        Assert.Throws<InvalidOperationException>(() => DevolucionCliente.CrearDesdeVenta(venta, factura));
    }

    [Fact]
    public void CrearDesdeVenta_FacturaDeOtraVenta_FallaCerrado()
    {
        var venta = CrearVentaConfirmada();
        var factura = CrearFactura(venta.Id + 1, EstadoFactura.Emitida);

        Assert.Throws<InvalidOperationException>(() => DevolucionCliente.CrearDesdeVenta(venta, factura));
    }

    [Fact]
    public void AgregarDetalle_ParcialValido_CopiaSnapshotsYDefineResolucionSinEfectosLaterales()
    {
        var venta = CrearVentaConfirmada();
        var detalle = CrearDetalleVenta(venta.Id, cantidad: 5, precioUnitario: 120m);
        venta.Detalles.Add(detalle);
        var devolucion = DevolucionCliente.CrearDesdeVenta(venta);

        devolucion.AgregarDetalle(detalle, cantidad: 2, cantidadYaDevuelta: 1, TipoResolucionDevolucionCliente.Reintegro);

        var devuelto = Assert.Single(devolucion.Detalles);
        Assert.Equal(detalle.Id, devuelto.VentaDetalleId);
        Assert.Equal(detalle.ProductoId, devuelto.ProductoId);
        Assert.Equal(detalle.ProductoVarianteId, devuelto.ProductoVarianteId);
        Assert.Equal(2, devuelto.Cantidad);
        Assert.Equal(5, devuelto.CantidadVendidaSnapshot);
        Assert.Equal(120m, devuelto.PrecioUnitarioSnapshot);
        Assert.Equal(TipoResolucionDevolucionCliente.Reintegro, devuelto.Resolucion);
        Assert.Equal(240m, devuelto.MontoReferencia);
        Assert.Equal(240m, devolucion.MontoReferencia);
        Assert.Equal(5, detalle.Cantidad);
        Assert.Equal(EstadoDocumento.Confirmada, venta.Estado);
    }

    [Fact]
    public void AgregarDetalle_SobredevolucionODuplicado_FallaCerrado()
    {
        var venta = CrearVentaConfirmada();
        var detalle = CrearDetalleVenta(venta.Id, cantidad: 3, precioUnitario: 50m);
        venta.Detalles.Add(detalle);
        var devolucion = DevolucionCliente.CrearDesdeVenta(venta);

        Assert.Throws<InvalidOperationException>(() =>
            devolucion.AgregarDetalle(detalle, cantidad: 2, cantidadYaDevuelta: 2, TipoResolucionDevolucionCliente.Cambio));

        devolucion.AgregarDetalle(detalle, cantidad: 1, cantidadYaDevuelta: 0, TipoResolucionDevolucionCliente.Cambio);
        Assert.Throws<InvalidOperationException>(() =>
            devolucion.AgregarDetalle(detalle, cantidad: 1, cantidadYaDevuelta: 0, TipoResolucionDevolucionCliente.CreditoAFavor));
    }

    [Fact]
    public void AgregarDetalle_DeOtraVenta_FallaCerrado()
    {
        var venta = CrearVentaConfirmada();
        var detalle = CrearDetalleVenta(venta.Id + 1, cantidad: 2, precioUnitario: 80m);
        var devolucion = DevolucionCliente.CrearDesdeVenta(venta);

        Assert.Throws<InvalidOperationException>(() =>
            devolucion.AgregarDetalle(detalle, 1, 0, TipoResolucionDevolucionCliente.Reintegro));
    }

    [Fact]
    public void EstablecerIdempotencia_NormalizaYNoPermiteSustitucion()
    {
        var devolucion = DevolucionCliente.CrearDesdeVenta(CrearVentaConfirmada());
        var fingerprint = new string('A', 64);

        devolucion.EstablecerIdempotencia(" devolucion-001 ", fingerprint);
        devolucion.EstablecerIdempotencia("devolucion-001", fingerprint.ToLowerInvariant());

        Assert.Equal("devolucion-001", devolucion.IdempotencyKey);
        Assert.Equal(fingerprint.ToLowerInvariant(), devolucion.IdempotencyFingerprint);
        Assert.Throws<InvalidOperationException>(() => devolucion.EstablecerIdempotencia("devolucion-002", fingerprint));
        Assert.Throws<InvalidOperationException>(() => devolucion.EstablecerIdempotencia("devolucion-001", new string('b', 64)));
    }

    [Fact]
    public void Confirmar_SinDetalleOSinIdempotencia_FallaCerrado()
    {
        var venta = CrearVentaConfirmada();
        var detalle = CrearDetalleVenta(venta.Id, cantidad: 2, precioUnitario: 100m);

        var sinDetalle = DevolucionCliente.CrearDesdeVenta(venta);
        sinDetalle.EstablecerIdempotencia("dev-1", new string('a', 64));
        Assert.Throws<InvalidOperationException>(() => sinDetalle.Confirmar(1, "qa", FechaBaseUtc));

        var sinIdempotencia = DevolucionCliente.CrearDesdeVenta(venta);
        sinIdempotencia.AgregarDetalle(detalle, 1, 0, TipoResolucionDevolucionCliente.Reintegro);
        Assert.Throws<InvalidOperationException>(() => sinIdempotencia.Confirmar(1, "qa", FechaBaseUtc));
    }

    [Fact]
    public void Confirmar_ContratoValido_RegistraLifecycleSinMutarVentaNiFactura()
    {
        var venta = CrearVentaConfirmada();
        var factura = CrearFactura(venta.Id, EstadoFactura.Pagada);
        var detalle = CrearDetalleVenta(venta.Id, cantidad: 4, precioUnitario: 75m);
        var devolucion = DevolucionCliente.CrearDesdeVenta(venta, factura);
        devolucion.AgregarDetalle(detalle, 4, 0, TipoResolucionDevolucionCliente.CreditoAFavor);
        devolucion.EstablecerIdempotencia("dev-total-1", new string('c', 64));

        devolucion.Confirmar(7, " qa ", FechaBaseUtc);

        Assert.Equal(EstadoDevolucionCliente.Confirmada, devolucion.Estado);
        Assert.True(devolucion.EstaConfirmada);
        Assert.False(devolucion.EsEditable);
        Assert.Equal(FechaBaseUtc, devolucion.FechaConfirmacion);
        Assert.Equal(7, devolucion.ConfirmadoPorUsuarioId);
        Assert.Equal("qa", devolucion.ConfirmadoPorNombreUsuario);
        Assert.Equal(EstadoDocumento.Confirmada, venta.Estado);
        Assert.Equal(EstadoFactura.Pagada, factura.Estado);
        Assert.Equal(0m, factura.TotalPagado);
    }

    [Fact]
    public void Anular_Confirmada_RegistraAuditoriaSinEjecutarEfectosFisicosOFinancieros()
    {
        var venta = CrearVentaConfirmada();
        var factura = CrearFactura(venta.Id, EstadoFactura.Emitida);
        var detalle = CrearDetalleVenta(venta.Id, cantidad: 2, precioUnitario: 90m);
        var devolucion = DevolucionCliente.CrearDesdeVenta(venta, factura);
        devolucion.AgregarDetalle(detalle, 1, 0, TipoResolucionDevolucionCliente.Cambio);
        devolucion.EstablecerIdempotencia("dev-2", new string('d', 64));
        devolucion.Confirmar(1, "qa", FechaBaseUtc);
        var fechaAnulacion = FechaBaseUtc.AddMinutes(10);

        devolucion.Anular(2, " admin ", " Cliente desistió ", fechaAnulacion);

        Assert.Equal(EstadoDevolucionCliente.Anulada, devolucion.Estado);
        Assert.True(devolucion.EstaAnulada);
        Assert.Equal(fechaAnulacion, devolucion.FechaAnulacion);
        Assert.Equal(2, devolucion.AnuladoPorUsuarioId);
        Assert.Equal("admin", devolucion.AnuladoPorNombreUsuario);
        Assert.Equal("Cliente desistió", devolucion.MotivoAnulacion);
        Assert.Equal(EstadoDocumento.Confirmada, venta.Estado);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
        Assert.Equal(2, detalle.Cantidad);
    }

    [Fact]
    public void ConfirmarOAnular_TransicionInvalida_FallaCerrado()
    {
        var venta = CrearVentaConfirmada();
        var detalle = CrearDetalleVenta(venta.Id, cantidad: 2, precioUnitario: 90m);
        var devolucion = DevolucionCliente.CrearDesdeVenta(venta);

        Assert.Throws<InvalidOperationException>(() =>
            devolucion.Anular(1, "qa", "motivo", FechaBaseUtc));

        devolucion.AgregarDetalle(detalle, 1, 0, TipoResolucionDevolucionCliente.Reintegro);
        devolucion.EstablecerIdempotencia("dev-3", new string('e', 64));
        devolucion.Confirmar(1, "qa", FechaBaseUtc);

        Assert.Throws<InvalidOperationException>(() => devolucion.Confirmar(1, "qa", FechaBaseUtc.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() =>
            devolucion.AgregarDetalle(detalle, 1, 0, TipoResolucionDevolucionCliente.Cambio));
    }

    private static Venta CrearVentaConfirmada(int id = 501) => new()
    {
        Id = id,
        NumeroVenta = "V-501",
        Estado = EstadoDocumento.Confirmada,
        ClienteNombre = "Cliente",
        Total = 500m
    };

    private static Factura CrearFactura(int ventaId, EstadoFactura estado) => new()
    {
        Id = 601,
        VentaId = ventaId,
        NumeroFactura = "F-601",
        Estado = estado,
        ClienteNombre = "Cliente",
        EmpresaNombre = "Storefront",
        Total = 500m
    };

    private static VentaDetalle CrearDetalleVenta(int ventaId, int cantidad, decimal precioUnitario) => new()
    {
        Id = 701,
        VentaId = ventaId,
        ProductoId = 11,
        ProductoVarianteId = 21,
        Cantidad = cantidad,
        PrecioUnitario = precioUnitario,
        ProductoSkuSnapshot = "SKU-001",
        ProductoNombreSnapshot = "Producto",
        ProductoMarcaSnapshot = "Marca",
        ProductoModeloSnapshot = "Modelo",
        ProductoColorSnapshot = "Negro",
        ProductoTallaSnapshot = "M"
    };
}
