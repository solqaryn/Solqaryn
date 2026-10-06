using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

public enum OracleDatabaseBootstrapMode
{
    Auto,
    Fresh,
    Adopt
}

public sealed record OracleDatabaseBootstrapResult(
    OracleDatabaseBootstrapMode EffectiveMode,
    long ApplicationTables,
    int PendingMigrations);

/// <summary>
/// Autoridad canónica para bootstrap/adopción Oracle EF10.
/// Fresh materializa el modelo físico actual y sus artefactos históricos certificados.
/// Adopt sólo registra/aplica la cadena Oracle sobre un esquema físico existente.
/// Auto elige Fresh únicamente cuando la base no contiene tablas de aplicación.
/// </summary>
public static class OracleDatabaseBootstrapper
{
    public const string BaselineMigrationId = "20261006111818_OracleBaseline";

    public static async Task<OracleDatabaseBootstrapResult> ApplyAsync(
        AppDbContext db,
        OracleDatabaseBootstrapMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        await db.Database.OpenConnectionAsync(cancellationToken);
        var applicationTablesBefore = await CountApplicationTablesAsync(db, cancellationToken);

        var effectiveMode = mode switch
        {
            OracleDatabaseBootstrapMode.Auto when applicationTablesBefore == 0 => OracleDatabaseBootstrapMode.Fresh,
            OracleDatabaseBootstrapMode.Auto => OracleDatabaseBootstrapMode.Adopt,
            _ => mode
        };

        if (effectiveMode == OracleDatabaseBootstrapMode.Fresh)
        {
            if (applicationTablesBefore != 0)
                throw new InvalidOperationException(
                    $"Fresh bootstrap refused because {applicationTablesBefore} application tables already exist.");

            await db.Database.CloseConnectionAsync();
            var created = await db.Database.EnsureCreatedAsync(cancellationToken);
            if (!created)
                throw new InvalidOperationException(
                    "Fresh bootstrap expected to create an empty database schema.");
            await db.Database.OpenConnectionAsync(cancellationToken);

            await EnsureHistoricalPhysicalTablesAsync(db, cancellationToken);
            await CanonicalMySqlPhysicalContract.ApplyFreshBootstrapSupplementsAsync(db, cancellationToken);
            await CanonicalMySqlPhysicalContract.VerifyAsync(db, cancellationToken);
        }
        else if (applicationTablesBefore < 100)
        {
            throw new InvalidOperationException(
                $"Adoption refused because only {applicationTablesBefore} application tables exist.");
        }

        var beforeAdoption = await CountApplicationTablesAsync(db, cancellationToken);

        await db.Database.CloseConnectionAsync();
        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);

        var afterAdoption = await CountApplicationTablesAsync(db, cancellationToken);
        if (afterAdoption != beforeAdoption)
            throw new InvalidOperationException(
                $"Oracle baseline adoption changed physical table count: before={beforeAdoption}, after={afterAdoption}.");

        var marker = await ScalarLongAsync(
            db,
            $"SELECT COUNT(*) AS Value FROM __EFMigrationsHistory WHERE MigrationId = '{BaselineMigrationId}';",
            cancellationToken);
        if (marker != 1)
            throw new InvalidOperationException($"Oracle baseline marker mismatch: {marker}.");

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pending.Length != 0)
            throw new InvalidOperationException(
                $"Pending Oracle migrations remain: {string.Join(",", pending)}");

        return new OracleDatabaseBootstrapResult(effectiveMode, afterAdoption, pending.Length);
    }

    private static Task<long> CountApplicationTablesAsync(
        AppDbContext db,
        CancellationToken cancellationToken) =>
        ScalarLongAsync(
            db,
            """
            SELECT COUNT(*) AS Value
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME <> '__EFMigrationsHistory';
            """,
            cancellationToken);

    private static async Task<long> ScalarLongAsync(
        AppDbContext db,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value);
    }

    private static async Task EnsureHistoricalPhysicalTablesAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
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
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `AutomatizacionConfiguraciones`
              (`Id`, `DiasBorradorVentaAlerta`, `DiasBorradorCompraAlerta`, `DiasCargaPendienteAlerta`,
               `DiasMovimientoFinancieroPendienteAlerta`, `LimiteSugerencias`, `LimiteAutocompletado`,
               `MostrarRecordatoriosDashboard`, `ActualizadoPor`)
            VALUES (1, 2, 7, 1, 7, 20, 10, 1, 'bootstrap-phase7')
            ON DUPLICATE KEY UPDATE `Id` = `Id`;
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
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
            """,
            cancellationToken);
    }
}
