-- ERP-N2.2.C — OrdenCompra / postcheck de persistencia
-- Solo lectura. Verifica estructura e integridad después de aplicar la migración.

SELECT 'N2.2.C_POSTCHECK_TABLAS' AS CheckName,
       SUM(TABLE_NAME = 'OrdenesCompra') AS OrdenesCompra,
       SUM(TABLE_NAME = 'OrdenCompraDetalles') AS OrdenCompraDetalles
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('OrdenesCompra', 'OrdenCompraDetalles');

SELECT 'N2.2.C_POSTCHECK_INDICES' AS CheckName,
       TABLE_NAME,
       INDEX_NAME,
       NON_UNIQUE,
       GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',') AS Columnas
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('OrdenesCompra', 'OrdenCompraDetalles')
GROUP BY TABLE_NAME, INDEX_NAME, NON_UNIQUE
ORDER BY TABLE_NAME, INDEX_NAME;

SELECT 'N2.2.C_POSTCHECK_FKS' AS CheckName,
       TABLE_NAME,
       CONSTRAINT_NAME,
       REFERENCED_TABLE_NAME,
       GROUP_CONCAT(COLUMN_NAME ORDER BY ORDINAL_POSITION SEPARATOR ',') AS Columnas
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('OrdenesCompra', 'OrdenCompraDetalles')
  AND REFERENCED_TABLE_NAME IS NOT NULL
GROUP BY TABLE_NAME, CONSTRAINT_NAME, REFERENCED_TABLE_NAME
ORDER BY TABLE_NAME, CONSTRAINT_NAME;

SELECT 'N2.2.C_POSTCHECK_DUP_NUMERO' AS CheckName,
       NumeroOrden,
       COUNT(*) AS Repeticiones
FROM OrdenesCompra
GROUP BY NumeroOrden
HAVING COUNT(*) > 1;

SELECT 'N2.2.C_POSTCHECK_HUERFANOS_DETALLE' AS CheckName,
       COUNT(*) AS Huerfanos
FROM OrdenCompraDetalles d
LEFT JOIN OrdenesCompra o ON o.Id = d.OrdenCompraId
WHERE o.Id IS NULL;

SELECT 'N2.2.C_POSTCHECK_HUERFANOS_REFERENCIAS' AS CheckName,
       SUM(p.Id IS NULL) AS ProveedorInvalido,
       SUM(o.SolicitudCompraId IS NOT NULL AND s.Id IS NULL) AS SolicitudCompraInvalida
FROM OrdenesCompra o
LEFT JOIN Proveedores p ON p.Id = o.ProveedorId
LEFT JOIN SolicitudesCompra s ON s.Id = o.SolicitudCompraId;

-- Resultado esperado: tablas presentes, UX_OrdenesCompra_NumeroOrden único,
-- FKs restrictivas hacia Proveedores/SolicitudesCompra/Productos/ProductoVariantes,
-- FK cascade únicamente cabecera -> detalle, cero números duplicados y cero huérfanos.
