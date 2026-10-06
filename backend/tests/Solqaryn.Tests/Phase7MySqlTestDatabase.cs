using Microsoft.EntityFrameworkCore;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Tests;

internal static class Phase7MySqlTestDatabase
{
    public static async Task InitializeFreshAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

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
              PRIMARY KEY (`Id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `AutomatizacionConfiguraciones`
              (`Id`, `DiasBorradorVentaAlerta`, `DiasBorradorCompraAlerta`, `DiasCargaPendienteAlerta`,
               `DiasMovimientoFinancieroPendienteAlerta`, `LimiteSugerencias`, `LimiteAutocompletado`,
               `MostrarRecordatoriosDashboard`, `ActualizadoPor`)
            VALUES (1, 2, 7, 1, 7, 20, 10, 1, 'phase7-integration')
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

        await db.Database.MigrateAsync();

        if ((await db.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException("Phase 7 test bootstrap left pending Oracle migrations.");
    }
}
