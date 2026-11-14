-- ERP-N1.4.C — Preflight de ExistenciaVariante
-- Solo lectura. No modifica datos ni esquema.
-- Objetivo: impedir un backfill ambiguo y cuantificar exactamente el legado
-- ProductoVariante.Cantidad antes de materializar la autoridad multi-almacén.

SET @schema_actual := DATABASE();

SELECT
    @schema_actual AS `Schema`,
    NOW(6) AS EjecutadoEnUtc,
    'N1.4.C' AS Punto;

-- 1. Contrato físico mínimo esperado.
SELECT
    esperado.TABLE_NAME_ESPERADA AS Tabla,
    CASE WHEN t.TABLE_NAME IS NULL THEN 'FALTA' ELSE 'OK' END AS Estado
FROM (
    SELECT 'ProductoVariantes' AS TABLE_NAME_ESPERADA
    UNION ALL SELECT 'Almacenes'
    UNION ALL SELECT 'ExistenciasVariante'
) esperado
LEFT JOIN information_schema.TABLES t
    ON t.TABLE_SCHEMA = @schema_actual
   AND t.TABLE_NAME = esperado.TABLE_NAME_ESPERADA
ORDER BY esperado.TABLE_NAME_ESPERADA;

-- 2. La fuente legacy debe conservarse durante toda N1.4.C.
SELECT
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.COLUMN_TYPE,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT,
    CASE WHEN c.COLUMN_NAME IS NULL THEN 'FALTA_CANTIDAD_LEGACY' ELSE 'OK' END AS Estado
FROM (SELECT 1 AS n) seed
LEFT JOIN information_schema.COLUMNS c
    ON c.TABLE_SCHEMA = @schema_actual
   AND c.TABLE_NAME = 'ProductoVariantes'
   AND c.COLUMN_NAME = 'Cantidad';

-- 3. Inventario de almacenes elegibles. El backfill debe fallar cerrado si no
-- existe una asignación determinística; nunca escoger un almacén arbitrariamente.
SELECT
    COUNT(*) AS TotalAlmacenes,
    SUM(CASE WHEN Activo = 1 AND Eliminado = 0 THEN 1 ELSE 0 END) AS AlmacenesActivos
FROM Almacenes;

SELECT
    Id,
    Codigo,
    Nombre,
    SucursalId,
    Tipo,
    Activo,
    Eliminado
FROM Almacenes
WHERE Eliminado = 0
ORDER BY SucursalId, Id;

-- 4. Magnitud y calidad del stock legacy que se migrará. No se admite cantidad
-- negativa para la nueva autoridad de stock.
SELECT
    COUNT(*) AS VariantesTotales,
    SUM(CASE WHEN Cantidad < 0 THEN 1 ELSE 0 END) AS VariantesConCantidadNegativa,
    SUM(CASE WHEN Cantidad = 0 THEN 1 ELSE 0 END) AS VariantesSinStock,
    SUM(CASE WHEN Cantidad > 0 THEN 1 ELSE 0 END) AS VariantesConStock,
    COALESCE(SUM(Cantidad), 0) AS StockLegacyTotal
FROM ProductoVariantes
WHERE Eliminado = 0;

SELECT
    Id AS ProductoVarianteId,
    ProductoId,
    Cantidad
FROM ProductoVariantes
WHERE Eliminado = 0
  AND Cantidad < 0
ORDER BY Id;

-- 5. Estado de la autoridad nueva antes del backfill. ExistenciaVariante no
-- implementa soft-delete: toda fila persistida es stock vivo.
SELECT
    COUNT(*) AS ExistenciasActuales,
    COALESCE(SUM(StockFisico), 0) AS StockFisicoActual,
    COALESCE(SUM(StockReservado), 0) AS StockReservadoActual,
    COALESCE(SUM(StockTransito), 0) AS StockTransitoActual
FROM ExistenciasVariante;

-- 6. Duplicados lógicos que harían inseguro un upsert posterior.
SELECT
    ProductoVarianteId,
    AlmacenId,
    COALESCE(UbicacionAlmacenId, 0) AS UbicacionNormalizada,
    COUNT(*) AS Repeticiones
FROM ExistenciasVariante
GROUP BY ProductoVarianteId, AlmacenId, COALESCE(UbicacionAlmacenId, 0)
HAVING COUNT(*) > 1
ORDER BY Repeticiones DESC, ProductoVarianteId;

-- 7. Evidencia de que Cantidad sigue presente: N1.4.C NO la elimina.
SELECT
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = @schema_actual
              AND TABLE_NAME = 'ProductoVariantes'
              AND COLUMN_NAME = 'Cantidad'
        ) THEN 'OK_LEGACY_PRESERVADO'
        ELSE 'FAIL_LEGACY_CANTIDAD_AUSENTE'
    END AS GateLegacyPreservado;