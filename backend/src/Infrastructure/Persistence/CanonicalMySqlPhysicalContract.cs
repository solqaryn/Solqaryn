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

        await SeedPaymentMethodsAsync(db, cancellationToken);
        await EnsureInventoryOriginBridgeAsync(db, cancellationToken);
    }

    public static async Task VerifyAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        var triggerCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TRIGGERS
                WHERE TRIGGER_SCHEMA = DATABASE()
                  AND TRIGGER_NAME IN (
                    'TR_MovimientosInventario_N06_OrigenTipado_BI',
                    'TR_MovimientosInventario_N06_OrigenTipado_BU'
                  );
                """)
            .SingleAsync(cancellationToken);

        if (triggerCount != 2)
            throw new InvalidOperationException(
                $"Canonical MySQL inventory bridge mismatch: triggers={triggerCount}.");

        var constraintCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'MovimientosInventario'
                  AND CONSTRAINT_TYPE = 'CHECK'
                  AND CONSTRAINT_NAME = 'CK_MovimientosInventario_OrigenTipado_Exclusivo_N06';
                """)
            .SingleAsync(cancellationToken);

        if (constraintCount != 1)
            throw new InvalidOperationException(
                $"Canonical MySQL inventory origin constraint mismatch: count={constraintCount}.");

        var paymentSeedCount = await db.Database
            .SqlQueryRaw<long>("""
                SELECT COUNT(*) AS Value
                FROM MetodosPago
                WHERE CAST(Codigo AS BINARY) IN (
                    CAST('Efectivo' AS BINARY),
                    CAST('Transferencia' AS BINARY),
                    CAST('Tarjeta' AS BINARY),
                    CAST('Otro' AS BINARY)
                )
                  AND Activo = 1
                  AND Eliminado = 0;
                """)
            .SingleAsync(cancellationToken);

        if (paymentSeedCount != 4)
            throw new InvalidOperationException(
                $"Canonical payment-method seed mismatch: count={paymentSeedCount}.");
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
                   '{"phase7CanonicalSeed":true,"legacyEnum":1}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Efectivo' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Transferencia', 'Transferencia', 'Transferencia', 1, 0, 0, 0, 20,
                   '{"phase7CanonicalSeed":true,"legacyEnum":2}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Transferencia' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Tarjeta', 'Tarjeta', 'Tarjeta', 1, 0, 0, 0, 30,
                   '{"phase7CanonicalSeed":true,"legacyEnum":3}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Tarjeta' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Otro', 'Otro', 'Otro', 1, 0, 0, 0, 40,
                   '{"phase7CanonicalSeed":true,"legacyEnum":4}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'SOLQARYN-F7', 'SOLQARYN-F7'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Otro' AS BINARY));
            """,
            cancellationToken);
    }

    private static async Task EnsureInventoryOriginBridgeAsync(
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
                  AND CONSTRAINT_NAME = 'CK_MovimientosInventario_OrigenTipado_Exclusivo_N06';
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
