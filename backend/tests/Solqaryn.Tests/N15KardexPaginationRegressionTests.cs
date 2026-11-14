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

public sealed class N15KardexPaginationRegressionTests
{
    [Fact]
    public async Task GetPagedAsync_MismaFecha_UsaIdDescComoDesempateYNoDuplicaEntrePaginas()
    {
        await using var context = CrearContexto();
        context.Productos.Add(new Producto { Id = 1, Nombre = "Producto Kardex", Activo = true });
        var fecha = new DateTime(2026, 8, 15, 18, 45, 0, DateTimeKind.Utc);

        for (var i = 1; i <= 21; i++)
        {
            context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = 1,
                Tipo = TipoMovimientoInventario.Salida,
                Causa = CausaMovimientoInventario.Venta,
                Cantidad = 1,
                StockAnterior = 100 - i,
                StockNuevo = 99 - i,
                CorrelationId = $"venta:{i}:confirmar",
                ReferenciaTipo = "Venta",
                ReferenciaId = i,
                VentaId = i,
                CreadoPorUsuarioId = 7,
                CreadoPorNombreUsuario = "qa-kardex",
                Fecha = fecha
            });
        }
        await context.SaveChangesAsync();

        var repository = new MovimientoInventarioRepository(context, CrearScopeAdministrador().Object);
        var (pagina1, total1) = await repository.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            Page = 1,
            PageSize = 10
        });
        var (pagina2, total2) = await repository.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            Page = 2,
            PageSize = 10
        });
        var (pagina3, total3) = await repository.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            Page = 3,
            PageSize = 10
        });

        Assert.Equal(21, total1);
        Assert.Equal(21, total2);
        Assert.Equal(21, total3);
        Assert.Equal(10, pagina1.Count);
        Assert.Equal(10, pagina2.Count);
        Assert.Single(pagina3);

        Assert.True(pagina1.Zip(pagina1.Skip(1), (a, b) => a.Id > b.Id).All(x => x));
        Assert.True(pagina2.Zip(pagina2.Skip(1), (a, b) => a.Id > b.Id).All(x => x));

        var ids = pagina1.Concat(pagina2).Concat(pagina3).Select(m => m.Id).ToArray();
        Assert.Equal(21, ids.Distinct().Count());
        Assert.Equal(ids.OrderByDescending(x => x), ids);
    }

    [Fact]
    public void MovimientoInventarioQueryDto_NormalizaPaginaYLimitaPageSizeA200()
    {
        var query = new MovimientoInventarioQueryDto
        {
            Page = -99,
            PageSize = 10_000
        };

        Assert.Equal(1, query.Page);
        Assert.Equal(200, query.PageSize);

        query.PageSize = 0;
        Assert.Equal(1, query.PageSize);
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n15-kardex-pagination-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<IUsuarioScopeService> CrearScopeAdministrador()
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));
        return scope;
    }
}
