-- ERP-N1.7.C — Postcheck read-only para conteos físicos.
-- Debe devolver cero filas en las consultas de VIOLACION.

SELECT TABLE_NAME
  FROM information_schema.TABLES
 WHERE TABLE_SCHEMA = DATABASE()
   AND TABLE_NAME IN ('ConteosInventario', 'ConteoInventarioDetalles')
 ORDER BY TABLE_NAME;

SELECT TABLE_NAME, CONSTRAINT_NAME, CONSTRAINT_TYPE
  FROM information_schema.TABLE_CONSTRAINTS
 WHERE CONSTRAINT_SCHEMA = DATABASE()
   AND TABLE_NAME IN ('ConteosInventario', 'ConteoInventarioDetalles')
 ORDER BY TABLE_NAME, CONSTRAINT_TYPE, CONSTRAINT_NAME;

SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE,
       GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',') AS Columnas
  FROM information_schema.STATISTICS
 WHERE TABLE_SCHEMA = DATABASE()
   AND TABLE_NAME IN ('ConteosInventario', 'ConteoInventarioDetalles')
 GROUP BY TABLE_NAME, INDEX_NAME, NON_UNIQUE
 ORDER BY TABLE_NAME, INDEX_NAME;

SELECT 'VIOLACION_SCOPE_UBICACION_CABECERA' AS Hallazgo, c.Id
  FROM ConteosInventario c
  JOIN UbicacionesAlmacen u ON u.Id = c.UbicacionAlmacenId
 WHERE c.UbicacionAlmacenId IS NOT NULL
   AND u.AlmacenId <> c.AlmacenId;

SELECT 'VIOLACION_SCOPE_UBICACION_DETALLE' AS Hallazgo, d.Id
  FROM ConteoInventarioDetalles d
  JOIN UbicacionesAlmacen u ON u.Id = d.UbicacionAlmacenId
 WHERE d.UbicacionAlmacenId IS NOT NULL
   AND u.AlmacenId <> d.AlmacenId;

SELECT 'VIOLACION_ALMACEN_DETALLE' AS Hallazgo, d.Id
  FROM ConteoInventarioDetalles d
  JOIN ConteosInventario c ON c.Id = d.ConteoInventarioId
 WHERE d.AlmacenId <> c.AlmacenId;

SELECT 'VIOLACION_SNAPSHOT_NEGATIVO' AS Hallazgo, d.Id
  FROM ConteoInventarioDetalles d
 WHERE d.StockEsperadoSnapshot < 0
    OR d.CantidadContada < 0;

SELECT 'DUPLICADO_CLAVE_FISICA' AS Hallazgo,
       ConteoInventarioId,
       ProductoVarianteId,
       AlmacenId,
       COALESCE(UbicacionAlmacenId, 0) AS UbicacionNormalizada,
       COUNT(*) AS Repeticiones
  FROM ConteoInventarioDetalles
 GROUP BY ConteoInventarioId, ProductoVarianteId, AlmacenId, COALESCE(UbicacionAlmacenId, 0)
HAVING COUNT(*) > 1;

SELECT MIGRATION_ID
  FROM __EFMigrationsHistory
 WHERE MIGRATION_ID LIKE '%N1_7%'
 ORDER BY MIGRATION_ID;
