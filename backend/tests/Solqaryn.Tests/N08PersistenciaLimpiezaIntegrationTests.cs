using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class N08PersistenciaLimpiezaIntegrationTests
{
    private const string TargetAnterior = "20260814014000_N0_7_AjusteInventarioVarianteIndex";

    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;Allow User Variables=True;";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(GetConnectionString(dbName), new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;

    [Fact]
    public async Task Migracion_N08C_Backfillea_Compra_Y_Modela_Origenes_Tipados_Sin_Borrar_Legacy()
    {
        var dbName = $"test_n08_c_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(TargetAnterior);

            await using (var connection = new MySqlConnection(GetConnectionString(dbName)))
            {
                await connection.OpenAsync();
                await using var seed = new MySqlCommand("""
                    INSERT INTO Compras
                        (NumeroCompra, Fecha, ProveedorNombre, Estado, EstadoPago, MetodoPago,
                         Subtotal, Descuento, Impuesto, Total, Eliminado, FechaCreacion, FechaActualizacion)
                    VALUES
                        ('N08C-000001', UTC_TIMESTAMP(6), 'Proveedor N08C', 'Borrador', 'Pendiente', 'Transferencia',
                         100.00, 0.00, 0.00, 100.00, 0, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));
                    """, connection);
                await seed.ExecuteNonQueryAsync();
            }

            await migrator.MigrateAsync();

            await using (var connection = new MySqlConnection(GetConnectionString(dbName)))
            {
                await connection.OpenAsync();

                await using (var verify = new MySqlCommand("""
                    SELECT mp.Codigo
                      FROM Compras c
                      JOIN MetodosPago mp ON mp.Id = c.MetodoPagoId
                     WHERE c.NumeroCompra = 'N08C-000001';
                    """, connection))
                {
                    var codigo = Convert.ToString(await verify.ExecuteScalarAsync());
                    Assert.Equal("Transferencia", codigo);
                }

                var postcheck = await File.ReadAllTextAsync(BuscarPostcheckN08C());
                await using var command = new MySqlCommand(postcheck, connection)
                {
                    CommandTimeout = 120
                };
                await using var reader = await command.ExecuteReaderAsync();
                var resultado = string.Empty;
                do
                {
                    var checkOrdinal = BuscarOrdinal(reader, "check_id");
                    if (checkOrdinal < 0)
                        continue;

                    while (await reader.ReadAsync())
                    {
                        if (!reader.GetString(checkOrdinal).Equals("N0.8.C_RESULT", StringComparison.OrdinalIgnoreCase))
                            continue;

                        resultado = reader.GetString(reader.GetOrdinal("result"));
                    }
                }
                while (await reader.NextResultAsync());

                Assert.Equal("PASS", resultado);
            }

            var compraType = context.Model.FindEntityType(typeof(Compra));
            Assert.NotNull(compraType?.FindProperty(nameof(Compra.MetodoPagoId)));

            var movimientoType = context.Model.FindEntityType(typeof(MovimientoInventario));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.CompraId)));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.VentaId)));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.ConsumoInsumoId)));
            Assert.NotNull(movimientoType?.FindProperty(nameof(MovimientoInventario.AjusteInventarioId)));

            var historica = await context.Compras
                .AsNoTracking()
                .SingleAsync(c => c.NumeroCompra == "N08C-000001");
            Assert.NotNull(historica.MetodoPagoId);
            Assert.Equal(MetodoPago.Transferencia, historica.MetodoPago);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static int BuscarOrdinal(MySqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static string BuscarPostcheckN08C()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "scripts",
                "postdeploy-erp-n0-8-c-persistencia.sql");
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        throw new FileNotFoundException(
            "No se encontró backend/scripts/postdeploy-erp-n0-8-c-persistencia.sql desde el output de pruebas.");
    }
}
