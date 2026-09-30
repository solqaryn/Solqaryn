-- ERP-N0.8.A — Preflight de migraciones y limpieza
-- Objetivo: producir evidencia de deuda legacy, históricos, autoridades y dependencias
-- antes de cualquier eliminación física. Este script es ESTRICTAMENTE DE SOLO LECTURA.
-- Compatible con MySQL 8.x. Ejecutar únicamente contra una base de dev.

SET @schema_name := DATABASE();

SELECT
    'N0.8_PRECHECK_CONTEXT' AS check_id,
    @schema_name AS schema_name,
    VERSION() AS mysql_version,
    @@sql_mode AS sql_mode,
    @@sql_require_primary_key AS sql_require_primary_key;

-- 1) Inventario de tablas que participan directamente en el saneamiento ERP-N0.8.
SELECT
    'N0.8_TABLE_INVENTORY' AS check_id,
    t.TABLE_NAME,
    t.ENGINE,
    t.TABLE_ROWS,
    t.CREATE_TIME,
    t.UPDATE_TIME
FROM information_schema.TABLES t
WHERE t.TABLE_SCHEMA = @schema_name
  AND LOWER(t.TABLE_NAME) REGEXP
      '^(productos|productovariantes|compras|ventas|facturapagos|metodospago|movimientosfinancieros|movimientosinventario|ajustesinventario|ajusteinventariodetalles|rolpermisos|usuarios|registrosauditoria|catalogosproducto)$'
ORDER BY t.TABLE_NAME;

-- 2) Deuda física/de compatibilidad confirmada que requiere decisión explícita.
-- La salida NO significa "DROP automático". decision_hint define el tratamiento esperado.
SELECT
    'N0.8_COMPATIBILITY_COLUMNS' AS check_id,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.COLUMN_TYPE,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT,
    c.COLUMN_KEY,
    CASE
        WHEN LOWER(c.TABLE_NAME) = 'productos'
             AND LOWER(c.COLUMN_NAME) IN
                 ('marca','modelo','cantidad','costo','precio','umbralstockbajo','colorid','tallaid','marcaid','modeloid')
            THEN 'RETIRE_AFTER_PRODUCTO_VARIANTE_RECONCILIATION'
        WHEN LOWER(c.TABLE_NAME) IN ('compras','ventas','facturapagos','movimientosfinancieros')
             AND LOWER(c.COLUMN_NAME) = 'metodopago'
            THEN 'RETIRE_AFTER_METODOPAGO_RELATIONAL_BACKFILL'
        WHEN LOWER(c.TABLE_NAME) = 'movimientosinventario'
             AND LOWER(c.COLUMN_NAME) IN ('referenciatipo','referenciaid')
            THEN 'RETIRE_AFTER_TYPED_ORIGIN_CONSUMERS_ZERO'
        WHEN LOWER(c.TABLE_NAME) = 'movimientosfinancieros'
             AND LOWER(c.COLUMN_NAME) IN ('moduloorigen','referenciaid')
            THEN 'REVIEW_KEEP_AS_AUDIT_SNAPSHOT_OR_RETIRE'
        ELSE 'REVIEW'
    END AS decision_hint
FROM information_schema.COLUMNS c
WHERE c.TABLE_SCHEMA = @schema_name
  AND (
      (LOWER(c.TABLE_NAME) = 'productos'
       AND LOWER(c.COLUMN_NAME) IN
           ('marca','modelo','cantidad','costo','precio','umbralstockbajo','colorid','tallaid','marcaid','modeloid'))
      OR
      (LOWER(c.TABLE_NAME) IN ('compras','ventas','facturapagos','movimientosfinancieros')
       AND LOWER(c.COLUMN_NAME) = 'metodopago')
      OR
      (LOWER(c.TABLE_NAME) = 'movimientosinventario'
       AND LOWER(c.COLUMN_NAME) IN ('referenciatipo','referenciaid'))
      OR
      (LOWER(c.TABLE_NAME) = 'movimientosfinancieros'
       AND LOWER(c.COLUMN_NAME) IN ('moduloorigen','referenciaid'))
  )
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;

-- 3) Snapshots históricos que NO deben confundirse con doble autoridad.
SELECT
    'N0.8_HISTORICAL_SNAPSHOTS' AS check_id,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.COLUMN_TYPE,
    'KEEP_UNLESS_EXPLICIT_HISTORICAL_PROOF_SUPPORTS_REMOVAL' AS decision_hint
FROM information_schema.COLUMNS c
WHERE c.TABLE_SCHEMA = @schema_name
  AND (
      LOWER(c.COLUMN_NAME) LIKE '%snapshot%'
      OR LOWER(c.COLUMN_NAME) LIKE '%histor%'
  )
  AND LOWER(c.TABLE_NAME) REGEXP
      'producto|compra|venta|factura|movimiento|ajuste|auditor'
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;

-- 4) Autoridades relacionales/tipadas que deben existir antes de retirar compatibilidad.
SELECT
    'N0.8_AUTHORITY_COLUMNS' AS check_id,
    expected.TABLE_NAME,
    expected.COLUMN_NAME,
    expected.PURPOSE,
    CASE WHEN c.COLUMN_NAME IS NULL THEN 0 ELSE 1 END AS present,
    c.COLUMN_TYPE,
    c.IS_NULLABLE,
    c.COLUMN_KEY
FROM (
    SELECT 'Productos' AS TABLE_NAME, 'Id' AS COLUMN_NAME, 'FAMILIA_PRODUCTO' AS PURPOSE
    UNION ALL SELECT 'ProductoVariantes','ProductoId','AUTORIDAD_UNIDAD_INVENTARIABLE'
    UNION ALL SELECT 'ProductoVariantes','Cantidad','AUTORIDAD_STOCK'
    UNION ALL SELECT 'ProductoVariantes','Costo','AUTORIDAD_COSTO'
    UNION ALL SELECT 'ProductoVariantes','Precio','AUTORIDAD_PRECIO'
    UNION ALL SELECT 'ProductoVariantes','UmbralStockBajo','AUTORIDAD_UMBRAL'
    UNION ALL SELECT 'ProductoVariantes','MarcaId','AUTORIDAD_MARCA'
    UNION ALL SELECT 'ProductoVariantes','ModeloId','AUTORIDAD_MODELO'
    UNION ALL SELECT 'ProductoVariantes','ColorId','AUTORIDAD_COLOR'
    UNION ALL SELECT 'ProductoVariantes','TallaId','AUTORIDAD_TALLA'
    UNION ALL SELECT 'Compras','MetodoPagoId','AUTORIDAD_METODO_PAGO_COMPRA_PENDIENTE_N08'
    UNION ALL SELECT 'Ventas','MetodoPagoId','AUTORIDAD_METODO_PAGO'
    UNION ALL SELECT 'FacturaPagos','MetodoPagoId','AUTORIDAD_METODO_PAGO'
    UNION ALL SELECT 'MovimientosFinancieros','MetodoPagoId','AUTORIDAD_METODO_PAGO'
    UNION ALL SELECT 'MovimientosFinancieros','CompraId','AUTORIDAD_ORIGEN_COMPRA'
    UNION ALL SELECT 'MovimientosFinancieros','VentaId','AUTORIDAD_ORIGEN_VENTA'
    UNION ALL SELECT 'MovimientosFinancieros','FacturaId','AUTORIDAD_ORIGEN_FACTURA'
    UNION ALL SELECT 'MovimientosInventario','CompraId','AUTORIDAD_ORIGEN_COMPRA'
    UNION ALL SELECT 'MovimientosInventario','VentaId','AUTORIDAD_ORIGEN_VENTA'
    UNION ALL SELECT 'MovimientosInventario','ConsumoInsumoId','AUTORIDAD_ORIGEN_CONSUMO'
    UNION ALL SELECT 'MovimientosInventario','AjusteInventarioId','AUTORIDAD_ORIGEN_AJUSTE'
    UNION ALL SELECT 'Usuarios','RolId','AUTORIDAD_RBAC_ROL'
    UNION ALL SELECT 'RolPermisos','RolId','AUTORIDAD_RBAC_ROL'
    UNION ALL SELECT 'RolPermisos','PermisoId','AUTORIDAD_RBAC_PERMISO'
) expected
LEFT JOIN information_schema.COLUMNS c
  ON c.TABLE_SCHEMA = @schema_name
 AND LOWER(c.TABLE_NAME) = LOWER(expected.TABLE_NAME)
 AND LOWER(c.COLUMN_NAME) = LOWER(expected.COLUMN_NAME)
ORDER BY expected.TABLE_NAME, expected.COLUMN_NAME;

-- 5) Estructuras que los puntos ya cerrados N0.2/N0.4 exigen AUSENTES.
-- Si aparecen, N0.8 no debe continuar con DROP adicionales: primero hay drift/regresión.
SELECT
    'N0.8_EXPECTED_ABSENT' AS check_id,
    x.object_kind,
    x.object_name,
    x.present,
    CASE WHEN x.present = 0 THEN 'PASS' ELSE 'FAIL_REGRESSION_OR_SCHEMA_DRIFT' END AS result
FROM (
    SELECT
        'TABLE' AS object_kind,
        'CatalogosProducto' AS object_name,
        CASE WHEN EXISTS (
            SELECT 1 FROM information_schema.TABLES t
            WHERE t.TABLE_SCHEMA = @schema_name
              AND LOWER(t.TABLE_NAME) = 'catalogosproducto'
        ) THEN 1 ELSE 0 END AS present
    UNION ALL
    SELECT
        'COLUMN', 'Usuarios.Rol',
        CASE WHEN EXISTS (
            SELECT 1 FROM information_schema.COLUMNS c
            WHERE c.TABLE_SCHEMA = @schema_name
              AND LOWER(c.TABLE_NAME) = 'usuarios'
              AND LOWER(c.COLUMN_NAME) = 'rol'
        ) THEN 1 ELSE 0 END
    UNION ALL
    SELECT
        'COLUMN', 'RolPermisos.Rol',
        CASE WHEN EXISTS (
            SELECT 1 FROM information_schema.COLUMNS c
            WHERE c.TABLE_SCHEMA = @schema_name
              AND LOWER(c.TABLE_NAME) = 'rolpermisos'
              AND LOWER(c.COLUMN_NAME) = 'rol'
        ) THEN 1 ELSE 0 END
    UNION ALL
    SELECT
        'COLUMN', 'RolPermisos.Modulo',
        CASE WHEN EXISTS (
            SELECT 1 FROM information_schema.COLUMNS c
            WHERE c.TABLE_SCHEMA = @schema_name
              AND LOWER(c.TABLE_NAME) = 'rolpermisos'
              AND LOWER(c.COLUMN_NAME) = 'modulo'
        ) THEN 1 ELSE 0 END
    UNION ALL
    SELECT
        'COLUMN', 'RolPermisos.Accion',
        CASE WHEN EXISTS (
            SELECT 1 FROM information_schema.COLUMNS c
            WHERE c.TABLE_SCHEMA = @schema_name
              AND LOWER(c.TABLE_NAME) = 'rolpermisos'
              AND LOWER(c.COLUMN_NAME) = 'accion'
        ) THEN 1 ELSE 0 END
    UNION ALL
    SELECT
        'COLUMN', 'RolPermisos.Permitido',
        CASE WHEN EXISTS (
            SELECT 1 FROM information_schema.COLUMNS c
            WHERE c.TABLE_SCHEMA = @schema_name
              AND LOWER(c.TABLE_NAME) = 'rolpermisos'
              AND LOWER(c.COLUMN_NAME) = 'permitido'
        ) THEN 1 ELSE 0 END
) x;

-- 6) Metadatos deliberados que deben preservarse salvo decisión funcional distinta.
SELECT
    'N0.8_EXPLICIT_KEEP' AS check_id,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.COLUMN_TYPE,
    'KEEP_RBAC_METADATA_NOT_AUTHORIZATION_BYPASS' AS decision_hint
FROM information_schema.COLUMNS c
WHERE c.TABLE_SCHEMA = @schema_name
  AND LOWER(c.TABLE_NAME) = 'roles'
  AND LOWER(c.COLUMN_NAME) = 'esadministrador';

-- 7) Foreign keys: ninguna columna/tabla puede retirarse si aún sostiene referencias.
SELECT
    'N0.8_FOREIGN_KEYS' AS check_id,
    kcu.TABLE_NAME,
    kcu.COLUMN_NAME,
    kcu.CONSTRAINT_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    rc.UPDATE_RULE,
    rc.DELETE_RULE
FROM information_schema.KEY_COLUMN_USAGE kcu
LEFT JOIN information_schema.REFERENTIAL_CONSTRAINTS rc
  ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
 AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
 AND rc.TABLE_NAME = kcu.TABLE_NAME
WHERE kcu.TABLE_SCHEMA = @schema_name
  AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
  AND (
      LOWER(kcu.TABLE_NAME) REGEXP
          'producto|metodo.*pago|rol.*permiso|movimiento.*inventario|movimiento.*financiero|ajuste.*inventario|venta|compra|factura|auditor'
      OR LOWER(kcu.REFERENCED_TABLE_NAME) REGEXP
          'producto|metodo.*pago|rol.*permiso|movimiento.*inventario|movimiento.*financiero|ajuste.*inventario|venta|compra|factura|auditor'
  )
ORDER BY kcu.TABLE_NAME, kcu.COLUMN_NAME;

-- 8) Índices/PK: inventario para rollback, backfill y compatibilidad con MySQL administrado.
SELECT
    'N0.8_INDEXES' AS check_id,
    s.TABLE_NAME,
    s.INDEX_NAME,
    s.NON_UNIQUE,
    GROUP_CONCAT(s.COLUMN_NAME ORDER BY s.SEQ_IN_INDEX SEPARATOR ',') AS columns_in_index
FROM information_schema.STATISTICS s
WHERE s.TABLE_SCHEMA = @schema_name
  AND LOWER(s.TABLE_NAME) REGEXP
      'producto|metodo.*pago|rol.*permiso|movimiento.*inventario|movimiento.*financiero|ajuste.*inventario|venta|compra|factura|auditor'
GROUP BY s.TABLE_NAME, s.INDEX_NAME, s.NON_UNIQUE
ORDER BY s.TABLE_NAME, s.INDEX_NAME;

-- 9) Migraciones aplicadas: reconciliar código vs. esquema antes de backfill/rollback.
SET @has_ef_history := (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = @schema_name
      AND TABLE_NAME = '__EFMigrationsHistory'
);

SELECT
    'N0.8_EF_HISTORY_PRESENT' AS check_id,
    @has_ef_history AS present;

SET @sql := IF(
    @has_ef_history > 0,
    'SELECT ''N0.8_EF_HISTORY'' AS check_id, MigrationId, ProductVersion FROM `__EFMigrationsHistory` ORDER BY MigrationId',
    'SELECT ''N0.8_EF_HISTORY'' AS check_id, ''MISSING'' AS MigrationId, NULL AS ProductVersion'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 10) Triggers y vistas pueden mantener dependencias invisibles para DROP/rename.
SELECT
    'N0.8_TRIGGERS' AS check_id,
    tr.TRIGGER_NAME,
    tr.EVENT_OBJECT_TABLE,
    tr.EVENT_MANIPULATION,
    tr.ACTION_TIMING
FROM information_schema.TRIGGERS tr
WHERE tr.TRIGGER_SCHEMA = @schema_name
  AND LOWER(tr.EVENT_OBJECT_TABLE) REGEXP
      'producto|metodo.*pago|rol.*permiso|movimiento.*inventario|movimiento.*financiero|ajuste.*inventario|venta|compra|factura|auditor'
ORDER BY tr.EVENT_OBJECT_TABLE, tr.TRIGGER_NAME;

SELECT
    'N0.8_VIEWS' AS check_id,
    v.TABLE_NAME AS VIEW_NAME,
    v.IS_UPDATABLE,
    v.SECURITY_TYPE
FROM information_schema.VIEWS v
WHERE v.TABLE_SCHEMA = @schema_name
  AND LOWER(v.VIEW_DEFINITION) REGEXP
      'producto|metodo.*pago|rol.*permiso|movimiento.*inventario|movimiento.*financiero|ajuste.*inventario|venta|compra|factura|auditor'
ORDER BY v.TABLE_NAME;

-- 11) Resultado contractual. PASS aquí significa "preflight ejecutable/contexto válido",
-- NO que sea seguro hacer DROP. La seguridad de limpieza exige cero bloqueos de datos,
-- backup/restauración vigente, consumidores runtime reconciliados y postcheck en N0.8.C/G.
SELECT
    'N0.8_PREFLIGHT_RESULT' AS check_id,
    CASE WHEN @schema_name IS NULL OR @schema_name = '' THEN 'FAIL' ELSE 'PASS' END AS result,
    CASE
        WHEN @schema_name IS NULL OR @schema_name = '' THEN 'No hay base seleccionada.'
        ELSE 'Inventario generado. No ejecutar DROP por este resultado: revisar compatibilidad, autoridades, snapshots, FKs, índices, migraciones, triggers, vistas y consumidores runtime.'
    END AS detail;
