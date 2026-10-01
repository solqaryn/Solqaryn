using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Tests;

public class VentaMetodoPagoLecturaPropagacionTests
{
    [Fact]
    public async Task GetByIdAsync_Muestra_Nombre_Del_Catalogo_Aunque_Legacy_Difiera()
    {
        var ventaRepo = new Mock<IVentaRepository>();
        var catalogo = new CatalogoMetodoPago { Id = 25, Codigo = "TRANSFERENCIA", Nombre = "Transferencia bancaria" };
        var venta = new Venta
        {
            Id = 1,
            NumeroVenta = "VEN-000001",
            ClienteNombre = "Cliente final",
            MetodoPagoId = 25,
            MetodoPagoCatalogo = catalogo,
            MetodoPago = MetodoPago.Efectivo
        };
        ventaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);

        var service = CrearServicio(ventaRepo, new Mock<IMovimientoFinancieroRepository>());
        var dto = await service.GetByIdAsync(1);

        Assert.Equal("Transferencia bancaria", dto!.MetodoPago);
    }

    [Fact]
    public async Task ConfirmarAsync_Propaga_Fk_Y_Catalogo_Sin_Leer_Enum_Legacy()
    {
        var ventaRepo = new Mock<IVentaRepository>();
        var movimientoFinancieroRepo = new Mock<IMovimientoFinancieroRepository>();
        var inventarioConcurrency = new Mock<IInventarioConcurrencyService>();
        var facturaRepo = new Mock<IFacturaRepository>();
        var catalogo = new CatalogoMetodoPago { Id = 31, Codigo = "TARJETA", Nombre = "Tarjeta" };
        var venta = new Venta
        {
            Id = 1,
            NumeroVenta = "VEN-000001",
            ClienteNombre = "Cliente final",
            Estado = EstadoDocumento.Borrador,
            EstadoPago = EstadoPago.Pagado,
            MetodoPagoId = 31,
            MetodoPagoCatalogo = catalogo,
            MetodoPago = MetodoPago.Efectivo,
            Total = 10m
        };
        venta.Detalles.Add(new VentaDetalle
        {
            ProductoId = 1,
            Cantidad = 1,
            PrecioUnitario = 10m,
            CostoUnitarioSnapshot = 5m,
            Subtotal = 10m,
            UtilidadBruta = 5m,
            ProductoNombreSnapshot = "Mouse",
            ProductoMarcaSnapshot = "Marca",
            ProductoModeloSnapshot = "Modelo"
        });
        ventaRepo.Setup(r => r.GetByIdForUpdateAsync(1)).ReturnsAsync(venta);
        ventaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);
        ventaRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var producto = new Producto { Id = 1, Nombre = "Mouse", Cantidad = 10, Costo = 5m, Precio = 10m };
        inventarioConcurrency
            .Setup(x => x.BloquearYValidarInventarioAsync(It.IsAny<IEnumerable<InventarioDemanda>>(), true))
            .ReturnsAsync(new InventarioLockSet(
                new Dictionary<int, Producto> { [1] = producto },
                new Dictionary<int, ProductoVariante>(),
                new List<InventarioDemanda> { new(1, null, 1) }));

        MovimientoFinanciero? creado = null;
        movimientoFinancieroRepo.Setup(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()))
            .Callback<MovimientoFinanciero>(m => creado = m)
            .Returns(Task.CompletedTask);
        facturaRepo.Setup(r => r.AddAsync(It.IsAny<Factura>()))
            .Callback<Factura>(f => f.Id = 1)
            .Returns(Task.CompletedTask);
        facturaRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var service = CrearServicio(ventaRepo, movimientoFinancieroRepo, inventarioConcurrency, facturaRepo);
        await service.ConfirmarAsync(1);

        Assert.NotNull(creado);
        Assert.Equal(31, creado!.MetodoPagoId);
        Assert.Same(catalogo, creado.MetodoPagoCatalogo);
        Assert.Equal(MetodoPago.Tarjeta, creado.MetodoPago);
        Assert.NotEqual(venta.MetodoPago, creado.MetodoPago);
    }

    private static VentaService CrearServicio(
        Mock<IVentaRepository> ventaRepo,
        Mock<IMovimientoFinancieroRepository> movimientoFinancieroRepo,
        Mock<IInventarioConcurrencyService>? inventarioConcurrency = null,
        Mock<IFacturaRepository>? facturaRepo = null)
    {
        var empresa = new Mock<IEmpresaConfiguracionService>();
        empresa.Setup(e => e.GetActivaEntidadAsync()).ReturnsAsync(new EmpresaConfiguracion());
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(4);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("tester");
        currentUser.SetupGet(x => x.NombreCompleto).Returns("Tester");

        return new VentaService(
            ventaRepo.Object,
            Mock.Of<IClienteRepository>(),
            Mock.Of<IProductoRepository>(),
            Mock.Of<IProductoVarianteRepository>(),
            (inventarioConcurrency ?? new Mock<IInventarioConcurrencyService>()).Object,
            (facturaRepo ?? new Mock<IFacturaRepository>()).Object,
            Mock.Of<IMovimientoInventarioRepository>(),
            movimientoFinancieroRepo.Object,
            empresa.Object,
            Mock.Of<ICalculoService>(),
            currentUser.Object,
            new TestUnitOfWork(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ITipoClientePredeterminadoResolver>());
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<Task> operation) => operation();
    }
}
