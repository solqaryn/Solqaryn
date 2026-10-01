using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

public class FacturaProveedorSecurityJulesDTests
{
    private readonly Mock<IFacturaProveedorRepository> _repositoryMock;
    private readonly Mock<IOrdenCompraRepository> _ordenCompraRepositoryMock;
    private readonly Mock<IRecepcionCompraRepository> _recepcionCompraRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<FacturaProveedorService>> _loggerMock;
    private readonly FacturaProveedorService _service;

    public FacturaProveedorSecurityJulesDTests()
    {
        _repositoryMock = new Mock<IFacturaProveedorRepository>();
        _ordenCompraRepositoryMock = new Mock<IOrdenCompraRepository>();
        _recepcionCompraRepositoryMock = new Mock<IRecepcionCompraRepository>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<FacturaProveedorService>>();

        _service = new FacturaProveedorService(
            _repositoryMock.Object,
            _ordenCompraRepositoryMock.Object,
            _recepcionCompraRepositoryMock.Object,
            _currentUserMock.Object,
            _unitOfWorkMock.Object,
            _auditoriaMock.Object,
            _loggerMock.Object
        );
    }

    private static FacturaProveedor CrearFacturaBorradorMock()
    {
        var detalle = new FacturaProveedorDetalle { OrdenCompraDetalleId = 1, ProductoId = 1, ProductoNombreSnapshot = "Prod" };
        detalle.EstablecerValores(1, 10, 0, 0);

        var orden = new OrdenCompra { Id = 1, NumeroOrden = "ORD-001" };
        var proveedor = new Proveedor { Id = 1, Nombre = "Prov" };

        return new FacturaProveedor
        {
            Id = 1,
            NumeroFactura = "F-123",
            Moneda = "HNL",
            FechaEmisionUtc = DateTime.UtcNow,
            ProveedorId = 1,
            OrdenCompraId = 1,
            ProveedorNombreSnapshot = "Prov",
            Detalles = new List<FacturaProveedorDetalle> { detalle },
            OrdenCompra = orden,
            Proveedor = proveedor
        };
    }

    private static FacturaProveedor CrearFacturaRegistradaMock()
    {
        var factura = CrearFacturaBorradorMock();
        factura.Registrar(1, "Test", DateTime.UtcNow);
        return factura;
    }

    [Fact]
    public async Task CreateAsync_WhenUserIsNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        _currentUserMock.Setup(x => x.EstaAutenticado).Returns(false);

        var dto = new CreateFacturaProveedorDto
        {
            ProveedorId = 1,
            OrdenCompraId = 1,
            NumeroFactura = "F-123",
            Moneda = "HNL",
            FechaEmisionUtc = DateTime.UtcNow,
            Detalles = new List<FacturaProveedorDetalleInputDto>
            {
                new() { OrdenCompraDetalleId = 1, CantidadFacturada = 1, PrecioUnitario = 100 }
            }
        };

        _repositoryMock.Setup(x => x.GetByProveedorNumeroAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync((FacturaProveedor?)null);

        var validOrden = new OrdenCompra { ProveedorId = 1, ProveedorNombreSnapshot = "Proveedor 1" };
        var prop = typeof(OrdenCompra).GetProperty("Estado");
        prop?.SetValue(validOrden, EstadoOrdenCompra.Aprobada);
        var detallesProp = typeof(OrdenCompra).GetProperty("Detalles");
        var detalle = new OrdenCompraDetalle { Id = 1, ProductoId = 1, ProductoNombreSnapshot = "Producto 1" };
        var precioProp = typeof(OrdenCompraDetalle).GetProperty("PrecioUnitario");
        precioProp?.SetValue(detalle, 100m);
        var detalles = new List<OrdenCompraDetalle> { detalle };
        detallesProp?.SetValue(validOrden, detalles);

        _ordenCompraRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(validOrden);
        _ordenCompraRepositoryMock.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<int>())).ReturnsAsync(validOrden);

        _unitOfWorkMock.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>())).Returns(async (Func<Task> action) =>
        {
            await action();
        });

        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.CreateAsync(dto));
        Assert.Equal("No existe un usuario autenticado válido para ejecutar la operación.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        _currentUserMock.Setup(x => x.EstaAutenticado).Returns(false);
        var dto = new UpdateFacturaProveedorDto
        {
            ProveedorId = 1,
            OrdenCompraId = 1,
            NumeroFactura = "F-123",
            Moneda = "HNL",
            FechaEmisionUtc = DateTime.UtcNow,
            Observaciones = "Test",
            Detalles = new List<FacturaProveedorDetalleInputDto>
            {
                new() { OrdenCompraDetalleId = 1, CantidadFacturada = 1, PrecioUnitario = 10 }
            }
        };
        var factura = CrearFacturaBorradorMock();

        _repositoryMock.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(factura);

        var validOrden = new OrdenCompra { ProveedorId = 1, ProveedorNombreSnapshot = "Proveedor 1" };
        var ordenEstadoProp = typeof(OrdenCompra).GetProperty("Estado");
        ordenEstadoProp?.SetValue(validOrden, EstadoOrdenCompra.Aprobada);
        var ordenDetallesProp = typeof(OrdenCompra).GetProperty("Detalles");
        var detalle = new OrdenCompraDetalle { Id = 1, ProductoId = 1, ProductoNombreSnapshot = "Producto 1" };
        var precioProp = typeof(OrdenCompraDetalle).GetProperty("PrecioUnitario");
        precioProp?.SetValue(detalle, 10m);
        var detalles = new List<OrdenCompraDetalle> { detalle };
        ordenDetallesProp?.SetValue(validOrden, detalles);

        _ordenCompraRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(validOrden);
        _ordenCompraRepositoryMock.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<int>())).ReturnsAsync(validOrden);

        _unitOfWorkMock.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>())).Returns(async (Func<Task> action) =>
        {
            await action();
        });

        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.UpdateAsync(1, dto));
        Assert.Equal("No existe un usuario autenticado válido para ejecutar la operación.", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WhenUserIsNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        _currentUserMock.Setup(x => x.EstaAutenticado).Returns(false);
        var factura = CrearFacturaBorradorMock();

        _repositoryMock.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(factura);
        _currentUserMock.Setup(x => x.UsuarioId).Returns((int?)null);

        var validOrden = new OrdenCompra { ProveedorId = 1, ProveedorNombreSnapshot = "Proveedor 1" };
        var ordenEstadoProp = typeof(OrdenCompra).GetProperty("Estado");
        ordenEstadoProp?.SetValue(validOrden, EstadoOrdenCompra.Aprobada);
        var ordenDetallesProp = typeof(OrdenCompra).GetProperty("Detalles");
        var detalle = new OrdenCompraDetalle { Id = 1, ProductoId = 1, ProductoNombreSnapshot = "Producto 1" };
        var precioProp = typeof(OrdenCompraDetalle).GetProperty("PrecioUnitario");
        precioProp?.SetValue(detalle, 10m);
        var detalles = new List<OrdenCompraDetalle> { detalle };
        ordenDetallesProp?.SetValue(validOrden, detalles);

        _ordenCompraRepositoryMock.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<int>())).ReturnsAsync(validOrden);
        _recepcionCompraRepositoryMock.Setup(x => x.GetCantidadAceptadaAcumuladaPorDetalleAsync(It.IsAny<int>(), null)).ReturnsAsync(10m);

        _unitOfWorkMock.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>())).Returns(async (Func<Task> action) =>
        {
            await action();
        });

        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.RegistrarAsync(1));
        Assert.Equal("No existe un usuario autenticado válido para ejecutar la operación.", exception.Message);
    }

    [Fact]
    public async Task AnularAsync_WhenUserIsNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        _currentUserMock.Setup(x => x.EstaAutenticado).Returns(false);
        var dto = new AnularFacturaProveedorDto { Motivo = "Prueba de seguridad" };
        var factura = CrearFacturaRegistradaMock();

        _repositoryMock.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(factura);

        _unitOfWorkMock.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>())).Returns(async (Func<Task> action) =>
        {
            await action();
        });

        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.AnularAsync(1, dto));
        Assert.Equal("No existe un usuario autenticado válido para ejecutar la operación.", exception.Message);
    }
}
