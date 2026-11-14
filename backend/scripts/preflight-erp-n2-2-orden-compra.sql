-- ERP-N2.2.C — OrdenCompra / preflight de persistencia
-- Solo lectura. No crea, modifica ni elimina datos.

SELECT 'N2.2.C_PRECHECK_DEPENDENCIAS' AS CheckName,
       SUM(TABLE_NAME = 'Proveedores') AS Proveedores,
       SUM(TABLE_NAME = 'SolicitudesCompra') AS SolicitudesCompra,
       SUM(TABLE_NAME = 'Productos') AS Productos,
       SUM(TABLE_NAME = 'ProductoVariantes') AS ProductoVariantes
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('Proveedores', 'SolicitudesCompra', 'Productos', 'ProductoVariantes');

SELECT 'N2.2.C_PRECHECK_COLISION_TABLAS' AS CheckName,
       SUM(TABLE_NAME = 'OrdenesCompra') AS OrdenesCompraExistente,
       SUM(TABLE_NAME = 'OrdenCompraDetalles') AS OrdenCompraDetallesExistente
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('OrdenesCompra', 'OrdenCompraDetalles');

SELECT 'N2.2.C_PRECHECK_MIGRACION' AS CheckName,
       MigrationId
FROM __EFMigrationsHistory
WHERE MigrationId LIKE '%N2_2%OrdenCompra%'
ORDER BY MigrationId;

-- Interpretación esperada antes de aplicar la migración canónica:
-- 1) Las cuatro dependencias físicas deben existir exactamente una vez.
-- 2) OrdenesCompra / OrdenCompraDetalles no deben existir aún, salvo re-ejecución controlada ya registrada.
-- 3) No debe existir una migración N2.2 aplicada sin las tablas correspondientes.
