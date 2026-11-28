using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaCheckoutFiscalCalculationTests
{
    [Fact]
    public async Task Checkout_EnvíoAdicional_NoReduceBaseFiscalNiDescuentos()
    {
        var descuentos = new Mock<IDescuentoRepository>();
        descuentos.Setup(x => x.GetVigentesConRelacionesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Descuento> { new() { Id=1, Nombre="Diez por ciento", Tipo=TipoDescuento.Porcentaje, Valor=10, Activo=true } });
        var impuestos = new Mock<IImpuestoRepository>();
        impuestos.Setup(x => x.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto> { new() { Id=2, Nombre="Impuesto QA", Codigo="QA", Tipo=TipoImpuesto.Porcentaje, Tasa=15, Activo=true } });
        var envios = new Mock<ICostoEnvioRepository>();
        envios.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(new CostoEnvio { Id=4, Nombre="Envío", Monto=30, Activo=true });
        var service = new CalculoService(descuentos.Object, impuestos.Object, envios.Object);
        var items = new List<DetalleCalculoInput> { new() { ProductoId=8, Cantidad=2, PrecioUnitario=100 } };

        var tienda = await service.CalcularVentaTiendaAsync(items, 9, 4);
        Assert.Equal(230m, tienda.ImporteBruto);
        Assert.Equal(200m, tienda.ImporteProductos);
        Assert.Equal(20m, tienda.TotalDescuento);
        Assert.Equal(27m, tienda.TotalImpuesto);
        Assert.Equal(30m, tienda.CostoEnvio);
        Assert.Equal(237m, tienda.Total);

        var legacy = await service.CalcularVentaAsync(items, 9, null, null, 4);
        Assert.Equal(200m, legacy.ImporteBruto);
        Assert.Equal(170m, legacy.ImporteProductos);
        Assert.Equal(17m, legacy.TotalDescuento);
        Assert.Equal(22.95m, legacy.TotalImpuesto);
        Assert.Equal(205.95m, legacy.Total);
    }

    [Fact]
    public async Task Checkout_NoAceptaTarifaAusente()
    {
        var service = new CalculoService(Mock.Of<IDescuentoRepository>(), Mock.Of<IImpuestoRepository>(), Mock.Of<ICostoEnvioRepository>());
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CalcularVentaTiendaAsync(new List<DetalleCalculoInput>(), 9, 0));
    }
}
