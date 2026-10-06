using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class N08MigracionesLimpiezaPreflightIntegrationTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;Allow User Variables=True;";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(GetConnectionString(dbName), mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

    [Fact]
    public async Task Preflight_N08_Ejecuta_ReadOnly_Y_Clasifica_Deuda_Real()
    {
        var dbName = $"test_n08_preflight_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            await context.Database.MigrateAsync();

            var scriptPath = BuscarScriptN08();
            var sql = await File.ReadAllTextAsync(scriptPath);

            Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TRUNCATE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);

            await using var connection = new MySqlConnection(GetConnectionString(dbName));
            await connection.OpenAsync();
            await using var command = new MySqlCommand(sql, connection)
            {
                CommandTimeout = 120
            };
            await using var reader = await command.ExecuteReaderAsync();

            var resultadoFinal = string.Empty;
            var expectedAbsent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var authorityPresence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var compatibilityColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            do
            {
                if (reader.FieldCount == 0)
                    continue;

                var checkIdOrdinal = BuscarOrdinal(reader, "check_id");
                if (checkIdOrdinal < 0)
                    continue;

                while (await reader.ReadAsync())
                {
                    var checkId = reader.IsDBNull(checkIdOrdinal)
                        ? string.Empty
                        : reader.GetString(checkIdOrdinal);

                    if (checkId.Equals("N0.8_PREFLIGHT_RESULT", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoFinal = reader.GetString(reader.GetOrdinal("result"));
                    }
                    else if (checkId.Equals("N0.8_EXPECTED_ABSENT", StringComparison.OrdinalIgnoreCase))
                    {
                        expectedAbsent[reader.GetString(reader.GetOrdinal("object_name"))] =
                            reader.GetString(reader.GetOrdinal("result"));
                    }
                    else if (checkId.Equals("N0.8_AUTHORITY_COLUMNS", StringComparison.OrdinalIgnoreCase))
                    {
                        var key = $"{reader.GetString(reader.GetOrdinal("TABLE_NAME"))}.{reader.GetString(reader.GetOrdinal("COLUMN_NAME"))}";
                        authorityPresence[key] = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("present")));
                    }
                    else if (checkId.Equals("N0.8_COMPATIBILITY_COLUMNS", StringComparison.OrdinalIgnoreCase))
                    {
                        compatibilityColumns.Add(
                            $"{reader.GetString(reader.GetOrdinal("TABLE_NAME"))}.{reader.GetString(reader.GetOrdinal("COLUMN_NAME"))}");
                    }
                }
            }
            while (await reader.NextResultAsync());

            Assert.Equal("PASS", resultadoFinal);
            Assert.NotEmpty(expectedAbsent);
            Assert.All(expectedAbsent.Values, result => Assert.Equal("PASS", result));

            Assert.Equal(1, authorityPresence["ProductoVariantes.Cantidad"]);
            Assert.Equal(1, authorityPresence["Ventas.MetodoPagoId"]);
            Assert.Equal(1, authorityPresence["MovimientosInventario.CompraId"]);
            Assert.Equal(1, authorityPresence["MovimientosInventario.AjusteInventarioId"]);

            // La brecha detectada por N0.8.A queda materializada desde N0.8.C.
            Assert.Equal(1, authorityPresence["Compras.MetodoPagoId"]);

            Assert.Contains("Productos.Cantidad", compatibilityColumns);
            Assert.Contains("Productos.Costo", compatibilityColumns);
            Assert.Contains("Compras.MetodoPago", compatibilityColumns);
            Assert.Contains("MovimientosInventario.ReferenciaTipo", compatibilityColumns);
            Assert.Contains("MovimientosInventario.ReferenciaId", compatibilityColumns);
            Assert.Contains("MovimientosFinancieros.ModuloOrigen", compatibilityColumns);
            Assert.Contains("MovimientosFinancieros.ReferenciaId", compatibilityColumns);
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

    private static string BuscarScriptN08()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "scripts",
                "preflight-erp-n0-8-migraciones-limpieza.sql");
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        throw new FileNotFoundException(
            "No se encontró backend/scripts/preflight-erp-n0-8-migraciones-limpieza.sql desde el output de pruebas.");
    }
}
