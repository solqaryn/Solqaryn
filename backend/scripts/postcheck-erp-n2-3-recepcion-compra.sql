-- ERP-N2.3.C — RecepcionCompra / postcheck de persistencia
-- Solo lectura. Verifica estructura e integridad después de aplicar la migración.

SELECT 'N2.3.C_POSTCHECK_TABLAS' AS CheckName,
       SUM(TABLE_NAME = 'RecepcionesCompra') AS RecepcionesCompra,
       SUM(TABLE_NAME = 'RecepcionCompraDetalles') AS RecepcionCompraDetalles
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('RecepcionesCompra', 'RecepcionCompraDetalles');

SELECT 'N2.3.C_POSTCHECK_INDICES' AS CheckName,
       TABLE_NAME,
       INDEX_NAME,
       NON_UNIQUE,
       GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',') AS Columnas
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('RecepcionesCompra', 'RecepcionCompraDetalles')
GROUP BY TABLE_NAME, INDEX_NAME, NON_UNIQUE
ORDER BY TABLE_NAME, INDEX_NAME;

SELECT 'N2.3.C_POSTCHECK_FKS' AS CheckName,
       TABLE_NAME,
       CONSTRAINT_NAME,
       REFERENCED_TABLE_NAME,
       GROUP_CONCAT(COLUMN_NAME ORDER BY ORDINAL_POSITION SEPARATOR ',') AS Columnas
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('RecepcionesCompra', 'RecepcionCompraDetalles')
  AND REFERENCED_TABLE_NAME IS NOT NULL
GROUP BY TABLE_NAME, CONSTRAINT_NAME, REFERENCED_TABLE_NAME
ORDER BY TABLE_NAME, CONSTRAINT_NAME;

SELECT 'N2.3.C_POSTCHECK_DUP_NUMERO' AS CheckName,
       NumeroRecepcion,
       COUNT(*) AS Repeticiones
FROM RecepcionesCompra
GROUP BY NumeroRecepcion
HAVING COUNT(*) > 1;

SELECT 'N2.3.C_POSTCHECK_DUP_IDEMPOTENCIA' AS CheckName,
       IdempotencyKey,
       COUNT(*) AS Repeticiones
FROM RecepcionesCompra
WHERE IdempotencyKey IS NOT NULL
GROUP BY IdempotencyKey
HAVING COUNT(*) > 1;

SELECT 'N2.3.C_POSTCHECK_IDEMPOTENCIA_INVALIDA' AS CheckName,
       COUNT(*) AS FilasInvalidas
FROM RecepcionesCompra
WHERE (IdempotencyKey IS NULL) <> (IdempotencyFingerprint IS NULL)
   OR (IdempotencyKey IS NOT NULL AND CHAR_LENGTH(TRIM(IdempotencyKey)) = 0)
   OR (IdempotencyFingerprint IS NOT NULL AND CHAR_LENGTH(IdempotencyFingerprint) <> 64);

SELECT 'N2.3.C_POSTCHECK_DUP_CLAVE_FISICA' AS CheckName,
       RecepcionCompraId,
       OrdenCompraDetalleId,
       AlmacenId,
       IFNULL(UbicacionAlmacenId, 0) AS UbicacionAlmacenIdUnica,
       COUNT(*) AS Repeticiones
FROM RecepcionCompraDetalles
GROUP BY RecepcionCompraId, OrdenCompraDetalleId, AlmacenId, IFNULL(UbicacionAlmacenId, 0)
HAVING COUNT(*) > 1;

SELECT 'N2.3.C_POSTCHECK_BALANCE_INVALIDO' AS CheckName,
       COUNT(*) AS FilasInvalidas
FROM RecepcionCompraDetalles
WHERE CantidadRecibida < 0
   OR CantidadDanada < 0
   OR CantidadFaltante < 0
   OR CantidadSobrante < 0
   OR CostoUnitarioSnapshot < 0
   OR CantidadDanada + CantidadSobrante > CantidadRecibida
   OR (CantidadRecibida = 0 AND CantidadFaltante = 0);

SELECT 'N2.3.C_POSTCHECK_HUERFANOS_CABECERA' AS CheckName,
       COUNT(*) AS Huerfanos
FROM RecepcionesCompra r
LEFT JOIN OrdenesCompra o ON o.Id = r.OrdenCompraId
WHERE o.Id IS NULL;

SELECT 'N2.3.C_POSTCHECK_HUERFANOS_DETALLE' AS CheckName,
       SUM(r.Id IS NULL) AS RecepcionInvalida,
       SUM(od.Id IS NULL) AS OrdenDetalleInvalida,
       SUM(p.Id IS NULL) AS ProductoInvalido,
       SUM(d.ProductoVarianteId IS NOT NULL AND pv.Id IS NULL) AS VarianteInvalida,
       SUM(a.Id IS NULL) AS AlmacenInvalido,
       SUM(d.UbicacionAlmacenId IS NOT NULL AND u.Id IS NULL) AS UbicacionInvalida,
       SUM(d.UbicacionAlmacenId IS NOT NULL AND u.AlmacenId <> d.AlmacenId) AS UbicacionOtroAlmacen
FROM RecepcionCompraDetalles d
LEFT JOIN RecepcionesCompra r ON r.Id = d.RecepcionCompraId
LEFT JOIN OrdenCompraDetalles od ON od.Id = d.OrdenCompraDetalleId
LEFT JOIN Productos p ON p.Id = d.ProductoId
LEFT JOIN ProductoVariantes pv ON pv.Id = d.ProductoVarianteId
LEFT JOIN Almacenes a ON a.Id = d.AlmacenId
LEFT JOIN UbicacionesAlmacen u ON u.Id = d.UbicacionAlmacenId;

-- Resultado esperado: tablas presentes; números/idempotencia únicos; idempotencia clave+fingerprint
-- atómica; una sola clave física por recepción+línea+almacén+ubicación; cantidades y costo sin
-- violaciones; FKs restrictivas a OrdenCompra/detalles/producto/variante/almacén/ubicación y cascade
-- únicamente cabecera->detalle; cero huérfanos y ninguna ubicación asociada a otro almacén.
