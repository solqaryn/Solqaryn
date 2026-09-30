using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N15KardexServiceContractTests
{
    [Fact]
    public async Task ConsultaKardex_ExponeContextoFisicoOrigenUsuarioYCorrrelacion()
    {
        var repository = new Mock<IMovimientoInventarioRepository>(MockBehavior.Strict);
        var movimiento = new MovimientoInventario
        {
            Id = 501,
            ProductoId = 11,
            ProductoVarianteId = 12,
            AlmacenId = 3,
            UbicacionAlmacenId = 7,
            Tipo = TipoMovimientoInventario.Salida,
            Cantidad = 2,
            StockAnterior = 8,
            StockNuevo = 6,
            CostoUnitario = 125.50m,
            CorrelationId = "venta:44:confirmar",
            CreadoPorNombreUsuario = "qa-kardex",
            Descripcion = "Salida por venta"
        };

        repository
            .Setup(r => r.GetFilteredAsync(11, "Salida", null, null))
            .ReturnsAsync(new List<MovimientoInventario> { movimiento });
        repository
            .Setup(r => r.GetOrigenesTipadosAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 501 }))))
            .ReturnsAsync(new Dictionary<int, MovimientoInventarioOrigenPersistido>
            {
                [501] = new(501, null, 44, null)
            });

        var service = new MovimientoInventarioService(repository.Object);
        var resultado = await service.GetFilteredAsync(11, "Salida", null, null);

        var dto = Assert.Single(resultado);
        Assert.Equal(12, dto.ProductoVarianteId);
        Assert.Equal(3, dto.AlmacenId);
        Assert.Equal(7, dto.UbicacionAlmacenId);
        Assert.Equal("venta:44:confirmar", dto.CorrelationId);
        Assert.Equal("Venta", dto.OrigenTipo);
        Assert.Equal(44, dto.OrigenId);
        Assert.Equal(44, dto.VentaId);
        Assert.Equal("qa-kardex", dto.CreadoPorNombreUsuario);
        Assert.Equal(125.50m, dto.CostoUnitario);
        repository.VerifyAll();
    }
}
