-- N1.4.C — Verificación read-only de invariantes de ExistenciaVariante
-- No modifica datos ni esquema. Diseñado para ejecutarse después de migración/backfill.

SELECT
    COUNT(*) AS variantes_totales,
    SUM(CASE WHEN pv.Cantidad < 0 THEN 1 ELSE 0 END) AS variantes_legacy_negativas,
    SUM(CASE WHEN ev.Id IS NULL THEN 1 ELSE 0 END) AS variantes_sin_existencia,
    SUM(CASE WHEN ev.StockFisico < 0 OR ev.StockReservado < 0 THEN 1 ELSE 0 END) AS existencias_negativas,
    SUM(CASE WHEN ev.StockReservado > ev.StockFisico THEN 1 ELSE 0 END) AS reservas_superiores_stock,
    SUM(CASE WHEN ev.StockFisico <> pv.Cantidad THEN 1 ELSE 0 END) AS diferencias_stock_legacy
FROM ProductoVariantes pv
LEFT JOIN ExistenciasVariante ev
    ON ev.ProductoVarianteId = pv.Id;

SELECT
    ev.ProductoVarianteId,
    ev.AlmacenId,
    COALESCE(ev.UbicacionAlmacenId, 0) AS UbicacionNormalizada,
    COUNT(*) AS duplicados
FROM ExistenciasVariante ev
GROUP BY
    ev.ProductoVarianteId,
    ev.AlmacenId,
    COALESCE(ev.UbicacionAlmacenId, 0)
HAVING COUNT(*) > 1;

SELECT
    ev.Id,
    ev.ProductoVarianteId,
    ev.AlmacenId,
    ev.UbicacionAlmacenId
FROM ExistenciasVariante ev
LEFT JOIN Almacenes a ON a.Id = ev.AlmacenId
LEFT JOIN UbicacionesAlmacen ua ON ua.Id = ev.UbicacionAlmacenId
WHERE a.Id IS NULL
   OR (ev.UbicacionAlmacenId IS NOT NULL AND (ua.Id IS NULL OR ua.AlmacenId <> ev.AlmacenId));
