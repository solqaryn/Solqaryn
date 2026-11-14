-- ERP-N0.1 — preflight y validación de autoridad ProductoVariante
-- Regla: la fila RESUMEN debe devolver Bloqueos = 0 antes del backfill y ErroresAutoridad = 0 después.

-- A. Bloqueos previos al backfill.
SELECT 'productos_stock_negativo' AS Regla, COUNT(*) AS Total
FROM Productos p WHERE p.Eliminado = 0 AND p.Cantidad < 0
UNION ALL
SELECT 'variantes_stock_negativo', COUNT(*)
FROM ProductoVariantes pv WHERE pv.Eliminado = 0 AND pv.Cantidad < 0
UNION ALL
SELECT 'sku_tecnico_colision', COUNT(*)
FROM Productos p
WHERE p.Eliminado = 0
  AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0)
  AND EXISTS (
      SELECT 1 FROM ProductoVariantes x
      WHERE UPPER(TRIM(x.Sku)) = UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))
  )
UNION ALL
SELECT 'tecnica_y_comercial_simultaneas', COUNT(*)
FROM (
    SELECT pv.ProductoId
    FROM ProductoVariantes pv
    WHERE pv.Eliminado = 0
    GROUP BY pv.ProductoId
    HAVING SUM(CASE WHEN pv.EsTecnica = 1 THEN 1 ELSE 0 END) > 0
       AND SUM(CASE WHEN pv.EsTecnica = 0 THEN 1 ELSE 0 END) > 0
) q
UNION ALL
SELECT 'mas_de_una_tecnica', COUNT(*)
FROM (
    SELECT pv.ProductoId
    FROM ProductoVariantes pv
    WHERE pv.Eliminado = 0 AND pv.EsTecnica = 1
    GROUP BY pv.ProductoId
    HAVING COUNT(*) > 1
) q;

SELECT
    (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND p.Cantidad < 0)
  + (SELECT COUNT(*) FROM ProductoVariantes pv WHERE pv.Eliminado = 0 AND pv.Cantidad < 0)
  + (SELECT COUNT(*)
       FROM Productos p
      WHERE p.Eliminado = 0
        AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0)
        AND EXISTS (SELECT 1 FROM ProductoVariantes x WHERE UPPER(TRIM(x.Sku)) = UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))))
  + (SELECT COUNT(*) FROM (
        SELECT pv.ProductoId FROM ProductoVariantes pv WHERE pv.Eliminado = 0
        GROUP BY pv.ProductoId
        HAVING SUM(CASE WHEN pv.EsTecnica = 1 THEN 1 ELSE 0 END) > 0
           AND SUM(CASE WHEN pv.EsTecnica = 0 THEN 1 ELSE 0 END) > 0
    ) a)
  + (SELECT COUNT(*) FROM (
        SELECT pv.ProductoId FROM ProductoVariantes pv WHERE pv.Eliminado = 0 AND pv.EsTecnica = 1
        GROUP BY pv.ProductoId HAVING COUNT(*) > 1
    ) b) AS Bloqueos;

-- B. Validación posterior: todas estas consultas deben devolver 0.
SELECT 'producto_sin_variante' AS Regla, COUNT(*) AS Total
FROM Productos p
WHERE p.Eliminado = 0
  AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0)
UNION ALL
SELECT 'variante_sin_costo_o_precio', COUNT(*)
FROM ProductoVariantes pv
WHERE pv.Eliminado = 0 AND (pv.Costo IS NULL OR pv.Precio IS NULL)
UNION ALL
SELECT 'stock_producto_desalineado', COUNT(*)
FROM Productos p
JOIN (
    SELECT ProductoId, SUM(Cantidad) Cantidad
    FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId
) pv ON pv.ProductoId = p.Id
WHERE p.Eliminado = 0 AND p.Cantidad <> pv.Cantidad
UNION ALL
SELECT 'costo_producto_desalineado', COUNT(*)
FROM Productos p
JOIN (
    SELECT ProductoId,
           CASE WHEN SUM(Cantidad) > 0
                THEN ROUND(SUM(COALESCE(Costo,0) * Cantidad) / SUM(Cantidad), 2)
                ELSE ROUND(AVG(COALESCE(Costo,0)), 2)
           END Costo
    FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId
) pv ON pv.ProductoId = p.Id
WHERE p.Eliminado = 0 AND ABS(p.Costo - pv.Costo) > 0.01
UNION ALL
SELECT 'precio_producto_desalineado', COUNT(*)
FROM Productos p
JOIN (
    SELECT ProductoId,
           COALESCE(MIN(CASE WHEN Activo = 1 THEN Precio END), MIN(Precio), 0) Precio
    FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId
) pv ON pv.ProductoId = p.Id
WHERE p.Eliminado = 0 AND ABS(p.Precio - pv.Precio) > 0.01
UNION ALL
SELECT 'tecnica_con_dimensiones', COUNT(*)
FROM ProductoVariantes pv
WHERE pv.Eliminado = 0 AND pv.EsTecnica = 1
  AND (pv.MarcaId IS NOT NULL OR pv.ModeloId IS NOT NULL OR pv.ColorId IS NOT NULL OR pv.TallaId IS NOT NULL);

SELECT
    (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0
      AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0))
  + (SELECT COUNT(*) FROM ProductoVariantes pv WHERE pv.Eliminado = 0 AND (pv.Costo IS NULL OR pv.Precio IS NULL))
  + (SELECT COUNT(*) FROM Productos p JOIN (
        SELECT ProductoId, SUM(Cantidad) Cantidad FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId
    ) pv ON pv.ProductoId = p.Id WHERE p.Eliminado = 0 AND p.Cantidad <> pv.Cantidad)
  + (SELECT COUNT(*) FROM ProductoVariantes pv WHERE pv.Eliminado = 0 AND pv.EsTecnica = 1
      AND (pv.MarcaId IS NOT NULL OR pv.ModeloId IS NOT NULL OR pv.ColorId IS NOT NULL OR pv.TallaId IS NOT NULL))
  + (SELECT COUNT(*) FROM (
        SELECT pv.ProductoId FROM ProductoVariantes pv WHERE pv.Eliminado = 0
        GROUP BY pv.ProductoId
        HAVING SUM(CASE WHEN pv.EsTecnica = 1 THEN 1 ELSE 0 END) > 0
           AND SUM(CASE WHEN pv.EsTecnica = 0 THEN 1 ELSE 0 END) > 0
    ) c) AS ErroresAutoridad;
