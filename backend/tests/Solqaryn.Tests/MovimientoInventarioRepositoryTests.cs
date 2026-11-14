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

public class MovimientoInventarioRepositoryTests
{
    [Fact]
    public async Task GetPagedAsync_Filtra_Contexto_Origen_Correlacion_Y_Pagina()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);
        var fecha = new DateTime(2026, 8, 15, 20, 0, 0, DateTimeKind.Utc);

        var producto = new Producto
        {
            Id = 10,
            Nombre = "Producto Kardex",
            Activo = true
        };
        producto.Variantes.Add(new ProductoVariante
        {
            Id = 101,
            ProductoId = producto.Id,
            Producto = producto,
            Sku = "KDX-101",
            Activo = true
        });
        context.Productos.Add(producto);

        context.MovimientosInventario.AddRange(
            new MovimientoInventario
            {
                ProductoId = 10,
                ProductoVarianteId = 101,
                AlmacenId = 4,
                UbicacionAlmacenId = 8,
                Tipo = TipoMovimientoInventario.Entrada,
                Causa = CausaMovimientoInventario.AnulacionVenta,
                Cantidad = 2,
                StockAnterior = 3,
                StockNuevo = 5,
                CorrelationId = "venta:9:anular",
                ReferenciaTipo = "VentaAnulada",
                ReferenciaId = 9,
                VentaId = 9,
                Fecha = fecha
            },
            new MovimientoInventario
            {
                ProductoId = 10,
                ProductoVarianteId = 101,
                AlmacenId = 4,
                UbicacionAlmacenId = 8,
                Tipo = TipoMovimientoInventario.Salida,
                Causa = CausaMovimientoInventario.NoEspecificada,
                Cantidad = 1,
                StockAnterior = 5,
                StockNuevo = 4,
                CorrelationId = "venta:10:confirmar",
                ReferenciaTipo = "Venta",
                ReferenciaId = 10,
                VentaId = 10,
                Fecha = fecha.AddMinutes(1)
            });
        await context.SaveChangesAsync();

        var query = new MovimientoInventarioQueryDto
        {
            ProductoId = 10,
            ProductoVarianteId = 101,
            AlmacenId = 4,
            UbicacionAlmacenId = 8,
            Tipo = "Entrada",
            Causa = "AnulacionVenta",
            CorrelationId = " venta:9:anular ",
            OrigenTipo = "Venta",
            OrigenId = 9,
            Desde = fecha.AddMinutes(-1),
            Hasta = fecha.AddMinutes(1),
            Page = 1,
            PageSize = 10
        };

        var (items, totalCount) = await repo.GetPagedAsync(query);

        Assert.Equal(1, totalCount);
        var item = Assert.Single(items);
        Assert.Equal(9, item.VentaId);
        Assert.Equal("venta:9:anular", item.CorrelationId);
        Assert.Equal(4, item.AlmacenId);
        Assert.Equal(8, item.UbicacionAlmacenId);
    }

    [Theory]
    [InlineData("tipo-invalido", null)]
    [InlineData(null, "causa-invalida")]
    public async Task GetPagedAsync_EnumInvalido_DevuelvePaginaVacia(string? tipo, string? causa)
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);
        context.MovimientosInventario.Add(CrearMovimiento(5, 50, "Venta", 20));
        await context.SaveChangesAsync();

        var (items, totalCount) = await repo.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            Tipo = tipo,
            Causa = causa,
            Page = 1,
            PageSize = 20
        });

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_OtraVarianteDelMismoProducto_NoBloquea()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(10, 101, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(10, 102, "Venta", 2));
        await context.SaveChangesAsync();

        var existe = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 10 });

        Assert.False(existe);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_MismaVariante_Bloquea()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(20, 201, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(20, 201, "Venta", 2));
        await context.SaveChangesAsync();

        var existe = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 20 });

        Assert.True(existe);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_ProductoHistoricoSinVariante_ExigeClaveNulaExacta()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(30, null, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(30, 301, "Ajuste", 2));
        await context.SaveChangesAsync();

        var conOtraVariante = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 30 });
        Assert.False(conOtraVariante);

        context.MovimientosInventario.Add(CrearMovimiento(30, null, "Ajuste", 3));
        await context.SaveChangesAsync();

        var conMismaClave = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 30 });
        Assert.True(conMismaClave);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_ProductosDuplicados_SeConsolidanSinCambiarResultado()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(40, 401, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(40, 401, "Venta", 2));
        await context.SaveChangesAsync();

        var existe = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 40, 40 });

        Assert.True(existe);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_CompraConDosVariantes_DetectaSoloClavesOriginales()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var originalA = CrearMovimiento(50, 501, "Compra", 7);
        var originalB = CrearMovimiento(50, 502, "Compra", 7);
        context.MovimientosInventario.AddRange(originalA, originalB);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(50, 503, "Venta", 8));
        await context.SaveChangesAsync();

        Assert.False(await repo.ExisteMovimientoPosteriorAsync(originalB.Id, new[] { 50 }));

        context.MovimientosInventario.Add(CrearMovimiento(50, 502, "Venta", 9));
        await context.SaveChangesAsync();

        Assert.True(await repo.ExisteMovimientoPosteriorAsync(originalB.Id, new[] { 50 }));
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"movimientos-{Guid.NewGuid():N}")
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

    private static MovimientoInventario CrearMovimiento(
        int productoId,
        int? varianteId,
        string referenciaTipo,
        int referenciaId) => new()
    {
        ProductoId = productoId,
        ProductoVarianteId = varianteId,
        Tipo = referenciaTipo == "Compra" ? TipoMovimientoInventario.Entrada : TipoMovimientoInventario.Salida,
        Cantidad = 1,
        StockAnterior = 1,
        StockNuevo = referenciaTipo == "Compra" ? 2 : 0,
        CorrelationId = $"{referenciaTipo.ToLowerInvariant()}:{referenciaId}",
        ReferenciaTipo = referenciaTipo,
        ReferenciaId = referenciaId
    };
}
