using System.Reflection;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class VentaFiscalSnapshotTests
{
    [Fact]
    public async Task CalcularTotales_Y_ToDto_PreservanIncluidoEnPrecioComoSnapshotHistorico()
    {
        var productoRepository = new Mock<IProductoRepository>();
        productoRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Producto
            {
                Id = 1,
                Nombre = "Producto fiscal",
                Marca = "Marca",
                Modelo = "Modelo",
                CategoriaId = 7,
                Activo = true
            });

        var calculoService = new Mock<ICalculoService>();
        calculoService
            .Setup(x => x.CalcularVentaAsync(
                It.IsAny<List<DetalleCalculoInput>>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ResultadoCalculoDto
            {
                ImporteBruto = 115m,
                ImporteProductos = 115m,
                Subtotal = 100m,
                TotalImpuesto = 15m,
                ImpuestoIncluido = 15m,
                Total = 115m,
                ImpuestosAplicados =
                [
                    new ImpuestoAplicadoDto
                    {
                        ImpuestoId = 9,
                        Nombre = "ISV histórico",
                        Codigo = "ISV15",
                        Tasa = 15m,
                        BaseImponible = 100m,
                        Monto = 15m,
                        IncluidoEnPrecio = true
                    }
                ]
            });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.RolId).Returns(3);

        var service = new VentaService(
            Mock.Of<IVentaRepository>(),
            Mock.Of<IClienteRepository>(),
            productoRepository.Object,
            Mock.Of<IProductoVarianteRepository>(),
            Mock.Of<IInventarioConcurrencyService>(),
            Mock.Of<IFacturaRepository>(),
            Mock.Of<IMovimientoInventarioRepository>(),
            Mock.Of<IMovimientoFinancieroRepository>(),
            Mock.Of<IEmpresaConfiguracionService>(),
            calculoService.Object,
            currentUser.Object,
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ITipoClientePredeterminadoResolver>());

        var venta = new Venta
        {
            ClienteNombre = "Cliente final",
            Detalles =
            [
                new VentaDetalle
                {
                    ProductoId = 1,
                    Cantidad = 1,
                    PrecioUnitario = 115m,
                    CostoUnitarioSnapshot = 70m,
                    Subtotal = 115m,
                    UtilidadBruta = 45m,
                    ProductoNombreSnapshot = "Producto fiscal",
                    ProductoMarcaSnapshot = "Marca",
                    ProductoModeloSnapshot = "Modelo"
                }
            ]
        };

        var calcular = typeof(VentaService).GetMethod(
            "CalcularTotalesAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("No se encontró CalcularTotalesAsync.");
        var tarea = calcular.Invoke(service, [venta, null, null, false, null]) as Task
            ?? throw new InvalidOperationException("CalcularTotalesAsync no devolvió Task.");
        await tarea;

        var snapshot = Assert.Single(venta.ImpuestosAplicados);
        Assert.True(snapshot.IncluidoEnPrecioSnapshot);
        Assert.Equal("ISV15", snapshot.ImpuestoCodigoSnapshot);
        Assert.Equal(15m, snapshot.TasaSnapshot);
        Assert.Equal(15m, snapshot.MontoAplicado);

        var toDto = typeof(VentaService).GetMethod(
            "ToDto",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("No se encontró ToDto.");
        var dto = toDto.Invoke(null, [venta]) as VentaDto
            ?? throw new InvalidOperationException("ToDto no devolvió VentaDto.");

        var impuestoDto = Assert.Single(dto.ImpuestosAplicados);
        Assert.True(impuestoDto.IncluidoEnPrecio);
        Assert.Equal("ISV15", impuestoDto.Codigo);
    }
}
