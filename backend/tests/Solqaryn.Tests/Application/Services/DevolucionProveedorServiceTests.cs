using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Services;

public class DevolucionProveedorServiceTests
{
    [Fact]
    public async Task CreateAsync_CreaBorradorIdempotenteConSnapshotsDeFacturaYRecepcion()
    {
        var repo = new Mock<IDevolucionProveedorRepository>();
        repo.Setup(x => x.GetByIdempotencyKeyAsync("key-1", false)).ReturnsAsync((DevolucionProveedor?)null);
        repo.Setup(x => x.GetByIdempotencyKeyAsync("key-1", true)).ReturnsAsync((DevolucionProveedor?)null);
        repo.Setup(x => x.ExisteNumeroAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        repo.Setup(x => x.AddAsync(It.IsAny<DevolucionProveedor>()))
            .Callback<DevolucionProveedor>(x => x.Id = 99)
            .Returns(Task.CompletedTask);
        repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var recepciones = new Mock<IRecepcionCompraRepository>();
        recepciones.Setup(x => x.GetByIdAsync(20, false)).ReturnsAsync(CrearRecepcion());

        var facturas = new Mock<IFacturaProveedorRepository>();
        facturas.Setup(x => x.GetByIdAsync(30, false)).ReturnsAsync(CrearFactura());

        var service = CrearService(repo, recepciones, facturas);
        var dto = new CreateDevolucionProveedorDto
        {
            RecepcionCompraId = 20,
            FacturaProveedorId = 30,
            Motivo = "Producto defectuoso",
            Detalles = new()
            {
                new DevolucionProveedorDetalleInputDto { RecepcionCompraDetalleId = 200, Cantidad = 2m }
            }
        };

        var result = await service.CreateAsync(dto, " key-1 ");

        Assert.Equal(99, result.Id);
        Assert.Equal(EstadoDevolucionProveedor.Borrador, result.Estado);
        Assert.Equal(10, result.ProveedorId);
        Assert.Equal(2m, result.Detalles.Single().Cantidad);
        Assert.Equal(9m, result.Detalles.Single().CostoUnitarioSnapshot);
        Assert.Equal(1.5m, result.Detalles.Single().ImpuestoUnitarioSnapshot);
        Assert.Equal(21m, result.TotalCredito);
        repo.Verify(x => x.AddAsync(It.IsAny<DevolucionProveedor>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MismaKeyPayloadDistinto_DevuelveConflict()
    {
        var existente = new DevolucionProveedor
        {
            Id = 4,
            NumeroDevolucion = "DVP-1",
            ProveedorId = 10,
            OrdenCompraId = 40,
            RecepcionCompraId = 20,
            FacturaProveedorId = 30,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            Motivo = "A"
        };
        existente.Detalles.Add(CrearDetalleDevolucion(1m));
        existente.EstablecerIdempotencia("key-1", new string('a', 64));

        var repo = new Mock<IDevolucionProveedorRepository>();
        repo.Setup(x => x.GetByIdempotencyKeyAsync("key-1", false)).ReturnsAsync(existente);

        var service = CrearService(repo, new Mock<IRecepcionCompraRepository>(), new Mock<IFacturaProveedorRepository>());
        var dto = new CreateDevolucionProveedorDto
        {
            RecepcionCompraId = 20,
            FacturaProveedorId = 30,
            Motivo = "B",
            Detalles = new()
            {
                new DevolucionProveedorDetalleInputDto { RecepcionCompraDetalleId = 200, Cantidad = 1m }
            }
        };

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(dto, "key-1"));
    }

    [Fact]
    public async Task ConfirmarAsync_RechazaDevolverMasDeLoRecibidoAcumulado()
    {
        var devolucion = new DevolucionProveedor
        {
            Id = 70,
            NumeroDevolucion = "DVP-70",
            ProveedorId = 10,
            OrdenCompraId = 40,
            RecepcionCompraId = 20,
            FacturaProveedorId = 30,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            Motivo = "Daño"
        };
        devolucion.Detalles.Add(CrearDetalleDevolucion(6m));

        var repo = new Mock<IDevolucionProveedorRepository>();
        repo.Setup(x => x.GetByIdForUpdateAsync(70)).ReturnsAsync(devolucion);
        repo.Setup(x => x.GetCantidadConfirmadaDevueltaPorDetalleAsync(200, 70)).ReturnsAsync(5m);
        repo.Setup(x => x.GetCantidadConfirmadaDevueltaPorFacturaLineaAsync(30, 400, 70)).ReturnsAsync(0m);

        var recepciones = new Mock<IRecepcionCompraRepository>();
        recepciones.Setup(x => x.GetByIdAsync(20, false)).ReturnsAsync(CrearRecepcion());
        var facturas = new Mock<IFacturaProveedorRepository>();
        facturas.Setup(x => x.GetByIdAsync(30, false)).ReturnsAsync(CrearFactura());

        var service = CrearService(repo, recepciones, facturas);
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConfirmarAsync(70));

        Assert.Contains("supera la cantidad aceptada", ex.Message);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static DevolucionProveedorService CrearService(
        Mock<IDevolucionProveedorRepository> repo,
        Mock<IRecepcionCompraRepository> recepciones,
        Mock<IFacturaProveedorRepository> facturas)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("tester");

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        return new DevolucionProveedorService(
            repo.Object,
            recepciones.Object,
            facturas.Object,
            currentUser.Object,
            uow.Object,
            auditoria.Object);
    }

    private static RecepcionCompra CrearRecepcion()
    {
        var detalle = new RecepcionCompraDetalle
        {
            Id = 200,
            OrdenCompraDetalleId = 400,
            ProductoId = 500,
            ProductoVarianteId = 600,
            AlmacenId = 700,
            CostoUnitarioSnapshot = 10m,
            ProductoNombreSnapshot = "Producto"
        };
        detalle.EstablecerCantidades(10m);

        var recepcion = new RecepcionCompra
        {
            Id = 20,
            NumeroRecepcion = "RC-20",
            OrdenCompraId = 40,
            OrdenCompra = new OrdenCompra { Id = 40, ProveedorId = 10 },
            Detalles = new List<RecepcionCompraDetalle> { detalle }
        };
        recepcion.Confirmar(7, "tester", DateTime.UtcNow);
        return recepcion;
    }

    private static FacturaProveedor CrearFactura()
    {
        var detalle = new FacturaProveedorDetalle
        {
            Id = 300,
            OrdenCompraDetalleId = 400,
            ProductoId = 500,
            ProductoVarianteId = 600,
            ProductoNombreSnapshot = "Producto"
        };
        detalle.EstablecerValores(10m, 10m, 10m, 15m);

        var factura = new FacturaProveedor
        {
            Id = 30,
            NumeroFactura = "FP-30",
            ProveedorId = 10,
            OrdenCompraId = 40,
            ProveedorNombreSnapshot = "Proveedor",
            Moneda = "HNL",
            FechaEmisionUtc = DateTime.UtcNow.Date,
            Detalles = new List<FacturaProveedorDetalle> { detalle }
        };
        factura.Registrar(7, "tester", DateTime.UtcNow);
        return factura;
    }

    private static DevolucionProveedorDetalle CrearDetalleDevolucion(decimal cantidad) => new()
    {
        Id = 800,
        RecepcionCompraDetalleId = 200,
        OrdenCompraDetalleId = 400,
        ProductoId = 500,
        ProductoVarianteId = 600,
        AlmacenId = 700,
        Cantidad = cantidad,
        CostoUnitarioSnapshot = 9m,
        ImpuestoUnitarioSnapshot = 1.5m,
        ProductoNombreSnapshot = "Producto"
    };
}
