using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16KardexTransferenciaReadTests
{
    [Fact]
    public async Task GetPaged_ExponeTransferenciaComoOrigenTipadoAutoritativo()
    {
        var repository = new Mock<IMovimientoInventarioRepository>();
        var movimiento = CrearMovimiento(501, "TransferenciaInventario", 42);
        var filtro = new MovimientoInventarioQueryDto { Page = 1, PageSize = 20 };

        repository
            .Setup(x => x.GetPagedAsync(filtro))
            .ReturnsAsync((new List<MovimientoInventario> { movimiento }, 1));
        repository
            .Setup(x => x.GetOrigenesTipadosAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new Dictionary<int, MovimientoInventarioOrigenPersistido>
            {
                [501] = new(501, null, null, null, null, 42)
            });

        var service = new MovimientoInventarioService(repository.Object);

        var result = await service.GetPagedAsync(filtro);

        var dto = Assert.Single(result.Items);
        Assert.Equal("TransferenciaInventario", dto.OrigenTipo);
        Assert.Equal(42, dto.OrigenId);
        Assert.Equal(42, dto.TransferenciaInventarioId);
        Assert.Null(dto.CompraId);
        Assert.Null(dto.VentaId);
        Assert.Null(dto.ConsumoInsumoId);
        Assert.Null(dto.AjusteInventarioId);
    }

    [Theory]
    [InlineData("TransferenciaInventario")]
    [InlineData("Transferencia")]
    public async Task GetPaged_PreservaOrigenTransferenciaLegacyCuandoAunNoExisteFkTipada(string referenciaTipo)
    {
        var repository = new Mock<IMovimientoInventarioRepository>();
        var movimiento = CrearMovimiento(502, referenciaTipo, 77);
        var filtro = new MovimientoInventarioQueryDto { Page = 1, PageSize = 20 };

        repository
            .Setup(x => x.GetPagedAsync(filtro))
            .ReturnsAsync((new List<MovimientoInventario> { movimiento }, 1));
        repository
            .Setup(x => x.GetOrigenesTipadosAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new Dictionary<int, MovimientoInventarioOrigenPersistido>());

        var service = new MovimientoInventarioService(repository.Object);

        var result = await service.GetPagedAsync(filtro);

        var dto = Assert.Single(result.Items);
        Assert.Equal("TransferenciaInventario", dto.OrigenTipo);
        Assert.Equal(77, dto.OrigenId);
        Assert.Equal(77, dto.TransferenciaInventarioId);
        Assert.Equal(referenciaTipo, dto.ReferenciaTipo);
        Assert.Equal(77, dto.ReferenciaId);
    }

    private static MovimientoInventario CrearMovimiento(int id, string referenciaTipo, int referenciaId) => new()
    {
        Id = id,
        ProductoId = 10,
        Tipo = TipoMovimientoInventario.Salida,
        Causa = CausaMovimientoInventario.TransferenciaDespacho,
        Cantidad = 4,
        StockAnterior = 12,
        StockNuevo = 8,
        CorrelationId = $"transferencia:{referenciaId}:despachar",
        ReferenciaTipo = referenciaTipo,
        ReferenciaId = referenciaId,
        Fecha = new DateTime(2026, 8, 16, 9, 0, 0, DateTimeKind.Utc)
    };
}
