using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N18ReservaInventarioStockReservadoTests
{
    [Fact]
    public async Task Activar_ReservaStockSobreExistenciaAutoritativa_YNoMutaStockFisico()
    {
        var reserva = CrearBorrador(cantidad: 3);
        var existencia = CrearExistencia(reserva, stockFisico: 12, stockReservado: 2);
        var clave = new InventarioExistenciaClave(10, 20, null);

        var repository = new Mock<IReservaInventarioRepository>();
        repository.Setup(x => x.GetByIdAsync(7, It.IsAny<bool>())).ReturnsAsync(reserva);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var existencias = new Mock<IExistenciaVarianteConcurrencyService>();
        existencias.Setup(x => x.BloquearYValidarExistenciasAsync(
                It.IsAny<IEnumerable<InventarioDemandaExistencia>>(), true))
            .ReturnsAsync(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
                new[] { new InventarioDemandaExistencia(1, 10, 20, null, 3) }));
        existencias.Setup(x => x.AjustarStockReservadoPesimistaAsync(clave, 2, 5))
            .Returns(Task.CompletedTask)
            .Callback(() => existencia.EstablecerStocks(
                existencia.StockFisico, 5, existencia.StockTransito,
                existencia.StockMinimo, existencia.StockMaximo));

        var service = CrearServicio(repository, existencias);
        var resultado = await service.ActivarAsync(7);

        Assert.Equal(EstadoReservaInventario.Activa.ToString(), resultado.Estado);
        Assert.Equal(12, existencia.StockFisico);
        Assert.Equal(5, existencia.StockReservado);
        existencias.Verify(x => x.AjustarStockReservadoPesimistaAsync(clave, 2, 5), Times.Once);
    }

    [Fact]
    public async Task Activar_DosVeces_EsIdempotente_YNoDuplicaStockReservado()
    {
        var reserva = CrearBorrador(cantidad: 3);
        var existencia = CrearExistencia(reserva, stockFisico: 12, stockReservado: 2);
        var clave = new InventarioExistenciaClave(10, 20, null);

        var repository = new Mock<IReservaInventarioRepository>();
        repository.Setup(x => x.GetByIdAsync(7, It.IsAny<bool>())).ReturnsAsync(reserva);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var existencias = new Mock<IExistenciaVarianteConcurrencyService>();
        existencias.Setup(x => x.BloquearYValidarExistenciasAsync(
                It.IsAny<IEnumerable<InventarioDemandaExistencia>>(), true))
            .ReturnsAsync(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
                new[] { new InventarioDemandaExistencia(1, 10, 20, null, 3) }));
        existencias.Setup(x => x.AjustarStockReservadoPesimistaAsync(clave, 2, 5))
            .Returns(Task.CompletedTask)
            .Callback(() => existencia.EstablecerStocks(
                existencia.StockFisico, 5, existencia.StockTransito,
                existencia.StockMinimo, existencia.StockMaximo));

        var service = CrearServicio(repository, existencias);
        await service.ActivarAsync(7);
        await service.ActivarAsync(7);

        Assert.Equal(5, existencia.StockReservado);
        existencias.Verify(x => x.BloquearYValidarExistenciasAsync(
            It.IsAny<IEnumerable<InventarioDemandaExistencia>>(), true), Times.Once);
        existencias.Verify(x => x.AjustarStockReservadoPesimistaAsync(clave, 2, 5), Times.Once);
    }

    [Fact]
    public async Task Liberar_ReservaActiva_RetiraExactamenteStockReservado_YPreservaFisico()
    {
        var reserva = CrearBorrador(cantidad: 3);
        reserva.Activar(usuarioId: 5, fechaUtc: DateTime.UtcNow);
        var existencia = CrearExistencia(reserva, stockFisico: 12, stockReservado: 5);
        var clave = new InventarioExistenciaClave(10, 20, null);

        var repository = new Mock<IReservaInventarioRepository>();
        repository.Setup(x => x.GetByIdAsync(7, It.IsAny<bool>())).ReturnsAsync(reserva);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var existencias = new Mock<IExistenciaVarianteConcurrencyService>();
        existencias.Setup(x => x.BloquearYValidarExistenciasAsync(
                It.IsAny<IEnumerable<InventarioDemandaExistencia>>(), false))
            .ReturnsAsync(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
                new[] { new InventarioDemandaExistencia(1, 10, 20, null, 3) }));
        existencias.Setup(x => x.AjustarStockReservadoPesimistaAsync(clave, 5, 2))
            .Returns(Task.CompletedTask)
            .Callback(() => existencia.EstablecerStocks(
                existencia.StockFisico, 2, existencia.StockTransito,
                existencia.StockMinimo, existencia.StockMaximo));

        var service = CrearServicio(repository, existencias);
        var resultado = await service.LiberarAsync(7, new() { Motivo = "Cliente desistió" });

        Assert.Equal(EstadoReservaInventario.Liberada.ToString(), resultado.Estado);
        Assert.Equal(12, existencia.StockFisico);
        Assert.Equal(2, existencia.StockReservado);
        existencias.Verify(x => x.AjustarStockReservadoPesimistaAsync(clave, 5, 2), Times.Once);
    }

    private static ReservaInventarioService CrearServicio(
        Mock<IReservaInventarioRepository> repository,
        Mock<IExistenciaVarianteConcurrencyService> existencias)
    {
        var variantes = new Mock<IProductoVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(5);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-reservas");

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

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());

        return new ReservaInventarioService(
            repository.Object,
            variantes.Object,
            existencias.Object,
            currentUser.Object,
            auditoria.Object,
            unitOfWork.Object);
    }

    private static ExistenciaVariante CrearExistencia(
        ReservaInventario reserva,
        int stockFisico,
        int stockReservado)
    {
        var existencia = new ExistenciaVariante
        {
            Id = 91,
            ProductoVarianteId = 10,
            AlmacenId = 20,
            ProductoVariante = reserva.Detalles.Single().ProductoVariante
        };
        existencia.EstablecerStocks(
            stockFisico,
            stockReservado,
            stockTransito: 0,
            stockMinimo: 0,
            stockMaximo: null);
        return existencia;
    }

    private static ReservaInventario CrearBorrador(int cantidad)
    {
        var variante = new ProductoVariante
        {
            Id = 10,
            ProductoId = 1,
            Activo = true,
            Sku = "SKU-RSV",
            Producto = new Producto { Id = 1, Nombre = "Producto reserva" }
        };
        var detalle = new ReservaInventarioDetalle
        {
            Id = 8,
            ProductoVarianteId = variante.Id,
            ProductoVariante = variante,
            AlmacenId = 20,
            ProductoSkuSnapshot = variante.Sku
        };
        detalle.EstablecerCantidadReservada(cantidad);

        return new ReservaInventario
        {
            Id = 7,
            Numero = "RSV-TEST-0007",
            CreadoPorUsuarioId = 5,
            Detalles = new List<ReservaInventarioDetalle> { detalle }
        };
    }
}
