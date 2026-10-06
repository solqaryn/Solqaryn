using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Persistence;

var mode = (Environment.GetEnvironmentVariable("SOLQARYN_DATABASE_BOOTSTRAP_MODE") ?? "adopt").Trim().ToLowerInvariant();
if (mode is not ("fresh" or "adopt"))
    throw new InvalidOperationException("SOLQARYN_DATABASE_BOOTSTRAP_MODE must be 'fresh' or 'adopt'.");

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be supplied through the environment.");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySQL(connectionString, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
    .Options;

await using var db = new AppDbContext(options);
await db.Database.OpenConnectionAsync();

static async Task<long> ScalarLongAsync(AppDbContext db, string sql)
{
    await using var command = db.Database.GetDbConnection().CreateCommand();
    command.CommandText = sql;
    return Convert.ToInt64(await command.ExecuteScalarAsync());
}

var applicationTablesBefore = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_TYPE = 'BASE TABLE'
      AND TABLE_NAME <> '__EFMigrationsHistory';
    """);

if (mode == "fresh")
{
    if (applicationTablesBefore != 0)
        throw new InvalidOperationException($"Fresh bootstrap refused because {applicationTablesBefore} application tables already exist.");

    await db.Database.CloseConnectionAsync();
    var created = await db.Database.EnsureCreatedAsync();
    if (!created)
        throw new InvalidOperationException("Fresh bootstrap expected to create an empty database schema.");
    await db.Database.OpenConnectionAsync();

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `AutomatizacionConfiguraciones` (
          `Id` int NOT NULL,
          `DiasBorradorVentaAlerta` int NOT NULL DEFAULT '2',
          `DiasBorradorCompraAlerta` int NOT NULL DEFAULT '7',
          `DiasCargaPendienteAlerta` int NOT NULL DEFAULT '1',
          `DiasMovimientoFinancieroPendienteAlerta` int NOT NULL DEFAULT '7',
          `LimiteSugerencias` int NOT NULL DEFAULT '20',
          `LimiteAutocompletado` int NOT NULL DEFAULT '10',
          `MostrarRecordatoriosDashboard` tinyint(1) NOT NULL DEFAULT '1',
          `FechaActualizacion` datetime(6) DEFAULT NULL,
          `ActualizadoPor` varchar(120) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
          PRIMARY KEY (`Id`),
          CONSTRAINT `CK_AutomatizacionConfiguraciones_Autocomplete` CHECK ((`LimiteAutocompletado` between 5 and 50)),
          CONSTRAINT `CK_AutomatizacionConfiguraciones_Carga` CHECK ((`DiasCargaPendienteAlerta` between 1 and 30)),
          CONSTRAINT `CK_AutomatizacionConfiguraciones_Compra` CHECK ((`DiasBorradorCompraAlerta` between 1 and 180)),
          CONSTRAINT `CK_AutomatizacionConfiguraciones_Finanzas` CHECK ((`DiasMovimientoFinancieroPendienteAlerta` between 1 and 180)),
          CONSTRAINT `CK_AutomatizacionConfiguraciones_Id` CHECK ((`Id` = 1)),
          CONSTRAINT `CK_AutomatizacionConfiguraciones_Sugerencias` CHECK ((`LimiteSugerencias` between 5 and 100)),
          CONSTRAINT `CK_AutomatizacionConfiguraciones_Venta` CHECK ((`DiasBorradorVentaAlerta` between 1 and 90))
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        """);

    await db.Database.ExecuteSqlRawAsync("""
        INSERT INTO `AutomatizacionConfiguraciones`
          (`Id`, `DiasBorradorVentaAlerta`, `DiasBorradorCompraAlerta`, `DiasCargaPendienteAlerta`,
           `DiasMovimientoFinancieroPendienteAlerta`, `LimiteSugerencias`, `LimiteAutocompletado`,
           `MostrarRecordatoriosDashboard`, `ActualizadoPor`)
        VALUES (1, 2, 7, 1, 7, 20, 10, 1, 'bootstrap-phase7')
        ON DUPLICATE KEY UPDATE `Id` = `Id`;
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `SuscripcionSaaSIdempotencia` (
          `EmpresaId` int NOT NULL,
          `IdempotencyKey` varchar(160) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
          `SuscripcionId` int NOT NULL,
          `CreadoUtc` datetime(6) NOT NULL,
          PRIMARY KEY (`EmpresaId`,`IdempotencyKey`),
          UNIQUE KEY `UX_SuscripcionSaaSIdempotencia_SuscripcionId` (`SuscripcionId`),
          CONSTRAINT `FK_SuscripcionSaaSIdempotencia_Empresas_EmpresaId`
            FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
          CONSTRAINT `FK_SuscripcionSaaSIdempotencia_Suscripciones_SuscripcionId`
            FOREIGN KEY (`SuscripcionId`) REFERENCES `Suscripciones` (`Id`) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
        """);
}
else
{
    if (applicationTablesBefore < 100)
        throw new InvalidOperationException($"Adoption refused because only {applicationTablesBefore} application tables exist.");
}

var beforeAdoption = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_TYPE = 'BASE TABLE'
      AND TABLE_NAME <> '__EFMigrationsHistory';
    """);

await db.Database.CloseConnectionAsync();
await db.Database.MigrateAsync();
await db.Database.OpenConnectionAsync();

var afterAdoption = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_TYPE = 'BASE TABLE'
      AND TABLE_NAME <> '__EFMigrationsHistory';
    """);

if (afterAdoption != beforeAdoption)
    throw new InvalidOperationException($"Oracle baseline adoption changed physical table count: before={beforeAdoption}, after={afterAdoption}.");

var marker = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM __EFMigrationsHistory
    WHERE MigrationId = '20261006111818_OracleBaseline';
    """);

if (marker != 1)
    throw new InvalidOperationException($"Oracle baseline marker mismatch: {marker}.");

var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
if (pending.Length != 0)
    throw new InvalidOperationException($"Pending Oracle migrations remain: {string.Join(",", pending)}.");

Console.WriteLine($"SOLQARYN_ORACLE_BOOTSTRAP=PASS mode={mode} tables={afterAdoption} pending=0");
