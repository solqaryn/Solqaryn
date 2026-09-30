using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class MovimientoInventarioRepositoryKardexOrderingTests
{
    [Theory]
    [InlineData("asc", true)]
    [InlineData("desc", false)]
    public async Task GetPagedAsync_FechaEmpatada_UsaIdEnMismoSentido(string direction, bool ascending)
    {
        await using var context = CrearContexto();
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));
        var repo = new MovimientoInventarioRepository(context, scope.Object);
        var fecha = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

        context.MovimientosInventario.AddRange(
            CrearMovimiento(10, fecha),
            CrearMovimiento(11, fecha),
            CrearMovimiento(12, fecha));
        await context.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            Page = 1,
            PageSize = 10,
            SortBy = "Fecha",
            SortDirection = direction
        });

        Assert.Equal(3, total);
        var ids = items.Select(x => x.Id).ToArray();
        var expected = ascending
            ? ids.OrderBy(x => x).ToArray()
            : ids.OrderByDescending(x => x).ToArray();
        Assert.Equal(expected, ids);
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"kardex-order-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static MovimientoInventario CrearMovimiento(int productoId, DateTime fecha) => new()
    {
        ProductoId = productoId,
        Tipo = TipoMovimientoInventario.Entrada,
        Cantidad = 1,
        StockAnterior = 0,
        StockNuevo = 1,
        CorrelationId = $"test:{productoId}",
        ReferenciaTipo = "AjusteInventario",
        ReferenciaId = productoId,
        Fecha = fecha
    };
}
