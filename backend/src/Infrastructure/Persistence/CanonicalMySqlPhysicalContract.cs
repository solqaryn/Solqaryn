using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Contrato físico complementario para bases MySQL nuevas en Fase 7.
/// Preserva artefactos históricos que no forman parte del modelo EF actual
/// pero sí son invariantes funcionales del esquema canónico certificado.
/// </summary>
public static class CanonicalMySqlPhysicalContract
{
    public static async Task ApplyFreshBootstrapSupplementsAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        await EnsureInventarioOriginBridgeAsync(db, cancellationToken);
        await QaCommercialOwnershipV1.InstallFreshPhysicalAsync(db, cancellationToken);
        await QaCommercialOwnershipV2.InstallFreshPhysicalAsync(db, cancellationToken);
        await QaCommercialOwnershipV3.InstallFreshPhysicalAsync(db, cancellationToken);
        await QaCommercialOwnershipV4.InstallFreshPhysicalAsync(db, cancellationToken);
        await VerifyFreshCheckoutPedidoAsync(db, cancellationToken);
        await SeedPaymentMethodsAsync(db, cancellationToken);
        await SeedTipoClientePredeterminadoAsync(db, cancellationToken);
    }

    public static async Task VerifyAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        await QaCommercialOwnershipV1.VerifyPhysicalAsync(db, cancellationToken);
        await QaCommercialOwnershipV2.VerifyPhysicalAsync(db, cancellationToken);
        await QaCommercialOwnershipV3.VerifyPhysicalAsync(db, cancellationToken);
        await QaCommercialOwnershipV4.VerifyPhysicalAsync(db, cancellationToken);

        var triggerCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TRIGGERS
                WHERE TRIGGER_SCHEMA = DATABASE()
                  AND TRIGGER_NAME IN (
                    'TR_MovimientosInventario_N06_OrigenTipado_BI',
                    'TR_MovimientosInventario_N06_OrigenTipado_BU'
                  )
                """)
            .SingleAsync(cancellationToken);

        if (triggerCount != 2)
            throw new InvalidOperationException(
                $"Canonical MySQL inventario bridge mismatch: triggers={triggerCount}.");

        var constraintCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'MovimientosInventario'
                  AND CONSTRAINT_TYPE = 'CHECK'
                  AND CONSTRAINT_NAME = 'CK_MovimientosInventario_OrigenTipado_Exclusivo_N06'
                """)
            .SingleAsync(cancellationToken);

        if (constraintCount != 1)
            throw new InvalidOperationException(
                $"Canonical MySQL inventario origin constraint mismatch: count={constraintCount}.");

        var paymentSeedCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM MetodosPago
                WHERE EmpresaId IS NULL AND CAST(Codigo AS BINARY) IN (
                    CAST('Efectivo' AS BINARY),
                    CAST('Transferencia' AS BINARY),
                    CAST('Tarjeta' AS BINARY),
                    CAST('Otro' AS BINARY)
                )
                  AND Activo = 1
                  AND Eliminado = 0
                """)
            .SingleAsync(cancellationToken);

        if (paymentSeedCount != 4)
            throw new InvalidOperationException(
                $"Canonical payment-method seed mismatch: count={paymentSeedCount}.");

        var defaultCustomerTypeSeedCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM TipoClientes
                WHERE EmpresaId IS NULL AND Codigo = 'SIN_CLASIFICAR'
                  AND EsSistema = 1
                  AND EsPredeterminado = 1
                  AND Activo = 1
                  AND Eliminado = 0
                """)
            .SingleAsync(cancellationToken);

        if (defaultCustomerTypeSeedCount != 1)
            throw new InvalidOperationException(
                $"Canonical default customer-type seed mismatch: count={defaultCustomerTypeSeedCount}.");
    }

    // EnsureCreated already materializes the current EF model. Record V6 only
    // for a newly bootstrapped schema after checking the actual contract.
    private static async Task VerifyFreshCheckoutPedidoAsync(AppDbContext db, CancellationToken ct)
    {
        var count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='PedidosVenta' AND IS_NULLABLE='YES'
            AND ((COLUMN_NAME='CostoEnvio' AND COLUMN_TYPE='decimal(18,2)')
              OR (COLUMN_NAME='MonedaCheckout' AND COLUMN_TYPE='varchar(3)')
              OR (COLUMN_NAME='TarifaEnvioCheckout' AND COLUMN_TYPE='varchar(120)')
              OR (COLUMN_NAME='EntregaCheckoutJson' AND COLUMN_TYPE='longtext'))
            """).SingleAsync(ct);
        var check = await db.Database.SqlQueryRaw<string>("""
            SELECT c.CHECK_CLAUSE AS Value FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t
            JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS c ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA
                AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME
            WHERE t.CONSTRAINT_SCHEMA=DATABASE() AND t.TABLE_NAME='PedidosVenta'
                AND t.CONSTRAINT_NAME='CK_PedidosVenta_Checkout_Atomico' AND t.ENFORCED='YES'
            """).SingleAsync(ct);
        static string Normalize(string value) =>
            string.Concat(value.Where(c => !char.IsWhiteSpace(c) && c != '`' && c != '(' && c != ')')).ToLowerInvariant();
        if (count != 4 || Normalize(check) != Normalize("((`CostoEnvio` IS NULL AND `MonedaCheckout` IS NULL AND `TarifaEnvioCheckout` IS NULL AND `EntregaCheckoutJson` IS NULL) OR (`CostoEnvio` IS NOT NULL AND `CostoEnvio` >= 0 AND `MonedaCheckout` IS NOT NULL AND CHAR_LENGTH(`MonedaCheckout`) = 3 AND `TarifaEnvioCheckout` IS NOT NULL AND `EntregaCheckoutJson` IS NOT NULL AND `CotizacionId` IS NULL))") ||
            await db.Set<Solqaryn.Domain.Entities.PedidoVenta>().IgnoreQueryFilters().AnyAsync(ct))
            throw new InvalidOperationException("Fresh checkout schema must match V6 and contain no orders.");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`,`ProductVersion`) VALUES ({0},{1})",
            new object[] { "20261128183000_QaCheckoutPedidoV6", "10.0.12" }, ct);
    }

    private static async Task SeedTipoClientePredeterminadoAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO TipoClientes
                (Codigo, EsSistema, Nombre, NombreNormalizado, Descripcion, ColorHex,
                 Activo, Orden, EsPredeterminado, Eliminado, FechaCreacion, FechaActualizacion)
            SELECT 'SIN_CLASIFICAR', 1, 'Sin clasificar', 'SIN CLASIFICAR',
                   'Cliente sin clasificar', '#9E9E9E', 1, 0, 1, 0,
                   UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
            WHERE NOT EXISTS (
                SELECT 1
                FROM TipoClientes
                WHERE EmpresaId IS NULL AND Codigo = 'SIN_CLASIFICAR'
            );
            """,
            cancellationToken);
    }

    private static async Task SeedPaymentMethodsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Efectivo', 'Efectivo', 'Efectivo', 1, 0, 0, 0, 10,
                   JSON_OBJECT('phase7CanonicalSeed', true, 'legacyEnum', 1), 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE EmpresaId IS NULL AND CAST(Codigo AS BINARY) = CAST('Efectivo' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Transferencia', 'Transferencia', 'Transferencia', 1, 0, 0, 0, 20,
                   JSON_OBJECT('phase7CanonicalSeed', true, 'legacyEnum', 2), 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE EmpresaId IS NULL AND CAST(Codigo AS BINARY) = CAST('Transferencia' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Tarjeta', 'Tarjeta', 'Tarjeta', 1, 0, 0, 0, 30,
                   JSON_OBJECT('phase7CanonicalSeed', true, 'legacyEnum', 3), 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE EmpresaId IS NULL AND CAST(Codigo AS BINARY) = CAST('Tarjeta' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Otro', 'Otro', 'Otro', 1, 0, 0, 0, 40,
                   JSON_OBJECT('phase7CanonicalSeed', true, 'legacyEnum', 4), 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE EmpresaId IS NULL AND CAST(Codigo AS BINARY) = CAST('Otro' AS BINARY));
            """,
            cancellationToken);
    }

    private static async Task EnsureInventarioOriginBridgeAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            "DROP TRIGGER IF EXISTS TR_MovimientosInventario_N06_OrigenTipado_BU;",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "DROP TRIGGER IF EXISTS TR_MovimientosInventario_N06_OrigenTipado_BI;",
            cancellationToken);

        var constraintCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'MovimientosInventario'
                  AND CONSTRAINT_TYPE = 'CHECK'
                  AND CONSTRAINT_NAME = 'CK_MovimientosInventario_OrigenTipado_Exclusivo_N06'
                """)
            .SingleAsync(cancellationToken);

        if (constraintCount > 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE MovimientosInventario DROP CHECK CK_MovimientosInventario_OrigenTipado_Exclusivo_N06;",
                cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER TR_MovimientosInventario_N06_OrigenTipado_BI
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.TransferenciaInventarioId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.RecepcionCompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.RecepcionCompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('RecepcionCompra' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER TR_MovimientosInventario_N06_OrigenTipado_BU
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.TransferenciaInventarioId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.RecepcionCompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.RecepcionCompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('RecepcionCompra' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE MovimientosInventario
            ADD CONSTRAINT CK_MovimientosInventario_OrigenTipado_Exclusivo_N06
            CHECK (
                ((CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) + (TransferenciaInventarioId IS NOT NULL) + (RecepcionCompraId IS NOT NULL) <= 1)
                AND
                (
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) AND CompraId = ReferenciaId AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL AND RecepcionCompraId IS NULL)
                    OR (CAST(ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) AND VentaId = ReferenciaId AND CompraId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL AND RecepcionCompraId IS NULL)
                    OR (CAST(ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) AND ConsumoInsumoId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL AND RecepcionCompraId IS NULL)
                    OR (CAST(ReferenciaTipo AS BINARY) = CAST('AjusteInventario' AS BINARY) AND AjusteInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND TransferenciaInventarioId IS NULL AND RecepcionCompraId IS NULL)
                    OR (CAST(ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) AND TransferenciaInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND RecepcionCompraId IS NULL)
                    OR (CAST(ReferenciaTipo AS BINARY) = CAST('RecepcionCompra' AS BINARY) AND RecepcionCompraId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL)
                    OR (CAST(Tipo AS BINARY) = CAST('Ajuste' AS BINARY) AND CAST(ReferenciaTipo AS BINARY) NOT IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY), CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY), CAST('ConsumoInsumo' AS BINARY), CAST('AjusteInventario' AS BINARY), CAST('TransferenciaInventario' AS BINARY), CAST('RecepcionCompra' AS BINARY)) AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL AND RecepcionCompraId IS NULL)
                )
            );
            """,
            cancellationToken);
    }
}
