-- Bloque 2C.1: preflight previo a AddVarianteTecnicaProductoSimple.
-- Devuelve exactamente un entero. Cualquier valor mayor que cero bloquea la migración.
SELECT
    (SELECT COUNT(*)
       FROM Productos p
      WHERE p.Eliminado = 0
        AND p.Cantidad < 0)
  + (SELECT COUNT(*)
       FROM ProductoVariantes pv
      WHERE pv.Eliminado = 0
        AND pv.Cantidad < 0)
  + (SELECT COUNT(*)
       FROM (
            SELECT UPPER(TRIM(pv.Sku)) AS Valor
              FROM ProductoVariantes pv
             WHERE pv.Eliminado = 0
               AND pv.Sku IS NOT NULL
               AND TRIM(pv.Sku) <> ''
             GROUP BY UPPER(TRIM(pv.Sku))
            HAVING COUNT(*) > 1
       ) duplicados_sku)
  + (SELECT COUNT(*)
       FROM (
            SELECT TRIM(pv.CodigoBarras) AS Valor
              FROM ProductoVariantes pv
             WHERE pv.Eliminado = 0
               AND pv.CodigoBarras IS NOT NULL
               AND TRIM(pv.CodigoBarras) <> ''
             GROUP BY TRIM(pv.CodigoBarras)
            HAVING COUNT(*) > 1
       ) duplicados_codigo)
  + (SELECT COUNT(*)
       FROM ProductoVariantes pv
      WHERE pv.Eliminado = 0
        AND pv.ColorId IS NULL)
  + (SELECT COUNT(*)
       FROM Productos p
       JOIN (
            SELECT pv.ProductoId, SUM(pv.Cantidad) AS CantidadVariantes
              FROM ProductoVariantes pv
             WHERE pv.Eliminado = 0
             GROUP BY pv.ProductoId
       ) inventario ON inventario.ProductoId = p.Id
      WHERE p.Eliminado = 0
        AND p.Cantidad <> inventario.CantidadVariantes)
  + (SELECT COUNT(*)
       FROM Productos p
      WHERE p.Eliminado = 0
        AND NOT EXISTS (
            SELECT 1
              FROM ProductoVariantes actual
             WHERE actual.ProductoId = p.Id
               AND actual.Eliminado = 0
        )
        AND EXISTS (
            SELECT 1
              FROM ProductoVariantes existente
             WHERE UPPER(TRIM(existente.Sku)) =
                   UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))
        ));
