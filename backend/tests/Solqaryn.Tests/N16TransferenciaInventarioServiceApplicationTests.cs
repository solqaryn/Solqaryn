using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioServiceApplicationTests
{
    [Fact]
    public async Task Solicitar_ReintentoSobreSolicitada_EsIdempotenteYSinNuevaEscritura()
    {
        var transferencia = CrearTransferenciaSolicitada();
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(transferencia.Id)).ReturnsAsync(transferencia);
        var service = CrearService(repository);

        var resultado = await service.SolicitarAsync(transferencia.Id);

        Assert.NotNull(resultado);
        Assert.Equal("Solicitada", resultado!.Estado);
        repository.Verify(x => x.Update(It.IsAny<TransferenciaInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Aprobar_RechazaDetalleDuplicadoAntesDeMutarDocumento()
    {
        var transferencia = CrearTransferenciaSolicitada();
        var detalleId = transferencia.Detalles.Single().Id;
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(transferencia.Id)).ReturnsAsync(transferencia);
        var service = CrearService(repository);
        var dto = new AprobarTransferenciaInventarioDto
        {
            Detalles =
            {
                new AprobarTransferenciaInventarioDetalleDto { DetalleId = detalleId, CantidadAprobada = 2 },
                new AprobarTransferenciaInventarioDetalleDto { DetalleId = detalleId, CantidadAprobada = 2 }
            }
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.AprobarAsync(transferencia.Id, dto));

        Assert.Equal("Solicitada", transferencia.Estado.ToString());
        repository.Verify(x => x.Update(It.IsAny<TransferenciaInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Lifecycle_AprobarDespacharRecibir_RecorreEstadosEmpresariales()
    {
        var transferencia = CrearTransferenciaSolicitada();
        var detalleId = transferencia.Detalles.Single().Id;
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(transferencia.Id)).ReturnsAsync(transferencia);
        var service = CrearService(repository);

        await AprobarYDespacharAsync(service, transferencia.Id, detalleId, 3);
        var recibida = await service.RecibirAsync(transferencia.Id, new RecibirTransferenciaInventarioDto
        {
            Detalles =
            {
                new RecibirTransferenciaInventarioDetalleDto
                {
                    DetalleId = detalleId,
                    CantidadRecibida = 3,
                    CantidadFaltante = 0,
                    CantidadDanada = 0,
                    CantidadSobrante = 0
                }
            }
        });

        Assert.NotNull(recibida);
        Assert.Equal("Recibida", recibida!.Estado);
        repository.Verify(x => x.Update(transferencia), Times.Exactly(3));
        repository.Verify(x => x.SaveChangesAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task Recibir_PreservaFaltantesDanosYSobrantesDelDocumento()
    {
        var transferencia = CrearTransferenciaSolicitada();
        var detalleId = transferencia.Detalles.Single().Id;
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(transferencia.Id)).ReturnsAsync(transferencia);
        var service = CrearService(repository);
        await AprobarYDespacharAsync(service, transferencia.Id, detalleId, 3);

        var recibida = await service.RecibirAsync(transferencia.Id, new RecibirTransferenciaInventarioDto
        {
            Detalles =
            {
                new RecibirTransferenciaInventarioDetalleDto
                {
                    DetalleId = detalleId,
                    CantidadRecibida = 1,
                    CantidadFaltante = 1,
                    CantidadDanada = 1,
                    CantidadSobrante = 2
                }
            }
        });

        var detalle = Assert.Single(recibida!.Detalles);
        Assert.Equal(1, detalle.CantidadRecibida);
        Assert.Equal(1, detalle.CantidadFaltante);
        Assert.Equal(1, detalle.CantidadDanada);
        Assert.Equal(2, detalle.CantidadSobrante);
        Assert.Equal("Recibida", recibida.Estado);
    }

    [Fact]
    public async Task Cancelar_DesdeSolicitada_RegistraMotivoYEsIdempotenteAlReintentar()
    {
        var transferencia = CrearTransferenciaSolicitada();
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(transferencia.Id)).ReturnsAsync(transferencia);
        var service = CrearService(repository);
        var dto = new CancelarTransferenciaInventarioDto { Motivo = "Solicitud duplicada" };

        var cancelada = await service.CancelarAsync(transferencia.Id, dto);
        var reintento = await service.CancelarAsync(transferencia.Id, dto);

        Assert.NotNull(cancelada);
        Assert.Equal("Cancelada", cancelada!.Estado);
        Assert.Equal("Solicitud duplicada", cancelada.MotivoCancelacion);
        Assert.Equal("Cancelada", reintento!.Estado);
        repository.Verify(x => x.Update(transferencia), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private static async Task AprobarYDespacharAsync(
        TransferenciaInventarioService service,
        int transferenciaId,
        int detalleId,
        int cantidad)
    {
        var aprobada = await service.AprobarAsync(transferenciaId, new AprobarTransferenciaInventarioDto
        {
            Detalles = { new AprobarTransferenciaInventarioDetalleDto { DetalleId = detalleId, CantidadAprobada = cantidad } }
        });
        Assert.NotNull(aprobada);
        Assert.Equal("Aprobada", aprobada!.Estado);

        var enTransito = await service.DespacharAsync(transferenciaId, new DespacharTransferenciaInventarioDto
        {
            Detalles = { new DespacharTransferenciaInventarioDetalleDto { DetalleId = detalleId, CantidadDespachada = cantidad } }
        });
        Assert.NotNull(enTransito);
        Assert.Equal("EnTransito", enTransito!.Estado);
    }

    private static TransferenciaInventarioService CrearService(Mock<ITransferenciaInventarioRepository> repository)
    {
        var almacenes = new Mock<IAlmacenRepository>();
        var variantes = new Mock<IProductoVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-n16");

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        return new TransferenciaInventarioService(
            repository.Object,
            almacenes.Object,
            variantes.Object,
            currentUser.Object,
            unitOfWork.Object);
    }

    private static TransferenciaInventario CrearTransferenciaSolicitada()
    {
        var detalle = new TransferenciaInventarioDetalle
        {
            Id = 41,
            ProductoVarianteId = 91
        };
        detalle.EstablecerCantidadSolicitada(3);

        var transferencia = new TransferenciaInventario
        {
            Id = 12,
            Numero = "TRF-N16-12",
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };
        transferencia.Solicitar(7, DateTime.UtcNow);
        return transferencia;
    }
}
