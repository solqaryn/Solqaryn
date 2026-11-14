using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;
using Xunit;

namespace Solqaryn.Tests;

public class N24FacturaProveedorDomainTests
{
    [Fact]
    public void Montos_consideran_descuento_y_normalizan_a_dos_decimales()
    {
        var montos = FacturaProveedorMontos.Crear(100.005m, 10.001m, 15.004m, 105.01m);

        Assert.Equal(100.01m, montos.Subtotal);
        Assert.Equal(10.00m, montos.Descuento);
        Assert.Equal(15.00m, montos.Impuesto);
        Assert.Equal(105.01m, montos.Total);
    }

    [Fact]
    public void Montos_rechazan_relacion_aritmetica_incoherente()
    {
        Assert.Throws<ArgumentException>(() =>
            FacturaProveedorMontos.Crear(100m, 10m, 15m, 106m));
    }

    [Fact]
    public void Registrar_y_anular_respetan_lifecycle_documental()
    {
        var factura = CrearFacturaValida();
        var registroUtc = new DateTime(2026, 8, 20, 3, 0, 0, DateTimeKind.Utc);
        var anulacionUtc = registroUtc.AddHours(1);

        factura.Registrar(7, "Analista Compras", registroUtc);

        Assert.Equal(EstadoFacturaProveedor.Registrada, factura.Estado);
        Assert.Equal(registroUtc, factura.FechaRegistroUtc);
        Assert.Equal(7, factura.RegistradaPorUsuarioId);
        Assert.Equal("Analista Compras", factura.RegistradaPorNombreSnapshot);
        Assert.Equal(105m, factura.Montos.Total);

        factura.Anular(9, "Documento duplicado", anulacionUtc);

        Assert.Equal(EstadoFacturaProveedor.Anulada, factura.Estado);
        Assert.Equal(anulacionUtc, factura.FechaAnulacionUtc);
        Assert.Equal(9, factura.AnuladaPorUsuarioId);
        Assert.Equal("Documento duplicado", factura.MotivoAnulacion);
    }

    [Fact]
    public void Registrar_sin_detalles_falla_cerrado_sin_mutar_estado()
    {
        var factura = new FacturaProveedor
        {
            NumeroFactura = "FAC-001",
            ProveedorId = 10,
            OrdenCompraId = 20,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            FechaEmisionUtc = new DateTime(2026, 8, 20, 3, 0, 0, DateTimeKind.Utc)
        };

        Assert.Throws<InvalidOperationException>(() =>
            factura.Registrar(7, "Analista", factura.FechaEmisionUtc));

        Assert.Equal(EstadoFacturaProveedor.Borrador, factura.Estado);
        Assert.Null(factura.FechaRegistroUtc);
        Assert.Null(factura.RegistradaPorUsuarioId);
    }

    private static FacturaProveedor CrearFacturaValida()
    {
        var detalle = new FacturaProveedorDetalle
        {
            OrdenCompraDetalleId = 30,
            ProductoId = 40,
            ProductoNombreSnapshot = "Producto"
        };
        detalle.EstablecerValores(1m, 100m, 10m, 15m);

        return new FacturaProveedor
        {
            NumeroFactura = "FAC-001",
            ProveedorId = 10,
            OrdenCompraId = 20,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            FechaEmisionUtc = new DateTime(2026, 8, 20, 3, 0, 0, DateTimeKind.Utc),
            Detalles = new List<FacturaProveedorDetalle> { detalle }
        };
    }
}
