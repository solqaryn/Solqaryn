-- ERP-N1.4.C — Postcheck inmediato posterior al backfill.
-- Solo lectura. Debe ejecutarse antes de habilitar mutaciones multi-almacén.
-- ProductoVariantes.Cantidad sigue siendo fuente legacy de reconciliación.

SET @schema_actual := DATABASE();

-- 1. La columna legacy debe seguir existiendo durante N1.4.C.
SELECT
    CASE WHEN COUNT(*) = 1 THEN 'OK_LEGACY_PRESERVADO'
         ELSE 'FAIL_LEGACY_AUSENTE' END AS GateLegacy
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = @schema_actual
  AND TABLE_NAME = 'ProductoVariantes'
  AND COLUMN_NAME = 'Cantidad';

-- 2. Ninguna existencia puede violar las invariantes del dominio.
SELECT
    COUNT(*) AS InvariantesInvalidas
FROM ExistenciasVariante
WHERE StockFisico < 0
   OR StockReservado < 0
   OR StockReservado > StockFisico
   OR StockTransito < 0
   OR StockMinimo < 0
   OR (StockMaximo IS NOT NULL AND StockMaximo < StockMinimo)
   OR StockDisponible <> StockFisico - StockReservado;

-- 3. No deben existir claves lógicas duplicadas, incluido NULL de ubicación.
SELECT
    ProductoVarianteId,
    AlmacenId,
    COALESCE(UbicacionAlmacenId, 0) AS UbicacionNormalizada,
    COUNT(*) AS Repeticiones
FROM ExistenciasVariante
GROUP BY ProductoVarianteId, AlmacenId, COALESCE(UbicacionAlmacenId, 0)
HAVING COUNT(*) > 1;

-- 4. Integridad referencial semántica explícita.
SELECT COUNT(*) AS VariantesHuerfanas
FROM ExistenciasVariante ev
LEFT JOIN ProductoVariantes pv ON pv.Id = ev.ProductoVarianteId
WHERE pv.Id IS NULL;

SELECT COUNT(*) AS AlmacenesHuerfanos
FROM ExistenciasVariante ev
LEFT JOIN Almacenes a ON a.Id = ev.AlmacenId
WHERE a.Id IS NULL;

SELECT COUNT(*) AS UbicacionesFueraDelAlmacen
FROM ExistenciasVariante ev
JOIN UbicacionesAlmacen ua ON ua.Id = ev.UbicacionAlmacenId
WHERE ev.UbicacionAlmacenId IS NOT NULL
  AND ua.AlmacenId <> ev.AlmacenId;

-- 5. Cada variante legacy no eliminada debe estar representada después del
-- backfill. En este punto inicial la suma física debe reconciliar con Cantidad.
SELECT
    pv.Id AS ProductoVarianteId,
    pv.Cantidad AS CantidadLegacy,
    COUNT(ev.Id) AS FilasExistencia,
    COALESCE(SUM(ev.StockFisico), 0) AS StockFisicoTotal
FROM ProductoVariantes pv
LEFT JOIN ExistenciasVariante ev ON ev.ProductoVarianteId = pv.Id
WHERE pv.Eliminado = 0
GROUP BY pv.Id, pv.Cantidad
HAVING COUNT(ev.Id) = 0
    OR COALESCE(SUM(ev.StockFisico), 0) <> pv.Cantidad
ORDER BY pv.Id;

-- 6. Totales de certificación del corte de migración.
SELECT
    (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0) AS VariantesLegacy,
    (SELECT COUNT(DISTINCT ProductoVarianteId) FROM ExistenciasVariante) AS VariantesConExistencia,
    (SELECT COALESCE(SUM(Cantidad), 0) FROM ProductoVariantes WHERE Eliminado = 0) AS StockLegacyTotal,
    (SELECT COALESCE(SUM(StockFisico), 0) FROM ExistenciasVariante) AS StockFisicoTotal,
    CASE
        WHEN (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0)
             = (SELECT COUNT(DISTINCT ProductoVarianteId) FROM ExistenciasVariante)
         AND (SELECT COALESCE(SUM(Cantidad), 0) FROM ProductoVariantes WHERE Eliminado = 0)
             = (SELECT COALESCE(SUM(StockFisico), 0) FROM ExistenciasVariante)
         AND NOT EXISTS (
             SELECT 1
             FROM ExistenciasVariante
             WHERE StockFisico < 0
                OR StockReservado < 0
                OR StockReservado > StockFisico
                OR StockTransito < 0
                OR StockMinimo < 0
                OR (StockMaximo IS NOT NULL AND StockMaximo < StockMinimo)
                OR StockDisponible <> StockFisico - StockReservado
         )
        THEN 'OK_RECONCILIADO'
        ELSE 'FAIL_REVISAR_DETALLE'
    END AS Estado;
