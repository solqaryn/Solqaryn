using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;
using CatalogoBanco = Solqaryn.Domain.Entities.Catalogos.Banco;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Tests;

public class FacturaServicePagosTests
{
    [Fact]
    public async Task RegistrarPagoParcial_ActualizaSaldoYEstado()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura);

        var resultado = await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = 100m, MetodoPago = "Transferencia" },
            7,
            "tester");

        Assert.Equal(100m, resultado.TotalPagado);
        Assert.Equal(200m, resultado.SaldoPendiente);
        Assert.Equal("ParcialmentePagada", resultado.Estado);
        Assert.Equal(100m, factura.Pagos.Single().MontoRecibido);
        Assert.Equal(0m, factura.Pagos.Single().Cambio);
        Assert.Equal(EstadoPago.Parcial, factura.Venta!.EstadoPago);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarPagoCompleto_MarcaFacturaPagada()
    {
        var factura = CrearFactura(300m);
        var (service, _) = CrearServicio(factura);

        var resultado = await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = 300m, MetodoPago = "Efectivo" },
            7,
            "tester");

        Assert.Equal(300m, resultado.TotalPagado);
        Assert.Equal(0m, resultado.SaldoPendiente);
        Assert.Equal("Pagada", resultado.Estado);
        Assert.Equal(EstadoPago.Pagado, factura.Venta!.EstadoPago);
    }

    [Fact]
    public async Task RegistrarPagoSuperiorAlSaldo_SiMetodoNoPermiteCambio_EsRechazado()
    {
        var factura = CrearFactura(300m);
        var (service, _) = CrearServicio(factura);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegistrarPagoAsync(
                factura.Id,
                new RegistrarFacturaPagoDto { Monto = 300.01m, MetodoPago = "Efectivo" },
                7,
                "tester"));

        Assert.Contains("saldo pendiente", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factura.Pagos);
    }

    [Fact]
    public async Task RegistrarPagoSuperiorAlSaldo_SiMetodoPermiteCambio_AplicaSoloSaldoYRegistraCambio()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura, efectivoPermiteCambio: true);

        var resultado = await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = 350m, MetodoPago = "Efectivo" },
            7,
            "tester");

        var pago = Assert.Single(factura.Pagos);
        Assert.Equal(300m, pago.Monto);
        Assert.Equal(350m, pago.MontoRecibido);
        Assert.Equal(50m, pago.Cambio);
        Assert.Equal(300m, resultado.TotalPagado);
        Assert.Equal(0m, resultado.SaldoPendiente);
        Assert.Equal(300m, resultado.Pagos.Single().Monto);
        Assert.Equal(350m, resultado.Pagos.Single().MontoRecibido);
        Assert.Equal(50m, resultado.Pagos.Single().Cambio);
        Assert.Equal("Pagada", resultado.Estado);
        Assert.Equal(EstadoPago.Pagado, factura.Venta!.EstadoPago);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarPago_SinReferenciaCuandoMetodoLaRequiere_EsRechazado()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura, transferenciaRequiereReferencia: true);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegistrarPagoAsync(
                factura.Id,
                new RegistrarFacturaPagoDto { Monto = 100m, MetodoPago = "Transferencia", Referencia = "   " },
                7,
                "tester"));

        Assert.Contains("referencia", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factura.Pagos);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarPago_ConReferenciaCuandoMetodoLaRequiere_SeRegistraNormalizada()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura, transferenciaRequiereReferencia: true);

        var resultado = await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto
            {
                Monto = 100m,
                MetodoPago = "Transferencia",
                Referencia = "  TRX-001  "
            },
            7,
            "tester");

        Assert.Equal(100m, resultado.TotalPagado);
        Assert.Equal("TRX-001", factura.Pagos.Single().Referencia);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarPago_SinBancoCuandoMetodoLoRequiere_EsRechazado()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura, transferenciaRequiereBanco: true);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegistrarPagoAsync(
                factura.Id,
                new RegistrarFacturaPagoDto { Monto = 100m, MetodoPago = "Transferencia" },
                7,
                "tester"));

        Assert.Contains("banco", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factura.Pagos);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarPago_ConBancoInexistente_EsRechazadoFailClosed()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura, transferenciaRequiereBanco: true);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegistrarPagoAsync(
                factura.Id,
                new RegistrarFacturaPagoDto { Monto = 100m, MetodoPago = "Transferencia", BancoId = 999 },
                7,
                "tester"));

        Assert.Contains("banco", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factura.Pagos);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarPago_ConBancoValido_PersisteFkYSnapshotsAuditables()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura, transferenciaRequiereBanco: true);

        var resultado = await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = 100m, MetodoPago = "Transferencia", BancoId = 5 },
            7,
            "tester");

        var pago = Assert.Single(factura.Pagos);
        Assert.Equal(5, pago.BancoId);
        Assert.Equal("BAC", pago.BancoCodigoSnapshot);
        Assert.Equal("BAC Credomatic", pago.BancoNombreSnapshot);
        Assert.Equal(5, resultado.Pagos.Single().BancoId);
        Assert.Equal("BAC Credomatic", resultado.Pagos.Single().BancoNombre);
        repository.Verify(x => x.GetBancoActivoPorIdAsync(5), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarPago_MetodoSinBanco_AceptaBancoOmitidoSinConsultarCatalogo()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura);

        await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = 100m, MetodoPago = "Transferencia" },
            7,
            "tester");

        Assert.Null(factura.Pagos.Single().BancoId);
        repository.Verify(x => x.GetBancoActivoPorIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AnularPago_RecalculaSaldoYConservaTrazabilidad()
    {
        var factura = CrearFactura(300m);
        factura.Pagos.Add(new FacturaPago
        {
            Id = 22,
            FacturaId = factura.Id,
            Monto = 300m,
            MontoRecibido = 300m,
            Cambio = 0m,
            MetodoPago = MetodoPago.Efectivo
        });
        factura.TotalPagado = 300m;
        factura.SaldoPendiente = 0m;
        factura.Estado = EstadoFactura.Pagada;

        var (service, _) = CrearServicio(factura);
        var resultado = await service.AnularPagoAsync(
            factura.Id,
            22,
            new AnularFacturaPagoDto { Motivo = "Pago registrado por error" },
            8,
            "supervisor");

        Assert.Equal(0m, resultado.TotalPagado);
        Assert.Equal(300m, resultado.SaldoPendiente);
        Assert.Equal("Emitida", resultado.Estado);
        Assert.True(factura.Pagos.Single().Anulado);
        Assert.Equal("Pago registrado por error", factura.Pagos.Single().MotivoAnulacion);
        Assert.Equal(8, factura.Pagos.Single().AnuladoPorUsuarioId);
    }

    [Fact]
    public async Task PagoCompletoYSuAnulacion_SincronizanMovimientoAutomaticoDeVenta()
    {
        var factura = CrearFactura(300m);
        var movimiento = new MovimientoFinanciero
        {
            Id = 91,
            VentaId = factura.VentaId,
            Tipo = TipoMovimientoFinanciero.Ingreso,
            Categoria = CategoriaMovimientoFinanciero.Venta,
            Monto = factura.Total,
            EsAutomatico = true,
            Estado = EstadoMovimientoFinanciero.Pendiente
        };

        var repository = new Mock<IFacturaRepository>();
        repository.Setup(x => x.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetMetodoPagoPorCodigoONombreAsync("Efectivo"))
            .ReturnsAsync(CrearMetodoPago(1, "EFECTIVO", "Efectivo"));

        var movimientoRepository = new Mock<IMovimientoFinancieroRepository>();
        movimientoRepository.Setup(x => x.GetByVentaIdAsync(factura.VentaId)).ReturnsAsync(movimiento);

        var empresa = new Mock<IEmpresaConfiguracionService>();
        empresa.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracionDto());
        var service = new FacturaService(repository.Object, empresa.Object, movimientoRepository.Object);

        await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = factura.Total, MetodoPago = "Efectivo" },
            7,
            "tester");

        Assert.Equal(EstadoMovimientoFinanciero.Pagado, movimiento.Estado);
        movimientoRepository.Verify(x => x.Update(movimiento), Times.Once);

        var pago = factura.Pagos.Single();
        pago.Id = 92;
        await service.AnularPagoAsync(
            factura.Id,
            pago.Id,
            new AnularFacturaPagoDto { Motivo = "Prueba UAT" },
            7,
            "tester");

        Assert.Equal(EstadoMovimientoFinanciero.Pendiente, movimiento.Estado);
        movimientoRepository.Verify(x => x.Update(movimiento), Times.Exactly(2));
    }

    private static Factura CrearFactura(decimal total) => new()
    {
        Id = 15,
        VentaId = 9,
        NumeroFactura = "FAC-000015",
        Estado = EstadoFactura.Emitida,
        EmpresaNombre = "Storefront",
        ClienteNombre = "Cliente prueba",
        VendedorUsuarioId = 7,
        VendedorNombreUsuario = "tester",
        Total = total,
        SaldoPendiente = total,
        Venta = new Venta
        {
            Id = 9,
            NumeroVenta = "VEN-000009",
            EstadoPago = EstadoPago.Pendiente,
            MetodoPagoId = 1,
            MetodoPagoCatalogo = CrearMetodoPago(1, "EFECTIVO", "Efectivo")
        }
    };

    private static (FacturaService Service, Mock<IFacturaRepository> Repository) CrearServicio(
        Factura factura,
        bool transferenciaRequiereReferencia = false,
        bool transferenciaRequiereBanco = false,
        bool efectivoPermiteCambio = false)
    {
        var repository = new Mock<IFacturaRepository>();
        repository.Setup(x => x.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetMetodoPagoPorCodigoONombreAsync(It.IsAny<string>()))
            .ReturnsAsync((string valor) => valor.Trim().Equals("Transferencia", StringComparison.OrdinalIgnoreCase)
                ? CrearMetodoPago(2, "TRANSFERENCIA", "Transferencia", transferenciaRequiereReferencia, transferenciaRequiereBanco)
                : valor.Trim().Equals("Efectivo", StringComparison.OrdinalIgnoreCase)
                    ? CrearMetodoPago(1, "EFECTIVO", "Efectivo", permiteCambio: efectivoPermiteCambio)
                    : null);
        repository.Setup(x => x.GetBancoActivoPorIdAsync(5))
            .ReturnsAsync(new CatalogoBanco
            {
                Id = 5,
                Codigo = "BAC",
                Nombre = "BAC Credomatic",
                Activo = true
            });

        var empresa = new Mock<IEmpresaConfiguracionService>();
        empresa.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracionDto());

        return (new FacturaService(repository.Object, empresa.Object), repository);
    }

    private static CatalogoMetodoPago CrearMetodoPago(
        int id,
        string codigo,
        string nombre,
        bool requiereReferencia = false,
        bool requiereBanco = false,
        bool permiteCambio = false) => new()
    {
        Id = id,
        Codigo = codigo,
        Nombre = nombre,
        Activo = true,
        RequiereReferencia = requiereReferencia,
        RequiereBanco = requiereBanco,
        PermiteCambio = permiteCambio
    };
}
