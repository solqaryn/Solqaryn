using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N22OrdenCompraDomainContractTests
{
    [Fact]
    public void EstadoOrdenCompra_MantieneLifecycleIndependiente()
    {
        Assert.Equal(1, (int)EstadoOrdenCompra.Borrador);
        Assert.Equal(2, (int)EstadoOrdenCompra.PendienteAprobacion);
        Assert.Equal(3, (int)EstadoOrdenCompra.Aprobada);
        Assert.Equal(4, (int)EstadoOrdenCompra.Cancelada);
    }

    [Fact]
    public void Agregado_NoHeredaSemanticaConfirmableDeCompra()
    {
        Assert.Equal("AuditableEntity", typeof(OrdenCompra).BaseType?.Name);
        Assert.NotEqual("ConfirmableEntity", typeof(OrdenCompra).BaseType?.Name);
    }

    [Fact]
    public void Lifecycle_ApruebaDocumentoValidoYSellaMutabilidad()
    {
        var orden = CrearOrdenValida();
        var ahora = DateTime.UtcNow;

        orden.EnviarAprobacion(10, ahora);
        Assert.Equal(EstadoOrdenCompra.PendienteAprobacion, orden.Estado);
        Assert.False(orden.EsEditable);

        orden.Aprobar(20, "Aprobador", ahora.AddMinutes(1));

        Assert.Equal(EstadoOrdenCompra.Aprobada, orden.Estado);
        Assert.Equal(20, orden.AprobadaPorUsuarioId);
        Assert.Throws<InvalidOperationException>(() => orden.AsegurarEditable());
    }

    [Fact]
    public void Totales_SeDerivanDeDetallesSinEfectosDeInventario()
    {
        var orden = CrearOrdenValida();

        Assert.Equal(20m, orden.Subtotal);
        Assert.Equal(2m, orden.Descuento);
        Assert.Equal(3m, orden.Impuesto);
        Assert.Equal(21m, orden.Total);

        var propiedades = typeof(OrdenCompra).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stock", propiedades);
        Assert.DoesNotContain("Kardex", propiedades);
        Assert.DoesNotContain("MovimientoFinanciero", propiedades);
        Assert.DoesNotContain("EstadoPago", propiedades);
    }

    [Fact]
    public void Contrato_CubreOrigenProveedorCondicionesMonedaFechaYDetalles()
    {
        var dto = new CreateOrdenCompraDto
        {
            SolicitudCompraId = 8,
            ProveedorId = 12,
            Moneda = "USD",
            CondicionesCompra = "Crédito 30 días",
            FechaEsperadaUtc = DateTime.UtcNow.AddDays(7),
            Detalles =
            {
                new OrdenCompraDetalleInputDto
                {
                    ProductoId = 4,
                    ProductoVarianteId = 9,
                    CantidadOrdenada = 2,
                    PrecioUnitario = 10,
                    Descuento = 2,
                    Impuesto = 3
                }
            }
        };

        Assert.Equal(8, dto.SolicitudCompraId);
        Assert.Equal(12, dto.ProveedorId);
        Assert.Equal("USD", dto.Moneda);
        Assert.Single(dto.Detalles);
    }

    [Fact]
    public void Detalle_RechazaDescuentoMayorAlSubtotal()
    {
        var detalle = new OrdenCompraDetalle { ProductoId = 1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => detalle.EstablecerValores(1, 10, 11, 0));
    }

    private static OrdenCompra CrearOrdenValida()
    {
        var detalle = new OrdenCompraDetalle { ProductoId = 1 };
        detalle.EstablecerValores(2, 10, 2, 3);

        return new OrdenCompra
        {
            NumeroOrden = "OC-2026-0001",
            ProveedorId = 1,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            Detalles = { detalle }
        };
    }
}
