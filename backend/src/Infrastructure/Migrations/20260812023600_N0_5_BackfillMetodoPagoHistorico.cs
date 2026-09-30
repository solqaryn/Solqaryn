using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.5: siembra el catálogo histórico de métodos de pago y realiza el backfill
/// relacional sin modificar ni reinterpretar los valores legacy existentes.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260812023600_N0_5_BackfillMetodoPagoHistorico")]
public sealed class N0_5_BackfillMetodoPagoHistorico : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preflight fail-closed. No se normalizan silenciosamente mayúsculas, espacios
        // ni valores desconocidos: cualquier dato fuera del contrato histórico aborta.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N05Guard;
            CREATE TEMPORARY TABLE __N05Guard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N05_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N05Guard (Id, Violaciones)
            SELECT 1,
                (SELECT COUNT(*)
                   FROM Ventas
                  WHERE MetodoPago IS NULL
                     OR CAST(MetodoPago AS BINARY) NOT IN
                        (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY)))
              + (SELECT COUNT(*)
                   FROM FacturaPagos
                  WHERE MetodoPago NOT IN (1, 2, 3, 4))
              + (SELECT COUNT(*)
                   FROM MovimientosFinancieros
                  WHERE MetodoPago IS NOT NULL
                    AND CAST(MetodoPago AS BINARY) NOT IN
                        (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY)))
              + (SELECT COUNT(*)
                   FROM Compras
                  WHERE MetodoPago IS NULL
                     OR CAST(MetodoPago AS BINARY) NOT IN
                        (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY)))
              + (SELECT COUNT(*) FROM Ventas WHERE MetodoPagoId IS NOT NULL)
              + (SELECT COUNT(*) FROM FacturaPagos WHERE MetodoPagoId IS NOT NULL)
              + (SELECT COUNT(*) FROM MovimientosFinancieros WHERE MetodoPagoId IS NOT NULL)
              + (SELECT COUNT(*)
                   FROM MetodosPago mp
                  WHERE LOWER(TRIM(mp.Codigo)) IN ('efectivo', 'transferencia', 'tarjeta', 'otro')
                    AND (
                        mp.Activo <> 1
                        OR mp.Eliminado <> 0
                        OR (LOWER(TRIM(mp.Codigo)) = 'efectivo' AND
                            (CAST(mp.Codigo AS BINARY) <> CAST('Efectivo' AS BINARY)
                             OR CAST(mp.Nombre AS BINARY) <> CAST('Efectivo' AS BINARY)
                             OR CAST(mp.Tipo AS BINARY) <> CAST('Efectivo' AS BINARY)))
                        OR (LOWER(TRIM(mp.Codigo)) = 'transferencia' AND
                            (CAST(mp.Codigo AS BINARY) <> CAST('Transferencia' AS BINARY)
                             OR CAST(mp.Nombre AS BINARY) <> CAST('Transferencia' AS BINARY)
                             OR CAST(mp.Tipo AS BINARY) <> CAST('Transferencia' AS BINARY)))
                        OR (LOWER(TRIM(mp.Codigo)) = 'tarjeta' AND
                            (CAST(mp.Codigo AS BINARY) <> CAST('Tarjeta' AS BINARY)
                             OR CAST(mp.Nombre AS BINARY) <> CAST('Tarjeta' AS BINARY)
                             OR CAST(mp.Tipo AS BINARY) <> CAST('Tarjeta' AS BINARY)))
                        OR (LOWER(TRIM(mp.Codigo)) = 'otro' AND
                            (CAST(mp.Codigo AS BINARY) <> CAST('Otro' AS BINARY)
                             OR CAST(mp.Nombre AS BINARY) <> CAST('Otro' AS BINARY)
                             OR CAST(mp.Tipo AS BINARY) <> CAST('Otro' AS BINARY)))
                    ));

            DROP TEMPORARY TABLE __N05Guard;
            """);

        // Snapshot temporal de cada valor legacy y su identidad. Se usa después del
        // backfill para demostrar que no cambió ni una fila ni un valor histórico.
        // Se declaran PK explícitas para ser compatibles con MySQL administrado/Aiven
        // cuando sql_require_primary_key está habilitado.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N05VentasAntes;
            DROP TEMPORARY TABLE IF EXISTS __N05FacturaPagosAntes;
            DROP TEMPORARY TABLE IF EXISTS __N05MovimientosAntes;
            DROP TEMPORARY TABLE IF EXISTS __N05ComprasAntes;

            CREATE TEMPORARY TABLE __N05VentasAntes
            (
                Id INT NOT NULL PRIMARY KEY,
                MetodoPago VARCHAR(20) NOT NULL
            );
            INSERT INTO __N05VentasAntes (Id, MetodoPago)
                SELECT Id, MetodoPago FROM Ventas;

            CREATE TEMPORARY TABLE __N05FacturaPagosAntes
            (
                Id INT NOT NULL PRIMARY KEY,
                MetodoPago INT NOT NULL
            );
            INSERT INTO __N05FacturaPagosAntes (Id, MetodoPago)
                SELECT Id, MetodoPago FROM FacturaPagos;

            CREATE TEMPORARY TABLE __N05MovimientosAntes
            (
                Id INT NOT NULL PRIMARY KEY,
                MetodoPago VARCHAR(20) NULL
            );
            INSERT INTO __N05MovimientosAntes (Id, MetodoPago)
                SELECT Id, MetodoPago FROM MovimientosFinancieros;

            CREATE TEMPORARY TABLE __N05ComprasAntes
            (
                Id INT NOT NULL PRIMARY KEY,
                MetodoPago VARCHAR(20) NOT NULL
            );
            INSERT INTO __N05ComprasAntes (Id, MetodoPago)
                SELECT Id, MetodoPago FROM Compras;
            """);

        // Seed idempotente por Codigo funcional. Los Id autoincrementales NO forman
        // parte de la equivalencia con el enum histórico 1..4.
        migrationBuilder.Sql("""
            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Efectivo', 'Efectivo', 'Efectivo', 1, 0, 0, 0, 10,
                   '{"erpN05Seed":true,"legacyEnum":1}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'ERP-N0.5', 'ERP-N0.5'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Efectivo' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Transferencia', 'Transferencia', 'Transferencia', 1, 0, 0, 0, 20,
                   '{"erpN05Seed":true,"legacyEnum":2}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'ERP-N0.5', 'ERP-N0.5'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Transferencia' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Tarjeta', 'Tarjeta', 'Tarjeta', 1, 0, 0, 0, 30,
                   '{"erpN05Seed":true,"legacyEnum":3}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'ERP-N0.5', 'ERP-N0.5'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Tarjeta' AS BINARY));

            INSERT INTO MetodosPago
                (Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco,
                 PermiteCambio, Orden, Metadata, Eliminado, FechaCreacion,
                 FechaActualizacion, CreadoPorNombreUsuario, ActualizadoPorNombreUsuario)
            SELECT 'Otro', 'Otro', 'Otro', 1, 0, 0, 0, 40,
                   '{"erpN05Seed":true,"legacyEnum":4}', 0, UTC_TIMESTAMP(6),
                   UTC_TIMESTAMP(6), 'ERP-N0.5', 'ERP-N0.5'
             WHERE NOT EXISTS (
                 SELECT 1 FROM MetodosPago
                  WHERE CAST(Codigo AS BINARY) = CAST('Otro' AS BINARY));
            """);

        // Strings legacy: correspondencia exacta por Codigo.
        migrationBuilder.Sql("""
            UPDATE Ventas v
            JOIN MetodosPago mp
              ON CAST(mp.Codigo AS BINARY) = CAST(v.MetodoPago AS BINARY)
               SET v.MetodoPagoId = mp.Id
             WHERE v.MetodoPagoId IS NULL;

            UPDATE MovimientosFinancieros mf
            JOIN MetodosPago mp
              ON CAST(mp.Codigo AS BINARY) = CAST(mf.MetodoPago AS BINARY)
               SET mf.MetodoPagoId = mp.Id
             WHERE mf.MetodoPago IS NOT NULL
               AND mf.MetodoPagoId IS NULL;
            """);

        // FacturaPago persistía el enum como int. La conversión es explícita por
        // Codigo estable y nunca por Id del nuevo catálogo.
        migrationBuilder.Sql("""
            UPDATE FacturaPagos fp
            JOIN MetodosPago mp
              ON CAST(mp.Codigo AS BINARY) = CAST(
                    CASE fp.MetodoPago
                        WHEN 1 THEN 'Efectivo'
                        WHEN 2 THEN 'Transferencia'
                        WHEN 3 THEN 'Tarjeta'
                        WHEN 4 THEN 'Otro'
                    END AS BINARY)
               SET fp.MetodoPagoId = mp.Id
             WHERE fp.MetodoPagoId IS NULL;
            """);

        // Postcheck dentro de la propia migración. Cada guard se ejecuta en un
        // statement independiente porque MySQL no permite reabrir la misma tabla
        // temporal más de una vez dentro de un SELECT compuesto.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N05PostGuard;
            CREATE TEMPORARY TABLE __N05PostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N05_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 1, IF(COUNT(*) = 4, 0, 1)
              FROM MetodosPago mp
             WHERE mp.Activo = 1
               AND mp.Eliminado = 0
               AND (
                    (CAST(mp.Codigo AS BINARY) = CAST('Efectivo' AS BINARY)
                     AND CAST(mp.Nombre AS BINARY) = CAST('Efectivo' AS BINARY)
                     AND CAST(mp.Tipo AS BINARY) = CAST('Efectivo' AS BINARY))
                 OR (CAST(mp.Codigo AS BINARY) = CAST('Transferencia' AS BINARY)
                     AND CAST(mp.Nombre AS BINARY) = CAST('Transferencia' AS BINARY)
                     AND CAST(mp.Tipo AS BINARY) = CAST('Transferencia' AS BINARY))
                 OR (CAST(mp.Codigo AS BINARY) = CAST('Tarjeta' AS BINARY)
                     AND CAST(mp.Nombre AS BINARY) = CAST('Tarjeta' AS BINARY)
                     AND CAST(mp.Tipo AS BINARY) = CAST('Tarjeta' AS BINARY))
                 OR (CAST(mp.Codigo AS BINARY) = CAST('Otro' AS BINARY)
                     AND CAST(mp.Nombre AS BINARY) = CAST('Otro' AS BINARY)
                     AND CAST(mp.Tipo AS BINARY) = CAST('Otro' AS BINARY))
               );

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 2, COUNT(*)
              FROM Ventas v
             WHERE v.MetodoPagoId IS NULL
                OR NOT EXISTS (
                    SELECT 1 FROM MetodosPago mp
                     WHERE mp.Id = v.MetodoPagoId
                       AND CAST(mp.Codigo AS BINARY) = CAST(v.MetodoPago AS BINARY));

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 3, COUNT(*)
              FROM FacturaPagos fp
             WHERE fp.MetodoPagoId IS NULL
                OR NOT EXISTS (
                    SELECT 1 FROM MetodosPago mp
                     WHERE mp.Id = fp.MetodoPagoId
                       AND CAST(mp.Codigo AS BINARY) = CAST(
                           CASE fp.MetodoPago
                               WHEN 1 THEN 'Efectivo'
                               WHEN 2 THEN 'Transferencia'
                               WHEN 3 THEN 'Tarjeta'
                               WHEN 4 THEN 'Otro'
                           END AS BINARY));

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 4, COUNT(*)
              FROM MovimientosFinancieros mf
             WHERE (mf.MetodoPago IS NULL AND mf.MetodoPagoId IS NOT NULL)
                OR (mf.MetodoPago IS NOT NULL AND
                    (mf.MetodoPagoId IS NULL OR NOT EXISTS (
                        SELECT 1 FROM MetodosPago mp
                         WHERE mp.Id = mf.MetodoPagoId
                           AND CAST(mp.Codigo AS BINARY) = CAST(mf.MetodoPago AS BINARY))));

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 5, ABS((SELECT COUNT(*) FROM Ventas) - COUNT(*))
              FROM __N05VentasAntes;

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 6, COUNT(*)
              FROM __N05VentasAntes a
              LEFT JOIN Ventas v ON v.Id = a.Id
             WHERE v.Id IS NULL
                OR CAST(v.MetodoPago AS BINARY) <> CAST(a.MetodoPago AS BINARY);

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 7, ABS((SELECT COUNT(*) FROM FacturaPagos) - COUNT(*))
              FROM __N05FacturaPagosAntes;

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 8, COUNT(*)
              FROM __N05FacturaPagosAntes a
              LEFT JOIN FacturaPagos fp ON fp.Id = a.Id
             WHERE fp.Id IS NULL OR fp.MetodoPago <> a.MetodoPago;

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 9, ABS((SELECT COUNT(*) FROM MovimientosFinancieros) - COUNT(*))
              FROM __N05MovimientosAntes;

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 10, COUNT(*)
              FROM __N05MovimientosAntes a
              LEFT JOIN MovimientosFinancieros mf ON mf.Id = a.Id
             WHERE mf.Id IS NULL
                OR NOT (
                    (mf.MetodoPago IS NULL AND a.MetodoPago IS NULL)
                    OR (mf.MetodoPago IS NOT NULL AND a.MetodoPago IS NOT NULL
                        AND CAST(mf.MetodoPago AS BINARY) = CAST(a.MetodoPago AS BINARY)));

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 11, ABS((SELECT COUNT(*) FROM Compras) - COUNT(*))
              FROM __N05ComprasAntes;

            INSERT INTO __N05PostGuard (Id, Violaciones)
            SELECT 12, COUNT(*)
              FROM __N05ComprasAntes a
              LEFT JOIN Compras c ON c.Id = a.Id
             WHERE c.Id IS NULL
                OR CAST(c.MetodoPago AS BINARY) <> CAST(a.MetodoPago AS BINARY);

            DROP TEMPORARY TABLE __N05PostGuard;
            DROP TEMPORARY TABLE __N05VentasAntes;
            DROP TEMPORARY TABLE __N05FacturaPagosAntes;
            DROP TEMPORARY TABLE __N05MovimientosAntes;
            DROP TEMPORARY TABLE __N05ComprasAntes;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "ERP-N0.5 es forward-only: el backfill histórico no debe revertirse por borrado automático. " +
            "Para volver al estado anterior debe restaurarse el respaldo/preflight correspondiente.");
}
