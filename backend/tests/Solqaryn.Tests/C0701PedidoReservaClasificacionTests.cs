using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class C0701PedidoReservaClasificacionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)] // Pedido mixto: producto válido + producto reclasificado.
    [InlineData(6)] // Respuesta de repositorio con cardinalidad correcta pero ID incorrecto.
    public async Task Confirmar_rechaza_pedido_inconsistente_antes_de_reservar(int caso)
    {
        var cotizacion = new Cotizacion
        {
            Id = 101, ClienteId = 7, ClienteNombreSnapshot = "Cliente QA",
            ClienteDocumentoSnapshot = "0801-0000-00000"
        };
        var detalle = new CotizacionDetalle
        {
            ProductoId = 11, ProductoVarianteId = 21,
            ProductoSkuSnapshot = "SKU-001", ProductoNombreSnapshot = "Producto QA"
        };
        detalle.EstablecerValores(2m, 125.50m);
        cotizacion.Detalles.Add(detalle);
        if (caso == 5)
        {
            var segundo = new CotizacionDetalle { ProductoId = 12, ProductoVarianteId = 22,
                ProductoSkuSnapshot = "SKU-002", ProductoNombreSnapshot = "Producto QA 2" };
            segundo.EstablecerValores(1m, 25m);
            cotizacion.Detalles.Add(segundo);
        }
        var fecha = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
        cotizacion.Enviar(1, fecha);
        cotizacion.Aceptar(2, fecha.AddMinutes(1));
        var pedido = PedidoVenta.CrearDesdeCotizacion(cotizacion);
        pedido.Id = 51;
        var pedidos = new Mock<IPedidoVentaRepository>(MockBehavior.Strict);
        pedidos.Setup(x => x.GetByIdForUpdateAsync(51)).ReturnsAsync(pedido);
        var reservas = new Mock<IReservaInventarioRepository>(MockBehavior.Strict);
        reservas.Setup(x => x.GetByPedidoVentaIdAsync(51, true)).ReturnsAsync((ReservaInventario?)null);
        var productos = new Mock<IProductoRepository>(MockBehavior.Strict);
        var producto = new Producto { Id = 11, Activo = caso != 1, Eliminado = caso == 2,
            TipoInventario = caso == 0 ? TipoInventario.InsumoAdministrativo : TipoInventario.MercaderiaVenta };
        var persistidos = caso switch
        {
            3 => new List<Producto>(),
            5 => new List<Producto> { producto, new() { Id = 12, Activo = true, TipoInventario = TipoInventario.InsumoAdministrativo } },
            6 => new List<Producto> { new() { Id = 99, Activo = true, TipoInventario = TipoInventario.MercaderiaVenta } },
            _ => new List<Producto> { producto }
        };
        productos.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(persistidos);
        var variantes = new Mock<IProductoVarianteRepository>(MockBehavior.Strict);
        if (caso == 4)
            variantes.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<ProductoVariante> { new() { Id = 21, ProductoId = 999, Activo = true } });
        var existencias = new Mock<IExistenciaVarianteConcurrencyService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var usuario = new Mock<ICurrentUserService>();
        usuario.SetupGet(x => x.UsuarioId).Returns(2);
        usuario.SetupGet(x => x.NombreCompleto).Returns("QA");
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> accion) => accion());
        var servicio = new PedidoVentaService(pedidos.Object, Mock.Of<ICotizacionRepository>(),
            auditoria.Object, usuario.Object, uow.Object, reservas.Object, variantes.Object,
            existencias.Object, productos.Object);
        var dto = new ConfirmarPedidoVentaDto();
        dto.Asignaciones.Add(new AsignacionReservaPedidoDto { ProductoVarianteId = 21, AlmacenId = 1, Cantidad = 2 });
        if (caso == 5)
            dto.Asignaciones.Add(new AsignacionReservaPedidoDto { ProductoVarianteId = 22, AlmacenId = 1, Cantidad = 1 });
        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.ConfirmarAsync(51, dto));
        Assert.Equal(EstadoPedidoVenta.Borrador, pedido.Estado);
        var expectedIds = caso == 5 ? new[] { 11, 12 } : new[] { 11 };
        productos.Verify(x => x.GetByIdsForUpdateAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(expectedIds))), Times.Once);
        productos.VerifyNoOtherCalls();
        if (caso == 4) variantes.Verify(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        variantes.VerifyNoOtherCalls();
        existencias.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
        pedidos.Verify(x => x.GetByIdForUpdateAsync(51), Times.Once);
        pedidos.VerifyNoOtherCalls();
        reservas.Verify(x => x.GetByPedidoVentaIdAsync(51, true), Times.Once);
        reservas.VerifyNoOtherCalls();
    }
}
