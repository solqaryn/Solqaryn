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

// Fail closed: no marcar una migración como adoptada si el ensamblado
// canónico EF10 ni siquiera la contiene (no aceptar historia falsa).
var known = db.Database.GetMigrations().ToArray();
if (!known.Contains("20261016095000_PublicStoreBindingsPersistence", StringComparer.Ordinal) ||
    !known.Contains("20261016200000_PublicStoreProofReplayGuard", StringComparer.Ordinal) ||
    !known.Contains("20261031140000_P1003ATemporaryPlatformSupportSessions", StringComparer.Ordinal))
    throw new InvalidOperationException("Las migraciones Oracle de tiendas y soporte P1-003 no están registradas en EF.");

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


    await CanonicalMySqlPhysicalContract.ApplyFreshBootstrapSupplementsAsync(db);
    await CanonicalMySqlPhysicalContract.VerifyAsync(db);}
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

var publicStoreTableBeforeAdoption = await ScalarLongAsync(db, """
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PublicStoreBindings'
      AND TABLE_TYPE = 'BASE TABLE';
    """);
var replayTableBeforeAdoption = await ScalarLongAsync(db, """
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PublicStoreProofReplays'
      AND TABLE_TYPE = 'BASE TABLE';
    """);
var supportTableBeforeAdoption = await ScalarLongAsync(db, """
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PlataformaSesionesSoporte'
      AND TABLE_TYPE = 'BASE TABLE';
    """);

await db.Database.CloseConnectionAsync();
if (mode == "fresh")
{
    // EnsureCreated ya generó las columnas/FK del modelo actual.
    // Adoptar OracleBaseline sin aplicar el DDL de expand duplicado.
    await db.Database.MigrateAsync("20261006111818_OracleBaseline");
    await db.Database.ExecuteSqlRawAsync("""
        INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
        VALUES ('20261013121500_CommercialTenantRootExpand', '10.0.12');
        """);
    // EnsureCreated ya construyó el modelo exacto del HEAD. No aplicar DDL
    // de migraciones incrementales sobre sus propias columnas o índices.
    // Esta adopción se autoriza solo tras cotejar el contrato físico preciso;
    // jamás etiquetar automáticamente todas las migraciones que aparezcan.
    // MigrateAsync/ExecuteSqlRawAsync pueden cerrar la conexión implícita del provider.
    // ScalarLongAsync crea un comando ADO directo y exige conexión abierta.
    await db.Database.OpenConnectionAsync();
    var indicesEmpresa = await ScalarLongAsync(db, """
        SELECT COUNT(*) FROM (
          SELECT e.Tabla, e.Indice
          FROM (
            SELECT 'Productos' AS Tabla, 'UX_Productos_Empresa_Id' AS Indice, 'EmpresaId,Id' AS Columnas
            UNION ALL SELECT 'Clientes', 'UX_Clientes_Empresa_Id', 'EmpresaId,Id'
            UNION ALL SELECT 'Proveedores', 'UX_Proveedores_Empresa_Id', 'EmpresaId,Id'
            UNION ALL SELECT 'Compras', 'UX_Compras_Empresa_Id', 'EmpresaId,Id'
            UNION ALL SELECT 'Ventas', 'UX_Ventas_Empresa_Id', 'EmpresaId,Id'
            UNION ALL SELECT 'Clientes', 'UX_Clientes_Empresa_IdentidadORTN', 'EmpresaId,IdentidadORTNNormalizada'
            UNION ALL SELECT 'Proveedores', 'UX_Proveedores_Empresa_Documento', 'EmpresaId,DocumentoNormalizado'
            UNION ALL SELECT 'Compras', 'UX_Compras_Empresa_NumeroCompra', 'EmpresaId,NumeroCompra'
            UNION ALL SELECT 'Ventas', 'UX_Ventas_Empresa_NumeroVenta', 'EmpresaId,NumeroVenta'
          ) e
          JOIN INFORMATION_SCHEMA.STATISTICS s
            ON s.TABLE_SCHEMA = DATABASE()
           AND s.TABLE_NAME = e.Tabla
           AND s.INDEX_NAME = e.Indice
          GROUP BY e.Tabla, e.Indice, e.Columnas
          HAVING MIN(s.NON_UNIQUE) = 0
             AND GROUP_CONCAT(s.COLUMN_NAME ORDER BY s.SEQ_IN_INDEX SEPARATOR ',') = e.Columnas
        ) verificados;
        """);
    if (indicesEmpresa != 9)
        throw new InvalidOperationException(
            $"Fresh bootstrap has an incomplete tenant-scoped index contract: {indicesEmpresa}/9.");

    var indicesGlobales = await ScalarLongAsync(db, """
        SELECT COUNT(DISTINCT CONCAT(TABLE_NAME, '/', INDEX_NAME))
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND (
             (TABLE_NAME = 'Clientes' AND INDEX_NAME = 'UX_Clientes_IdentidadORTN_Normalizada')
          OR (TABLE_NAME = 'Proveedores' AND INDEX_NAME = 'UX_Proveedores_Documento_Normalizado')
          OR (TABLE_NAME = 'Compras' AND INDEX_NAME = 'IX_Compras_NumeroCompra')
          OR (TABLE_NAME = 'Ventas' AND INDEX_NAME = 'IX_Ventas_NumeroVenta')
          );
        """);
    if (indicesGlobales != 0)
        throw new InvalidOperationException(
            $"Fresh bootstrap retains {indicesGlobales} global commercial uniqueness constraints.");

    await db.Database.ExecuteSqlRawAsync("""
        INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
        VALUES ('20261013181500_CommercialTenantScopedRootIndexes', '10.0.12');
        """);
    // EnsureCreated ya construyo columnas e indices; tres FKs se aplicaron en supplements.
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

    // EnsureCreated ya creó el índice compuesto del modelo actual.
    // Registrar la migración P4 únicamente DESPUÉS de verificar en MySQL
    // el nombre, orden de columnas y unicidad, nunca por presencia en Git.
    await db.Database.OpenConnectionAsync();
    var indiceCorreoTienda = await ScalarLongAsync(db, """
        SELECT COUNT(*) FROM (
          SELECT INDEX_NAME
            FROM INFORMATION_SCHEMA.STATISTICS
           WHERE TABLE_SCHEMA = DATABASE()
             AND TABLE_NAME = 'TiendaCuentasCliente'
             AND INDEX_NAME = 'UX_TiendaCuentasCliente_Correo'
           GROUP BY INDEX_NAME
          HAVING MIN(NON_UNIQUE) = 0
             AND GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',')
                 = 'EmpresaId,CorreoNormalizado'
        ) indice;
        """);
    if (indiceCorreoTienda != 1)
        throw new InvalidOperationException(
            "Fresh bootstrap refused to adopt P4 account email migration without its exact tenant-scoped unique index.");
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

    // La nueva tabla proviene de EnsureCreated: sólo adoptar al verificar
    // el índice físico exacto y la FK contra Empresas.
    var storeUnique = await ScalarLongAsync(db, """
        SELECT COUNT(*) FROM (
            SELECT INDEX_NAME FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'PublicStoreBindings'
              AND INDEX_NAME = 'UX_PublicStoreBindings_Host_Key'
              AND NON_UNIQUE = 0
            GROUP BY INDEX_NAME
            HAVING GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',') = 'Host,StoreKey'
        ) v;
        """);
    if (storeUnique != 1)
        throw new InvalidOperationException(
            "Fresh bootstrap refused store-binding migration: unique host/key contract missing.");
    await db.Database.ExecuteSqlRawAsync("""
        INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
        VALUES ('20261016095000_PublicStoreBindingsPersistence', '10.0.12');
        """);
    await db.Database.CloseConnectionAsync();
}
if (mode == "adopt")
{
    await db.Database.OpenConnectionAsync();
    var supportMigrationRecorded = await ScalarLongAsync(db, """
        SELECT COUNT(*) FROM __EFMigrationsHistory
        WHERE MigrationId = '20261031140000_P1003ATemporaryPlatformSupportSessions';
        """);
    await db.Database.CloseConnectionAsync();

    if ((supportTableBeforeAdoption == 1) != (supportMigrationRecorded == 1))
        throw new InvalidOperationException(
            "Support-session table and P1-003 migration marker are inconsistent; refusing implicit adoption.");
}
else
{
    // Fresh mode has already rejected non-empty databases. EnsureCreated also
    // emits the current model, so remove its empty support table and apply the
    // real migration DDL below instead of attempting to create the table twice.
    await db.Database.ExecuteSqlRawAsync(
        "DROP TABLE IF EXISTS `PlataformaSesionesSoporte`;");
}

// Both modes apply P1-003 through the registered migration on an empty table slot.
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

// Persisted schemas gain tables only when absent. Fresh mode drops and recreates
// the empty support table, so its net count remains unchanged.
var expectedAfterAdoption = beforeAdoption + (publicStoreTableBeforeAdoption == 0 ? 1 : 0)
    + (replayTableBeforeAdoption == 0 ? 1 : 0)
    + (supportTableBeforeAdoption == 0 ? 1 : 0);
if (afterAdoption != expectedAfterAdoption)
    throw new InvalidOperationException($"Oracle bootstrap changed unexpected table count: before={beforeAdoption}, after={afterAdoption}, expected={expectedAfterAdoption}.");

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

await CommercialTenantPhysicalConstraints.VerifyAsync(db);
await CommercialTenantInvoicePhysicalConstraints.VerifyAsync(db);
await CommercialTenantSalesPhysicalConstraints.VerifyAsync(db);
await CommercialTenantTaxDiscountPhysicalConstraints.VerifyAsync(db);
await CommercialTenantOwnershipGraphPhysicalConstraints.VerifyAsync(db);
await CommercialTenantRemainingPhysicalConstraints.VerifyAsync(db);
await CommercialTenantNaturalKeysPhysicalContract.VerifyAsync(db);

Console.WriteLine($"SOLQARYN_ORACLE_BOOTSTRAP=PASS mode={mode} tables={afterAdoption} pending=0");
