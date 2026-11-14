using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N24FacturaProveedorServiceTests
{
    private readonly Mock<IFacturaProveedorRepository> _repository = new();
    private readonly Mock<IOrdenCompraRepository> _ordenes = new();
    private readonly Mock<IRecepcionCompraRepository> _recepciones = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<ILogger<FacturaProveedorService>> _logger = new();
    private readonly FacturaProveedorService _service;

    public N24FacturaProveedorServiceTests()
    {
        _currentUser.Setup(x => x.EstaAutenticado).Returns(true);
        _currentUser.Setup(x => x.UsuarioId).Returns(7);
        _currentUser.Setup(x => x.NombreUsuario).Returns("compras.qa");
        _currentUser.Setup(x => x.NombreCompleto).Returns("Compras QA");
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());

        _service = new FacturaProveedorService(
            _repository.Object,
            _ordenes.Object,
            _recepciones.Object,
            _currentUser.Object,
            _unitOfWork.Object,
            _auditoria.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetPagedAsync_NormalizaPaginacionAntesDeConsultarRepositorio()
    {
        _repository
            .Setup(x => x.GetPagedAsync(It.Is<FacturaProveedorFiltroDto>(f => f.Page == 1 && f.PageSize == 100)))
            .ReturnsAsync(((IReadOnlyList<FacturaProveedor>)Array.Empty<FacturaProveedor>(), 0));

        var result = await _service.GetPagedAsync(new FacturaProveedorFiltroDto
        {
            Page = 0,
            PageSize = 500
        });

        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_ReplayMismoDocumento_EsIdempotenteSinEscrituraNiAuditoria()
    {
        var existente = CrearFacturaExistente();
        var dto = CrearDto();
        _repository
            .Setup(x => x.GetByProveedorNumeroAsync(10, "FAC-001", false))
            .ReturnsAsync(existente);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(existente.Id, result.Id);
        Assert.Equal("FAC-001", result.NumeroFactura);
        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
        _repository.Verify(x => x.AddAsync(It.IsAny<FacturaProveedor>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ReutilizaNumeroConPayloadDiferente_FallaCerrado()
    {
        var existente = CrearFacturaExistente();
        var dto = CrearDto();
        dto.Observaciones = "otro payload";
        _repository
            .Setup(x => x.GetByProveedorNumeroAsync(10, "FAC-001", false))
            .ReturnsAsync(existente);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(dto));

        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
        _repository.Verify(x => x.AddAsync(It.IsAny<FacturaProveedor>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_FechaSinUtc_FallaAntesDeConsultarPersistencia()
    {
        var dto = CrearDto();
        dto.FechaEmisionUtc = DateTime.SpecifyKind(dto.FechaEmisionUtc, DateTimeKind.Unspecified);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(dto));

        _repository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrarAsync_YaRegistrada_EsIdempotenteSinPersistirNiAuditar()
    {
        var factura = CrearFacturaExistente();
        factura.Registrar(7, "Compras QA", DateTime.UtcNow);
        _repository.Setup(x => x.GetByIdForUpdateAsync(55)).ReturnsAsync(factura);

        var result = await _service.RegistrarAsync(55);

        Assert.Equal(EstadoFacturaProveedor.Registrada, result.Estado);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _ordenes.Verify(x => x.GetByIdForUpdateAsync(It.IsAny<int>()), Times.Never);
        _recepciones.VerifyNoOtherCalls();
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrarAsync_SuperaCantidadComprada_FallaAntesDePersistir()
    {
        var factura = CrearFacturaExistente();
        var orden = CrearOrdenAprobada(2m);
        _repository.Setup(x => x.GetByIdForUpdateAsync(55)).ReturnsAsync(factura);
        _ordenes.Setup(x => x.GetByIdForUpdateAsync(20)).ReturnsAsync(orden);
        _repository.Setup(x => x.GetCantidadRegistradaAcumuladaPorDetalleAsync(21, 55)).ReturnsAsync(1m);
        _recepciones.Setup(x => x.GetCantidadAceptadaAcumuladaPorDetalleAsync(21, null)).ReturnsAsync(10m);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarAsync(55));

        Assert.Contains("cantidad comprada", ex.Message, StringComparison.OrdinalIgnoreCase);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrarAsync_SuperaCantidadRecibidaAceptada_FallaAntesDePersistir()
    {
        var factura = CrearFacturaExistente();
        var orden = CrearOrdenAprobada(5m);
        _repository.Setup(x => x.GetByIdForUpdateAsync(55)).ReturnsAsync(factura);
        _ordenes.Setup(x => x.GetByIdForUpdateAsync(20)).ReturnsAsync(orden);
        _repository.Setup(x => x.GetCantidadRegistradaAcumuladaPorDetalleAsync(21, 55)).ReturnsAsync(1m);
        _recepciones.Setup(x => x.GetCantidadAceptadaAcumuladaPorDetalleAsync(21, null)).ReturnsAsync(2m);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarAsync(55));

        Assert.Contains("cantidad recibida", ex.Message, StringComparison.OrdinalIgnoreCase);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrarAsync_DentroDeLimites_BloqueaOrdenYRegistra()
    {
        var factura = CrearFacturaExistente();
        var orden = CrearOrdenAprobada(5m);
        _repository.Setup(x => x.GetByIdForUpdateAsync(55)).ReturnsAsync(factura);
        _ordenes.Setup(x => x.GetByIdForUpdateAsync(20)).ReturnsAsync(orden);
        _repository.Setup(x => x.GetCantidadRegistradaAcumuladaPorDetalleAsync(21, 55)).ReturnsAsync(1m);
        _recepciones.Setup(x => x.GetCantidadAceptadaAcumuladaPorDetalleAsync(21, null)).ReturnsAsync(4m);

        var result = await _service.RegistrarAsync(55);

        Assert.Equal(EstadoFacturaProveedor.Registrada, result.Estado);
        _ordenes.Verify(x => x.GetByIdForUpdateAsync(20), Times.Once);
        _repository.Verify(x => x.GetCantidadRegistradaAcumuladaPorDetalleAsync(21, 55), Times.Once);
        _recepciones.Verify(x => x.GetCantidadAceptadaAcumuladaPorDetalleAsync(21, null), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AnularAsync_YaAnulada_EsIdempotenteSinPersistirNiAuditar()
    {
        var factura = CrearFacturaExistente();
        factura.Registrar(7, "Compras QA", DateTime.UtcNow.AddMinutes(-1));
        factura.Anular(7, "Duplicado", DateTime.UtcNow);
        _repository.Setup(x => x.GetByIdForUpdateAsync(55)).ReturnsAsync(factura);

        var result = await _service.AnularAsync(55, new AnularFacturaProveedorDto { Motivo = "Duplicado" });

        Assert.Equal(EstadoFacturaProveedor.Anulada, result.Estado);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrarAsync_SinUsuarioAutenticado_NoPersisteNiAudita()
    {
        var factura = CrearFacturaExistente();
        _currentUser.Setup(x => x.EstaAutenticado).Returns(false);
        _currentUser.Setup(x => x.UsuarioId).Returns((int?)null);
        _repository.Setup(x => x.GetByIdForUpdateAsync(55)).ReturnsAsync(factura);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.RegistrarAsync(55));

        _ordenes.Verify(x => x.GetByIdForUpdateAsync(It.IsAny<int>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    private static FacturaProveedor CrearFacturaExistente()
    {
        var detalle = new FacturaProveedorDetalle
        {
            Id = 101,
            OrdenCompraDetalleId = 21,
            ProductoId = 31,
            ProductoNombreSnapshot = "Producto QA"
        };
        detalle.EstablecerValores(2m, 100m, 5m, 15m);

        return new FacturaProveedor
        {
            Id = 55,
            NumeroFactura = "FAC-001",
            ProveedorId = 10,
            OrdenCompraId = 20,
            ProveedorNombreSnapshot = "Proveedor QA",
            Moneda = "HNL",
            FechaEmisionUtc = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            Observaciones = "documento estable",
            Detalles = new List<FacturaProveedorDetalle> { detalle }
        };
    }

    private static OrdenCompra CrearOrdenAprobada(decimal cantidadOrdenada)
    {
        var detalle = new OrdenCompraDetalle
        {
            Id = 21,
            OrdenCompraId = 20,
            ProductoId = 31,
            ProductoNombreSnapshot = "Producto QA"
        };
        detalle.EstablecerValores(cantidadOrdenada, 100m);

        var orden = new OrdenCompra
        {
            Id = 20,
            NumeroOrden = "OC-TEST-20",
            ProveedorId = 10,
            ProveedorNombreSnapshot = "Proveedor QA",
            Moneda = "HNL",
            Detalles = new List<OrdenCompraDetalle> { detalle }
        };
        orden.EnviarAprobacion(7, DateTime.UtcNow.AddMinutes(-2));
        orden.Aprobar(7, "Compras QA", DateTime.UtcNow.AddMinutes(-1));
        return orden;
    }

    private static CreateFacturaProveedorDto CrearDto() => new()
    {
        ProveedorId = 10,
        OrdenCompraId = 20,
        NumeroFactura = " fac-001 ",
        Moneda = " hnl ",
        FechaEmisionUtc = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
        Observaciones = "documento estable",
        Detalles = new List<FacturaProveedorDetalleInputDto>
        {
            new()
            {
                OrdenCompraDetalleId = 21,
                CantidadFacturada = 2m,
                PrecioUnitario = 100m,
                Descuento = 5m,
                Impuesto = 15m
            }
        }
    };
}
