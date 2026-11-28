using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaCheckoutPublicFiscalTests
{
    [Fact]
    public async Task Publico_DescuentoNoSeDuplica_ImpuestoExactoPorProducto_Y_EnvioNoGravado()
    {
        var descuentos = new Mock<IDescuentoRepository>(MockBehavior.Strict);
        var impuestos = new Mock<IImpuestoRepository>();
        impuestos.Setup(x => x.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto>
            {
                new() { Id=1, Nombre="Adicional", Codigo="AD", Tipo=TipoImpuesto.Porcentaje, Tasa=15,
                    Productos={ new ImpuestoProducto { ProductoId=1 } }, Prioridad=1 },
                new() { Id=2, Nombre="Incluido", Codigo="INC", Tipo=TipoImpuesto.Porcentaje, Tasa=10,
                    IncluidoEnPrecio=true, Productos={ new ImpuestoProducto { ProductoId=2 } }, Prioridad=2 },
                new() { Id=3, Nombre="Antes", Codigo="ANT", Tipo=TipoImpuesto.Porcentaje, Tasa=5,
                    SeCalculaAntesDescuento=true, Productos={ new ImpuestoProducto { ProductoId=1 } }, Prioridad=3 }
            });
        var servicio = new CalculoService(descuentos.Object, impuestos.Object, Mock.Of<ICostoEnvioRepository>());
        var bruto = new List<DetalleCalculoInput> { new() { ProductoId=1, Cantidad=2, PrecioUnitario=100 }, new() { ProductoId=2, Cantidad=1, PrecioUnitario=110 } };
        var neto = new List<DetalleCalculoInput> { new() { ProductoId=1, Cantidad=2, PrecioUnitario=90 }, new() { ProductoId=2, Cantidad=1, PrecioUnitario=110 } };
        var calculados = await servicio.CalcularImpuestosCheckoutPublicoAsync(bruto, neto, DateTime.UtcNow);
        Assert.Equal(27m, calculados[0].Monto);
        Assert.Equal(180m, calculados[0].BaseImponible);
        Assert.Equal(10m, calculados[1].Monto);
        Assert.True(calculados[1].IncluidoEnPrecio);
        Assert.Equal(10m, calculados[2].Monto);
        Assert.Equal(200m, calculados[2].BaseImponible);
        descuentos.VerifyNoOtherCalls();
        var fiscal = new FiscalCheckoutSnapshot(1, new FiscalCheckoutLinea[] { new(1,null,2,100,90), new(2,null,1,110,110) },
            calculados.Select(i => new FiscalCheckoutImpuesto(i.ImpuestoId,i.Nombre,i.Codigo,i.Tasa,i.BaseImponible,i.Monto,i.IncluidoEnPrecio)).ToArray());
        fiscal.Validar();
        Assert.Equal(310m, fiscal.ImporteProductos);
        Assert.Equal(20m, fiscal.TotalDescuento);
        Assert.Equal(290m, fiscal.Subtotal);
        Assert.Equal(10m, fiscal.ImpuestoIncluido);
        Assert.Equal(37m, fiscal.ImpuestoAdicional);
        Assert.Equal(357m, fiscal.Subtotal + 30m + fiscal.ImpuestoAdicional);
    }

    [Fact]
    public void Pedido_ConservaV6SinFiscal_Y_RechazaSnapshotInconsistente()
    {
        var cliente = new Cliente { Id=1, Nombre="Cliente QA", Activo=true };
        var lineas = new[] { new LineaPedidoCheckout(1,null,"QA",null,null,2,90m) };
        var antiguo = PedidoVenta.CrearDesdeCheckout(cliente,lineas,30,"HNL","Envío","{}",null,true);
        Assert.Null(antiguo.FiscalCheckout);
        Assert.Equal(210m, antiguo.Total);
        var fiscal = new FiscalCheckoutSnapshot(1,new FiscalCheckoutLinea[] { new(1,null,2,100,90) },
            new FiscalCheckoutImpuesto[] { new(1,"ISV","ISV",15,180,27,false) });
        var nuevo = PedidoVenta.CrearDesdeCheckout(cliente,lineas,30,"HNL","Envío","{}",null,true,fiscal);
        Assert.Equal(180m,nuevo.Subtotal);
        Assert.Equal(237m,nuevo.Total);
        Assert.Equal(20m,nuevo.FiscalCheckout!.TotalDescuento);
        Assert.Contains("\"Fiscal\"",nuevo.EntregaCheckoutJson);
        Assert.Throws<InvalidOperationException>(() => PedidoVenta.CrearDesdeCheckout(cliente,lineas,30,"HNL","Envío","{}",null,true,
            fiscal with { Lineas = new FiscalCheckoutLinea[] { new(1,null,2,100,91) } }));
    }

    [Fact]
    public void LegacyRetry_NoAgregaCamposNulosAlFingerprint()
    {
        var dto = new CrearPedidoCheckoutDto { SubtotalEsperado=50m, EnvioEsperado=7m };
        var anterior = System.Text.Json.JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("HuellaFiscalEsperada", anterior);
        Assert.DoesNotContain("TotalEsperado", anterior);
        dto.HuellaFiscalEsperada = new string('a',64);
        dto.TotalEsperado = 64.50m;
        Assert.Contains("HuellaFiscalEsperada", System.Text.Json.JsonSerializer.Serialize(dto));
    }
}
