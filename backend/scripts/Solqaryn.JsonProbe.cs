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

var marker = Guid.NewGuid().ToString("N");
var metadataValue = $"metadata-{marker}";
var oldValue = $"before-{marker}";
var newValue = $"after-{marker}";

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

db.Set<MetodoPagoEntity>().Add(metodoPago);
db.RegistrosAuditoria.Add(auditoria);
await db.SaveChangesAsync();
db.ChangeTracker.Clear();

var metodoPagoRead = await db.Set<MetodoPagoEntity>().AsNoTracking().SingleAsync(x => x.Id == metodoPago.Id);
var auditoriaRead = await db.RegistrosAuditoria.AsNoTracking().SingleAsync(x => x.Id == auditoria.Id);
Require(ReadJsonValue(metodoPagoRead.Metadata, "probe") == metadataValue, "JSON_PROBE_METADATA_EF_ROUNDTRIP_FAIL");
Require(ReadJsonValue(auditoriaRead.ValoresAnteriores, "probe") == oldValue, "JSON_PROBE_BEFORE_EF_ROUNDTRIP_FAIL");
Require(ReadJsonValue(auditoriaRead.ValoresNuevos, "probe") == newValue, "JSON_PROBE_AFTER_EF_ROUNDTRIP_FAIL");

var metadataExtracted = await ExtractAsync(db, "MetodosPago", "Metadata", metodoPago.Id);
var oldExtracted = await ExtractAsync(db, "RegistrosAuditoria", "ValoresAnteriores", auditoria.Id);
var newExtracted = await ExtractAsync(db, "RegistrosAuditoria", "ValoresNuevos", auditoria.Id);
Require(metadataExtracted == metadataValue, "JSON_PROBE_METADATA_SQL_EXTRACTION_FAIL");
Require(oldExtracted == oldValue, "JSON_PROBE_BEFORE_SQL_EXTRACTION_FAIL");
Require(newExtracted == newValue, "JSON_PROBE_AFTER_SQL_EXTRACTION_FAIL");

await transaction.RollbackAsync();
Console.WriteLine("JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true");
