using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class MovimientoInventarioServiceTests
{
    [Fact]
    public async Task GetFilteredAsync_Incluye_Contexto_Empresarial_Completo_Y_Origen_Tipado()
    {
        var producto = new Producto { Id = 2, Nombre = "Teclado", Marca = "Logitech", Modelo = "K120" };
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 22,
            Url = "https://res.cloudinary.com/demo/image/upload/teclado.webp",
            EsPrincipal = true,
            Orden = 0
        });
        var movimiento = CrearMovimiento(producto);
        var repository = new Mock<IMovimientoInventarioRepository>();
        repository
            .Setup(r => r.GetFilteredAsync(null, null, null, null))
            .ReturnsAsync(new List<MovimientoInventario> { movimiento });
        repository
            .Setup(r => r.GetOrigenesTipadosAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(CrearOrigenes(movimiento));
        var service = new MovimientoInventarioService(repository.Object);

        var resultado = await service.GetFilteredAsync(null, null, null, null);

        ValidarDto(resultado.Single());
    }

    [Fact]
    public async Task GetPagedAsync_Conserva_Paginacion_Contexto_Y_Origen_Tipado()
    {
        var producto = new Producto { Id = 2, Nombre = "Teclado", Marca = "Logitech", Modelo = "K120" };
        var movimiento = CrearMovimiento(producto);
        var query = new MovimientoInventarioQueryDto
        {
            ProductoVarianteId = 12,
            AlmacenId = 4,
            CorrelationId = "ajuste:2026-08-15:3",
            Page = 2,
            PageSize = 25
        };
        var repository = new Mock<IMovimientoInventarioRepository>();
        repository
            .Setup(r => r.GetPagedAsync(query))
            .ReturnsAsync((new List<MovimientoInventario> { movimiento }, 51));
        repository
            .Setup(r => r.GetOrigenesTipadosAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(CrearOrigenes(movimiento));
        var service = new MovimientoInventarioService(repository.Object);

        var resultado = await service.GetPagedAsync(query);

        Assert.Equal(2, resultado.Page);
        Assert.Equal(25, resultado.PageSize);
        Assert.Equal(51, resultado.TotalCount);
        Assert.Equal(3, resultado.TotalPages);
        ValidarDto(resultado.Items.Single());
    }

    [Fact]
    public async Task GetPagedAsync_Rechaza_Rango_De_Fechas_Invertido_Antes_De_Consultar()
    {
        var repository = new Mock<IMovimientoInventarioRepository>();
        var service = new MovimientoInventarioService(repository.Object);
        var query = new MovimientoInventarioQueryDto
        {
            Desde = new DateTime(2026, 8, 16),
            Hasta = new DateTime(2026, 8, 15)
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetPagedAsync(query));

        Assert.Contains("fecha inicial", ex.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(r => r.GetPagedAsync(It.IsAny<MovimientoInventarioQueryDto>()), Times.Never);
    }

    private static MovimientoInventario CrearMovimiento(Producto producto) => new()
    {
        Id = 3,
        ProductoId = producto.Id,
        ProductoVarianteId = 12,
        AlmacenId = 4,
        UbicacionAlmacenId = 9,
        Producto = producto,
        Tipo = TipoMovimientoInventario.Entrada,
        Causa = CausaMovimientoInventario.AjusteManual,
        Cantidad = 2,
        StockAnterior = 0,
        StockNuevo = 2,
        CostoUnitario = 125.50m,
        PrecioUnitario = 180m,
        CorrelationId = "ajuste:2026-08-15:3",
        ReferenciaTipo = "AjusteInventario",
        ReferenciaId = 5
    };

    private static Dictionary<int, MovimientoInventarioOrigenPersistido> CrearOrigenes(MovimientoInventario movimiento) =>
        new()
        {
            [movimiento.Id] = new(movimiento.Id, null, null, null, 5)
        };

    private static void ValidarDto(MovimientoInventarioDto dto)
    {
        Assert.Equal(12, dto.ProductoVarianteId);
        Assert.Equal(4, dto.AlmacenId);
        Assert.Equal(9, dto.UbicacionAlmacenId);
        Assert.Equal("AjusteManual", dto.Causa);
        Assert.Equal(125.50m, dto.CostoUnitario);
        Assert.Equal(180m, dto.PrecioUnitario);
        Assert.Equal("ajuste:2026-08-15:3", dto.CorrelationId);
        Assert.Equal("AjusteInventario", dto.OrigenTipo);
        Assert.Equal(5, dto.OrigenId);
        Assert.Equal(5, dto.AjusteInventarioId);
        Assert.Null(dto.CompraId);
        Assert.Null(dto.VentaId);
        Assert.Null(dto.ConsumoInsumoId);
        Assert.Equal("AjusteInventario", dto.ReferenciaTipo);
        Assert.Equal(5, dto.ReferenciaId);
    }
}
