using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N78DPagoOnlineServiceTests
{
    [Fact]
    public async Task IniciarAsync_MismaClave_ReutilizaPagoSinInvocarDosVecesAlProveedor()
    {
        PagoOnline? guardado = null;
        var repository = CrearRepositoryBase(() => guardado);
        repository
            .Setup(x => x.AgregarAsync(It.IsAny<PagoOnline>(), It.IsAny<CancellationToken>()))
            .Callback<PagoOnline, CancellationToken>((pago, _) =>
            {
                pago.Id = 91;
                guardado = pago;
            })
            .Returns(Task.CompletedTask);

        var provider = new Mock<IPagoOnlineProvider>();
        provider.SetupGet(x => x.Codigo).Returns("provider-test");
        provider
            .Setup(x => x.IniciarAsync(It.IsAny<SolicitudInicioPagoOnline>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoInicioPagoOnline(
                "ref-91",
                new Uri("https://payments.example.test/p/91"),
                EstadoPagoOnline.Pendiente));

        var service = CrearService(repository.Object, provider.Object);
        var solicitud = new IniciarPagoOnlineDto(7, 41, "PROVIDER-TEST", 125.50m, "hnl");
        const string clave = "idem-N78D-0000000001";

        var primero = await service.IniciarAsync(solicitud, clave);
        var segundo = await service.IniciarAsync(solicitud, clave);

        Assert.False(primero.Reutilizado);
        Assert.True(segundo.Reutilizado);
        Assert.Equal(91, segundo.Pago.Id);
        Assert.NotNull(guardado);
        Assert.NotEqual(clave, guardado!.ClaveIdempotenciaHash);
        Assert.Equal(64, guardado.ClaveIdempotenciaHash!.Length);
        Assert.Equal("provider-test", guardado.Proveedor);
        provider.Verify(
            x => x.IniciarAsync(It.IsAny<SolicitudInicioPagoOnline>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_FalloProveedor_PersisteCodigoSeguroSinFiltrarErrorExterno()
    {
        PagoOnline? guardado = null;
        var repository = CrearRepositoryBase(() => guardado);
        repository
            .Setup(x => x.AgregarAsync(It.IsAny<PagoOnline>(), It.IsAny<CancellationToken>()))
            .Callback<PagoOnline, CancellationToken>((pago, _) =>
            {
                pago.Id = 92;
                guardado = pago;
            })
            .Returns(Task.CompletedTask);

        var provider = new Mock<IPagoOnlineProvider>();
        provider.SetupGet(x => x.Codigo).Returns("provider-test");
        provider
            .Setup(x => x.IniciarAsync(It.IsAny<SolicitudInicioPagoOnline>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider-secret-detail"));

        var service = CrearService(repository.Object, provider.Object);
        var solicitud = new IniciarPagoOnlineDto(7, 41, "provider-test", 50m, "HNL");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.IniciarAsync(solicitud, "idem-N78D-0000000002"));

        Assert.NotNull(guardado);
        Assert.Equal(EstadoPagoOnline.Fallido, guardado!.Estado);
        Assert.Equal("PROVIDER_INIT_FAILED", guardado.UltimoError);
        Assert.DoesNotContain("provider-secret-detail", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IniciarAsync_UrlHttpProveedor_RechazaCheckoutNoSeguro()
    {
        PagoOnline? guardado = null;
        var repository = CrearRepositoryBase(() => guardado);
        repository
            .Setup(x => x.AgregarAsync(It.IsAny<PagoOnline>(), It.IsAny<CancellationToken>()))
            .Callback<PagoOnline, CancellationToken>((pago, _) =>
            {
                pago.Id = 93;
                guardado = pago;
            })
            .Returns(Task.CompletedTask);

        var provider = new Mock<IPagoOnlineProvider>();
        provider.SetupGet(x => x.Codigo).Returns("provider-test");
        provider
            .Setup(x => x.IniciarAsync(It.IsAny<SolicitudInicioPagoOnline>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoInicioPagoOnline(
                "ref-http-93",
                new Uri("http://payments.example.test/p/93"),
                EstadoPagoOnline.Pendiente));

        var service = CrearService(repository.Object, provider.Object);
        var solicitud = new IniciarPagoOnlineDto(7, 41, "provider-test", 75m, "HNL");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.IniciarAsync(solicitud, "idem-N78F-http-00000001"));

        Assert.NotNull(guardado);
        Assert.Equal(EstadoPagoOnline.Fallido, guardado!.Estado);
        Assert.Equal("PROVIDER_INIT_FAILED", guardado.UltimoError);
        Assert.Null(guardado.UrlPago);
    }

    [Fact]
    public async Task IniciarAsync_ProveedorNoConfigurado_NoCreaIntentoNiAlmacenaDatosDeTarjeta()
    {
        PagoOnline? guardado = null;
        var repository = CrearRepositoryBase(() => guardado);
        var service = CrearService(repository.Object);
        var solicitud = new IniciarPagoOnlineDto(7, 41, "missing-provider", 50m, "HNL");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.IniciarAsync(solicitud, "idem-N78D-0000000003"));

        Assert.Null(guardado);
        var nombres = typeof(PagoOnline).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(nombres, x => x.Contains("Pan", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, x => x.Contains("Cvv", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, x => x.Contains("CardNumber", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(42, 100, "HNL")]
    [InlineData(41, 101, "HNL")]
    [InlineData(41, 100, "USD")]
    public async Task IniciarAsync_RechazaReplayConPayloadDistinto(int facturaId, int monto, string moneda)
    {
        var existente = new PagoOnline
        {
            Id = 95, EmpresaId = 7, FacturaId = 41, Proveedor = "provider-test",
            Monto = 100m, Moneda = "HNL", Estado = EstadoPagoOnline.Pendiente
        };
        var repository = CrearRepositoryBase(() => existente);
        var provider = new Mock<IPagoOnlineProvider>(MockBehavior.Strict);
        var service = CrearService(repository.Object, provider.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.IniciarAsync(new IniciarPagoOnlineDto(7, facturaId, "provider-test", monto, moneda),
                "idem-N78D-conflict-0001"));

        provider.VerifyNoOtherCalls();
        repository.Verify(x => x.AgregarAsync(It.IsAny<PagoOnline>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IniciarAsync_RechazaFraccionesInferioresAlCentavo()
    {
        var repository = new Mock<IPagoOnlineRepository>(MockBehavior.Strict);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CrearService(repository.Object).IniciarAsync(
                new IniciarPagoOnlineDto(7, 41, "provider-test", 1.001m, "HNL"),
                "idem-N78D-scale-0001"));
        repository.VerifyNoOtherCalls();
    }

    private static Mock<IPagoOnlineRepository> CrearRepositoryBase(Func<PagoOnline?> pagoActual)
    {
        var repository = new Mock<IPagoOnlineRepository>();
        repository
            .Setup(x => x.ObtenerPorIdempotenciaAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => pagoActual());
        repository
            .Setup(x => x.ObtenerFacturaAsync(41, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Factura
            {
                Id = 41,
                NumeroFactura = "FAC-00041",
                Moneda = "HNL",
                Total = 500m,
                TotalPagado = 100m,
                SaldoPendiente = 400m
            });
        repository
            .Setup(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return repository;
    }

    private static PagoOnlineService CrearService(
        IPagoOnlineRepository repository,
        params IPagoOnlineProvider[] providers)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(5);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("tester");
        return new PagoOnlineService(repository, providers, currentUser.Object);
    }
}
