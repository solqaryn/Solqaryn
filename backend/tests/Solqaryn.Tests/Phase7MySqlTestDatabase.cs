using Microsoft.EntityFrameworkCore;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Tests;

internal static class Phase7MySqlTestDatabase
{
    public static async Task InitializeFreshAsync(
        AppDbContext db, bool applyPendingOracleMigrations = true)
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

        await CanonicalMySqlPhysicalContract.ApplyFreshBootstrapSupplementsAsync(db);
        await CanonicalMySqlPhysicalContract.VerifyAsync(db);

        // EnsureCreated materializa todas las columnas actuales. Adoptar solo la
        // migración histórica y marcar expand para NO duplicar columnas ni FKs.
        await db.Database.MigrateAsync("20261006111818_OracleBaseline");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013121500_CommercialTenantRootExpand', '10.0.12');
            """);
        // EnsureCreated incluye el modelo de índices empresariales actual, y
        // ApplyFreshBootstrapSupplementsAsync ya impuso/verificó sus 12 FKs.
        // Adoptar únicamente esta migración exacta, nunca un set abierto.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013181500_CommercialTenantScopedRootIndexes', '10.0.12');
            """);

        // El esquema de EnsureCreated ya incluye las tres relaciones compuestas
        // adicionales de documentos comerciales verificadas por el contrato físico.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013190000_CommercialTenantInvoiceDocuments', '10.0.12');
            """);

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013193000_CommercialTenantSalesDocuments', '10.0.12');
            """);

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013210000_CommercialTenantTaxDiscountExpand', '10.0.12');
            """);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013220000_CommercialTenantOwnershipGraphExpand', '10.0.12');
            """);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013221500_CommercialTenantOwnershipGraphScopedFks', '10.0.12');
            """);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013230000_CommercialTenantRemainingModules', '10.0.12');
            """);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261013231000_CommercialTenantRemainingScopedFks', '10.0.12');
            """);

        // Adoptar la migración P4 solo si EnsureCreated materializó el índice
        // exacto en la base efímera, nunca basándose únicamente en el nombre.
        await db.Database.OpenConnectionAsync();
        await using (var check = db.Database.GetDbConnection().CreateCommand())
        {
            check.CommandText = """
                SELECT GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',')
                  FROM INFORMATION_SCHEMA.STATISTICS
                 WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'TiendaCuentasCliente'
                   AND INDEX_NAME = 'UX_TiendaCuentasCliente_Correo'
                   AND NON_UNIQUE = 0
                """;
            var columns = Convert.ToString(await check.ExecuteScalarAsync());
            if (columns != "EmpresaId,CorreoNormalizado")
                throw new InvalidOperationException(
                    "Phase 7 test bootstrap: P4 email uniqueness index is not physically tenant-scoped.");
        }
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261014112000_PublicStoreAccountEmailPerEmpresa', '10.0.12');
            """);
        // Adoptar solo tras comprobar físicamente el contrato de 12 índices
        // comerciales nuevos y la unicidad de tienda ya vigente.
        await CommercialTenantNaturalKeysPhysicalContract.VerifyAsync(db);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20261014130000_CommercialTenantNaturalKeysPostStorefront', '10.0.12');
            """);
        // EnsureCreated ya materializó PublicStoreBindings. Registrar sólo
        // tras comprobar el índice UNIQUE exacto de identidad pública A/B.
        await db.Database.OpenConnectionAsync();
        await using (var check = db.Database.GetDbConnection().CreateCommand())
        {
            check.CommandText = """
                SELECT GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',')
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'PublicStoreBindings'
                  AND INDEX_NAME = 'UX_PublicStoreBindings_Host_Key'
                  AND NON_UNIQUE = 0
                """;
            var columns = Convert.ToString(await check.ExecuteScalarAsync());
            if (columns != "Host,StoreKey")
                throw new InvalidOperationException(
                    "Phase 7 fresh bootstrap: public store unique host/key index is missing.");
        }
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
            VALUES ('20261016095000_PublicStoreBindingsPersistence', '10.0.12');
            """);
        await db.Database.CloseConnectionAsync();

        // El bootstrap EnsureCreated contiene el modelo actual. Aplicar primero
        // todas las migraciones previas a P1-003.A y luego dejar que la migración
        // nueva cree su tabla real; no se adopta una tabla creada por EnsureCreated.
        if (applyPendingOracleMigrations)
        {
            // EnsureCreated ya materializó el modelo final, incluida la tabla
            // de sesiones de soporte. Retirarla solo en la DB efímera para que
            // la migración pendiente la cree y certifique su DDL real.
            await db.Database.ExecuteSqlRawAsync(
                "DROP TABLE IF EXISTS PlataformaSesionesSoporte;");
            await db.Database.MigrateAsync("20261029150000_EnforceTenantRoleNameUniqueIndex");
            await db.Database.MigrateAsync();
            if ((await db.Database.GetPendingMigrationsAsync()).Any())
                throw new InvalidOperationException("Phase 7 test bootstrap left pending Oracle migrations.");
        }
    }
}
