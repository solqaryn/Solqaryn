using Solqaryn.Domain.Entities;
using Solqaryn.Domain.ValueObjects;
using Xunit;

namespace Solqaryn.Tests;

public class N33ReservaAutomaticaDomainTests
{
    private static readonly DateTime FechaBaseUtc = new(2026, 8, 24, 16, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PrepararReservaAutomatica_AgregaLineasDeLaMismaVariante_YExigeAsignacionExacta()
    {
        var pedido = CrearPedidoPersistido((1, 10m), (1, 5m));

        var contrato = pedido.PrepararReservaAutomatica(new[]
        {
            AsignacionReservaAutomatica.Crear(1, 100, null, 15)
        });

        Assert.Equal(15, contrato.RequerimientosPorVariante[1]);
        Assert.Single(contrato.Asignaciones);
        Assert.Equal(15, contrato.Asignaciones[0].Cantidad);
    }

    [Fact]
    public void PrepararReservaAutomatica_RechazaCantidadFraccionaria()
    {
        var pedido = CrearPedidoPersistido((1, 1.5m));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            pedido.PrepararReservaAutomatica(new[]
            {
                AsignacionReservaAutomatica.Crear(1, 100, null, 1)
            }));

        Assert.Contains("entero positivo representable", ex.Message);
    }

    [Fact]
    public void PrepararReservaAutomatica_RechazaFaltantesExtrasYDuplicadosFisicos()
    {
        var pedido = CrearPedidoPersistido((1, 3m), (2, 2m));

        Assert.Throws<InvalidOperationException>(() =>
            pedido.PrepararReservaAutomatica(new[]
            {
                AsignacionReservaAutomatica.Crear(1, 100, null, 3)
            }));

        Assert.Throws<InvalidOperationException>(() =>
            pedido.PrepararReservaAutomatica(new[]
            {
                AsignacionReservaAutomatica.Crear(1, 100, null, 3),
                AsignacionReservaAutomatica.Crear(2, 100, null, 2),
                AsignacionReservaAutomatica.Crear(99, 100, null, 1)
            }));

        Assert.Throws<InvalidOperationException>(() =>
            pedido.PrepararReservaAutomatica(new[]
            {
                AsignacionReservaAutomatica.Crear(1, 100, null, 1),
                AsignacionReservaAutomatica.Crear(1, 100, null, 2),
                AsignacionReservaAutomatica.Crear(2, 100, null, 2)
            }));
    }

    [Fact]
    public void PrepararReservaAutomatica_RechazaPedidoNoPersistidoONoBorrador()
    {
        var pedidoNoPersistido = CrearPedidoPersistido((1, 1m));
        pedidoNoPersistido.Id = 0;

        Assert.Throws<InvalidOperationException>(() =>
            pedidoNoPersistido.PrepararReservaAutomatica(new[]
            {
                AsignacionReservaAutomatica.Crear(1, 100, null, 1)
            }));

        var pedidoConfirmado = CrearPedidoPersistido((1, 1m));
        pedidoConfirmado.Confirmar(7, "qa", FechaBaseUtc);

        Assert.Throws<InvalidOperationException>(() =>
            pedidoConfirmado.PrepararReservaAutomatica(new[]
            {
                AsignacionReservaAutomatica.Crear(1, 100, null, 1)
            }));
    }

    private static PedidoVenta CrearPedidoPersistido(params (int VarianteId, decimal Cantidad)[] lineas)
    {
        var cotizacion = new Cotizacion
        {
            Id = 900,
            ClienteId = 7,
            ClienteNombreSnapshot = "Cliente QA"
        };

        var indice = 0;
        foreach (var linea in lineas)
        {
            var detalle = new CotizacionDetalle
            {
                ProductoId = 100 + indice,
                ProductoVarianteId = linea.VarianteId,
                ProductoSkuSnapshot = $"SKU-{linea.VarianteId}-{indice}",
                ProductoNombreSnapshot = "Producto"
            };
            detalle.EstablecerValores(linea.Cantidad, 10m);
            cotizacion.Detalles.Add(detalle);
            indice++;
        }

        cotizacion.Enviar(1, FechaBaseUtc.AddMinutes(-2));
        cotizacion.Aceptar(2, FechaBaseUtc.AddMinutes(-1));

        var pedido = PedidoVenta.CrearDesdeCotizacion(cotizacion);
        pedido.Id = 1000;
        return pedido;
    }
}
