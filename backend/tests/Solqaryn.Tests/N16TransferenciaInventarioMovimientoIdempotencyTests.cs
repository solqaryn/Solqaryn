using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioMovimientoIdempotencyTests
{
    [Fact]
    public async Task Despachar_ReintentoEnTransito_NoVuelveAMutarExistenciasNiKardex()
    {
        var transferencia = CrearTransferenciaEnTransito();
        var (service, repository, existencias, kardex) = CrearServicio(transferencia);

        var result = await service.DespacharAsync(
            transferencia.Id,
            new DespacharTransferenciaInventarioDto());

        Assert.NotNull(result);
        Assert.Equal("EnTransito", result!.Estado);
        existencias.VerifyNoOtherCalls();
        kardex.VerifyNoOtherCalls();
        repository.Verify(x => x.Update(It.IsAny<TransferenciaInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Recibir_ReintentoRecibido_NoVuelveAMutarExistenciasNiKardex()
    {
        var transferencia = CrearTransferenciaRecibida();
        var (service, repository, existencias, kardex) = CrearServicio(transferencia);

        var result = await service.RecibirAsync(
            transferencia.Id,
            new RecibirTransferenciaInventarioDto());

        Assert.NotNull(result);
        Assert.Equal("Recibida", result!.Estado);
        existencias.VerifyNoOtherCalls();
        kardex.VerifyNoOtherCalls();
        repository.Verify(x => x.Update(It.IsAny<TransferenciaInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Cancelar_ReintentoCancelado_NoVuelveAMutarExistenciasNiKardex()
    {
        var transferencia = CrearTransferenciaBase();
        transferencia.Cancelar(7, "Cancelación original", DateTime.UtcNow);
        var (service, repository, existencias, kardex) = CrearServicio(transferencia);

        var result = await service.CancelarAsync(
            transferencia.Id,
            new CancelarTransferenciaInventarioDto { Motivo = "Reintento" });

        Assert.NotNull(result);
        Assert.Equal("Cancelada", result!.Estado);
        existencias.VerifyNoOtherCalls();
        kardex.VerifyNoOtherCalls();
        repository.Verify(x => x.Update(It.IsAny<TransferenciaInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static (
        TransferenciaInventarioMovimientoService Service,
        Mock<ITransferenciaInventarioRepository> Repository,
        Mock<IExistenciaVarianteConcurrencyService> Existencias,
        Mock<IKardexMovimientoWriter> Kardex) CrearServicio(TransferenciaInventario transferencia)
    {
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository
            .Setup(x => x.GetByIdForUpdateAsync(transferencia.Id))
            .ReturnsAsync(transferencia);

        var existencias = new Mock<IExistenciaVarianteConcurrencyService>(MockBehavior.Strict);
        var kardex = new Mock<IKardexMovimientoWriter>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-n16");

        var service = new TransferenciaInventarioMovimientoService(
            repository.Object,
            new TransferenciaInventarioExistenciaService(existencias.Object),
            kardex.Object,
            currentUser.Object,
            new FakeUnitOfWork());

        return (service, repository, existencias, kardex);
    }

    private static TransferenciaInventario CrearTransferenciaBase()
    {
        var detalle = new TransferenciaInventarioDetalle
        {
            Id = 401,
            ProductoVarianteId = 91,
            ProductoVariante = new ProductoVariante { Id = 91, ProductoId = 44, Activo = true },
            UbicacionOrigenId = 101,
            UbicacionDestinoId = 202,
            CreadoPorUsuarioId = 7
        };
        detalle.EstablecerCantidadSolicitada(5);

        return new TransferenciaInventario
        {
            Id = 31,
            Numero = "TRF-IDEM-31",
            AlmacenOrigenId = 10,
            AlmacenDestinoId = 20,
            CreadoPorUsuarioId = 7,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };
    }

    private static TransferenciaInventario CrearTransferenciaEnTransito()
    {
        var transferencia = CrearTransferenciaBase();
        var detalle = transferencia.Detalles.Single();
        transferencia.Solicitar(7, DateTime.UtcNow);
        detalle.AprobarCantidad(5);
        transferencia.Aprobar(7, DateTime.UtcNow);
        detalle.RegistrarDespacho(5);
        transferencia.MarcarEnTransito(7, DateTime.UtcNow);
        return transferencia;
    }

    private static TransferenciaInventario CrearTransferenciaRecibida()
    {
        var transferencia = CrearTransferenciaEnTransito();
        transferencia.Detalles.Single().RegistrarRecepcion(5, 0, 0, 0);
        transferencia.Recibir(7, DateTime.UtcNow);
        return transferencia;
    }
}
