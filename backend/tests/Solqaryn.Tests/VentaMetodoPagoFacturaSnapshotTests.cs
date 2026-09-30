using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class VentaMetodoPagoFacturaSnapshotTests
{
    [Fact]
    public async Task ConfirmarAsync_Copia_Codigo_Y_Nombre_Del_MetodoPago_A_Factura()
    {
        var ventaRepo = new Mock<IVentaRepository>();
        var clienteRepo = new Mock<IClienteRepository>();
        var productoRepo = new Mock<IProductoRepository>();
        var varianteRepo = new Mock<IProductoVarianteRepository>();
        var inventario = new Mock<IInventarioConcurrencyService>();
        var facturaRepo = new Mock<IFacturaRepository>();
        var movInvRepo = new Mock<IMovimientoInventarioRepository>();
        var movFinRepo = new Mock<IMovimientoFinancieroRepository>();
        var empresa = new Mock<IEmpresaConfiguracionService>();
        var calculo = new Mock<ICalculoService>();
        var currentUser = new Mock<ICurrentUserService>();
        var auditoria = new Mock<IAuditoriaService>();
        var predeterminado = new Mock<ITipoClientePredeterminadoResolver>();

        var metodoPago = new Solqaryn.Domain.Entities.Catalogos.MetodoPago
        {
            Id = 2,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia",
            Activo = true
        };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Mouse",
            Cantidad = 10,
            Costo = 5,
            Precio = 10
        };
        var venta = new Venta
        {
            Id = 1,
            NumeroVenta = "VEN-000001",
            ClienteNombre = "Cliente final",
            Estado = EstadoDocumento.Borrador,
            MetodoPagoId = metodoPago.Id,
            MetodoPagoCatalogo = metodoPago,
            EstadoPago = EstadoPago.Pagado,
            Total = 20
        };
        venta.Detalles.Add(new VentaDetalle
        {
            ProductoId = producto.Id,
            Cantidad = 2,
            PrecioUnitario = 10,
            CostoUnitarioSnapshot = 5,
            Subtotal = 20,
            UtilidadBruta = 10,
            ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = "Marca",
            ProductoModeloSnapshot = "Modelo"
        });

        ventaRepo.Setup(r => r.GetByIdForUpdateAsync(venta.Id)).ReturnsAsync(venta);
        ventaRepo.Setup(r => r.GetByIdAsync(venta.Id)).ReturnsAsync(venta);
        ventaRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        inventario.Setup(x => x.BloquearYValidarInventarioAsync(
                It.IsAny<IEnumerable<InventarioDemanda>>(), true))
            .ReturnsAsync(new InventarioLockSet(
                new Dictionary<int, Producto> { [producto.Id] = producto },
                new Dictionary<int, ProductoVariante>(),
                new List<InventarioDemanda> { new(producto.Id, null, 2) }));
        empresa.Setup(x => x.GetActivaEntidadAsync()).ReturnsAsync(new EmpresaConfiguracion());
        currentUser.Setup(x => x.UsuarioId).Returns(4);
        currentUser.Setup(x => x.NombreUsuario).Returns("vendedor1");
        currentUser.Setup(x => x.NombreCompleto).Returns("Vendedor Uno");
        predeterminado.Setup(x => x.ResolverIdPredeterminadoAsync()).ReturnsAsync(1);

        Factura? facturaCreada = null;
        facturaRepo.Setup(r => r.AddAsync(It.IsAny<Factura>()))
            .Callback<Factura>(f => { f.Id = 1; facturaCreada = f; })
            .Returns(Task.CompletedTask);
        facturaRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var service = new VentaService(
            ventaRepo.Object,
            clienteRepo.Object,
            productoRepo.Object,
            varianteRepo.Object,
            inventario.Object,
            facturaRepo.Object,
            movInvRepo.Object,
            movFinRepo.Object,
            empresa.Object,
            calculo.Object,
            currentUser.Object,
            new FakeUnitOfWork(),
            auditoria.Object,
            predeterminado.Object);

        await service.ConfirmarAsync(venta.Id);

        Assert.NotNull(facturaCreada);
        Assert.Equal("TRANSFERENCIA", facturaCreada!.MetodoPagoCodigoSnapshot);
        Assert.Equal("Transferencia", facturaCreada.MetodoPagoNombreSnapshot);
    }
}
