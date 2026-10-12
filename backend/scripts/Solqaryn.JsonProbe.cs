using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using MetodoPagoEntity = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

static void Require(bool condition, string code)
{
    if (!condition)
        throw new InvalidOperationException(code);
}

static string? ReadJsonValue(string? json, string property)
{
    if (json is null)
        return null;
    using var document = JsonDocument.Parse(json);
    return document.RootElement.GetProperty(property).GetString();
}

static async Task<string?> ExtractAsync(AppDbContext db, string table, string column, int id)
{
    var connection = db.Database.GetDbConnection();
    await using var command = connection.CreateCommand();
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = $"SELECT JSON_UNQUOTE(JSON_EXTRACT(`{column}`, '$.probe')) FROM `{table}` WHERE `Id` = @rowId";

    var parameter = command.CreateParameter();
    parameter.ParameterName = "@rowId";
    parameter.Value = id;
    command.Parameters.Add(parameter);

    var result = await command.ExecuteScalarAsync();
    return result is null or DBNull ? null : Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture);
}

static async Task<string?> ReadPhysicalColumnTypeAsync(AppDbContext db, string table, string column)
{
    var connection = db.Database.GetDbConnection();
    await using var command = connection.CreateCommand();
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = "SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName AND COLUMN_NAME = @columnName";

    var tableParameter = command.CreateParameter();
    tableParameter.ParameterName = "@tableName";
    tableParameter.Value = table;
    command.Parameters.Add(tableParameter);
    var columnParameter = command.CreateParameter();
    columnParameter.ParameterName = "@columnName";
    columnParameter.Value = column;
    command.Parameters.Add(columnParameter);

    var result = await command.ExecuteScalarAsync();
    return result is null or DBNull ? null : Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture);
}

var factory = new AppDbContextFactory();
await using var db = factory.CreateDbContext([]);
Require(await db.Database.CanConnectAsync(), "JSON_PROBE_DATABASE_UNAVAILABLE");

var mappings = new (Type Entity, string Table, string Property, string Column)[]
{
    (typeof(MetodoPagoEntity), "MetodosPago", nameof(MetodoPagoEntity.Metadata), "Metadata"),
    (typeof(RegistroAuditoria), "RegistrosAuditoria", nameof(RegistroAuditoria.ValoresAnteriores), "ValoresAnteriores"),
    (typeof(RegistroAuditoria), "RegistrosAuditoria", nameof(RegistroAuditoria.ValoresNuevos), "ValoresNuevos")
};

foreach (var mapping in mappings)
{
    var entityType = db.Model.FindEntityType(mapping.Entity)
        ?? throw new InvalidOperationException($"JSON_PROBE_ENTITY_MISSING:{mapping.Entity.Name}");
    Require(entityType.GetTableName() == mapping.Table, $"JSON_PROBE_TABLE_MISMATCH:{mapping.Entity.Name}");

    var property = entityType.FindProperty(mapping.Property)
        ?? throw new InvalidOperationException($"JSON_PROBE_PROPERTY_MISSING:{mapping.Entity.Name}.{mapping.Property}");
    var table = StoreObjectIdentifier.Table(mapping.Table, entityType.GetSchema());
    Require(string.Equals(property.GetColumnName(table), mapping.Column, StringComparison.Ordinal),
        $"JSON_PROBE_COLUMN_MISMATCH:{mapping.Entity.Name}.{mapping.Property}");
    Require(string.Equals(property.GetColumnType(), "json", StringComparison.OrdinalIgnoreCase),
        $"JSON_PROBE_TYPE_MISMATCH:{mapping.Entity.Name}.{mapping.Property}:{property.GetColumnType()}");
}

var compraEntity = db.Model.FindEntityType(typeof(Compra))
    ?? throw new InvalidOperationException("DECIMAL_PROBE_COMPRA_ENTITY_MISSING");
var compraTotalProperty = compraEntity.FindProperty(nameof(Compra.Total))
    ?? throw new InvalidOperationException("DECIMAL_PROBE_COMPRA_TOTAL_MISSING");
Require(string.Equals(compraTotalProperty.GetColumnType(), "decimal(18,2)", StringComparison.OrdinalIgnoreCase),
    $"DECIMAL_PROBE_MODEL_18_2_MISMATCH:{compraTotalProperty.GetColumnType()}");
var compraFechaProperty = compraEntity.FindProperty(nameof(Compra.Fecha))
    ?? throw new InvalidOperationException("DATETIME6_PROBE_COMPRA_FECHA_MISSING");
Require(string.Equals(compraFechaProperty.GetColumnType(), "datetime(6)", StringComparison.OrdinalIgnoreCase),
    $"DATETIME6_PROBE_MODEL_MISMATCH:{compraFechaProperty.GetColumnType()}");

var cuentasPorPagarEntity = db.Model.FindEntityType(typeof(CuentaPorPagar))
    ?? throw new InvalidOperationException("DECIMAL_PROBE_CXP_ENTITY_MISSING");
var montoOriginalProperty = cuentasPorPagarEntity.FindProperty(nameof(CuentaPorPagar.MontoOriginal))
    ?? throw new InvalidOperationException("DECIMAL_PROBE_CXP_MONTO_MISSING");
Require(string.Equals(montoOriginalProperty.GetColumnType(), "decimal(18,4)", StringComparison.OrdinalIgnoreCase),
    $"DECIMAL_PROBE_MODEL_18_4_MISMATCH:{montoOriginalProperty.GetColumnType()}");

var marker = Guid.NewGuid().ToString("N");
var metadataValue = $"metadata-{marker}";
var oldValue = $"before-{marker}";
var newValue = $"after-{marker}";
const decimal amount18_2 = 9999999999999999.99m;
const decimal amount18_4 = 99999999999999.9999m;
var expectedCompraFecha = new DateTime(2026, 10, 4, 12, 34, 56, DateTimeKind.Utc).AddTicks(1_234_560);

await db.Database.OpenConnectionAsync();
await db.Database.ExecuteSqlRawAsync("DROP TEMPORARY TABLE IF EXISTS SolqarynModernizationDecimalProbe");
await db.Database.ExecuteSqlRawAsync("CREATE TEMPORARY TABLE SolqarynModernizationDecimalProbe (Amount2 DECIMAL(18,2) NOT NULL, Amount4 DECIMAL(18,4) NOT NULL)");
await using var transaction = await db.Database.BeginTransactionAsync();
var metodoPago = new MetodoPagoEntity
{
    Codigo = $"JSON_PROBE_{marker}",
    Nombre = $"JSON probe {marker}",
    Tipo = "ModernizationProbe",
    Metadata = JsonSerializer.Serialize(new { probe = metadataValue })
};
var auditoria = new RegistroAuditoria
{
    NombreUsuario = "modernization-json-probe",
    Modulo = ModuloSistema.Auditoria,
    Accion = AccionPermiso.Crear,
    Entidad = "JsonCompatibilityProbe",
    Descripcion = "Transient JSON provider compatibility probe",
    ValoresAnteriores = JsonSerializer.Serialize(new { probe = oldValue }),
    ValoresNuevos = JsonSerializer.Serialize(new { probe = newValue }),
    Resultado = "Exito"
};
var compra = new Compra
{
    NumeroCompra = $"D{marker[..18]}",
    ProveedorNombre = "Modernization decimal probe",
    Fecha = expectedCompraFecha,
    Subtotal = amount18_2,
    Total = amount18_2
};

db.Set<MetodoPagoEntity>().Add(metodoPago);
db.RegistrosAuditoria.Add(auditoria);
db.Set<Compra>().Add(compra);
await db.SaveChangesAsync();
await db.Database.ExecuteSqlInterpolatedAsync(
    $"INSERT INTO SolqarynModernizationDecimalProbe (Amount2, Amount4) VALUES ({amount18_2}, {amount18_4})");
db.ChangeTracker.Clear();

var metodoPagoRead = await db.Set<MetodoPagoEntity>().AsNoTracking().SingleAsync(x => x.Id == metodoPago.Id);
var auditoriaRead = await db.RegistrosAuditoria.AsNoTracking().SingleAsync(x => x.Id == auditoria.Id);
var compraRead = await db.Set<Compra>().AsNoTracking().SingleAsync(x => x.Id == compra.Id);
Require(ReadJsonValue(metodoPagoRead.Metadata, "probe") == metadataValue, "JSON_PROBE_METADATA_EF_ROUNDTRIP_FAIL");
Require(ReadJsonValue(auditoriaRead.ValoresAnteriores, "probe") == oldValue, "JSON_PROBE_BEFORE_EF_ROUNDTRIP_FAIL");
Require(ReadJsonValue(auditoriaRead.ValoresNuevos, "probe") == newValue, "JSON_PROBE_AFTER_EF_ROUNDTRIP_FAIL");
Require(compraRead.Total == amount18_2 && compraRead.Subtotal == amount18_2, "DECIMAL_PROBE_18_2_EF_ROUNDTRIP_FAIL");
Require(compraRead.Fecha.Ticks == expectedCompraFecha.Ticks, "DATETIME6_PROBE_EF_MICROSECONDS_ROUNDTRIP_FAIL");

var metadataExtracted = await ExtractAsync(db, "MetodosPago", "Metadata", metodoPago.Id);
var oldExtracted = await ExtractAsync(db, "RegistrosAuditoria", "ValoresAnteriores", auditoria.Id);
var newExtracted = await ExtractAsync(db, "RegistrosAuditoria", "ValoresNuevos", auditoria.Id);
Require(metadataExtracted == metadataValue, "JSON_PROBE_METADATA_SQL_EXTRACTION_FAIL");
Require(oldExtracted == oldValue, "JSON_PROBE_BEFORE_SQL_EXTRACTION_FAIL");
Require(newExtracted == newValue, "JSON_PROBE_AFTER_SQL_EXTRACTION_FAIL");

var physical18_2 = await ReadPhysicalColumnTypeAsync(db, "Compras", "Total");
var physical18_4 = await ReadPhysicalColumnTypeAsync(db, "CuentasPorPagar", "MontoOriginal");
var physicalDateTime6 = await ReadPhysicalColumnTypeAsync(db, "Compras", "Fecha");
Require(string.Equals(physical18_2, "decimal(18,2)", StringComparison.OrdinalIgnoreCase),
    $"DECIMAL_PROBE_PHYSICAL_18_2_MISMATCH:{physical18_2}");
Require(string.Equals(physical18_4, "decimal(18,4)", StringComparison.OrdinalIgnoreCase),
    $"DECIMAL_PROBE_PHYSICAL_18_4_MISMATCH:{physical18_4}");
Require(string.Equals(physicalDateTime6, "datetime(6)", StringComparison.OrdinalIgnoreCase),
    $"DATETIME6_PROBE_PHYSICAL_TYPE_MISMATCH:{physicalDateTime6}");

var decimalConnection = db.Database.GetDbConnection();
await using (var command = decimalConnection.CreateCommand())
{
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = "SELECT DATE_FORMAT(`Fecha`, '%Y-%m-%d %H:%i:%s.%f') FROM `Compras` WHERE `Id` = @rowId";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "@rowId";
    parameter.Value = compra.Id;
    command.Parameters.Add(parameter);
    var timestamp = Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    Require(timestamp == "2026-10-04 12:34:56.123456", $"DATETIME6_PROBE_SQL_MICROSECONDS_MISMATCH:{timestamp}");
}

await using (var command = decimalConnection.CreateCommand())
{
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = "SELECT CAST(Amount2 AS CHAR), CAST(Amount4 AS CHAR) FROM SolqarynModernizationDecimalProbe";
    await using var reader = await command.ExecuteReaderAsync();
    Require(await reader.ReadAsync(), "DECIMAL_PROBE_ROW_MISSING");
    Require(reader.GetString(0) == "9999999999999999.99", $"DECIMAL_PROBE_18_2_SCALE_OR_PRECISION_CHANGED:{reader.GetString(0)}");
    Require(reader.GetString(1) == "99999999999999.9999", $"DECIMAL_PROBE_18_4_SCALE_OR_PRECISION_CHANGED:{reader.GetString(1)}");
    Require(!await reader.ReadAsync(), "DECIMAL_PROBE_UNEXPECTED_ROWS");
}

await transaction.RollbackAsync();
await db.Database.ExecuteSqlRawAsync("DROP TEMPORARY TABLE SolqarynModernizationDecimalProbe");
await db.Database.CloseConnectionAsync();
Console.WriteLine("JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true");
Console.WriteLine("DECIMAL_PROVIDER_CONTRACT=PASS model18_2=Compras.Total model18_4=CuentasPorPagar.MontoOriginal maxPrecisionScaleRoundTrips=2 rolledBack=true");
Console.WriteLine("DATETIME6_PROVIDER_CONTRACT=PASS model=Compras.Fecha physical=datetime(6) efRoundTrip=true sqlMicroseconds=123456 rolledBack=true");
