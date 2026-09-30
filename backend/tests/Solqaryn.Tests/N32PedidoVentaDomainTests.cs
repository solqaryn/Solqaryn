using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N32PedidoVentaDomainTests
{
    private static readonly DateTime FechaBaseUtc = new(2026, 8, 24, 4, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CrearDesdeCotizacion_AceptadaPersistida_CopiaOrigenSnapshotsYDetallesSinMutarla()
    {
        var cotizacion = CrearCotizacionAceptada();

        var pedido = PedidoVenta.CrearDesdeCotizacion(cotizacion);

        Assert.Equal(EstadoPedidoVenta.Borrador, pedido.Estado);
        Assert.True(pedido.EsEditable);
        Assert.Equal(cotizacion.Id, pedido.CotizacionId);
        Assert.Same(cotizacion, pedido.Cotizacion);
        Assert.Equal(cotizacion.ClienteId, pedido.ClienteId);
        Assert.Equal(cotizacion.ClienteNombreSnapshot, pedido.ClienteNombreSnapshot);
        Assert.Equal(cotizacion.ClienteDocumentoSnapshot, pedido.ClienteDocumentoSnapshot);
        Assert.Equal(cotizacion.Observaciones, pedido.Observaciones);
        Assert.Single(pedido.Detalles);
        Assert.Equal(cotizacion.Total, pedido.Total);

        var detalleOrigen = Assert.Single(cotizacion.Detalles);
        var detallePedido = Assert.Single(pedido.Detalles);
        Assert.Equal(detalleOrigen.ProductoId, detallePedido.ProductoId);
        Assert.Equal(detalleOrigen.ProductoVarianteId, detallePedido.ProductoVarianteId);
        Assert.Equal(detalleOrigen.ProductoSkuSnapshot, detallePedido.ProductoSkuSnapshot);
        Assert.Equal(detalleOrigen.ProductoNombreSnapshot, detallePedido.ProductoNombreSnapshot);
        Assert.Equal(detalleOrigen.ProductoMarcaSnapshot, detallePedido.ProductoMarcaSnapshot);
        Assert.Equal(detalleOrigen.ProductoModeloSnapshot, detallePedido.ProductoModeloSnapshot);
        Assert.Equal(detalleOrigen.ProductoColorSnapshot, detallePedido.ProductoColorSnapshot);
        Assert.Equal(detalleOrigen.ProductoTallaSnapshot, detallePedido.ProductoTallaSnapshot);
        Assert.Equal(detalleOrigen.Cantidad, detallePedido.Cantidad);
        Assert.Equal(detalleOrigen.PrecioUnitario, detallePedido.PrecioUnitario);

        Assert.Equal(EstadoCotizacion.Aceptada, cotizacion.Estado);
        Assert.Null(cotizacion.FechaConversionUtc);
        Assert.Null(cotizacion.ConvertidaPorUsuarioId);
    }

    [Fact]
    public void CrearDesdeCotizacion_NulaNoPersistidaONoAceptada_FallaCerrado()
    {
        Assert.Throws<ArgumentNullException>(() => PedidoVenta.CrearDesdeCotizacion(null!));

        var noPersistida = CrearCotizacionAceptada(id: 0);
        Assert.Throws<InvalidOperationException>(() => PedidoVenta.CrearDesdeCotizacion(noPersistida));

        var borrador = CrearCotizacionValida(id: 101);
        Assert.Throws<InvalidOperationException>(() => PedidoVenta.CrearDesdeCotizacion(borrador));
    }

    [Fact]
    public void CotizacionId_NoExponeSetterPublico()
    {
        var propiedad = typeof(PedidoVenta).GetProperty(nameof(PedidoVenta.CotizacionId));

        Assert.NotNull(propiedad);
        var setter = propiedad!.GetSetMethod(nonPublic: true);
        Assert.NotNull(setter);
        Assert.False(setter!.IsPublic);
    }

    [Fact]
    public void Detalles_SeExponenComoColeccionSoloLectura()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        var coleccion = Assert.IsAssignableFrom<ICollection<PedidoVentaDetalle>>(pedido.Detalles);

        Assert.True(coleccion.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => coleccion.Add(new PedidoVentaDetalle()));
    }

    [Fact]
    public void EstablecerIdempotencia_NormalizaYNoPermiteSustitucion()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        var fingerprint = new string('A', 64);

        pedido.EstablecerIdempotencia(" pedido-001 ", fingerprint);
        pedido.EstablecerIdempotencia("pedido-001", fingerprint.ToLowerInvariant());

        Assert.Equal("pedido-001", pedido.IdempotencyKey);
        Assert.Equal(fingerprint.ToLowerInvariant(), pedido.IdempotencyFingerprint);
        Assert.Throws<InvalidOperationException>(() => pedido.EstablecerIdempotencia("pedido-002", fingerprint));
        Assert.Throws<InvalidOperationException>(() => pedido.EstablecerIdempotencia("pedido-001", new string('b', 64)));
    }

    [Fact]
    public void EstablecerIdempotencia_ContratoInvalido_FallaCerrado()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());

        Assert.Throws<ArgumentException>(() => pedido.EstablecerIdempotencia(new string('k', 129), new string('a', 64)));
        Assert.Throws<ArgumentException>(() => pedido.EstablecerIdempotencia("pedido-001", new string('a', 63)));
        Assert.Throws<ArgumentException>(() => pedido.EstablecerIdempotencia("pedido-001", new string('g', 64)));
    }

    [Fact]
    public void Confirmar_BorradorValido_RegistraLifecycleYAuditoriaSinConvertirCotizacion()
    {
        var cotizacion = CrearCotizacionAceptada();
        var pedido = PedidoVenta.CrearDesdeCotizacion(cotizacion);
        var fecha = FechaBaseUtc.AddHours(1);

        pedido.Confirmar(7, " qa ", fecha);

        Assert.Equal(EstadoPedidoVenta.Confirmado, pedido.Estado);
        Assert.True(pedido.EstaConfirmado);
        Assert.False(pedido.EsEditable);
        Assert.Equal(fecha, pedido.FechaConfirmacion);
        Assert.Equal(7, pedido.ConfirmadoPorUsuarioId);
        Assert.Equal("qa", pedido.ConfirmadoPorNombreUsuario);
        Assert.Equal(EstadoCotizacion.Aceptada, cotizacion.Estado);
        Assert.Null(cotizacion.FechaConversionUtc);
    }

    [Fact]
    public void Confirmar_DobleConfirmacion_FallaCerrado()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        pedido.Confirmar(1, "qa", FechaBaseUtc.AddHours(1));

        Assert.Throws<InvalidOperationException>(() =>
            pedido.Confirmar(1, "qa", FechaBaseUtc.AddHours(2)));
    }

    [Fact]
    public void Confirmar_FechaNoUtc_NoCambiaEstadoNiAuditoria()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        var noUtc = new DateTime(2026, 8, 24, 5, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => pedido.Confirmar(1, "qa", noUtc));

        Assert.Equal(EstadoPedidoVenta.Borrador, pedido.Estado);
        Assert.Null(pedido.FechaConfirmacion);
        Assert.Null(pedido.ConfirmadoPorUsuarioId);
    }

    [Fact]
    public void ActualizarObservaciones_DespuesDeConfirmar_FallaCerrado()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        pedido.ActualizarObservaciones("Nueva observación");
        Assert.Equal("Nueva observación", pedido.Observaciones);
        pedido.Confirmar(1, "qa", FechaBaseUtc.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => pedido.ActualizarObservaciones("No permitido"));
        Assert.Equal("Nueva observación", pedido.Observaciones);
    }

    [Fact]
    public void Anular_Confirmado_RegistraMotivoYAuditoria()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        pedido.Confirmar(1, "qa", FechaBaseUtc.AddHours(1));
        var fechaAnulacion = FechaBaseUtc.AddHours(2);

        pedido.Anular(2, " admin ", " Cliente desistió ", fechaAnulacion);

        Assert.Equal(EstadoPedidoVenta.Anulado, pedido.Estado);
        Assert.True(pedido.EstaAnulado);
        Assert.False(pedido.EsEditable);
        Assert.Equal(fechaAnulacion, pedido.FechaAnulacion);
        Assert.Equal(2, pedido.AnuladoPorUsuarioId);
        Assert.Equal("admin", pedido.AnuladoPorNombreUsuario);
        Assert.Equal("Cliente desistió", pedido.MotivoAnulacion);
    }

    [Fact]
    public void Anular_DesdeBorradorODosVeces_FallaCerrado()
    {
        var borrador = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        Assert.Throws<InvalidOperationException>(() =>
            borrador.Anular(1, "qa", "motivo", FechaBaseUtc.AddHours(1)));

        var anulado = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        anulado.Confirmar(1, "qa", FechaBaseUtc.AddHours(1));
        anulado.Anular(2, "qa", "motivo", FechaBaseUtc.AddHours(2));

        Assert.Throws<InvalidOperationException>(() =>
            anulado.Anular(2, "qa", "otro motivo", FechaBaseUtc.AddHours(3)));
        Assert.Throws<InvalidOperationException>(() =>
            anulado.Confirmar(1, "qa", FechaBaseUtc.AddHours(3)));
    }

    [Fact]
    public void Anular_FechaNoUtc_NoCambiaEstadoConfirmado()
    {
        var pedido = PedidoVenta.CrearDesdeCotizacion(CrearCotizacionAceptada());
        pedido.Confirmar(1, "qa", FechaBaseUtc.AddHours(1));
        var noUtc = new DateTime(2026, 8, 24, 6, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => pedido.Anular(2, "qa", "motivo", noUtc));

        Assert.Equal(EstadoPedidoVenta.Confirmado, pedido.Estado);
        Assert.Null(pedido.FechaAnulacion);
        Assert.Null(pedido.AnuladoPorUsuarioId);
    }

    private static Cotizacion CrearCotizacionAceptada(int id = 101)
    {
        var cotizacion = CrearCotizacionValida(id);
        cotizacion.Enviar(1, FechaBaseUtc);
        cotizacion.Aceptar(2, FechaBaseUtc.AddMinutes(1));
        return cotizacion;
    }

    private static Cotizacion CrearCotizacionValida(int id)
    {
        var cotizacion = new Cotizacion
        {
            Id = id,
            ClienteId = 7,
            ClienteNombreSnapshot = "Cliente SA",
            ClienteDocumentoSnapshot = "0801-0000-00000",
            Observaciones = "Entrega prioritaria"
        };

        var detalle = new CotizacionDetalle
        {
            ProductoId = 11,
            ProductoVarianteId = 21,
            ProductoSkuSnapshot = "SKU-001",
            ProductoNombreSnapshot = "Producto",
            ProductoMarcaSnapshot = "Marca",
            ProductoModeloSnapshot = "Modelo",
            ProductoColorSnapshot = "Negro",
            ProductoTallaSnapshot = "M"
        };
        detalle.EstablecerValores(2m, 125.50m);
        cotizacion.Detalles.Add(detalle);

        return cotizacion;
    }
}
