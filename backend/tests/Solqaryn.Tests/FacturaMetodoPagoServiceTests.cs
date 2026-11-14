using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Tests;

public class FacturaMetodoPagoServiceTests
{
    [Fact]
    public async Task RegistrarPagoAsync_Usa_Catalogo_Relacional_Y_Captura_Snapshot_Inmutable()
    {
        var repo = new Mock<IFacturaRepository>();
        var empresa = new Mock<IEmpresaConfiguracionService>();
        var catalogo = new CatalogoMetodoPago { Id = 44, Codigo = "TRANSFERENCIA", Nombre = "Transferencia bancaria" };
        var venta = new Venta
        {
            Id = 10,
            NumeroVenta = "VEN-000010",
            ClienteNombre = "Cliente",
            MetodoPago = MetodoPago.Efectivo,
            MetodoPagoId = 44,
            MetodoPagoCatalogo = catalogo
        };
        var factura = new Factura
        {
            Id = 20,
            VentaId = 10,
            Venta = venta,
            NumeroFactura = "FAC-000020",
            Estado = EstadoFactura.Emitida,
            Total = 100m,
            SaldoPendiente = 100m,
            ClienteNombre = "Cliente",
            EmpresaNombre = "Storefront",
            MetodoPagoCodigoSnapshot = "TRANSFERENCIA",
            MetodoPagoNombreSnapshot = "Transferencia bancaria"
        };
        repo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(factura);
        repo.Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("Transferencia bancaria")).ReturnsAsync(catalogo);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        empresa.Setup(e => e.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracionDto());

        var service = new FacturaService(repo.Object, empresa.Object);
        var dto = await service.RegistrarPagoAsync(20, new RegistrarFacturaPagoDto
        {
            Monto = 25m,
            MetodoPago = "Transferencia bancaria",
            Referencia = "REF-1"
        }, 7, "tester");

        var pago = Assert.Single(factura.Pagos);
        Assert.Equal(44, pago.MetodoPagoId);
        Assert.Same(catalogo, pago.MetodoPagoCatalogo);
        Assert.Equal(MetodoPago.Transferencia, pago.MetodoPago);
        Assert.Equal("TRANSFERENCIA", pago.MetodoPagoCodigoSnapshot);
        Assert.Equal("Transferencia bancaria", pago.MetodoPagoNombreSnapshot);
        Assert.Equal("Transferencia bancaria", Assert.Single(dto.Pagos).MetodoPago);
        Assert.Equal("Transferencia bancaria", dto.MetodoPago);
    }

    [Fact]
    public async Task GetByIdAsync_Renombrar_Catalogo_No_Altera_Factura_Ni_Pago_Historicos()
    {
        var repo = new Mock<IFacturaRepository>();
        var empresa = new Mock<IEmpresaConfiguracionService>();
        var catalogo = new CatalogoMetodoPago
        {
            Id = 44,
            Codigo = "TRANSFERENCIA-NUEVA",
            Nombre = "Nombre renombrado posteriormente"
        };
        var venta = new Venta
        {
            Id = 10,
            NumeroVenta = "VEN-000010",
            ClienteNombre = "Cliente",
            MetodoPago = MetodoPago.Transferencia,
            MetodoPagoId = 44,
            MetodoPagoCatalogo = catalogo,
            EstadoPago = EstadoPago.Parcial
        };
        var factura = new Factura
        {
            Id = 20,
            VentaId = 10,
            Venta = venta,
            NumeroFactura = "FAC-000020",
            Estado = EstadoFactura.ParcialmentePagada,
            Total = 100m,
            ClienteNombre = "Cliente",
            EmpresaNombre = "Storefront",
            MetodoPagoCodigoSnapshot = "TRANSFERENCIA",
            MetodoPagoNombreSnapshot = "Transferencia bancaria",
            Pagos = new List<FacturaPago>
            {
                new()
                {
                    Id = 30,
                    FacturaId = 20,
                    Monto = 25m,
                    MontoRecibido = 25m,
                    MetodoPago = MetodoPago.Transferencia,
                    MetodoPagoId = 44,
                    MetodoPagoCatalogo = catalogo,
                    MetodoPagoCodigoSnapshot = "TRANSFERENCIA",
                    MetodoPagoNombreSnapshot = "Transferencia bancaria"
                }
            }
        };
        repo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(factura);
        empresa.Setup(e => e.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracionDto());

        var service = new FacturaService(repo.Object, empresa.Object);
        var dto = await service.GetByIdAsync(20);

        Assert.NotNull(dto);
        Assert.Equal("Transferencia bancaria", dto!.MetodoPago);
        Assert.Equal("Transferencia bancaria", Assert.Single(dto.Pagos).MetodoPago);
        Assert.DoesNotContain("renombrado", dto.MetodoPago, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarPagoAsync_Metodo_Desconocido_Falla_Sin_Default_Enum()
    {
        var repo = new Mock<IFacturaRepository>();
        var empresa = new Mock<IEmpresaConfiguracionService>();
        var factura = new Factura
        {
            Id = 20,
            VentaId = 10,
            NumeroFactura = "FAC-000020",
            Estado = EstadoFactura.Emitida,
            Total = 100m,
            SaldoPendiente = 100m,
            ClienteNombre = "Cliente",
            EmpresaNombre = "Storefront"
        };
        repo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(factura);
        repo.Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("Cripto"))
            .ReturnsAsync((CatalogoMetodoPago?)null);

        var service = new FacturaService(repo.Object, empresa.Object);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.RegistrarPagoAsync(
            20,
            new RegistrarFacturaPagoDto { Monto = 25m, MetodoPago = "Cripto" },
            7,
            "tester"));

        Assert.Contains("no existe en el catálogo", error.Message);
        Assert.Empty(factura.Pagos);
        repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
