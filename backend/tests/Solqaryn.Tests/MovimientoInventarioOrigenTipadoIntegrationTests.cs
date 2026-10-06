using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using MySqlConnector;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public class MovimientoInventarioOrigenTipadoIntegrationTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(GetConnectionString(dbName), mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

    private static MovimientoInventarioRepository CrearRepositorio(AppDbContext context)
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));
        return new MovimientoInventarioRepository(context, scope.Object);
    }

    [Fact]
    public async Task ConsultasDeCompra_UsanCompraId_AunqueSnapshotLegacyNoCoincida()
    {
        var dbName = $"test_n06_d1_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            await context.Database.MigrateAsync();

            var producto = new Producto
            {
                Nombre = "Producto N06 D1",
                Marca = "VAEP",
                Modelo = "D1",
                Cantidad = 2,
                Costo = 10m,
                Precio = 15m,
                Activo = true
            };
            var compra = new Compra
            {
                NumeroCompra = $"D1-{Guid.NewGuid():N}"[..18],
                ProveedorNombre = "Proveedor D1",
                Estado = EstadoDocumento.Borrador,
                EstadoPago = EstadoPago.Pendiente,
                MetodoPago = MetodoPago.Efectivo
            };
            context.Productos.Add(producto);
            context.Compras.Add(compra);
            await context.SaveChangesAsync();

            // Base aislada de prueba: se retira temporalmente el bridge/constraint C3
            // para poder demostrar que el repositorio decide por CompraId y no por
            // el snapshot legacy. Las FKs reales de C2 permanecen activas.
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS TR_MovimientosInventario_N06_OrigenTipado_BU");
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS TR_MovimientosInventario_N06_OrigenTipado_BI");
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosInventario DROP CHECK CK_MovimientosInventario_OrigenTipado_Exclusivo_N06");

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO MovimientosInventario
                    (ProductoId, Tipo, Causa, Cantidad, StockAnterior, StockNuevo,
                     ReferenciaTipo, ReferenciaId, CompraId, VentaId, ConsumoInsumoId, Fecha)
                VALUES
                    ({producto.Id}, 'Entrada', 0, 1, 0, 1,
                     'Venta', 999999, {compra.Id}, NULL, NULL, UTC_TIMESTAMP(6))
                """);

            var originalId = await context.Database
                .SqlQueryRaw<int>("SELECT Id AS Value FROM MovimientosInventario ORDER BY Id DESC LIMIT 1")
                .SingleAsync();

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO MovimientosInventario
                    (ProductoId, Tipo, Causa, Cantidad, StockAnterior, StockNuevo,
                     ReferenciaTipo, ReferenciaId, CompraId, VentaId, ConsumoInsumoId, Fecha)
                VALUES
                    ({producto.Id}, 'Salida', 0, 1, 1, 0,
                     'AjustePrueba', 123456, NULL, NULL, NULL, UTC_TIMESTAMP(6))
                """);

            var repo = CrearRepositorio(context);
            var encontrado = await repo.GetUltimoMovimientoOriginalCompraIdAsync(compra.Id);
            Assert.Equal(originalId, encontrado);
            Assert.True(await repo.ExisteMovimientoPosteriorAsync(originalId, new[] { producto.Id }));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task EscrituraTipada_EsAutoridad_YBridgeSoloCubreLegacySinFk()
    {
        var dbName = $"test_n06_d2a_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            await context.Database.MigrateAsync();

            var producto = new Producto
            {
                Nombre = "Producto N06 D2A",
                Marca = "VAEP",
                Modelo = "D2A",
                Cantidad = 3,
                Costo = 10m,
                Precio = 15m,
                Activo = true
            };
            var compra = new Compra
            {
                NumeroCompra = $"D2A-{Guid.NewGuid():N}"[..18],
                ProveedorNombre = "Proveedor D2A",
                Estado = EstadoDocumento.Borrador,
                EstadoPago = EstadoPago.Pendiente,
                MetodoPago = MetodoPago.Efectivo
            };
            context.Productos.Add(producto);
            context.Compras.Add(compra);
            await context.SaveChangesAsync();

            var repo = CrearRepositorio(context);
            await repo.AddConOrigenTipadoAsync(
                new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Causa = CausaMovimientoInventario.Compra,
                    Cantidad = 1,
                    StockAnterior = 0,
                    StockNuevo = 1,
                    Descripcion = "D2A typed-first",
                    Fecha = DateTime.UtcNow
                },
                OrigenMovimientoInventario.DesdeCompra(compra.Id));
            await context.SaveChangesAsync();

            var compraIdTipado = await context.Database
                .SqlQueryRaw<int>("SELECT CompraId AS Value FROM MovimientosInventario ORDER BY Id DESC LIMIT 1")
                .SingleAsync();
            var referenciaId = await context.Database
                .SqlQueryRaw<int>("SELECT ReferenciaId AS Value FROM MovimientosInventario ORDER BY Id DESC LIMIT 1")
                .SingleAsync();
            var referenciaTipo = await context.Database
                .SqlQueryRaw<string>("SELECT ReferenciaTipo AS Value FROM MovimientosInventario ORDER BY Id DESC LIMIT 1")
                .SingleAsync();

            Assert.Equal(compra.Id, compraIdTipado);
            Assert.Equal(compra.Id, referenciaId);
            Assert.Equal("Compra", referenciaTipo);

            // Compatibilidad transitoria: un escritor antiguo sin FKs tipadas todavía
            // recibe el bridge desde el snapshot legacy.
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO MovimientosInventario
                    (ProductoId, Tipo, Causa, Cantidad, StockAnterior, StockNuevo,
                     ReferenciaTipo, ReferenciaId, Fecha)
                VALUES
                    ({producto.Id}, 'Entrada', 1, 1, 1, 2,
                     'Compra', {compra.Id}, UTC_TIMESTAMP(6))
                """);

            var compraIdBridge = await context.Database
                .SqlQueryRaw<int>("SELECT CompraId AS Value FROM MovimientosInventario ORDER BY Id DESC LIMIT 1")
                .SingleAsync();
            Assert.Equal(compra.Id, compraIdBridge);

            // Si un escritor ya aporta una FK tipada, el trigger no puede sustituirla
            // silenciosamente por el snapshot legacy. El CHECK C3 debe rechazar mismatch.
            await Assert.ThrowsAsync<MySqlException>(() =>
                context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO MovimientosInventario
                        (ProductoId, Tipo, Causa, Cantidad, StockAnterior, StockNuevo,
                         ReferenciaTipo, ReferenciaId, CompraId, Fecha)
                    VALUES
                        ({producto.Id}, 'Salida', 2, 1, 2, 1,
                         'Venta', {compra.Id}, {compra.Id}, UTC_TIMESTAMP(6))
                    """));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
