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

public class VentaMetodoPagoServiceTests
{
    private readonly Mock<IVentaRepository> _ventaRepo = new();
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IProductoRepository> _productoRepo = new();
    private readonly Mock<IProductoVarianteRepository> _varianteRepo = new();
    private readonly Mock<IInventarioConcurrencyService> _inventarioConcurrency = new();
    private readonly Mock<IFacturaRepository> _facturaRepo = new();
    private readonly Mock<IMovimientoInventarioRepository> _movInvRepo = new();
    private readonly Mock<IMovimientoFinancieroRepository> _movFinRepo = new();
    private readonly Mock<IEmpresaConfiguracionService> _empresa = new();
    private readonly Mock<ICalculoService> _calculo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<ITipoClientePredeterminadoResolver> _predeterminado = new();

    public VentaMetodoPagoServiceTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("tester");
        _ventaRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _ventaRepo.Setup(x => x.AddAsync(It.IsAny<Venta>()))
            .Callback<Venta>(v => v.Id = 77)
            .Returns(Task.CompletedTask);

        var producto = new Producto
        {
            Id = 1,
            Nombre = "Mouse",
            Cantidad = 10,
            Costo = 5m,
            Precio = 10m,
            Activo = true
        };
        var variante = new ProductoVariante
        {
            Id = 10,
            ProductoId = 1,
            Sku = "MOUSE-BASE",
            Cantidad = 10,
            Costo = 5m,
            Precio = 10m,
            Activo = true
        };
        _productoRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(producto);
        _varianteRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(variante);
        _calculo.Setup(x => x.CalcularVentaAsync(
                It.IsAny<List<DetalleCalculoInput>>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ResultadoCalculoDto
            {
                ImporteBruto = 10m,
                ImporteProductos = 10m,
                Subtotal = 10m,
                Total = 10m
            });
    }

    [Fact]
    public async Task CreateAsync_Resuelve_Catalogo_Y_Establece_Fk()
    {
        var catalogo = new CatalogoMetodoPago { Id = 41, Codigo = "TARJETA", Nombre = "Tarjeta" };
        _ventaRepo.Setup(x => x.GetMetodoPagoPorCodigoONombreAsync("Tarjeta")).ReturnsAsync(catalogo);
        Venta? persistida = null;
        _ventaRepo.Setup(x => x.AddAsync(It.IsAny<Venta>()))
            .Callback<Venta>(v => { v.Id = 77; persistida = v; })
            .Returns(Task.CompletedTask);

        await CrearServicio().CreateAsync(CrearDto("Tarjeta"));

        Assert.NotNull(persistida);
        Assert.Equal(41, persistida!.MetodoPagoId);
        Assert.Same(catalogo, persistida.MetodoPagoCatalogo);
        Assert.Equal(MetodoPago.Tarjeta, persistida.MetodoPago);
    }

    [Fact]
    public async Task CreateAsync_Metodo_Inexistente_Falla_Sin_Default_Silencioso()
    {
        _ventaRepo.Setup(x => x.GetMetodoPagoPorCodigoONombreAsync("Bitcoin"))
            .ReturnsAsync((CatalogoMetodoPago?)null);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => CrearServicio().CreateAsync(CrearDto("Bitcoin")));

        Assert.Contains("no existe en el catálogo", error.Message);
        _ventaRepo.Verify(x => x.AddAsync(It.IsAny<Venta>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Reemplaza_Autoridad_Por_Catalogo_Relacional()
    {
        var catalogo = new CatalogoMetodoPago { Id = 52, Codigo = "TRANSFERENCIA", Nombre = "Transferencia" };
        var venta = new Venta
        {
            Id = 9,
            NumeroVenta = "VEN-000009",
            Estado = EstadoDocumento.Borrador,
            ClienteNombre = "Cliente final",
            MetodoPago = MetodoPago.Efectivo
        };
        _ventaRepo.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(venta);
        _ventaRepo.Setup(x => x.GetMetodoPagoPorCodigoONombreAsync("Transferencia")).ReturnsAsync(catalogo);

        await CrearServicio().UpdateAsync(9, new UpdateVentaDto
        {
            MetodoPago = "Transferencia",
            EstadoPago = "Pendiente",
            Detalles = { new VentaDetalleInputDto { ProductoId = 1, ProductoVarianteId = 10, Cantidad = 1, PrecioUnitario = 10m } }
        });

        Assert.Equal(52, venta.MetodoPagoId);
        Assert.Same(catalogo, venta.MetodoPagoCatalogo);
        Assert.Equal(MetodoPago.Transferencia, venta.MetodoPago);
    }

    private VentaService CrearServicio() => new(
        _ventaRepo.Object,
        _clienteRepo.Object,
        _productoRepo.Object,
        _varianteRepo.Object,
        _inventarioConcurrency.Object,
        _facturaRepo.Object,
        _movInvRepo.Object,
        _movFinRepo.Object,
        _empresa.Object,
        _calculo.Object,
        _currentUser.Object,
        new TestUnitOfWork(),
        _auditoria.Object,
        _predeterminado.Object);

    private static CreateVentaDto CrearDto(string metodoPago) => new()
    {
        MetodoPago = metodoPago,
        EstadoPago = "Pendiente",
        Detalles =
        {
            new VentaDetalleInputDto
            {
                ProductoId = 1,
                ProductoVarianteId = 10,
                Cantidad = 1,
                PrecioUnitario = 10m
            }
        }
    };

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<Task> operation) => operation();
    }
}
