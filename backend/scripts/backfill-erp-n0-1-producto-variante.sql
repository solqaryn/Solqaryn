-- ERP-N0.1 — backfill Producto -> ProductoVariante
-- MySQL 8.x. Idempotente. Ejecutar SOLO después de que preflight-erp-n0-1-producto-variante.sql devuelva Bloqueos = 0.
-- Objetivo: garantizar una variante vigente por cada producto, completar costo/precio de variantes
-- y recalcular las columnas legacy de Producto como proyección, no como autoridad.

START TRANSACTION;

-- 1) Completar datos económicos nulos de variantes existentes desde la última proyección legacy.
UPDATE ProductoVariantes pv
JOIN Productos p ON p.Id = pv.ProductoId
SET pv.Costo = COALESCE(pv.Costo, p.Costo),
    pv.Precio = COALESCE(pv.Precio, p.Precio),
    pv.FechaActualizacion = COALESCE(pv.FechaActualizacion, UTC_TIMESTAMP())
WHERE pv.Eliminado = 0
  AND (pv.Costo IS NULL OR pv.Precio IS NULL);

-- 2) Materializar variante técnica para productos que todavía no tienen ninguna variante vigente.
-- Las dimensiones legacy NO se copian por ID: Productos.*Id referencia CatalogosProducto mientras
-- ProductoVariantes.*Id referencia las tablas normalizadas. Esa reconciliación pertenece a N0.2.
INSERT INTO ProductoVariantes
(
    ProductoId, MarcaId, ModeloId, ColorId, TallaId,
    Sku, CodigoBarras, Cantidad, UmbralStockBajo, Costo, Precio,
    EsTecnica, Activo, Eliminado,
    CreadoPorUsuarioId, CreadoPorNombreUsuario, FechaCreacion,
    ActualizadoPorUsuarioId, ActualizadoPorNombreUsuario, FechaActualizacion
)
SELECT
    p.Id, NULL, NULL, NULL, NULL,
    CONCAT('TEC-', LPAD(p.Id, 10, '0')), NULL,
    p.Cantidad, p.UmbralStockBajo, p.Costo, p.Precio,
    1, p.Activo, 0,
    p.CreadoPorUsuarioId, COALESCE(p.CreadoPorNombreUsuario, 'ERP-N0.1'), COALESCE(p.FechaCreacion, UTC_TIMESTAMP()),
    p.ActualizadoPorUsuarioId, 'ERP-N0.1 backfill', UTC_TIMESTAMP()
FROM Productos p
WHERE p.Eliminado = 0
  AND NOT EXISTS (
      SELECT 1
      FROM ProductoVariantes pv
      WHERE pv.ProductoId = p.Id
        AND pv.Eliminado = 0
  )
  AND NOT EXISTS (
      SELECT 1
      FROM ProductoVariantes colision
      WHERE UPPER(TRIM(colision.Sku)) = UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))
  );

-- 3) Recalcular Producto como proyección materializada de ProductoVariante.
UPDATE Productos p
JOIN (
    SELECT
        pv.ProductoId,
        SUM(pv.Cantidad) AS Cantidad,
        CASE
            WHEN SUM(pv.Cantidad) > 0 THEN ROUND(SUM(COALESCE(pv.Costo, 0) * pv.Cantidad) / SUM(pv.Cantidad), 2)
            ELSE ROUND(AVG(COALESCE(pv.Costo, 0)), 2)
        END AS Costo,
        COALESCE(
            MIN(CASE WHEN pv.Activo = 1 THEN pv.Precio END),
            MIN(pv.Precio),
            0
        ) AS Precio,
        SUM(pv.UmbralStockBajo) AS UmbralStockBajo
    FROM ProductoVariantes pv
    WHERE pv.Eliminado = 0
    GROUP BY pv.ProductoId
) agg ON agg.ProductoId = p.Id
SET p.Cantidad = agg.Cantidad,
    p.Costo = agg.Costo,
    p.Precio = agg.Precio,
    p.UmbralStockBajo = agg.UmbralStockBajo,
    p.FechaActualizacion = UTC_TIMESTAMP()
WHERE p.Eliminado = 0;

COMMIT;

-- Debe devolver 0. Si devuelve >0, NO continuar con ningún DROP de columnas legacy.
SELECT
    (SELECT COUNT(*)
       FROM Productos p
      WHERE p.Eliminado = 0
        AND NOT EXISTS (
            SELECT 1 FROM ProductoVariantes pv
             WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0
        ))
  + (SELECT COUNT(*) FROM ProductoVariantes pv WHERE pv.Eliminado = 0 AND (pv.Costo IS NULL OR pv.Precio IS NULL))
  + COALESCE((SELECT COUNT(*)
       FROM (
           SELECT pv.ProductoId
             FROM ProductoVariantes pv
            WHERE pv.Eliminado = 0
            GROUP BY pv.ProductoId
           HAVING SUM(CASE WHEN pv.EsTecnica = 1 THEN 1 ELSE 0 END) > 0
              AND SUM(CASE WHEN pv.EsTecnica = 0 THEN 1 ELSE 0 END) > 0
       ) conflicto), 0) AS ErroresPostBackfill;
