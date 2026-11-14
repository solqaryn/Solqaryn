using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N26DevolucionProveedorDomainTests
{
    [Fact]
    public void Confirmar_y_anular_respetan_lifecycle_y_montos_credito()
    {
        var devolucion = CrearValida();
        var confirmacionUtc = new DateTime(2026, 8, 21, 16, 30, 0, DateTimeKind.Utc);
        var anulacionUtc = confirmacionUtc.AddHours(1);

        devolucion.Confirmar(7, "Compras", confirmacionUtc);

        Assert.Equal(EstadoDevolucionProveedor.Confirmada, devolucion.Estado);
        Assert.Equal(confirmacionUtc, devolucion.FechaConfirmacionUtc);
        Assert.Equal(7, devolucion.ConfirmadaPorUsuarioId);
        Assert.Equal(200m, devolucion.SubtotalCredito);
        Assert.Equal(30m, devolucion.ImpuestoCredito);
        Assert.Equal(230m, devolucion.TotalCredito);

        devolucion.Anular(9, "Retorno cancelado", anulacionUtc);

        Assert.Equal(EstadoDevolucionProveedor.Anulada, devolucion.Estado);
        Assert.Equal(anulacionUtc, devolucion.FechaAnulacionUtc);
        Assert.Equal(9, devolucion.AnuladaPorUsuarioId);
        Assert.Equal("Retorno cancelado", devolucion.MotivoAnulacion);
    }

    [Fact]
    public void Confirmar_con_linea_de_recepcion_duplicada_falla_cerrado()
    {
        var devolucion = CrearValida();
        devolucion.Detalles.Add(new DevolucionProveedorDetalle
        {
            RecepcionCompraDetalleId = 30,
            OrdenCompraDetalleId = 40,
            ProductoId = 50,
            AlmacenId = 60,
            Cantidad = 1m,
            CostoUnitarioSnapshot = 100m,
            ProductoNombreSnapshot = "Producto"
        });

        Assert.Throws<InvalidOperationException>(() =>
            devolucion.Confirmar(7, "Compras", DateTime.UtcNow));

        Assert.Equal(EstadoDevolucionProveedor.Borrador, devolucion.Estado);
        Assert.Null(devolucion.FechaConfirmacionUtc);
        Assert.Null(devolucion.ConfirmadaPorUsuarioId);
    }

    [Fact]
    public void Detalle_con_cantidad_invalida_falla_sin_mutar_lifecycle()
    {
        var devolucion = CrearValida();
        devolucion.Detalles.Single().Cantidad = 0m;

        Assert.Throws<InvalidOperationException>(() =>
            devolucion.Confirmar(7, "Compras", DateTime.UtcNow));

        Assert.Equal(EstadoDevolucionProveedor.Borrador, devolucion.Estado);
    }

    [Fact]
    public void Idempotencia_es_inmutable_y_exige_sha256_hexadecimal()
    {
        var devolucion = CrearValida();
        var fingerprint = new string('a', 64);

        devolucion.EstablecerIdempotencia("return-001", fingerprint);
        devolucion.EstablecerIdempotencia("return-001", fingerprint.ToUpperInvariant());

        Assert.Equal("return-001", devolucion.IdempotencyKey);
        Assert.Equal(fingerprint, devolucion.IdempotencyFingerprint);
        Assert.Throws<InvalidOperationException>(() => devolucion.EstablecerIdempotencia("return-002", fingerprint));
        Assert.Throws<ArgumentException>(() => devolucion.EstablecerIdempotencia("return-001", "xyz"));
    }

    [Fact]
    public void Query_rechaza_rango_temporal_invertido()
    {
        var query = new DevolucionProveedorQueryDto
        {
            DesdeUtc = new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc),
            HastaUtc = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc)
        };

        var errores = query.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(query)).ToList();

        Assert.Single(errores);
    }

    private static DevolucionProveedor CrearValida()
    {
        return new DevolucionProveedor
        {
            NumeroDevolucion = "DEV-001",
            ProveedorId = 10,
            OrdenCompraId = 20,
            RecepcionCompraId = 21,
            FacturaProveedorId = 22,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            Motivo = "Producto defectuoso",
            Detalles = new List<DevolucionProveedorDetalle>
            {
                new()
                {
                    RecepcionCompraDetalleId = 30,
                    OrdenCompraDetalleId = 40,
                    ProductoId = 50,
                    ProductoVarianteId = 51,
                    AlmacenId = 60,
                    UbicacionAlmacenId = 61,
                    Cantidad = 2m,
                    CostoUnitarioSnapshot = 100m,
                    ImpuestoUnitarioSnapshot = 15m,
                    ProductoNombreSnapshot = "Producto"
                }
            }
        };
    }
}
