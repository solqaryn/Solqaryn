using Solqaryn.API.Middleware;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexSecurityObservabilityTests
{
    [Fact]
    public async Task GetPagedAsync_ScopeNoAdministrador_SoloExponeMovimientosDelUsuarioActual()
    {
        await using var context = CrearContexto();
        context.Productos.Add(new Producto { Id = 1, Nombre = "Producto Kardex", Activo = true });
        context.MovimientosInventario.AddRange(
            CrearMovimiento(1, 7, "venta:10:confirmar"),
            CrearMovimiento(1, 8, "venta:11:confirmar"));
        await context.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(7, 2, "Operador", false));
        var repository = new MovimientoInventarioRepository(context, scope.Object);

        var (items, totalCount) = await repository.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            Page = 1,
            PageSize = 25
        });

        Assert.Equal(1, totalCount);
        var movimiento = Assert.Single(items);
        Assert.Equal(7, movimiento.CreadoPorUsuarioId);
        Assert.Equal("venta:10:confirmar", movimiento.CorrelationId);
    }

    [Fact]
    public async Task GetPagedAsync_ScopeNoResuelto_FallaCerradoConPaginaVacia()
    {
        await using var context = CrearContexto();
        context.Productos.Add(new Producto { Id = 1, Nombre = "Producto Kardex", Activo = true });
        context.MovimientosInventario.Add(CrearMovimiento(1, 7, "venta:10:confirmar"));
        await context.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync()).ReturnsAsync((UsuarioScopeActual?)null);
        var repository = new MovimientoInventarioRepository(context, scope.Object);

        var (items, totalCount) = await repository.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            Page = 1,
            PageSize = 25
        });

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task CorrelationMiddleware_HeaderSeguro_SePropagaARequestResponseTraceYScope()
    {
        const string correlationId = "kardex:consulta:abc-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = $"  {correlationId}  ";
        string? observado = null;

        var middleware = new CorrelationIdMiddleware(
            next: ctx =>
            {
                observado = ctx.Items[CorrelationIdMiddleware.ItemKey]?.ToString();
                return Task.CompletedTask;
            },
            Mock.Of<ILogger<CorrelationIdMiddleware>>());

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, observado);
        Assert.Equal(correlationId, context.TraceIdentifier);
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]?.ToString());
    }

    [Theory]
    [InlineData("kardex/consulta")]
    [InlineData("correlation con espacios")]
    public async Task CorrelationMiddleware_HeaderInseguro_SeReemplazaPorIdentificadorGenerado(string provided)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            Mock.Of<ILogger<CorrelationIdMiddleware>>());

        await middleware.InvokeAsync(context);

        var generated = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.NotEqual(provided, generated);
        Assert.Equal(32, generated.Length);
        Assert.All(generated, c => Assert.True(char.IsAsciiHexDigit(c)));
        Assert.Equal(generated, context.TraceIdentifier);
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n15-kardex-security-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static MovimientoInventario CrearMovimiento(int productoId, int usuarioId, string correlationId) => new()
    {
        ProductoId = productoId,
        Tipo = TipoMovimientoInventario.Salida,
        Causa = CausaMovimientoInventario.Venta,
        Cantidad = 1,
        StockAnterior = 2,
        StockNuevo = 1,
        CorrelationId = correlationId,
        ReferenciaTipo = "Venta",
        ReferenciaId = usuarioId,
        VentaId = usuarioId,
        CreadoPorUsuarioId = usuarioId,
        CreadoPorNombreUsuario = $"usuario-{usuarioId}",
        Fecha = DateTime.UtcNow
    };
}
