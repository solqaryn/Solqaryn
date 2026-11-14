-- ERP-N2.3.C — RecepcionCompra / preflight de persistencia
-- Solo lectura. No crea, modifica ni elimina datos.

SELECT 'N2.3.C_PRECHECK_DEPENDENCIAS' AS CheckName,
       SUM(TABLE_NAME = 'OrdenesCompra') AS OrdenesCompra,
       SUM(TABLE_NAME = 'OrdenCompraDetalles') AS OrdenCompraDetalles,
       SUM(TABLE_NAME = 'Productos') AS Productos,
       SUM(TABLE_NAME = 'ProductoVariantes') AS ProductoVariantes,
       SUM(TABLE_NAME = 'Almacenes') AS Almacenes,
       SUM(TABLE_NAME = 'UbicacionesAlmacen') AS UbicacionesAlmacen
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('OrdenesCompra', 'OrdenCompraDetalles', 'Productos', 'ProductoVariantes', 'Almacenes', 'UbicacionesAlmacen');

SELECT 'N2.3.C_PRECHECK_COLISION_TABLAS' AS CheckName,
       SUM(TABLE_NAME = 'RecepcionesCompra') AS RecepcionesCompraExistente,
       SUM(TABLE_NAME = 'RecepcionCompraDetalles') AS RecepcionCompraDetallesExistente
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('RecepcionesCompra', 'RecepcionCompraDetalles');

SELECT 'N2.3.C_PRECHECK_MIGRACION' AS CheckName,
       MigrationId
FROM __EFMigrationsHistory
WHERE MigrationId LIKE '%N2_3%Recepcion%'
ORDER BY MigrationId;

SELECT 'N2.3.C_PRECHECK_UBICACION_AK' AS CheckName,
       CONSTRAINT_NAME,
       GROUP_CONCAT(COLUMN_NAME ORDER BY ORDINAL_POSITION SEPARATOR ',') AS Columnas
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'UbicacionesAlmacen'
  AND CONSTRAINT_NAME = 'AK_UbicacionesAlmacen_AlmacenId_Id'
GROUP BY CONSTRAINT_NAME;

-- Interpretación esperada antes de la migración canónica:
-- 1) Las seis dependencias físicas deben existir exactamente una vez.
-- 2) RecepcionesCompra / RecepcionCompraDetalles no deben existir, salvo re-ejecución ya registrada.
-- 3) No debe existir una migración N2.3 aplicada sin sus tablas.
-- 4) Debe existir la clave alterna AlmacenId+Id de UbicacionesAlmacen para la FK same-almacén.