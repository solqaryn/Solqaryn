-- ERP-N1.7.C — Preflight read-only para persistencia de conteos físicos.
-- No modifica datos ni esquema. Debe ejecutarse antes de aplicar la migración N1.7.

SELECT DATABASE() AS BaseDatosActual,
       VERSION() AS VersionMySql;

SELECT TABLE_NAME
  FROM information_schema.TABLES
 WHERE TABLE_SCHEMA = DATABASE()
   AND TABLE_NAME IN ('ConteosInventario', 'ConteoInventarioDetalles')
 ORDER BY TABLE_NAME;

SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, EXTRA
  FROM information_schema.COLUMNS
 WHERE TABLE_SCHEMA = DATABASE()
   AND TABLE_NAME IN ('Almacenes', 'UbicacionesAlmacen', 'ProductoVariantes', 'AjustesInventario')
   AND COLUMN_NAME IN ('Id', 'AlmacenId')
 ORDER BY TABLE_NAME, ORDINAL_POSITION;

SELECT CONSTRAINT_NAME, TABLE_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
  FROM information_schema.KEY_COLUMN_USAGE
 WHERE TABLE_SCHEMA = DATABASE()
   AND (
        (TABLE_NAME = 'UbicacionesAlmacen' AND CONSTRAINT_NAME = 'AK_UbicacionesAlmacen_AlmacenId_Id')
        OR TABLE_NAME IN ('Almacenes', 'ProductoVariantes', 'AjustesInventario')
       )
 ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;

SELECT MIGRATION_ID
  FROM __EFMigrationsHistory
 ORDER BY MIGRATION_ID DESC
 LIMIT 20;

-- Guardas informativas: cualquier fila devuelta requiere reconciliación antes de migrar.
SELECT 'TABLA_CONTEOS_YA_EXISTE' AS Hallazgo, TABLE_NAME AS Evidencia
  FROM information_schema.TABLES
 WHERE TABLE_SCHEMA = DATABASE()
   AND TABLE_NAME IN ('ConteosInventario', 'ConteoInventarioDetalles');

SELECT 'ALT_KEY_UBICACION_AUSENTE' AS Hallazgo, 'AK_UbicacionesAlmacen_AlmacenId_Id' AS Evidencia
 WHERE NOT EXISTS (
       SELECT 1
         FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'UbicacionesAlmacen'
          AND CONSTRAINT_NAME = 'AK_UbicacionesAlmacen_AlmacenId_Id'
     );
