using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class MovimientoInventarioRepositoryCorrelationTests
{
    [Fact]
    public async Task OrigenTipado_Legacy_SinCorrelationId_RecibeCorrelacionCompartidaPorOperacion()
    {
        await using var context = CrearContexto();
        var repository = CrearRepositorio(context);
        var primero = new MovimientoInventario { Tipo = TipoMovimientoInventario.Entrada };
        var segundo = new MovimientoInventario { Tipo = TipoMovimientoInventario.Entrada };

        await repository.AddConOrigenTipadoAsync(primero, OrigenMovimientoInventario.DesdeCompra(42));
        await repository.AddConOrigenTipadoAsync(segundo, OrigenMovimientoInventario.DesdeCompra(42));

        Assert.Equal("compra:42", primero.CorrelationId);
        Assert.Equal(primero.CorrelationId, segundo.CorrelationId);
    }

    [Theory]
    [InlineData("venta", 21)]
    [InlineData("consumoinsumo", 22)]
    [InlineData("ajusteinventario", 23)]
    public async Task Fallback_Cubre_Todos_Los_Origenes_Tipados_Sin_Inventar_Contexto_Fisico(
        string esperadoPrefijo,
        int documentoId)
    {
        await using var context = CrearContexto();
        var repository = CrearRepositorio(context);
        var movimiento = new MovimientoInventario { Tipo = TipoMovimientoInventario.Salida };
        var origen = esperadoPrefijo switch
        {
            "venta" => OrigenMovimientoInventario.DesdeVenta(documentoId),
            "consumoinsumo" => OrigenMovimientoInventario.DesdeConsumoInsumo(documentoId),
            "ajusteinventario" => OrigenMovimientoInventario.DesdeAjusteInventario(documentoId),
            _ => throw new InvalidOperationException()
        };

        await repository.AddConOrigenTipadoAsync(movimiento, origen);

        Assert.Equal($"{esperadoPrefijo}:{documentoId}", movimiento.CorrelationId);
        Assert.Null(movimiento.AlmacenId);
        Assert.Null(movimiento.UbicacionAlmacenId);
    }

    [Fact]
    public async Task Anulacion_Compra_Usa_Correlacion_Distinta_De_La_Confirmacion()
    {
        await using var context = CrearContexto();
        var repository = CrearRepositorio(context);
        var confirmacion = new MovimientoInventario { Tipo = TipoMovimientoInventario.Entrada };
        var anulacion = new MovimientoInventario
        {
            Tipo = TipoMovimientoInventario.Salida,
            Causa = CausaMovimientoInventario.AnulacionCompra
        };

        await repository.AddConOrigenTipadoAsync(confirmacion, OrigenMovimientoInventario.DesdeCompra(7));
        await repository.AddConOrigenTipadoAsync(anulacion, OrigenMovimientoInventario.DesdeCompra(7));

        Assert.Equal("compra:7", confirmacion.CorrelationId);
        Assert.Equal("compraanulada:7", anulacion.CorrelationId);
        Assert.NotEqual(confirmacion.CorrelationId, anulacion.CorrelationId);
    }

    [Fact]
    public async Task Anulacion_Venta_Usa_Correlacion_Distinta_De_La_Confirmacion()
    {
        await using var context = CrearContexto();
        var repository = CrearRepositorio(context);
        var confirmacion = new MovimientoInventario { Tipo = TipoMovimientoInventario.Salida };
        var anulacion = new MovimientoInventario
        {
            Tipo = TipoMovimientoInventario.Entrada,
            Causa = CausaMovimientoInventario.AnulacionVenta
        };

        await repository.AddConOrigenTipadoAsync(confirmacion, OrigenMovimientoInventario.DesdeVenta(8));
        await repository.AddConOrigenTipadoAsync(anulacion, OrigenMovimientoInventario.DesdeVenta(8));

        Assert.Equal("venta:8", confirmacion.CorrelationId);
        Assert.Equal("ventaanulada:8", anulacion.CorrelationId);
        Assert.NotEqual(confirmacion.CorrelationId, anulacion.CorrelationId);
    }

    [Fact]
    public async Task CorrelationId_Explicito_Valido_Se_Normaliza_Sin_Sobrescribirse()
    {
        await using var context = CrearContexto();
        var repository = CrearRepositorio(context);
        var movimiento = new MovimientoInventario
        {
            Tipo = TipoMovimientoInventario.Salida,
            CorrelationId = "  venta:99:transaccion-explicita  "
        };

        await repository.AddConOrigenTipadoAsync(movimiento, OrigenMovimientoInventario.DesdeVenta(99));

        Assert.Equal("venta:99:transaccion-explicita", movimiento.CorrelationId);
    }

    [Fact]
    public async Task CorrelationId_Explicito_Inseguro_Falla_Cerrado_Antes_De_Trackear()
    {
        await using var context = CrearContexto();
        var repository = CrearRepositorio(context);
        var movimiento = new MovimientoInventario
        {
            Tipo = TipoMovimientoInventario.Salida,
            CorrelationId = "venta/99"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddConOrigenTipadoAsync(movimiento, OrigenMovimientoInventario.DesdeVenta(99)));

        Assert.Empty(context.ChangeTracker.Entries<MovimientoInventario>());
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n15-repository-correlation-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static MovimientoInventarioRepository CrearRepositorio(AppDbContext context)
    {
        var scope = new Mock<IUsuarioScopeService>();
        return new MovimientoInventarioRepository(context, scope.Object);
    }
}
